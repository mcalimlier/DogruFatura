using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using TesseractOCR.Enums;
using TesseractEngine = TesseractOCR.Engine;
using TesseractPix = TesseractOCR.Pix.Image;
using Windows.Storage;
using Windows.Storage.Streams;

namespace DogruFatura.Wpf;

/// <summary>
/// Eski HYS / Ödeme Emri PDF'lerindeki fatura eklerini semantik sütun başlıklarıyla okur.
/// Sabit koordinat veya tek bir başlık yazımına bağımlı değildir. PDF metin katmanı bozuksa
/// Yerleşik OCR modeli son çare olarak kullanılır.
/// </summary>
internal sealed class LegacyHysPdfImportService
{
    private const uint OrientationProbeWidth = 1000;
    private const uint InitialOcrWidth = 1500;

    private enum OcrOrientation
    {
        Normal,
        Clockwise,
        CounterClockwise
    }

    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly decimal[] CandidateVatRates = { 0.01m, 0.10m, 0.20m };
    private static readonly string[] DateFormats =
    {
        "dd.MM.yyyy", "d.M.yyyy",
        "dd-MM-yyyy", "d-M-yyyy",
        "dd/MM/yyyy", "d/M/yyyy"
    };

    private static readonly IReadOnlyDictionary<ColumnKind, string[]> HeaderAliases =
        new Dictionary<ColumnKind, string[]>
        {
            [ColumnKind.Sequence] = new[] { "no", "sira no", "sıra no", "s no", "s.no", "sno" },
            [ColumnKind.District] = new[] { "ilce adi", "ilçe adı", "ilce", "ilçe" },
            [ColumnKind.Institution] = new[]
            {
                "okul adi", "okul adı", "kurum adi", "kurum adı",
                "okul kurum adi", "okul / kurum adi", "okul/kurum adi"
            },
            [ColumnKind.ServiceNumber] = new[]
            {
                "hizmet numarasi", "hizmet numarası", "hizmet no",
                "abone numarasi", "abone numarası", "abone no",
                "tesisat numarasi", "tesisat numarası", "tesisat no",
                "tuketici numarasi", "tüketici numarası", "tuketici no", "tüketici no"
            },
            [ColumnKind.InvoiceNumber] = new[]
            {
                "fatura no", "fatura numarasi", "fatura numarası",
                "belge no", "belge numarasi", "belge numarası"
            },
            [ColumnKind.InvoiceDate] = new[]
            {
                "fatura tarihi", "belge tarihi"
            },
            [ColumnKind.DueDate] = new[]
            {
                "son odeme tarihi", "son ödeme tarihi", "vade tarihi", "son odeme", "son ödeme"
            },
            [ColumnKind.Payable] = new[]
            {
                "odenecek tutar", "ödenecek tutar", "fatura tutari", "fatura tutarı",
                "brut tutar", "brüt tutar", "toplam tutar"
            },
            [ColumnKind.Vat] = new[]
            {
                "kdv tutari", "kdv tutarı", "kdv"
            },
            [ColumnKind.StampDuty] = new[]
            {
                "damga vergisi tutari", "damga vergisi tutarı", "damga vergisi",
                "damga tutari", "damga tutarı"
            },
            [ColumnKind.Payment] = new[]
            {
                "odeme tutari", "ödeme tutarı", "net odeme", "net ödeme", "net tutar",
                "ele gecen miktar", "ele geçen miktar", "ele miktar"
            }
        };

    private static readonly ColumnKind[] RequiredColumns =
    {
        ColumnKind.Institution,
        ColumnKind.ServiceNumber,
        ColumnKind.InvoiceNumber,
        ColumnKind.InvoiceDate,
        ColumnKind.Payable,
        ColumnKind.Vat,
        ColumnKind.StampDuty,
        ColumnKind.Payment
    };

    private readonly TaxCalculationService _taxCalculationService;

    public LegacyHysPdfImportService(TaxCalculationService taxCalculationService)
    {
        _taxCalculationService = taxCalculationService;
    }

    public HistoricalPdfImportPreview Read(
        string sourcePath,
        IReadOnlyList<SchoolRecord> schools,
        AuthorityProfile fallbackAuthorityProfile,
        CancellationToken cancellationToken,
        IProgress<string>? progress = null,
        HistoricalPdfImportScanMode scanMode = HistoricalPdfImportScanMode.Automatic,
        IReadOnlyList<HistoricalPdfImportService.PdfPageData>? sharedExtractedPages = null)
    {
        var fileName = Path.GetFileName(sourcePath);
        var ocrAttempted = false;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var textTimer = Stopwatch.StartNew();
            var extractedPages = sharedExtractedPages is null
                ? ExtractPdfTextPages(sourcePath, cancellationToken)
                : ConvertExtractedPages(sharedExtractedPages);
            progress?.Report(
                sharedExtractedPages is null
                    ? $"Legacy metin katmanı çıkarıldı ({textTimer.ElapsedMilliseconds} ms)"
                    : $"Legacy metin katmanı tekrar açılmadan kullanıldı ({textTimer.ElapsedMilliseconds} ms)");
            var extractedWasParsed = TryParsePages(
                sourcePath,
                fileName,
                extractedPages,
                schools,
                fallbackAuthorityProfile,
                usedOcr: false,
                out var extractedPreview);

            if (extractedWasParsed && extractedPreview.IsValid)
                return extractedPreview;

            // Bazı HYS dosyalarında fatura listesi tablosu yerine e-Fatura eki vardır.
            // Metin katmanı okunuyorsa gerçek ödeme emri toplamıyla ek fatura eşleştirilir.
            if (TryParseEmbeddedInvoicePages(
                    sourcePath, fileName, extractedPages, schools,
                    fallbackAuthorityProfile, out var embeddedPreview))
                return embeddedPreview;

            // Tam metinli, çok sayfalı fatura eklerinde yeniden görüntü OCR'ı
            // aynı bilgileri okumaya çalışıp süreyi doldurur. Somut doğrulama
            // hatasını hızlı aşamada göster; eksik metinli ekler OCR'a kalır.
            if (scanMode == HistoricalPdfImportScanMode.Fast
                && HasCompleteTextualMultiPageInvoices(extractedPages))
                return embeddedPreview with
                {
                    OcrSkipped = true,
                    ScanDetail = "Hızlı metin taraması • ekler okunuyor; tutar/alan doğrulaması başarısız"
                };

            if (TryParseDetailedAllowancePages(
                    sourcePath, fileName, extractedPages, schools,
                    fallbackAuthorityProfile, out var detailedPreview))
                return detailedPreview;

            if (extractedWasParsed && scanMode == HistoricalPdfImportScanMode.Automatic)
                return extractedPreview;

            if (scanMode == HistoricalPdfImportScanMode.Fast)
            {
                return extractedWasParsed
                    ? extractedPreview.WithError(
                        extractedPreview.Status + " Güçlü taramada yeniden denenecek.")
                    : Invalid(
                        sourcePath,
                        fileName,
                        "Hızlı taramada güvenilir fatura tablosu bulunamadı. Güçlü taramada yeniden denenecek.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report("Eski/bozuk PDF için OCR hazırlanıyor");
            var ocrResult = TryExtractOcrPages(
                sourcePath,
                extractedPages,
                progress,
                () => ocrAttempted = true,
                cancellationToken);
            ocrAttempted |= ocrResult.OcrRan;
            var ocrSkipped = !ocrAttempted && string.IsNullOrWhiteSpace(ocrResult.ErrorMessage);
            var combinedPages = MergePages(extractedPages, ocrResult.Pages);
            HistoricalPdfImportPreview? ocrPreview = null;
            if (combinedPages.Count > 0
                && TryParsePages(
                    sourcePath,
                    fileName,
                    combinedPages,
                    schools,
                    fallbackAuthorityProfile,
                    usedOcr: true,
                    out var parsedOcrPreview))
            {
                ocrPreview = parsedOcrPreview;
                if (ocrPreview.IsValid)
                    return ocrPreview with { OcrAttempted = ocrAttempted, OcrSkipped = ocrSkipped };
            }

            if (TryParseEmbeddedInvoicePages(
                    sourcePath, fileName, combinedPages, schools,
                    fallbackAuthorityProfile, out var ocrEmbeddedPreview))
                return ocrEmbeddedPreview with { OcrAttempted = ocrAttempted, OcrSkipped = ocrSkipped };

            if (TryParseDetailedAllowancePages(
                    sourcePath, fileName, combinedPages, schools,
                    fallbackAuthorityProfile, out var ocrDetailedPreview))
                return ocrDetailedPreview with { OcrAttempted = ocrAttempted, OcrSkipped = ocrSkipped };

            if (ocrPreview is not null)
                return ocrPreview with { OcrAttempted = ocrAttempted, OcrSkipped = ocrSkipped };

            if (extractedWasParsed)
            {
                var ocrNote = string.IsNullOrWhiteSpace(ocrResult.ErrorMessage)
                    ? ocrAttempted
                        ? " Güçlü OCR denendi ancak sonuç doğrulanamadı."
                        : " Güçlü taramada OCR uygulanmadı; metin sayfaları atlandı."
                    : " Güçlü OCR yolu tamamlanamadı: " + ocrResult.ErrorMessage;
                return extractedPreview.WithError(extractedPreview.Status + ocrNote)
                    with { OcrAttempted = ocrAttempted, OcrSkipped = ocrSkipped };
            }

            var hasOrder = extractedPages.Any(page =>
                NormalizeKey(string.Join(" ", page.Lines.Select(line => line.Text)))
                    .Contains("odeme emri belgesi", StringComparison.Ordinal));
            var reason = !hasOrder
                ? "PDF içinde ödeme emri belgesi bulunamadı. Yalnız e-Fatura üzerinden "
                  + "resmî HYS ödeme emri veya kapalı arşiv kaydı oluşturulamaz."
                : "Ödeme emriyle eşleşen doğrulanabilir fatura listesi veya e-Fatura eki bulunamadı.";
            if (!string.IsNullOrWhiteSpace(ocrResult.ErrorMessage))
                reason += " OCR: " + ocrResult.ErrorMessage;
            else if (!ocrAttempted)
                reason += " Görüntü OCR'ı uygulanmadı; metin sayfaları atlandı.";
            return Invalid(sourcePath, fileName, reason)
                with { OcrAttempted = ocrAttempted, OcrSkipped = ocrSkipped };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Invalid(sourcePath, fileName, "Eski ödeme emri PDF'si okunamadı: " + ex.Message)
                with { OcrAttempted = ocrAttempted };
        }
    }

    // HYS ödeme emrinde satır tablosu yoksa, e-Fatura ekindeki her belgeyi
    // mevcut ödeme emri toplamıyla doğrulayarak iç arşiv kaydına dönüştürür.
    // Resmî ödeme emri olmayan yalnız faturalar bu yoldan kapalı arşive alınmaz.
    private static bool TryFindEmbeddedOrderTotals(
        IReadOnlyList<PageData> paymentPages,
        out DocumentTotals totals)
    {
        totals = default!;
        foreach (var page in paymentPages)
        {
            // Resmî ödeme emri sayfasında aynı toplam satırı başlık yerleşimi
            // değişse de altı para hücresini taşır. Başlığı ipucu say; tutarları
            // yalnız brüt - damga = net eşitliğiyle kabul et.
            foreach (var line in page.Lines.Where(line =>
                         line.Top > page.Height * 0.30d))
            {
                var amounts = new List<decimal>();
                foreach (Match match in Regex.Matches(
                             line.Text,
                             @"(?<!\d)(?:\d{1,3}(?:\.\d{3})+|\d+),\d{2}(?!\d)",
                             RegexOptions.CultureInvariant))
                {
                    if (NumberParser.TryNonNegativeDecimal(match.Value, out var amount))
                        amounts.Add(amount);
                }

                if (amounts.Count < 5)
                    continue;
                var gross = amounts[0];
                var stamp = amounts[1];
                var payment = amounts[^1];
                if (gross <= 0m || stamp <= 0m || stamp >= gross * 0.1m
                    || decimal.Round(gross - stamp, 2, MidpointRounding.AwayFromZero)
                       != payment
                    || (amounts.Count > 5 && amounts[^2] != 0m))
                    continue;

                totals = new DocumentTotals(gross, stamp, payment);
                return true;
            }
        }

        return false;
    }

    private static bool HasCompleteTextualMultiPageInvoices(
        IReadOnlyList<PageData> pages)
    {
        var paymentPages = pages.Where(page =>
            NormalizeKey(string.Join(" ", page.Lines.Select(line => line.Text)))
                .Contains("odeme emri belgesi", StringComparison.Ordinal)).ToArray();
        if (paymentPages.Length == 0)
            return false;
        var attachments = pages.Except(paymentPages)
            .OrderBy(page => page.PageNumber).ToArray();
        if (attachments.Length < 4 || attachments.Length % 2 != 0)
            return false;

        for (var index = 0; index < attachments.Length; index += 2)
        {
            var first = string.Join(" ",
                attachments[index].Lines.Select(line => line.Text));
            var compact = NormalizeKey(first)
                .Replace(" ", string.Empty, StringComparison.Ordinal);
            var detail = string.Join(" ",
                attachments[index + 1].Lines.Select(line => line.Text));
            if (!Regex.IsMatch(first, @"\b[A-Z]{3}20\d{11}\b",
                    RegexOptions.CultureInvariant)
                || !compact.Contains("faturatarihi", StringComparison.Ordinal)
                || !compact.Contains("odenecektutar", StringComparison.Ordinal)
                || !compact.Contains("tuketicino", StringComparison.Ordinal)
                || !Regex.IsMatch(detail,
                    @"\bKDV\s*\(\s*%\s*\d{1,2}\s*\)\s*(?:\d{1,3}(?:\.\d{3})+|\d+),\d{2}",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                return false;
        }

        return true;
    }

    private bool TryParseEmbeddedInvoicePages(
        string sourcePath,
        string fileName,
        IReadOnlyList<PageData> pages,
        IReadOnlyList<SchoolRecord> schools,
        AuthorityProfile fallbackAuthorityProfile,
        out HistoricalPdfImportPreview preview)
    {
        preview = Invalid(sourcePath, fileName, "Ek fatura bulunamadı.");
        var paymentPages = pages
            .Where(page => NormalizeKey(string.Join(" ", page.Lines.Select(line => line.Text)))
                .Contains("odeme emri belgesi", StringComparison.Ordinal))
            .ToArray();
        if (paymentPages.Length == 0)
            return false;
        if (!TryFindEmbeddedOrderTotals(paymentPages, out var totals)
            && !TryFindDocumentTotals(paymentPages, out totals))
        {
            preview = Invalid(sourcePath, fileName,
                "Ödeme emrinin brüt, damga ve net toplamı doğrulanamadı.");
            return false;
        }

        var paymentText = string.Join(" ",
            paymentPages.SelectMany(page => page.Lines).Select(line => line.Text));
        if (!TryResolveHysNumber(sourcePath, paymentText, out var hysNumber, out var hysError))
        {
            preview = Invalid(sourcePath, fileName, hysError);
            return false;
        }

        var invoiceType = FindInvoiceType(paymentText);
        var reportDate = FindReportDate(paymentPages);
        if (invoiceType is null || !reportDate.HasValue)
        {
            preview = Invalid(sourcePath, fileName,
                "Ödeme emrinin fatura türü veya düzenleme tarihi okunamadı.", hysNumber);
            return false;
        }

        var invoices = new List<InvoiceRecord>();
        var attachmentPages = pages.Except(paymentPages)
            .OrderBy(page => page.PageNumber).ToArray();
        for (var index = 0; index < attachmentPages.Length;)
        {
            var page = attachmentPages[index];
            var pageText = string.Join(" ", page.Lines.Select(line => line.Text));
            var hasInvoice = Regex.IsMatch(
                pageText, @"\b[A-Z]{3}20\d{11}\b",
                RegexOptions.CultureInvariant);
            if (!hasInvoice)
            {
                preview = Invalid(sourcePath, fileName,
                    $"{page.PageNumber}. sayfada fatura numarası okunamadı.",
                    hysNumber, invoiceType);
                return false;
            }

            // Elektrik faturasında KDV dökümü çoğu kez sonraki sayfadadır.
            var group = new List<PageData> { page };
            index++;
            while (index < attachmentPages.Length)
            {
                var next = attachmentPages[index];
                var nextText = string.Join(" ", next.Lines.Select(line => line.Text));
                if (Regex.IsMatch(nextText, @"\b[A-Z]{3}20\d{11}\b",
                        RegexOptions.CultureInvariant))
                    break;
                group.Add(next);
                index++;
            }

            if (!TryReadEmbeddedInvoice(
                    group, invoiceType, reportDate.Value, schools,
                    out var invoice, out var invoiceError)
                && (group.Count != 1
                    || !TryReadEmbeddedInvoiceSinglePage(
                        group[0], invoiceType, reportDate.Value, schools,
                        out invoice)))
            {
                preview = Invalid(sourcePath, fileName,
                    $"{page.PageNumber}. sayfadan başlayan fatura doğrulanamadı: {invoiceError}",
                    hysNumber, invoiceType);
                return false;
            }
            invoices.Add(invoice!);
        }

        if (invoices.Select(invoice => invoice.InvoiceNumber)
            .Distinct(StringComparer.Ordinal).Count() != invoices.Count)
        {
            preview = Invalid(sourcePath, fileName,
                "Eklerde yinelenen fatura numarası var.", hysNumber, invoiceType);
            return false;
        }

        if (invoices.Count == 0 || !MatchesDocumentTotals(invoices, totals))
        {
            preview = Invalid(sourcePath, fileName,
                $"Ek fatura toplamları ödeme emriyle eşleşmiyor: "
                + $"brüt {NumberParser.Money(invoices.Sum(item => item.PayableAmount))} / {NumberParser.Money(totals.GrossAmount)}, "
                + $"damga {NumberParser.Money(decimal.Round(invoices.Sum(item => item.StampDuty), 2, MidpointRounding.AwayFromZero))} / {NumberParser.Money(totals.StampDutyAmount)}, "
                + $"net {NumberParser.Money(decimal.Round(invoices.Sum(item => item.PaymentAmount), 2, MidpointRounding.AwayFromZero))} / {NumberParser.Money(totals.PaymentAmount)}.",
                hysNumber, invoiceType);
            return false;
        }

        var authorityProfile = ExtractAuthorityProfile(paymentPages, fallbackAuthorityProfile);
        var archive = ClosedArchiveSnapshotService.Seal(new ArchivedInvoiceList(
            Guid.NewGuid(),
            "DF-PDF-" + hysNumber[4..],
            reportDate.Value.Date,
            invoiceType,
            invoices,
            authorityProfile,
            hysNumber,
            true,
            reportDate.Value.Date,
            hysNumber + ".pdf"));
        var validation = ArchivedInvoiceListValidationService.Validate(archive);
        if (!validation.IsValid)
        {
            preview = Invalid(sourcePath, fileName,
                "Arşiv doğrulaması: " + validation.Message,
                hysNumber, invoiceType);
            return false;
        }

        preview = new HistoricalPdfImportPreview(
            sourcePath, fileName, true,
            $"{paymentPages.Length} ödeme emri sayfası ve {invoices.Count} fatura eki doğrulandı; "
            + "brüt, damga vergisi ve net tutar resmî ödeme emri toplamlarıyla eşleşti.",
            hysNumber, invoiceType, invoices.Count,
            invoices.Sum(invoice => invoice.PaymentAmount), archive);
        return true;
    }

    // Bakanlık ödenek/fatura listesindeki satırlar iki fiziksel satıra taşabilir.
    // Sütunlar başlıkların yatay konumlarından okunur; ödeme emri toplamı şarttır.
    private bool TryParseDetailedAllowancePages(
        string sourcePath,
        string fileName,
        IReadOnlyList<PageData> pages,
        IReadOnlyList<SchoolRecord> schools,
        AuthorityProfile fallbackAuthorityProfile,
        out HistoricalPdfImportPreview preview)
    {
        preview = Invalid(sourcePath, fileName, "Ayrıntılı fatura listesi bulunamadı.");
        var paymentPages = pages.Where(page =>
            NormalizeKey(string.Join(" ", page.Lines.Select(line => line.Text)))
                .Contains("odeme emri belgesi", StringComparison.Ordinal)).ToArray();
        if (paymentPages.Length == 0
            || (!TryFindEmbeddedOrderTotals(paymentPages, out var totals)
                && !TryFindDocumentTotals(paymentPages, out totals)))
            return false;

        var reportPages = pages.Except(paymentPages)
            .Where(page => NormalizeKey(string.Join(" ", page.Lines.Select(line => line.Text)))
                .Contains("ayrintili gonderilen odenek fatura listesi", StringComparison.Ordinal))
            .ToArray();
        if (reportPages.Length == 0)
            return false;
        var paymentText = string.Join(" ",
            paymentPages.SelectMany(page => page.Lines).Select(line => line.Text));
        if (!TryResolveHysNumber(sourcePath, paymentText, out var hysNumber, out _))
            return false;
        var invoiceType = FindInvoiceType(paymentText);
        var reportDate = FindReportDate(paymentPages);
        if (invoiceType is null || !reportDate.HasValue)
            return false;

        var entries = new List<(string Name, string Number, string Service,
            DateTime InvoiceDate, DateTime DueDate, decimal Payable)>();
        foreach (var page in reportPages)
        {
            var header = page.Words.Where(word =>
                word.Top > page.Height * 0.11d
                && word.Top < page.Height * 0.20d).ToArray();
            var invoiceHeaders = header.Where(word =>
                NormalizeKey(word.Text) == "fatura")
                .OrderBy(word => word.Left).ToArray();
            var institutionHeaders = header.Where(word =>
                NormalizeKey(word.Text) == "kurum")
                .OrderBy(word => word.Left).ToArray();
            double HeaderX(string name) => header.FirstOrDefault(word =>
                NormalizeKey(word.Text) == name)?.Left ?? -1d;
            if (invoiceHeaders.Length < 2 || institutionHeaders.Length < 2)
                return false;
            var institutionX = institutionHeaders[0].Left;
            var institutionEnd = institutionHeaders[1].Left;
            var amountX = HeaderX("gonderilen");
            var amountEnd = HeaderX("tuketim");
            var invoiceX = invoiceHeaders[0].Left;
            var invoiceDateX = invoiceHeaders[1].Left;
            var dueX = HeaderX("son");
            var serviceX = HeaderX("abone");
            var serviceEnd = header.Where(word =>
                    NormalizeKey(word.Text) == "odeme" && word.Left > serviceX)
                .OrderBy(word => word.Left).FirstOrDefault()?.Left ?? -1d;
            if (institutionX < 0d || amountX < 0d || amountEnd < amountX
                || invoiceX < amountEnd || invoiceDateX < invoiceX
                || dueX < invoiceDateX || serviceX < dueX
                || serviceEnd < serviceX)
                return false;

            // Birden fazla fatura sayfası da sıralı olarak desteklenir.
            var allAnchors = page.Words.Where(word =>
                    word.Left < institutionX
                    && word.Top > page.Height * 0.15d
                    && int.TryParse(word.Text, NumberStyles.None,
                        CultureInfo.InvariantCulture, out var order)
                    && order > 0 && order <= 500)
                .OrderBy(word => word.Top).ToArray();
            if (allAnchors.Length == 0)
                return false;
            for (var i = 0; i < allAnchors.Length; i++)
            {
                var anchor = allAnchors[i];
                var top = anchor.Top - 2d;
                var bottom = i + 1 < allAnchors.Length
                    ? allAnchors[i + 1].Top - 2d
                    : Math.Min(page.Height, anchor.Top + 35d);
                var words = page.Words.Where(word =>
                        word.Top >= top && word.Top < bottom)
                    .ToArray();
                string Cell(double left, double right) => string.Join(" ",
                    words.Where(word =>
                            word.Left >= left - 2d && word.Left < right - 2d)
                        .OrderBy(word => word.Top).ThenBy(word => word.Left)
                        .Select(word => word.Text));
                var name = NormalizeInstitutionCell(
                    Cell(institutionX, institutionEnd));
                var amountText = Cell(amountX, amountEnd);
                var invoiceNumber = Regex.Replace(
                    Cell(invoiceX, invoiceDateX),
                    @"[^A-Za-z0-9]", string.Empty);
                var serviceNumber = DigitsOnly(Cell(serviceX, serviceEnd));
                if (string.IsNullOrWhiteSpace(name)
                    || !Regex.IsMatch(invoiceNumber, @"^[A-Z]{3}20\d{11}$",
                        RegexOptions.CultureInvariant)
                    || serviceNumber.Length < 6
                    || !TryDate(Cell(invoiceDateX, dueX), out var invoiceDate)
                    || !TryDate(Cell(dueX, serviceX), out var dueDate)
                    || !NumberParser.TryNonNegativeDecimal(
                        Regex.Match(amountText,
                            @"(?:\d{1,3}(?:\.\d{3})+|\d+),\d{2}",
                            RegexOptions.CultureInvariant).Value,
                        out var payable)
                    || payable <= 0m)
                    return false;
                entries.Add((name, invoiceNumber, serviceNumber,
                    invoiceDate, dueDate, payable));
            }
        }

        if (entries.Count == 0
            || entries.Select(entry => entry.Number).Distinct(StringComparer.Ordinal)
                .Count() != entries.Count
            || entries.Sum(entry => entry.Payable) != totals.GrossAmount)
            return false;

        // Kaynak listesi KDV göstermiyor. Fatura başına %1, %10, %20
        // adaylarını yalnız ödeme emrindeki damga/net hesabı tek bir
        // oranı doğruladığında kullan; aksi halde manuel incelemeye bırak.
        var candidates = new List<(decimal Rate, InvoiceRecord[] Invoices)>();
        foreach (var rate in CandidateVatRates)
        {
            var invoices = new List<InvoiceRecord>();
            foreach (var entry in entries)
            {
                var vat = decimal.Round(
                    entry.Payable * rate / (1m + rate), 2,
                    MidpointRounding.AwayFromZero);
                var calculation = _taxCalculationService.Calculate(
                    entry.Payable, vat, invoiceType);
                if (!calculation.IsValid)
                    break;
                var byService = schools.Where(school =>
                    string.Equals(
                        DigitsOnly(school.GetServiceNumber(invoiceType))
                            .TrimStart('0'),
                        entry.Service.TrimStart('0'),
                        StringComparison.Ordinal)).ToArray();
                if (byService.Length > 1)
                    break;
                var school = byService.SingleOrDefault()
                    ?? ResolveSchool(entry.Name, schools);
                var invoice = new InvoiceRecord(
                    Guid.NewGuid(), reportDate.Value,
                    school?.InstitutionType ?? InferInstitutionType(entry.Name),
                    school?.Name ?? entry.Name, school?.Vkn ?? string.Empty,
                    invoiceType, entry.Number, entry.Service,
                    entry.InvoiceDate, entry.DueDate, entry.Payable, vat,
                    calculation.VatBase, calculation.StampDuty,
                    calculation.PaymentAmount, 0m,
                    school?.SchoolId ?? Guid.Empty);
                if (!InvoiceValidationService.Validate(invoice).IsValid)
                    break;
                invoices.Add(invoice);
            }
            if (invoices.Count == entries.Count
                && MatchesDocumentTotals(invoices, totals))
                candidates.Add((rate, invoices.ToArray()));
        }
        if (candidates.Count != 1)
            return false;

        var selected = candidates[0];
        var authorityProfile = ExtractAuthorityProfile(paymentPages,
            fallbackAuthorityProfile);
        var archive = ClosedArchiveSnapshotService.Seal(new ArchivedInvoiceList(
            Guid.NewGuid(), "DF-PDF-" + hysNumber[4..],
            reportDate.Value.Date, invoiceType, selected.Invoices.ToList(),
            authorityProfile, hysNumber, true, reportDate.Value.Date,
            hysNumber + ".pdf"));
        if (!ArchivedInvoiceListValidationService.Validate(archive).IsValid)
            return false;

        preview = new HistoricalPdfImportPreview(
            sourcePath, fileName, true,
            $"{reportPages.Length} ayrıntılı liste sayfasından {entries.Count} fatura "
            + "okundu. Listede KDV yer almıyor: %" +
            (selected.Rate * 100m).ToString("0", CultureInfo.InvariantCulture)
            + " oranıyla hesaplanan KDV, brüt/damga/net ödeme emri toplamıyla "
            + "eşleşti; KDV kaynaktan okunmuş değildir.",
            hysNumber, invoiceType, selected.Invoices.Length,
            selected.Invoices.Sum(invoice => invoice.PaymentAmount), archive);
        return true;
    }

    private bool TryReadEmbeddedInvoice(
        IReadOnlyList<PageData> pages,
        string invoiceType,
        DateTime reportDate,
        IReadOnlyList<SchoolRecord> schools,
        out InvoiceRecord? invoice,
        out string error)
    {
        invoice = null;
        error = string.Empty;
        var first = pages[0];
        var invoiceNumbers = Regex.Matches(
                string.Join(" ", first.Lines.Select(line => line.Text)),
                @"\b[A-Z]{3}20\d{11}\b", RegexOptions.CultureInvariant)
            .Select(match => match.Value)
            .Distinct(StringComparer.Ordinal).ToArray();
        if (invoiceNumbers.Length != 1)
        {
            error = "Bu ekin ilk sayfasında tek bir fatura numarası bulunamadı.";
            return false;
        }

        var headerLine = first.Lines.FirstOrDefault(line =>
            NormalizeKey(line.Text).Replace(" ", string.Empty, StringComparison.Ordinal)
                .Contains("faturatarihi", StringComparison.Ordinal));
        if (headerLine is null)
        {
            error = "Fatura tarihi başlığı okunamadı.";
            return false;
        }
        var headerText = string.Join(" ", first.Lines
            .Where(line => Math.Abs(line.Top - headerLine.Top) < 26d)
            .OrderBy(line => line.Top).Select(line => line.Text));
        var compactHeader = Regex.Replace(headerText, @"\s+", string.Empty);
        var headerWords = first.Words
            .Where(word => Math.Abs(word.Top - headerLine.Top) < 30d).ToArray();
        string ColumnText(double left, double right) => Regex.Replace(
            string.Join("", headerWords
                .Where(word => word.CenterX >= left && word.CenterX < right)
                .OrderBy(word => word.Top).ThenBy(word => word.Left)
                .Select(word => word.Text)), @"\s+", string.Empty);
        var invoiceColumnText = ColumnText(0d, first.Width * 0.34d);
        var amountColumnText = ColumnText(first.Width * 0.30d, first.Width * 0.64d);
        var dueColumnText = ColumnText(first.Width * 0.62d, first.Width);
        static DateTime[] HeaderDates(string value) => Regex.Matches(
                value,
                @"(?<!\d)[0-3]?\d\s*[-./]\s*[01]?\d\s*[-./]?\s*20\d{2}(?!\d)",
                RegexOptions.CultureInvariant)
            .Select(match => Regex.Replace(match.Value, @"\s+", string.Empty))
            .Select(value => TryDate(value, out var parsed) ? parsed : (DateTime?)null)
            .Where(date => date.HasValue).Select(date => date!.Value)
            .Distinct().ToArray();
        var dates = HeaderDates(headerText);
        // Tarihler PDF koordinatlarında ayrı satırlara bölünebilir. Örneğin
        // son ödeme tarihi faturanın tarihinden önce çıkarılır; dizi sırası
        // bu iki alanın anlamını belirlemez.
        if (!TryEmbeddedDate(compactHeader,
                @"faturatar[iİı]h[iİı]", out var invoiceDate))
        {
            var columnDates = HeaderDates(invoiceColumnText);
            if (columnDates.Length != 1)
            {
                error = "Fatura tarihi başlığın sol sütunundan okunamadı. Sol alan: "
                    + invoiceColumnText[..Math.Min(invoiceColumnText.Length, 120)]
                    + "; başlık: " + headerText[..Math.Min(headerText.Length, 180)];
                return false;
            }
            invoiceDate = columnDates[0];
        }
        if (!TryEmbeddedMoney(compactHeader,
                @"[öo]denecektutar", out var payable))
        {
            var columnAmounts = Regex.Matches(amountColumnText,
                    @"(?<!\d)(?:\d{1,3}(?:\.\d{3})+|\d+),\d{2}(?!\d)",
                    RegexOptions.CultureInvariant)
                .Select(match => NumberParser.TryNonNegativeDecimal(
                    match.Value, out var amount) ? amount : (decimal?)null)
                .Where(amount => amount.HasValue && amount.Value > 0m)
                .Select(amount => amount!.Value).Distinct().ToArray();
            if (columnAmounts.Length != 1)
            {
                error = "Ödenecek tutar başlığın orta sütunundan okunamadı. Orta alan: "
                    + amountColumnText[..Math.Min(amountColumnText.Length, 120)]
                    + "; başlık: " + headerText[..Math.Min(headerText.Length, 180)];
                return false;
            }
            payable = columnAmounts[0];
        }
        if (!TryEmbeddedDate(compactHeader,
                @"son[öo]demetar[iİı]h[iİı]", out var dueDate))
        {
            var otherDates = HeaderDates(dueColumnText)
                .Concat(dates.Where(date => date != invoiceDate))
                .Where(date => date != invoiceDate).Distinct().ToArray();
            if (otherDates.Length != 1)
            {
                error = "Son ödeme tarihi başlık yakınında tekil olarak okunamadı.";
                return false;
            }
            dueDate = otherDates[0];
        }

        var vatLines = pages.SelectMany(page => page.Lines)
            .Where(line => Regex.IsMatch(
                line.Text, @"\bKDV\s*\(\s*%\s*\d{1,2}\s*\)",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            .ToArray();
        if (vatLines.Length == 0)
        {
            error = "Ek sayfalarda KDV satırı bulunamadı.";
            return false;
        }
        var vat = 0m;
        foreach (var line in vatLines)
        {
            var label = Regex.Match(
                line.Text, @"\bKDV\s*\(\s*%\s*\d{1,2}\s*\)",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            var money = Regex.Match(line.Text[(label.Index + label.Length)..],
                @"(?<value>(?:\d{1,3}(?:\.\d{3})+|\d+),\d{2})",
                RegexOptions.CultureInvariant);
            if (!money.Success
                || !NumberParser.TryNonNegativeDecimal(
                    money.Groups["value"].Value, out var amount))
            {
                error = "KDV satırındaki tutar okunamadı.";
                return false;
            }
            vat += amount;
        }
        if (vat <= 0m)
        {
            error = "KDV toplamı sıfır veya eksik.";
            return false;
        }

        var consumerLine = first.Lines.FirstOrDefault(line =>
            NormalizeKey(line.Text).Replace(" ", string.Empty, StringComparison.Ordinal)
                .Contains("tuketicino", StringComparison.Ordinal));
        var serviceNumber = consumerLine is null
            ? string.Empty
            : Regex.Matches(consumerLine.Text, @"\d{7,20}(?!\d)",
                RegexOptions.CultureInvariant).LastOrDefault()?.Value ?? string.Empty;
        if (string.IsNullOrWhiteSpace(serviceNumber))
        {
            error = "Tüketici / abone numarası okunamadı.";
            return false;
        }

        var recipientName = ExtractEmbeddedRecipient(first);
        var byService = schools.Where(school =>
            string.Equals(
                DigitsOnly(school.GetServiceNumber(invoiceType))
                    .TrimStart('0'),
                serviceNumber.TrimStart('0'),
                StringComparison.Ordinal)).ToArray();
        if (byService.Length > 1)
        {
            error = "Abone numarası birden fazla kurumla eşleşti.";
            return false;
        }
        var school = byService.SingleOrDefault();
        var institutionName = school?.Name ?? recipientName;
        if (string.IsNullOrWhiteSpace(institutionName))
        {
            error = "Kurum adı ekten veya kayıtlı aboneden bulunamadı.";
            return false;
        }

        var calculation = _taxCalculationService.Calculate(payable, vat, invoiceType);
        if (!calculation.IsValid)
        {
            error = "Fatura vergi hesabı: " + calculation.Message;
            return false;
        }
        var candidate = new InvoiceRecord(
            Guid.NewGuid(), reportDate,
            school?.InstitutionType ?? InferInstitutionType(institutionName),
            institutionName, school?.Vkn ?? string.Empty, invoiceType,
            invoiceNumbers[0], serviceNumber, invoiceDate, dueDate,
            payable, vat, calculation.VatBase, calculation.StampDuty,
            calculation.PaymentAmount, 0m, school?.SchoolId ?? Guid.Empty);
        var validation = InvoiceValidationService.Validate(candidate);
        if (!validation.IsValid)
        {
            error = "Fatura " + invoiceNumbers[0] + ": " + validation.Message;
            return false;
        }

        invoice = candidate;
        return true;
    }

    private bool TryReadEmbeddedInvoiceSinglePage(
        PageData page,
        string invoiceType,
        DateTime reportDate,
        IReadOnlyList<SchoolRecord> schools,
        out InvoiceRecord? invoice)
    {
        invoice = null;
        var text = string.Join(" ", page.Lines.Select(line => line.Text));
        var invoiceNumber = Regex.Match(
            text, @"\bfatura\s+no\s*:\s*(?<value>[A-Z0-9-]{8,50})",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
            .Groups["value"].Value;
        var serviceNumber = Regex.Match(
            text, @"s[öo]zle[şs]me\s+hesap\s+no\s*:\s*(?<value>\d{4,50})",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
            .Groups["value"].Value;
        if (string.IsNullOrWhiteSpace(serviceNumber))
        {
            serviceNumber = Regex.Match(
                text, @"t[üu]ket[iİı]ci\s+no\s*:\s*(?<value>\d{4,50})",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
                .Groups["value"].Value;
        }

        if (string.IsNullOrWhiteSpace(invoiceNumber)
            || !TryEmbeddedDate(text, @"fatura\s+tar[iİı]h[iİı]", out var invoiceDate)
            || !TryEmbeddedDate(text, @"son\s+[öo]deme\s+tar[iİı]h[iİı]", out var dueDate)
            || !TryEmbeddedMoney(text, @"[öo]denecek\s+tutar", out var payable)
            || !TryEmbeddedMoney(text, @"(?:elektr[iİı]k\s+)?kdv\s*\(\s*%\s*\d{1,2}\s*\)", out var vat))
            return false;

        var recipientName = ExtractEmbeddedRecipient(page);
        var recipientKey = NormalizeKey(recipientName);
        var schoolsByName = string.IsNullOrWhiteSpace(recipientKey)
            ? Array.Empty<SchoolRecord>()
            : schools.Where(school =>
                NormalizeKey(school.Name) == recipientKey).ToArray();
        if (string.IsNullOrWhiteSpace(serviceNumber))
        {
            if (schoolsByName.Length != 1)
                return false;
            serviceNumber = schoolsByName[0].GetServiceNumber(invoiceType);
            if (string.IsNullOrWhiteSpace(serviceNumber))
                return false;
        }

        var matchedSchools = schools
            .Where(school => string.Equals(
                school.GetServiceNumber(invoiceType),
                serviceNumber,
                StringComparison.Ordinal))
            .ToArray();
        if (matchedSchools.Length > 1
            || (schoolsByName.Length == 1 && matchedSchools.Length == 1
                && !ReferenceEquals(schoolsByName[0], matchedSchools[0])))
            return false;
        var school = matchedSchools.SingleOrDefault();
        var institutionName = school?.Name ?? recipientName;
        if (string.IsNullOrWhiteSpace(institutionName))
            return false;

        var calculation = _taxCalculationService.Calculate(payable, vat, invoiceType);
        if (!calculation.IsValid)
            return false;

        var candidate = new InvoiceRecord(
            Guid.NewGuid(), reportDate, school?.InstitutionType
                ?? InferInstitutionType(institutionName),
            institutionName, school?.Vkn ?? string.Empty, invoiceType,
            invoiceNumber, serviceNumber, invoiceDate, dueDate,
            payable, vat, calculation.VatBase, calculation.StampDuty,
            calculation.PaymentAmount, 0m, school?.SchoolId ?? Guid.Empty);
        if (!InvoiceValidationService.Validate(candidate).IsValid)
            return false;

        invoice = candidate;
        return true;
    }

    private static string ExtractEmbeddedRecipient(PageData page)
    {
        var anchor = page.Words.FirstOrDefault(word =>
            NormalizeKey(word.Text) == "sayin");
        if (anchor is null)
            return string.Empty;

        var following = page.Words
            .Where(word => word.Left >= anchor.Left - 2d
                && word.Top > anchor.Top + 2d
                && word.Top < anchor.Top + 25d)
            .ToArray();
        if (following.Length == 0)
            return string.Empty;

        var nameTop = following.Min(word => word.Top);
        return string.Join(" ", following
            .Where(word => Math.Abs(word.Top - nameTop) < 3d)
            .OrderBy(word => word.Left)
            .Select(word => word.Text)).Trim();
    }

    private static bool TryEmbeddedDate(
        string text,
        string label,
        out DateTime date)
    {
        date = default;
        var match = Regex.Match(
            text,
            label + @"\s*:?\s*(?<day>[0-3]?\d)\s*[-./]\s*(?<month>[01]?\d)\s*[-./]?\s*(?<year>20\d{2})(?!\d)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success && DateTime.TryParseExact(
            $"{match.Groups["day"].Value}-{match.Groups["month"].Value}-{match.Groups["year"].Value}",
            "d-M-yyyy",
            Tr, DateTimeStyles.None, out date);
    }

    private static bool TryEmbeddedMoney(
        string text,
        string label,
        out decimal amount)
    {
        amount = 0m;
        var match = Regex.Match(
            text,
            label + @"\s*:?\s*(?<amount>(?:\d{1,3}(?:\.\d{3})+|\d+),\d{2})",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        return match.Success
            && NumberParser.TryNonNegativeDecimal(
                match.Groups["amount"].Value, out amount)
            && amount > 0m;
    }

    private static List<PageData> MergePages(
        IReadOnlyList<PageData> extractedPages,
        IReadOnlyList<PageData> ocrPages)
    {
        var byPageNumber = extractedPages.ToDictionary(page => page.PageNumber);
        foreach (var ocrPage in ocrPages)
            byPageNumber[ocrPage.PageNumber] = ocrPage;

        return byPageNumber.Values
            .OrderBy(page => page.PageNumber)
            .ToList();
    }

    private static bool TryFindDocumentTotals(
        IReadOnlyList<PageData> pages,
        out DocumentTotals totals)
    {
        totals = default!;

        foreach (var page in pages)
        {
            for (var index = 0; index < page.Lines.Count; index++)
            {
                var headingWindow = string.Join(
                    " ",
                    page.Lines
                        .Skip(index)
                        .Take(3)
                        .Select(line => line.Text));
                var normalizedHeading = NormalizeKey(headingWindow);
                if (!normalizedHeading.Contains(
                        "odeme emri toplam tutari",
                        StringComparison.Ordinal))
                    continue;

                var valueWindow = string.Join(
                    " ",
                    page.Lines
                        .Skip(index)
                        .Take(14)
                        .Select(line => line.Text));

                var values = Regex.Matches(
                        valueWindow,
                        @"(?<!\d)\d{1,3}(?:\.\d{3})*,\d{2}(?!\d)|(?<!\d)\d+,\d{2}(?!\d)",
                        RegexOptions.CultureInvariant)
                    .Select(match => match.Value)
                    .SelectMany(MoneyCandidates)
                    .Distinct()
                    .ToArray();

                if (values.Length < 4)
                    continue;

                var gross = values.FirstOrDefault(value => value > 0m);
                if (gross <= 0m)
                    continue;

                var payment = values
                    .Where(value => value > gross * 0.5m && value <= gross)
                    .LastOrDefault();
                var stamp = values
                    .Skip(1)
                    .FirstOrDefault(value => value > 0m && value < gross * 0.1m
                        && decimal.Round(gross - value, 2, MidpointRounding.AwayFromZero)
                           == payment);

                if (stamp <= 0m || payment <= 0m)
                    continue;

                totals = new DocumentTotals(gross, stamp, payment);
                return true;
            }
        }

        return false;
    }

    private static bool MatchesDocumentTotals(
        IReadOnlyList<InvoiceRecord> invoices,
        DocumentTotals totals)
    {
        var payableTotal = decimal.Round(
            invoices.Sum(invoice => invoice.PayableAmount),
            2,
            MidpointRounding.AwayFromZero);
        var stampTotal = decimal.Round(
            invoices.Sum(invoice => invoice.StampDuty),
            2,
            MidpointRounding.AwayFromZero);
        var paymentTotal = decimal.Round(
            invoices.Sum(invoice => invoice.PaymentAmount),
            2,
            MidpointRounding.AwayFromZero);

        return payableTotal == totals.GrossAmount
               && stampTotal == totals.StampDutyAmount
               && paymentTotal == totals.PaymentAmount;
    }

    private bool TryParsePages(
        string sourcePath,
        string fileName,
        IReadOnlyList<PageData> pages,
        IReadOnlyList<SchoolRecord> schools,
        AuthorityProfile fallbackAuthorityProfile,
        bool usedOcr,
        out HistoricalPdfImportPreview preview)
    {
        preview = Invalid(sourcePath, fileName, "Belge tanınamadı.");
        if (pages.Count == 0)
            return false;

        var allText = string.Join(
            " ",
            pages.SelectMany(page => page.Lines).Select(line => line.Text));

        if (!TryResolveHysNumber(sourcePath, allText, out var hysNumber, out var hysError))
        {
            preview = Invalid(sourcePath, fileName, hysError);
            return true;
        }

        var hasRecognizedTable = pages.Any(page => TryFindHeaderLayout(page, out _));
        if (!hasRecognizedTable)
            return false;

        var invoiceType = FindInvoiceType(allText);
        if (invoiceType is null)
        {
            preview = Invalid(
                sourcePath,
                fileName,
                "PDF içinde fatura türü güvenilir biçimde belirlenemedi. "
                + "Elektrik, Telefon, İnternet veya Su ibaresi aranır.",
                hysNumber);
            return true;
        }

        var reportDate = FindReportDate(pages);
        if (!reportDate.HasValue)
        {
            preview = Invalid(
                sourcePath,
                fileName,
                "PDF içinde belge/düzenleme tarihi okunamadı.",
                hysNumber,
                invoiceType);
            return true;
        }

        var hasDocumentTotals = TryFindDocumentTotals(pages, out var documentTotals);
        var invoices = new List<InvoiceRecord>();
        var missingDueDateCount = 0;
        var derivedResultCellCount = 0;
        HeaderLayout? usedHeader = null;
        foreach (var page in pages)
        {
            if (!TryFindHeaderLayout(page, out var header))
                continue;

            usedHeader ??= header;
            foreach (var line in BuildInvoiceRows(page, header))
            {
                if (!TryParseInvoiceLine(
                        line,
                        header,
                        invoiceType,
                        schools,
                        reportDate.Value,
                        allowDerivedResultCells: hasDocumentTotals,
                        out var invoice,
                        out var lineLooksLikeInvoice,
                        out var dueDateWasMissing,
                        out var resultCellsDerived,
                        out var errorMessage))
                {
                    if (lineLooksLikeInvoice)
                    {
                        preview = Invalid(
                            sourcePath,
                            fileName,
                            $"{page.PageNumber}. sayfadaki fatura satırı okunamadı: {errorMessage}",
                            hysNumber,
                            invoiceType);
                        return true;
                    }

                    continue;
                }

                invoices.Add(invoice!);
                if (dueDateWasMissing)
                    missingDueDateCount++;
                if (resultCellsDerived)
                    derivedResultCellCount++;
            }
        }

        if (usedHeader is null)
            return false;

        if (invoices.Count == 0)
        {
            preview = Invalid(
                sourcePath,
                fileName,
                "Fatura sütun başlıkları bulundu ancak okunabilir fatura satırı bulunamadı.",
                hysNumber,
                invoiceType);
            return true;
        }

        if (derivedResultCellCount > 0)
        {
            if (!hasDocumentTotals || !MatchesDocumentTotals(invoices, documentTotals))
            {
                preview = Invalid(
                    sourcePath,
                    fileName,
                    "OCR ile eksik para hücreleri hesaplandı ancak Ödeme Emri toplamlarıyla doğrulanamadı.",
                    hysNumber,
                    invoiceType);
                return true;
            }
        }

        var authorityProfile = ExtractAuthorityProfile(pages, fallbackAuthorityProfile);
        var archive = ClosedArchiveSnapshotService.Seal(new ArchivedInvoiceList(
            Guid.NewGuid(),
            "DF-PDF-" + hysNumber[4..],
            reportDate.Value.Date,
            invoiceType,
            invoices,
            authorityProfile,
            hysNumber,
            true,
            reportDate.Value.Date,
            hysNumber + ".pdf"));

        var validation = ArchivedInvoiceListValidationService.Validate(archive);
        if (!validation.IsValid)
        {
            preview = Invalid(
                sourcePath,
                fileName,
                validation.Message,
                hysNumber,
                invoiceType);
            return true;
        }

        var dueDateNote = missingDueDateCount > 0
            ? $" • {missingDueDateCount} kayıtta son ödeme tarihi belgede bulunmadığı için fatura tarihi kullanıldı."
            : string.Empty;
        var derivedAmountNote = derivedResultCellCount > 0
            ? $" • {derivedResultCellCount} kayıtta OCR'ın okuyamadığı sonuç tutarları Ödeme Emri toplamlarıyla doğrulanarak tamamlandı."
            : string.Empty;

        preview = new HistoricalPdfImportPreview(
            sourcePath,
            fileName,
            true,
            $"{pages.Count} sayfa ve {invoices.Count} fatura semantik sütun başlıklarıyla doğrulandı"
            + (usedOcr ? " • bozuk/eski PDF metin katmanı için OCR kullanıldı." : ".")
            + dueDateNote
            + derivedAmountNote,
            hysNumber,
            invoiceType,
            invoices.Count,
            invoices.Sum(invoice => invoice.PaymentAmount),
            archive);
        return true;
    }

    private bool TryParseInvoiceLine(
        LineData line,
        HeaderLayout header,
        string invoiceType,
        IReadOnlyList<SchoolRecord> schools,
        DateTime reportDate,
        bool allowDerivedResultCells,
        out InvoiceRecord? invoice,
        out bool lineLooksLikeInvoice,
        out bool dueDateWasMissing,
        out bool resultCellsDerived,
        out string errorMessage)
    {
        invoice = null;
        dueDateWasMissing = false;
        resultCellsDerived = false;
        errorMessage = string.Empty;

        var invoiceDateText = ReadColumn(line.Words, header, ColumnKind.InvoiceDate);
        var hasInvoiceDate = TryDate(invoiceDateText, out var invoiceDate);
        var headerHasDueDate = header.Ranges.ContainsKey(ColumnKind.DueDate);
        var dueDate = default(DateTime);
        var hasDueDate = false;
        if (headerHasDueDate)
        {
            var dueDateText = ReadColumn(line.Words, header, ColumnKind.DueDate);
            hasDueDate = TryDate(dueDateText, out dueDate);
        }

        lineLooksLikeInvoice = hasInvoiceDate || hasDueDate;
        if (!hasInvoiceDate)
            return false;

        if (headerHasDueDate && !hasDueDate)
        {
            errorMessage = "Son ödeme/vade tarihi okunamadı.";
            return false;
        }

        if (!headerHasDueDate)
        {
            dueDate = invoiceDate;
            dueDateWasMissing = true;
        }

        var institutionText = NormalizeInstitutionCell(
            ReadColumn(line.Words, header, ColumnKind.Institution));
        var serviceNumber = DigitsOnly(ReadColumn(line.Words, header, ColumnKind.ServiceNumber));
        var invoiceNumber = ReadInvoiceNumber(line.Words, header);

        if (string.IsNullOrWhiteSpace(institutionText))
        {
            errorMessage = "Okul/kurum adı boş.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(serviceNumber))
        {
            errorMessage = "Hizmet/abone numarası boş.";
            return false;
        }

        if (!TryResolveAmounts(
                invoiceType,
                ReadColumn(line.Words, header, ColumnKind.Payable),
                ReadColumn(line.Words, header, ColumnKind.Vat),
                ReadColumn(line.Words, header, ColumnKind.StampDuty),
                ReadColumn(line.Words, header, ColumnKind.Payment),
                allowDerivedResultCells,
                out var payableAmount,
                out var vatAmount,
                out var printedStampDuty,
                out var printedPaymentAmount,
                out var calculation,
                out var compensationAmount,
                out resultCellsDerived)
            && !TryResolveAmountsFromRowGeometry(
                line.Words,
                header,
                invoiceType,
                allowDerivedResultCells,
                out payableAmount,
                out vatAmount,
                out printedStampDuty,
                out printedPaymentAmount,
                out calculation,
                out compensationAmount,
                out resultCellsDerived))
        {
            errorMessage = "Tutar/KDV/Damga Vergisi/Ödeme sütunları güvenilir biçimde doğrulanamadı.";
            return false;
        }

        var school = ResolveSchool(institutionText, schools);
        var institutionName = school?.Name ?? institutionText;
        var institutionType = school?.InstitutionType ?? InferInstitutionType(institutionText);

        var candidate = new InvoiceRecord(
            Guid.NewGuid(),
            reportDate,
            institutionType,
            institutionName,
            school?.Vkn ?? string.Empty,
            invoiceType,
            invoiceNumber,
            serviceNumber,
            invoiceDate,
            dueDate,
            payableAmount,
            vatAmount,
            calculation.VatBase,
            calculation.StampDuty,
            calculation.PaymentAmount,
            compensationAmount,
            school?.SchoolId ?? Guid.Empty);

        var validation = InvoiceValidationService.Validate(candidate);
        if (!validation.IsValid)
        {
            errorMessage = validation.Message;
            return false;
        }

        invoice = candidate;
        return true;
    }

    private bool TryResolveAmounts(
        string invoiceType,
        string payableText,
        string vatText,
        string stampDutyText,
        string paymentText,
        bool allowDerivedResultCells,
        out decimal payableAmount,
        out decimal vatAmount,
        out decimal printedStampDuty,
        out decimal printedPaymentAmount,
        out TaxCalculationResult calculation,
        out decimal compensationAmount,
        out bool resultCellsDerived)
    {
        payableAmount = 0m;
        vatAmount = 0m;
        printedStampDuty = 0m;
        printedPaymentAmount = 0m;
        calculation = default!;
        compensationAmount = 0m;
        resultCellsDerived = false;

        var payableCandidates = MoneyCandidates(payableText);
        var vatCandidates = MoneyCandidates(vatText);
        var stampCandidates = MoneyCandidates(stampDutyText);
        var paymentCandidates = MoneyCandidates(paymentText);
        if (payableCandidates.Count == 0 || vatCandidates.Count == 0)
            return false;

        foreach (var payable in payableCandidates)
        foreach (var vat in vatCandidates)
        foreach (var stamp in stampCandidates)
        foreach (var payment in paymentCandidates)
        {
            if (!TryResolveCalculation(
                    invoiceType,
                    payable,
                    vat,
                    stamp,
                    payment,
                    out var candidateCalculation,
                    out var candidateCompensation))
                continue;

            payableAmount = payable;
            vatAmount = vat;
            printedStampDuty = stamp;
            printedPaymentAmount = payment;
            calculation = candidateCalculation;
            compensationAmount = candidateCompensation;
            return true;
        }

        // Eski OCR çıktılarında özellikle tablo çizgileri Damga Vergisi veya
        // Ele Geçen Miktar hücresini bozabiliyor. Tutar + KDV güvenilir okunmuşsa
        // ve basılı sonuç alanlarından en az biri hesap motoruyla eşleşiyorsa,
        // eksik olan sonuç alanı güvenli biçimde hesaplamadan tamamlanabilir.
        if (stampCandidates.Count == 0
            && paymentCandidates.Count == 0
            && !allowDerivedResultCells)
            return false;

        foreach (var payable in payableCandidates)
        foreach (var vat in vatCandidates)
        {
            var candidateCalculation = _taxCalculationService.Calculate(
                payable,
                vat,
                invoiceType);
            if (!candidateCalculation.IsValid)
                continue;

            var roundedStamp = decimal.Round(
                candidateCalculation.StampDuty,
                2,
                MidpointRounding.AwayFromZero);
            var roundedPayment = decimal.Round(
                candidateCalculation.PaymentAmount,
                2,
                MidpointRounding.AwayFromZero);

            var stampMatches = stampCandidates.Count == 0
                               || stampCandidates.Contains(roundedStamp);
            var paymentMatches = paymentCandidates.Count == 0
                                 || paymentCandidates.Contains(roundedPayment);
            if (!stampMatches || !paymentMatches)
                continue;

            payableAmount = payable;
            vatAmount = vat;
            printedStampDuty = roundedStamp;
            printedPaymentAmount = roundedPayment;
            calculation = candidateCalculation;
            compensationAmount = 0m;
            return true;
        }

        if (allowDerivedResultCells)
        {
            foreach (var payable in payableCandidates)
            foreach (var vat in vatCandidates)
            {
                var candidateCalculation = _taxCalculationService.Calculate(
                    payable,
                    vat,
                    invoiceType);
                if (!candidateCalculation.IsValid)
                    continue;

                payableAmount = payable;
                vatAmount = vat;
                printedStampDuty = decimal.Round(
                    candidateCalculation.StampDuty,
                    2,
                    MidpointRounding.AwayFromZero);
                printedPaymentAmount = decimal.Round(
                    candidateCalculation.PaymentAmount,
                    2,
                    MidpointRounding.AwayFromZero);
                calculation = candidateCalculation;
                compensationAmount = 0m;
                resultCellsDerived = true;
                return true;
            }
        }

        return false;
    }

    private bool TryResolveAmountsFromRowGeometry(
        IReadOnlyList<WordData> words,
        HeaderLayout header,
        string invoiceType,
        bool allowDerivedResultCells,
        out decimal payableAmount,
        out decimal vatAmount,
        out decimal printedStampDuty,
        out decimal printedPaymentAmount,
        out TaxCalculationResult calculation,
        out decimal compensationAmount,
        out bool resultCellsDerived)
    {
        payableAmount = 0m;
        vatAmount = 0m;
        printedStampDuty = 0m;
        printedPaymentAmount = 0m;
        calculation = default!;
        compensationAmount = 0m;
        resultCellsDerived = false;

        if (!header.Ranges.TryGetValue(ColumnKind.Payable, out var payableRange))
            return false;

        // Gerçek eski taramalarda OCR kelime merkezini komşu sütuna birkaç
        // piksel taşıyabiliyor. Sütun-slice başarısız olduğunda, para bölgesindeki
        // tokenları soldan sağa tekrar değerlendir. Böylece başlık geometrisine
        // tamamen bağımlı olmadan Tutar -> KDV -> Damga -> Ödeme sırası korunur.
        var moneyWords = words
            .Where(word => word.Right > payableRange.Left)
            .OrderBy(word => word.Left)
            .Select(word => new RowMoneyWord(
                MoneyCandidates(word.Text)))
            .Where(item => item.Candidates.Count > 0)
            .ToArray();

        if (moneyWords.Length < 2)
            return false;

        // Önce satırdaki 4 para hücresini doğrudan doğrulamayı dene.
        for (var payableIndex = 0; payableIndex < moneyWords.Length; payableIndex++)
        for (var vatIndex = payableIndex + 1; vatIndex < moneyWords.Length; vatIndex++)
        for (var stampIndex = vatIndex + 1; stampIndex < moneyWords.Length; stampIndex++)
        for (var paymentIndex = stampIndex + 1; paymentIndex < moneyWords.Length; paymentIndex++)
        {
            foreach (var payable in moneyWords[payableIndex].Candidates)
            foreach (var vat in moneyWords[vatIndex].Candidates)
            foreach (var stamp in moneyWords[stampIndex].Candidates)
            foreach (var payment in moneyWords[paymentIndex].Candidates)
            {
                if (!TryResolveCalculation(
                        invoiceType,
                        payable,
                        vat,
                        stamp,
                        payment,
                        out var candidateCalculation,
                        out var candidateCompensation))
                    continue;

                payableAmount = payable;
                vatAmount = vat;
                printedStampDuty = stamp;
                printedPaymentAmount = payment;
                calculation = candidateCalculation;
                compensationAmount = candidateCompensation;
                return true;
            }
        }

        if (!allowDerivedResultCells)
            return false;

        // Ana Ödeme Emri toplamları mevcutsa Tutar + KDV'nin soldan sağa ilk
        // güvenilir ikilisinden sonuç hücreleri hesap motoruyla üretilebilir.
        // Belge seviyesindeki aggregate kontrol bu sonucu ayrıca doğrular.
        for (var payableIndex = 0; payableIndex < moneyWords.Length; payableIndex++)
        for (var vatIndex = payableIndex + 1; vatIndex < moneyWords.Length; vatIndex++)
        {
            foreach (var payable in moneyWords[payableIndex].Candidates)
            foreach (var vat in moneyWords[vatIndex].Candidates)
            {
                if (payable <= 0m
                    || vat < 0m
                    || vat > payable
                    || vat > payable * 0.5m)
                    continue;

                var candidateCalculation = _taxCalculationService.Calculate(
                    payable,
                    vat,
                    invoiceType);
                if (!candidateCalculation.IsValid)
                    continue;

                payableAmount = payable;
                vatAmount = vat;
                printedStampDuty = decimal.Round(
                    candidateCalculation.StampDuty,
                    2,
                    MidpointRounding.AwayFromZero);
                printedPaymentAmount = decimal.Round(
                    candidateCalculation.PaymentAmount,
                    2,
                    MidpointRounding.AwayFromZero);
                calculation = candidateCalculation;
                compensationAmount = 0m;
                resultCellsDerived = true;
                return true;
            }
        }

        return false;
    }

    private bool TryResolveCalculation(
        string invoiceType,
        decimal payableAmount,
        decimal vatAmount,
        decimal printedStampDuty,
        decimal printedPaymentAmount,
        out TaxCalculationResult calculation,
        out decimal compensationAmount)
    {
        calculation = _taxCalculationService.Calculate(payableAmount, vatAmount, invoiceType);
        compensationAmount = 0m;
        if (MatchesPrinted(calculation, printedStampDuty, printedPaymentAmount))
            return true;

        if (!TaxCalculationService.IsInterruptionCompensationAllowed(invoiceType))
            return false;

        var estimate = printedStampDuty / TaxCalculationService.StampDutyRate + vatAmount - payableAmount;
        var centerInCents = decimal.ToInt32(
            decimal.Round(estimate * 100m, 0, MidpointRounding.AwayFromZero));

        for (var distance = 0; distance <= 110; distance++)
        {
            foreach (var candidateInCents in distance == 0
                         ? new[] { centerInCents }
                         : new[] { centerInCents - distance, centerInCents + distance })
            {
                if (candidateInCents <= 0)
                    continue;

                var candidate = candidateInCents / 100m;
                var candidateCalculation = _taxCalculationService.Calculate(
                    payableAmount,
                    vatAmount,
                    invoiceType,
                    candidate);
                if (!MatchesPrinted(candidateCalculation, printedStampDuty, printedPaymentAmount))
                    continue;

                calculation = candidateCalculation;
                compensationAmount = candidate;
                return true;
            }
        }

        return false;
    }

    private static bool MatchesPrinted(
        TaxCalculationResult calculation,
        decimal printedStampDuty,
        decimal printedPaymentAmount) =>
        calculation.IsValid
        && decimal.Round(calculation.StampDuty, 2, MidpointRounding.AwayFromZero) == printedStampDuty
        && decimal.Round(calculation.PaymentAmount, 2, MidpointRounding.AwayFromZero) == printedPaymentAmount;

    private static string NormalizeInstitutionCell(string text)
    {
        var value = (text ?? string.Empty).Trim();

        // Bazı eski PDF'lerde sıra numarası ile okul/kurum sütunu geometrik olarak
        // birleşebiliyor. Yalnız baştaki saf sıra numarasını temizle; kurum adındaki
        // diğer rakamlara dokunma.
        value = Regex.Replace(
            value,
            @"^\s*\d{1,4}\s+(?=\p{L})",
            string.Empty,
            RegexOptions.CultureInvariant);

        return value.Trim();
    }

    private static SchoolRecord? ResolveSchool(
        string institutionText,
        IReadOnlyList<SchoolRecord> schools)
    {
        var key = NormalizeKey(institutionText);
        var compactKey = key.Replace(" ", string.Empty, StringComparison.Ordinal);
        return schools.FirstOrDefault(school =>
        {
            var schoolKey = NormalizeKey(school.Name);
            return string.Equals(schoolKey, key, StringComparison.Ordinal)
                   || string.Equals(
                       schoolKey.Replace(" ", string.Empty, StringComparison.Ordinal),
                       compactKey,
                       StringComparison.Ordinal);
        });
    }

    private static bool TryFindHeaderLayout(PageData page, out HeaderLayout layout)
    {
        layout = default!;
        HeaderLayout? best = null;

        for (var lineIndex = 0; lineIndex < page.Lines.Count; lineIndex++)
        {
            var candidateWords = new List<WordData>(page.Lines[lineIndex].Words);
            var bottom = page.Lines[lineIndex].Bottom;

            // Bazı eski PDF'lerde başlık aynı hücre içinde iki satıra bölünür.
            if (lineIndex + 1 < page.Lines.Count)
            {
                var nextLine = page.Lines[lineIndex + 1];
                var averageHeight = candidateWords.Count == 0
                    ? 12d
                    : candidateWords.Average(word => word.Height);
                var maxHeaderLineGap = Math.Max(28d, averageHeight * 2.25d);
                if (nextLine.Top - page.Lines[lineIndex].Top < maxHeaderLineGap
                    && !LooksLikeInvoiceDataLine(nextLine))
                {
                    candidateWords.AddRange(nextLine.Words);
                    bottom = Math.Max(bottom, nextLine.Bottom);
                }
            }

            var centers = new Dictionary<ColumnKind, double>();
            foreach (var pair in HeaderAliases)
            {
                if (TryFindAliasCenter(candidateWords, pair.Value, out var center))
                    centers[pair.Key] = center;
            }

            var detectedRequiredCount = RequiredColumns.Count(centers.ContainsKey);
            if (detectedRequiredCount < RequiredColumns.Length - 1)
                continue;

            // Eski tarama/OCR çıktılarında sağdaki "Damga Vergisi" veya
            // "Ele Geçen Miktar" başlığından biri kaybolabiliyor. Yedi temel
            // sütun güvenilir bulunduysa tek eksik sütunun merkezi komşu
            // semantik sütunlardan güvenli biçimde türetilir.
            if (detectedRequiredCount == RequiredColumns.Length - 1)
                TryInferSingleMissingRequiredCenter(page.Width, centers);

            var requiredCount = RequiredColumns.Count(centers.ContainsKey);
            if (requiredCount != RequiredColumns.Length)
                continue;

            var candidate = BuildHeaderLayout(page.Width, centers, bottom);
            if (candidate is null)
                continue;

            if (best is null || detectedRequiredCount > best.DetectedRequiredCount)
                best = candidate with { DetectedRequiredCount = detectedRequiredCount };
        }

        if (best is null || RequiredColumns.Any(column => !best.Ranges.ContainsKey(column)))
            return false;

        layout = best;
        return true;
    }

    private static bool TryInferSingleMissingRequiredCenter(
        double pageWidth,
        Dictionary<ColumnKind, double> centers)
    {
        var missing = RequiredColumns
            .Where(column => !centers.ContainsKey(column))
            .ToArray();
        if (missing.Length != 1)
            return false;

        var semanticOrder = new[]
        {
            ColumnKind.Institution,
            ColumnKind.ServiceNumber,
            ColumnKind.InvoiceNumber,
            ColumnKind.InvoiceDate,
            ColumnKind.DueDate,
            ColumnKind.Payable,
            ColumnKind.Vat,
            ColumnKind.StampDuty,
            ColumnKind.Payment
        };

        var missingIndex = Array.IndexOf(semanticOrder, missing[0]);
        if (missingIndex < 0)
            return false;

        double? leftCenter = null;
        for (var index = missingIndex - 1; index >= 0; index--)
        {
            if (!centers.TryGetValue(semanticOrder[index], out var center))
                continue;
            leftCenter = center;
            break;
        }

        double? rightCenter = null;
        for (var index = missingIndex + 1; index < semanticOrder.Length; index++)
        {
            if (!centers.TryGetValue(semanticOrder[index], out var center))
                continue;
            rightCenter = center;
            break;
        }

        double inferredCenter;
        if (leftCenter.HasValue && rightCenter.HasValue)
        {
            inferredCenter = (leftCenter.Value + rightCenter.Value) / 2d;
        }
        else if (leftCenter.HasValue && missing[0] == ColumnKind.Payment)
        {
            inferredCenter = (leftCenter.Value + pageWidth) / 2d;
        }
        else
        {
            return false;
        }

        if (inferredCenter <= 0d || inferredCenter >= pageWidth)
            return false;

        centers[missing[0]] = inferredCenter;
        return true;
    }

    private static HeaderLayout? BuildHeaderLayout(
        double pageWidth,
        IReadOnlyDictionary<ColumnKind, double> detectedCenters,
        double bottom)
    {
        var ordered = detectedCenters
            .OrderBy(pair => pair.Value)
            .ToArray();
        if (ordered.Length < 7)
            return null;

        var ranges = new Dictionary<ColumnKind, ColumnRange>();
        for (var index = 0; index < ordered.Length; index++)
        {
            var left = index == 0
                ? 0d
                : (ordered[index - 1].Value + ordered[index].Value) / 2d;
            var right = index == ordered.Length - 1
                ? pageWidth
                : (ordered[index].Value + ordered[index + 1].Value) / 2d;
            ranges[ordered[index].Key] = new ColumnRange(left, right);
        }

        return new HeaderLayout(ranges, bottom, 0);
    }

    private static bool LooksLikeInvoiceDataLine(LineData line)
    {
        if (line.Words.Count == 0)
            return false;

        var dateCount = ExtractDates(line.Text).Take(2).Count();
        if (dateCount >= 1)
            return true;

        var numericTokenCount = 0;
        foreach (var word in line.Words)
        {
            var compact = word.Text
                .Replace(".", string.Empty, StringComparison.Ordinal)
                .Replace(",", string.Empty, StringComparison.Ordinal)
                .Replace("-", string.Empty, StringComparison.Ordinal)
                .Replace("/", string.Empty, StringComparison.Ordinal)
                .Replace(":", string.Empty, StringComparison.Ordinal)
                .Trim();

            if (compact.Length >= 3 && compact.All(char.IsDigit))
                numericTokenCount++;

            var hasLetter = compact.Any(char.IsLetter);
            var hasDigit = compact.Any(char.IsDigit);
            if (compact.Length >= 8 && hasLetter && hasDigit)
                return true;

            // Sparse OCR bir gerçek fatura satırını birkaç text-line parçasına
            // bölebilir. Tek bir uzun hizmet/tutar değeri bile başlığın devamı
            // olarak yutulmamalıdır.
            if (compact.Length >= 5 && compact.All(char.IsDigit))
                return true;
        }

        return numericTokenCount >= 2;
    }

    private static bool TryFindAliasCenter(
        IReadOnlyList<WordData> words,
        IEnumerable<string> aliases,
        out double center)
    {
        center = 0d;
        var available = words
            .Where(word => !string.IsNullOrWhiteSpace(word.Text))
            .ToArray();

        var bestScore = double.MaxValue;
        var found = false;

        foreach (var alias in aliases.OrderByDescending(alias => NormalizeKey(alias).Length))
        {
            var compactAlias = NormalizeToken(alias);
            if (!string.IsNullOrEmpty(compactAlias))
            {
                var compactMatch = available
                    .Where(word => TokenMatches(
                        NormalizeToken(word.Text),
                        compactAlias))
                    .OrderBy(word => word.Right - word.Left)
                    .FirstOrDefault();
                if (compactMatch is not null)
                {
                    center = compactMatch.CenterX;
                    return true;
                }
            }

            var tokens = Tokenize(alias);
            if (tokens.Length == 0)
                continue;

            var candidateSets = tokens
                .Select(token => available
                    .Where(word => TokenMatches(
                        NormalizeToken(word.Text),
                        token))
                    .ToArray())
                .ToArray();

            if (candidateSets.Any(set => set.Length == 0))
                continue;

            var selected = new WordData[tokens.Length];
            SearchAliasCombination(
                candidateSets,
                selected,
                0,
                ref bestScore,
                ref center,
                ref found);
        }

        return found;
    }

    private static void SearchAliasCombination(
        IReadOnlyList<WordData[]> candidateSets,
        WordData[] selected,
        int index,
        ref double bestScore,
        ref double bestCenter,
        ref bool found)
    {
        if (index == candidateSets.Count)
        {
            if (selected.Distinct().Count() != selected.Length)
                return;

            var minLeft = selected.Min(word => word.Left);
            var maxRight = selected.Max(word => word.Right);
            var minTop = selected.Min(word => word.Top);
            var maxBottom = selected.Max(word => word.Bottom);
            var horizontalSpan = maxRight - minLeft;
            var verticalSpan = maxBottom - minTop;

            // Aynı hücredeki çok satırlı başlıkları destekler; uzak sütunlardan
            // yanlış kelimelerin eşleşmesini ise en küçük geometrik kapsama ile eler.
            var score = horizontalSpan + verticalSpan * 0.65d;
            if (score >= bestScore)
                return;

            bestScore = score;
            bestCenter = (minLeft + maxRight) / 2d;
            found = true;
            return;
        }

        foreach (var candidate in candidateSets[index])
        {
            selected[index] = candidate;
            SearchAliasCombination(
                candidateSets,
                selected,
                index + 1,
                ref bestScore,
                ref bestCenter,
                ref found);
        }
    }

    private static bool TokenMatches(string actual, string expected)
    {
        if (string.Equals(actual, expected, StringComparison.Ordinal))
            return true;
        if (actual.Length < 4 || expected.Length < 4)
            return false;

        var maxDistance = Math.Max(actual.Length, expected.Length) >= 8 ? 2 : 1;
        return LevenshteinDistance(actual, expected, maxDistance) <= maxDistance;
    }

    private static int LevenshteinDistance(string left, string right, int stopAfter)
    {
        if (Math.Abs(left.Length - right.Length) > stopAfter)
            return stopAfter + 1;

        var previous = Enumerable.Range(0, right.Length + 1).ToArray();
        var current = new int[right.Length + 1];

        for (var i = 1; i <= left.Length; i++)
        {
            current[0] = i;
            var rowMinimum = current[0];
            for (var j = 1; j <= right.Length; j++)
            {
                var cost = left[i - 1] == right[j - 1] ? 0 : 1;
                current[j] = Math.Min(
                    Math.Min(current[j - 1] + 1, previous[j] + 1),
                    previous[j - 1] + cost);
                rowMinimum = Math.Min(rowMinimum, current[j]);
            }

            if (rowMinimum > stopAfter)
                return stopAfter + 1;

            (previous, current) = (current, previous);
        }

        return previous[right.Length];
    }

    private static LineData[] BuildInvoiceRows(
        PageData page,
        HeaderLayout header)
    {
        if (!header.Ranges.TryGetValue(ColumnKind.InvoiceDate, out var dateRange))
            return page.Lines
                .Where(line => line.Top > header.Bottom + 1d)
                .ToArray();

        var dataWords = page.Words
            .Where(word => word.Top > header.Bottom + 1d)
            .ToArray();

        var anchors = dataWords
            .Where(word => word.CenterX >= dateRange.Left && word.CenterX < dateRange.Right)
            .Where(word => TryDate(word.Text, out _))
            .OrderBy(word => word.CenterY)
            .ToArray();

        if (anchors.Length == 0)
        {
            return page.Lines
                .Where(line => line.Top > header.Bottom + 1d)
                .ToArray();
        }

        var uniqueAnchors = new List<WordData>();
        foreach (var anchor in anchors)
        {
            if (uniqueAnchors.Any(existing => Math.Abs(existing.CenterY - anchor.CenterY) < 8d))
                continue;

            uniqueAnchors.Add(anchor);
        }

        var rows = new List<LineData>();
        foreach (var anchor in uniqueAnchors)
        {
            var tolerance = Math.Clamp(anchor.Height * 1.25d, 14d, 24d);
            var rowWords = dataWords
                .Where(word =>
                {
                    var distance = Math.Abs(word.CenterY - anchor.CenterY);
                    if (distance > tolerance)
                        return false;

                    // OCR aynı fiziksel satırdaki hücreleri birkaç piksel yukarı/aşağı
                    // kaydırabilir. Sabit bantlar komşu satırları üst üste bindirebildiği
                    // için her kelimeyi en yakın fatura-tarihi ankrajına ait say.
                    var nearestAnchorDistance = uniqueAnchors
                        .Min(candidate => Math.Abs(word.CenterY - candidate.CenterY));
                    return Math.Abs(distance - nearestAnchorDistance) < 0.01d;
                })
                .OrderBy(word => word.Left)
                .ToArray();

            if (rowWords.Length == 0)
                continue;

            rows.Add(new LineData(
                rowWords.Average(word => word.Top),
                rowWords.Max(word => word.Bottom),
                string.Join(" ", rowWords.Select(word => word.Text)),
                rowWords));
        }

        return rows.Count > 0
            ? rows.OrderBy(row => row.Top).ToArray()
            : page.Lines.Where(line => line.Top > header.Bottom + 1d).ToArray();
    }

    private static string ReadInvoiceNumber(
        IReadOnlyList<WordData> words,
        HeaderLayout header)
    {
        var direct = ReadColumn(words, header, ColumnKind.InvoiceNumber)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Trim();
        if (!string.IsNullOrWhiteSpace(direct))
            return direct;

        if (!header.Ranges.TryGetValue(ColumnKind.InvoiceNumber, out var range))
            return string.Empty;

        // Uzun fatura numaraları dar başlığın merkez sınırını aşabilir.
        // Merkez tabanlı normal okuma boş kaldığında, kendi sütunuyla anlamlı
        // geometrik örtüşmesi olan kelimeleri güvenli fallback olarak kullan.
        var overlapping = words
            .Where(word =>
            {
                var width = Math.Max(1d, word.Right - word.Left);
                var overlap = Math.Max(
                    0d,
                    Math.Min(word.Right, range.Right) - Math.Max(word.Left, range.Left));
                return overlap / width >= 0.35d
                       || word.Left >= range.Left && word.Left < range.Right;
            })
            .OrderBy(word => word.Left)
            .Select(word => word.Text)
            .ToArray();

        return string.Concat(overlapping)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Trim();
    }

    private static string ReadColumn(
        IReadOnlyList<WordData> words,
        HeaderLayout header,
        ColumnKind column)
    {
        if (!header.Ranges.TryGetValue(column, out var range))
            return string.Empty;

        return string.Join(
            " ",
            words
                .Where(word => word.CenterX >= range.Left && word.CenterX < range.Right)
                .OrderBy(word => word.Left)
                .Select(word => word.Text));
    }

    private static string? FindInvoiceType(string text)
    {
        var normalized = NormalizeKey(text);
        if (Regex.IsMatch(normalized, @"\belektrik\b", RegexOptions.CultureInvariant))
            return "Elektrik";
        if (Regex.IsMatch(normalized, @"\btelefon\b|\bgsm\b", RegexOptions.CultureInvariant))
            return "Telefon";
        if (Regex.IsMatch(normalized, @"\binternet\b|\badsl\b|\bfiber\b", RegexOptions.CultureInvariant))
            return "İnternet";
        if (Regex.IsMatch(normalized, @"\bsu\b", RegexOptions.CultureInvariant))
            return "Su";
        return null;
    }

    private static DateTime? FindReportDate(IReadOnlyList<PageData> pages)
    {
        foreach (var page in pages)
        {
            for (var index = 0; index < page.Lines.Count; index++)
            {
                var normalized = NormalizeKey(page.Lines[index].Text);
                if (!normalized.Contains("duzenleme", StringComparison.Ordinal)
                    && !normalized.Contains("belge tarihi", StringComparison.Ordinal))
                    continue;

                var neighborhood = string.Join(
                    " ",
                    page.Lines
                        .Skip(index)
                        .Take(3)
                        .Select(line => line.Text));
                if (TryFindDate(neighborhood, out var date))
                    return date;
            }
        }

        // Başlık etiketi OCR'da parçalanmışsa imza/belge tarihi genellikle aynı
        // tarihin birden fazla kez yazılmasıyla ayırt edilebilir.
        var repeatedDate = pages
            .SelectMany(page => page.Lines)
            .SelectMany(line => ExtractDates(line.Text))
            .GroupBy(date => date.Date)
            .OrderByDescending(group => group.Count())
            .ThenByDescending(group => group.Key)
            .FirstOrDefault();
        if (repeatedDate is not null && repeatedDate.Count() >= 2)
            return repeatedDate.Key;

        foreach (var page in pages.Take(2))
        {
            foreach (var line in page.Lines.Where(line => line.Top <= page.Height * 0.28d))
            {
                if (TryFindDate(line.Text, out var date))
                    return date;
            }
        }

        return null;
    }

    private static AuthorityProfile ExtractAuthorityProfile(
        IReadOnlyList<PageData> pages,
        AuthorityProfile fallback)
    {
        foreach (var page in pages)
        {
            var left = ExtractSignature(page, true);
            var right = ExtractSignature(page, false);
            if (left.Name is null && right.Name is null)
                continue;

            return new AuthorityProfile(
                left.Name ?? fallback.RealizationOfficerName,
                left.Title ?? fallback.RealizationOfficerTitle,
                right.Name ?? fallback.SpendingAuthorityName,
                right.Title ?? fallback.SpendingAuthorityTitle).Normalize();
        }

        return fallback.Normalize();
    }

    private static (string? Name, string? Title) ExtractSignature(PageData page, bool leftSide)
    {
        var sideLines = page.Lines
            .Where(line =>
            {
                var center = line.Words.Count == 0
                    ? 0d
                    : line.Words.Average(word => word.CenterX);
                return leftSide ? center < page.Width / 2d : center >= page.Width / 2d;
            })
            .ToArray();

        var roleIndex = Array.FindIndex(
            sideLines,
            line => NormalizeKey(line.Text).Contains(
                leftSide ? "gerceklestirme gorevlisi" : "harcama yetkilisi",
                StringComparison.Ordinal));
        if (roleIndex < 0)
            return (null, null);

        var candidates = sideLines
            .Skip(roleIndex + 1)
            .Take(8)
            .Where(line => !TryFindDate(line.Text, out _))
            .Select(line => line.Text.Trim())
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Where(text => !NormalizeKey(text).Contains("kontrol edilmis", StringComparison.Ordinal))
            .Where(text => !NormalizeKey(text).Contains("uygun gorulmustur", StringComparison.Ordinal))
            .ToArray();

        return candidates.Length switch
        {
            >= 2 => (candidates[0], candidates[1]),
            1 => (candidates[0], null),
            _ => (null, null)
        };
    }

    private static bool TryResolveHysNumber(
        string sourcePath,
        string documentText,
        out string hysNumber,
        out string errorMessage)
    {
        var hasFileNumber = HistoricalPdfImportService.TryExtractHysNumberFromFileName(
            Path.GetFileNameWithoutExtension(sourcePath),
            out var fileNumber,
            out var fileError);
        var hasDocumentNumber = TryExtractPaymentOrderNumberFromText(
            documentText,
            out var documentNumber);

        if (hasFileNumber && hasDocumentNumber
            && !string.Equals(fileNumber, documentNumber, StringComparison.Ordinal))
        {
            hysNumber = string.Empty;
            errorMessage = $"Dosya adındaki Ödeme Emri Belge No ({fileNumber}) ile PDF içindeki değer ({documentNumber}) uyuşmuyor.";
            return false;
        }

        if (hasDocumentNumber)
        {
            hysNumber = documentNumber;
            errorMessage = string.Empty;
            return true;
        }

        if (hasFileNumber)
        {
            hysNumber = fileNumber;
            errorMessage = string.Empty;
            return true;
        }

        hysNumber = string.Empty;
        errorMessage = "Ödeme Emri Belge No ne dosya adından ne de PDF içeriğinden okunabildi. " + fileError;
        return false;
    }

    private static bool TryExtractPaymentOrderNumberFromText(string text, out string hysNumber)
    {
        hysNumber = string.Empty;
        var normalized = NormalizeKey(text);
        var match = Regex.Match(
            normalized,
            @"\b(?<prefix>hys|mys)\s*[-:]?\s*(?<year>20[0-9]{2})\s*[- ]?\s*(?<number>[0-9]{4,24})\b",
            RegexOptions.CultureInvariant);
        if (!match.Success)
            return false;

        var raw = match.Groups["prefix"].Value.ToUpperInvariant()
                  + "-"
                  + match.Groups["year"].Value
                  + match.Groups["number"].Value;
        return HysPaymentOrderService.TryNormalize(raw, out hysNumber, out _);
    }

    private static List<PageData> ConvertExtractedPages(
        IReadOnlyList<HistoricalPdfImportService.PdfPageData> sourcePages)
    {
        var pages = new List<PageData>(sourcePages.Count);
        foreach (var sourcePage in sourcePages)
        {
            var words = sourcePage.Words
                .Select(word => new WordData(
                    word.Text,
                    word.Left,
                    word.Right,
                    word.Top,
                    word.Bottom))
                .OrderBy(word => word.Top)
                .ThenBy(word => word.Left)
                .ToArray();

            pages.Add(new PageData(
                sourcePage.PageNumber,
                sourcePage.Width,
                sourcePage.Height,
                words,
                BuildLines(words)));
        }

        return pages;
    }

    private static List<PageData> ExtractPdfTextPages(
        string sourcePath,
        CancellationToken cancellationToken)
    {
        using var document = PdfDocument.Open(sourcePath);
        var pages = new List<PageData>(document.NumberOfPages);
        foreach (var page in document.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var pageHeight = Convert.ToDouble(page.Height, CultureInfo.InvariantCulture);
            var words = page.GetWords()
                .Where(word => !string.IsNullOrWhiteSpace(word.Text))
                .Select(word => new WordData(
                    word.Text.Trim(),
                    Convert.ToDouble(word.BoundingBox.Left, CultureInfo.InvariantCulture),
                    Convert.ToDouble(word.BoundingBox.Right, CultureInfo.InvariantCulture),
                    pageHeight - Convert.ToDouble(word.BoundingBox.Top, CultureInfo.InvariantCulture),
                    pageHeight - Convert.ToDouble(word.BoundingBox.Bottom, CultureInfo.InvariantCulture)))
                .OrderBy(word => word.Top)
                .ThenBy(word => word.Left)
                .ToArray();

            pages.Add(new PageData(
                page.Number,
                Convert.ToDouble(page.Width, CultureInfo.InvariantCulture),
                pageHeight,
                words,
                BuildLines(words)));
        }

        return pages;
    }

    private static OcrPageResult TryExtractOcrPages(
        string sourcePath,
        IReadOnlyList<PageData> extractedPages,
        IProgress<string>? progress,
        Action onOcrStarted,
        CancellationToken cancellationToken)
    {
        try
        {
            return ExtractOcrPagesAsync(
                    sourcePath,
                    extractedPages,
                    progress,
                    onOcrStarted,
                    cancellationToken)
                .GetAwaiter()
                .GetResult();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (DllNotFoundException ex)
        {
            return new OcrPageResult(
                Array.Empty<PageData>(),
                "OCR motorunun yerel bileşenleri yüklenemedi: " + ex.Message, false);
        }
        catch (Exception ex)
        {
            return new OcrPageResult(Array.Empty<PageData>(), ex.Message, false);
        }
    }

    private static async Task<OcrPageResult> ExtractOcrPagesAsync(
        string sourcePath,
        IReadOnlyList<PageData> extractedPages,
        IProgress<string>? progress,
        Action onOcrStarted,
        CancellationToken cancellationToken)
    {
        var tessdataDirectory = Path.Combine(AppContext.BaseDirectory, "tessdata");
        var tessdataFile = Path.Combine(tessdataDirectory, "tur.traineddata");
        if (!File.Exists(tessdataFile))
        {
            return new OcrPageResult(
                Array.Empty<PageData>(),
                "Türkçe OCR modeli kurulumda bulunamadı: " + tessdataFile, false);
        }

        using var engine = new TesseractEngine(
            tessdataDirectory,
            Language.Turkish,
            EngineMode.LstmOnly);

        var file = await StorageFile.GetFileFromPathAsync(sourcePath);
        var document = await Windows.Data.Pdf.PdfDocument.LoadFromFileAsync(file);
        var pages = new List<PageData>((int)document.PageCount);
        var ocrRan = false;
        var extractedByNumber = extractedPages.ToDictionary(page => page.PageNumber);

        for (uint pageIndex = 0; pageIndex < document.PageCount; pageIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var pageTimer = Stopwatch.StartNew();
            var pageNumber = checked((int)pageIndex + 1);
            extractedByNumber.TryGetValue(pageNumber, out var extractedPage);
            // Tanıdık birkaç sözcük fatura tablosunun veya ek faturanın
            // okunabildiğini kanıtlamaz. Yalnız toplamları okunan ödeme emri
            // sayfasını atla; eksik fatura sayfasını hedefli OCR'a bırak.
            if (extractedPage is not null
                && NormalizeKey(string.Join(" ",
                    extractedPage.Lines.Select(line => line.Text)))
                    .Contains("odeme emri belgesi", StringComparison.Ordinal)
                && (TryFindDocumentTotals(new[] { extractedPage }, out _)
                    || TryFindEmbeddedOrderTotals(new[] { extractedPage }, out _)))
            {
                progress?.Report($"OCR {pageNumber}/{document.PageCount}: ödeme emri toplamları metinden doğrulandı; sayfa atlandı");
                continue;
            }

            var useFocusedTableOcr = IsImageDominantLegacyPage(extractedPage);
            using var pdfPage = document.GetPage(pageIndex);

            var probeTimer = Stopwatch.StartNew();
            progress?.Report($"OCR {pageNumber}/{document.PageCount}: yön hızlıca belirleniyor");
            OrientationProbeResult probe;
            {
                var probePngBytes = await RenderPdfPageAsync(pdfPage, OrientationProbeWidth);
                cancellationToken.ThrowIfCancellationRequested();
                using var probeImage = TesseractPix.LoadFromMemory(probePngBytes);
                using var probeGray = probeImage.ConvertRGBToGray();
                ocrRan = true;
                onOcrStarted();
                probe = DetectBestOcrOrientation(
                    engine,
                    probeGray,
                    pageNumber,
                    cancellationToken);
            }

            progress?.Report(
                $"OCR {pageNumber}/{document.PageCount}: {OrientationText(probe.Orientation)} seçildi "
                + $"({probeTimer.ElapsedMilliseconds} ms)");

            if (IsTableOcrComplete(probe.BestPage))
            {
                pages.Add(probe.BestPage!);
                progress?.Report(
                    $"OCR {pageNumber}/{document.PageCount}: 1000 px ön tarama yeterli; "
                    + $"yüksek çözünürlük atlandı ({pageTimer.ElapsedMilliseconds} ms)");
                continue;
            }

            var initialTimer = Stopwatch.StartNew();
            var bestPage = await ProcessOcrAtWidthAsync(
                pdfPage,
                InitialOcrWidth,
                engine,
                useFocusedTableOcr,
                probe.Orientation,
                pageNumber,
                cancellationToken);
            progress?.Report(
                $"OCR {pageNumber}/{document.PageCount}: {InitialOcrWidth} px tarama "
                + $"{initialTimer.ElapsedMilliseconds} ms");

            if (IsTableOcrComplete(bestPage))
            {
                pages.Add(bestPage!);
                progress?.Report(
                    $"OCR {pageNumber}/{document.PageCount}: ara çözünürlük yeterli "
                    + $"({pageTimer.ElapsedMilliseconds} ms)");
                continue;
            }

            var finalWidth = RecommendedRenderWidth(pdfPage);
            if (finalWidth > InitialOcrWidth)
            {
                var finalTimer = Stopwatch.StartNew();
                var finalPage = await ProcessOcrAtWidthAsync(
                    pdfPage,
                    finalWidth,
                    engine,
                    useFocusedTableOcr,
                    probe.Orientation,
                    pageNumber,
                    cancellationToken);
                bestPage = SelectMoreCompleteOcrPage(bestPage, finalPage);
                progress?.Report(
                    $"OCR {pageNumber}/{document.PageCount}: son kademe {finalWidth} px "
                    + $"{finalTimer.ElapsedMilliseconds} ms");
            }

            if (bestPage is not null)
            {
                pages.Add(bestPage);
                progress?.Report(
                    $"OCR {pageNumber}/{document.PageCount}: sayfa tamamlandı "
                    + $"({pageTimer.ElapsedMilliseconds} ms)");
            }
        }

        return new OcrPageResult(pages, string.Empty, ocrRan);
    }

    private static async Task<PageData?> ProcessOcrAtWidthAsync(
        Windows.Data.Pdf.PdfPage pdfPage,
        uint destinationWidth,
        TesseractEngine engine,
        bool useFocusedTableOcr,
        OcrOrientation orientation,
        int pageNumber,
        CancellationToken cancellationToken)
    {
        var pngBytes = await RenderPdfPageAsync(pdfPage, destinationWidth);
        cancellationToken.ThrowIfCancellationRequested();
        using var sourceImage = TesseractPix.LoadFromMemory(pngBytes);

        if (!useFocusedTableOcr)
        {
            return ProcessGeneralOcrForOrientation(
                engine,
                sourceImage,
                orientation,
                pageNumber,
                cancellationToken);
        }

        using var gray = sourceImage.ConvertRGBToGray();
        return ProcessFocusedOcrForOrientation(
            engine,
            gray,
            orientation,
            pageNumber,
            cancellationToken);
    }

    private static PageData? SelectMoreCompleteOcrPage(
        PageData? first,
        PageData? second)
    {
        if (first is null)
            return second;
        if (second is null)
            return first;

        return OcrCandidateScore(second, 0d) > OcrCandidateScore(first, 0d)
            ? second
            : first;
    }

    private static uint RecommendedRenderWidth(Windows.Data.Pdf.PdfPage page) =>
        (uint)Math.Clamp(
            Math.Round(page.Size.Width * 3d),
            1600d,
            2600d);

    private static OrientationProbeResult DetectBestOcrOrientation(
        TesseractEngine engine,
        TesseractPix image,
        int pageNumber,
        CancellationToken cancellationToken)
    {
        var bestOrientation = OcrOrientation.Normal;
        var bestScore = int.MinValue;
        PageData? bestPage = null;

        EvaluateOrientation(
            engine,
            image,
            OcrOrientation.Normal,
            pageNumber,
            ref bestOrientation,
            ref bestScore,
            ref bestPage,
            cancellationToken);

        using (var clockwise = image.Rotate90(RotationDirection.Clockwise))
        {
            EvaluateOrientation(
                engine,
                clockwise,
                OcrOrientation.Clockwise,
                pageNumber,
                ref bestOrientation,
                ref bestScore,
                ref bestPage,
                cancellationToken);
        }

        using (var counterClockwise = image.Rotate90(RotationDirection.CounterClockwise))
        {
            EvaluateOrientation(
                engine,
                counterClockwise,
                OcrOrientation.CounterClockwise,
                pageNumber,
                ref bestOrientation,
                ref bestScore,
                ref bestPage,
                cancellationToken);
        }

        return new OrientationProbeResult(bestOrientation, bestPage);
    }

    private static void EvaluateOrientation(
        TesseractEngine engine,
        TesseractPix image,
        OcrOrientation orientation,
        int pageNumber,
        ref OcrOrientation bestOrientation,
        ref int bestScore,
        ref PageData? bestPage,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var page = engine.Process(image, PageSegMode.SparseText);
        var candidate = CreateOcrPage(pageNumber, image, page);
        var score = OcrCandidateScore(candidate, page.MeanConfidence);
        if (score <= bestScore)
            return;

        bestScore = score;
        bestOrientation = orientation;
        bestPage = candidate;
    }

    private static PageData? ProcessFocusedOcrForOrientation(
        TesseractEngine engine,
        TesseractPix gray,
        OcrOrientation orientation,
        int pageNumber,
        CancellationToken cancellationToken)
    {
        if (orientation == OcrOrientation.Normal)
            return ProcessFocusedOcr(engine, gray, pageNumber, cancellationToken);

        var direction = orientation == OcrOrientation.Clockwise
            ? RotationDirection.Clockwise
            : RotationDirection.CounterClockwise;
        using var orientedGray = gray.Rotate90(direction);
        return ProcessFocusedOcr(
            engine,
            orientedGray,
            pageNumber,
            cancellationToken);
    }

    private static PageData? ProcessFocusedOcr(
        TesseractEngine engine,
        TesseractPix gray,
        int pageNumber,
        CancellationToken cancellationToken)
    {
        PageData? bestPage = null;
        var bestScore = int.MinValue;
        bestPage = SelectBetterTableRegionOcr(
            engine,
            gray,
            pageNumber,
            bestPage,
            ref bestScore,
            cancellationToken);
        if (IsTableOcrComplete(bestPage))
            return bestPage;

        using var cleaned = gray.RemoveLines(150, 150);
        return SelectBetterTableRegionOcr(
            engine,
            cleaned,
            pageNumber,
            bestPage,
            ref bestScore,
            cancellationToken);
    }

    private static PageData? ProcessGeneralOcrForOrientation(
        TesseractEngine engine,
        TesseractPix source,
        OcrOrientation orientation,
        int pageNumber,
        CancellationToken cancellationToken)
    {
        if (orientation == OcrOrientation.Normal)
            return ProcessGeneralOcr(engine, source, pageNumber, cancellationToken);

        var direction = orientation == OcrOrientation.Clockwise
            ? RotationDirection.Clockwise
            : RotationDirection.CounterClockwise;
        using var orientedSource = source.Rotate90(direction);
        return ProcessGeneralOcr(
            engine,
            orientedSource,
            pageNumber,
            cancellationToken);
    }

    private static PageData? ProcessGeneralOcr(
        TesseractEngine engine,
        TesseractPix source,
        int pageNumber,
        CancellationToken cancellationToken)
    {
        PageData? bestPage = null;
        var bestScore = int.MinValue;
        bestPage = SelectBetterOcrPage(
            engine,
            source,
            PageSegMode.Auto,
            pageNumber,
            bestPage,
            ref bestScore,
            cancellationToken);
        if (IsTableOcrComplete(bestPage))
            return bestPage;

        using var gray = source.ConvertRGBToGray();
        using var cleaned = gray.RemoveLines(150, 150);
        bestPage = SelectBetterOcrPage(
            engine,
            cleaned,
            PageSegMode.SingleBlock,
            pageNumber,
            bestPage,
            ref bestScore,
            cancellationToken);
        if (IsTableOcrComplete(bestPage))
            return bestPage;

        return SelectBetterOcrPage(
            engine,
            cleaned,
            PageSegMode.SparseText,
            pageNumber,
            bestPage,
            ref bestScore,
            cancellationToken);
    }

    private static string OrientationText(OcrOrientation orientation) => orientation switch
    {
        OcrOrientation.Clockwise => "90° saat yönü",
        OcrOrientation.CounterClockwise => "90° ters yön",
        _ => "normal yön"
    };

    private static PageData? SelectBetterOcrPage(
        TesseractEngine engine,
        TesseractPix image,
        PageSegMode pageSegMode,
        int pageNumber,
        PageData? currentBest,
        ref int bestScore,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var page = engine.Process(image, pageSegMode);
        var candidate = CreateOcrPage(pageNumber, image, page);
        return SelectBetterOcrCandidate(
            candidate,
            page.MeanConfidence,
            currentBest,
            ref bestScore);
    }

    private static PageData? SelectBetterTableRegionOcr(
        TesseractEngine engine,
        TesseractPix image,
        int pageNumber,
        PageData? currentBest,
        ref int bestScore,
        CancellationToken cancellationToken)
    {
        if (image.Width <= image.Height || image.Height < 300)
            return currentBest;

        cancellationToken.ThrowIfCancellationRequested();

        // Bu fallback yalnız dönük/görüntü tabanlı eski ekler içindir.
        // Gerçek MYS-2023-4010364 örneğinde tablo, doğru yöne çevrilmiş
        // landscape sayfanın yaklaşık üst %5-%36 bandında yer alıyor.
        // İmza/kaşe ve büyük boş alanları OCR dışında bırakarak tabloyu
        // ayrı segmentasyonlarla okutuyoruz.
        var top = Math.Clamp(
            (int)Math.Round(image.Height * 0.05d),
            1,
            image.Height - 2);
        var bottom = Math.Clamp(
            (int)Math.Round(image.Height * 0.36d),
            top + 120,
            image.Height - 1);
        var region = TesseractOCR.Rect.FromCoords(
            1,
            top,
            image.Width - 1,
            bottom);

        var candidates = new List<(PageData Page, double Confidence)>();
        PageData? merged = null;

        foreach (var mode in new[]
                 {
                     PageSegMode.SingleBlock,
                     PageSegMode.SingleColumn,
                     PageSegMode.Auto
                 })
        {
            cancellationToken.ThrowIfCancellationRequested();

            PageData candidate;
            double confidence;
            using (var page = engine.Process(image, region, mode))
            {
                candidate = CreateOcrPage(pageNumber, image, page);
                confidence = page.MeanConfidence;
            }

            candidates.Add((candidate, confidence));
            merged = merged is null
                ? candidate
                : MergeOcrCandidates(merged, candidate);
            currentBest = SelectBetterOcrCandidate(
                candidate,
                confidence,
                currentBest,
                ref bestScore);

            if (!IsTableOcrComplete(merged))
                continue;

            return SelectBetterOcrCandidate(
                merged,
                candidates.Average(item => item.Confidence),
                currentBest,
                ref bestScore);
        }

        if (merged is null)
            return currentBest;

        // Gerçek MYS-2023-4010364 taramasında tablo bandı genel olarak okunuyor
        // ancak 2. ve 3. satırda özellikle KDV gibi tekil para hücreleri tam tablo
        // OCR'ında kaybolabiliyor. Başlık ve fatura-tarihi ankrajı bulunduysa yalnız
        // doğrulanamayan satırın sağdaki para bandını tekrar tara. Bu ikinci geçiş
        // genel 2023 parserını değiştirmez; yalnız image-dominant legacy fallback
        // içinde ve yalnız tutarsız para satırlarında çalışır.
        if (!IsTableOcrComplete(merged))
        {
            merged = EnhanceMissingMoneyRowsWithFocusedOcr(
                engine,
                image,
                pageNumber,
                merged,
                cancellationToken);
        }

        return SelectBetterOcrCandidate(
            merged,
            candidates.Average(item => item.Confidence),
            currentBest,
            ref bestScore);
    }

    private static PageData EnhanceMissingMoneyRowsWithFocusedOcr(
        TesseractEngine engine,
        TesseractPix image,
        int pageNumber,
        PageData page,
        CancellationToken cancellationToken)
    {
        if (!TryFindHeaderLayout(page, out var header)
            || !header.Ranges.TryGetValue(ColumnKind.Payable, out var payableRange))
            return page;

        var enhanced = page;
        var rows = BuildInvoiceRows(page, header);
        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var invoiceDateText = ReadColumn(row.Words, header, ColumnKind.InvoiceDate);
            if (!TryDate(invoiceDateText, out _)
                || HasConsistentMoneyTuple(row, header))
                continue;

            var rowHeight = Math.Max(1d, row.Bottom - row.Top);
            var padding = Math.Clamp(rowHeight * 0.80d, 8d, 24d);
            var top = Math.Clamp(
                (int)Math.Floor(row.Top - padding),
                1,
                image.Height - 2);
            var bottom = Math.Clamp(
                (int)Math.Ceiling(row.Bottom + padding),
                top + 4,
                image.Height - 1);
            var left = Math.Clamp(
                (int)Math.Floor(payableRange.Left),
                1,
                image.Width - 2);

            if (bottom <= top || left >= image.Width - 1)
                continue;

            var moneyRegion = TesseractOCR.Rect.FromCoords(
                left,
                top,
                image.Width - 1,
                bottom);

            foreach (var mode in new[]
                     {
                         PageSegMode.SingleBlock,
                         PageSegMode.SingleLine,
                         PageSegMode.SparseText
                     })
            {
                cancellationToken.ThrowIfCancellationRequested();

                PageData rowCandidate;
                using (var ocrPage = engine.Process(image, moneyRegion, mode))
                {
                    rowCandidate = CreateOcrPage(pageNumber, image, ocrPage);
                }

                enhanced = MergeOcrCandidates(enhanced, rowCandidate);
                if (HasConsistentMoneyRowNear(enhanced, row))
                    break;
            }
        }

        return enhanced;
    }

    private static bool HasConsistentMoneyRowNear(
        PageData page,
        LineData referenceRow)
    {
        if (!TryFindHeaderLayout(page, out var header))
            return false;

        var referenceCenter = (referenceRow.Top + referenceRow.Bottom) / 2d;
        var tolerance = Math.Max(12d, (referenceRow.Bottom - referenceRow.Top) * 1.5d);
        return BuildInvoiceRows(page, header)
            .Where(candidate =>
                Math.Abs(((candidate.Top + candidate.Bottom) / 2d) - referenceCenter)
                <= tolerance)
            .Any(candidate => HasConsistentMoneyTuple(candidate, header));
    }

    private static bool IsImageDominantLegacyPage(PageData? extractedPage)
    {
        if (extractedPage is null)
            return true;

        if (SemanticDocumentScore(extractedPage) > 0)
            return false;

        var meaningfulCharacters = extractedPage.Lines
            .SelectMany(line => line.Text)
            .Count(ch => char.IsLetterOrDigit(ch));

        return extractedPage.Words.Count < 80
               || meaningfulCharacters < 180;
    }

    private static bool IsTableOcrComplete(PageData? page)
    {
        if (page is null || !TryFindHeaderLayout(page, out var header))
            return false;

        var invoiceRows = BuildInvoiceRows(page, header)
            .Where(row => TryDate(
                ReadColumn(row.Words, header, ColumnKind.InvoiceDate),
                out _))
            .ToArray();

        // Tek satırlı görüntü belgelerinde tüm modlar korunur. Erken çıkış,
        // yalnız en az iki satırın da para dörtlüsü tutarlıysa devreye girer.
        if (invoiceRows.Length < 2
            || invoiceRows.Any(row => !HasConsistentMoneyTuple(row, header)))
            return false;

        if (!header.Ranges.TryGetValue(ColumnKind.Sequence, out var sequenceRange))
            return true;

        var sequenceNumbers = new HashSet<int>();
        foreach (var word in page.Words.Where(word =>
                     word.Top > header.Bottom + 1d
                     && word.CenterX >= sequenceRange.Left
                     && word.CenterX < sequenceRange.Right))
        {
            var digits = DigitsOnly(word.Text);
            if (digits.Length <= 3
                && int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var sequence)
                && sequence > 0)
                sequenceNumbers.Add(sequence);
        }

        return sequenceNumbers.Count <= invoiceRows.Length;
    }

    private static PageData MergeOcrCandidates(
        PageData first,
        PageData second)
    {
        var words = new List<WordData>(first.Words);

        foreach (var candidate in second.Words)
        {
            var duplicate = words.Any(existing =>
                string.Equals(
                    NormalizeToken(existing.Text),
                    NormalizeToken(candidate.Text),
                    StringComparison.Ordinal)
                && Math.Abs(existing.CenterX - candidate.CenterX) <= 18d
                && Math.Abs(existing.CenterY - candidate.CenterY) <= 12d);

            if (!duplicate)
                words.Add(candidate);
        }

        var ordered = words
            .OrderBy(word => word.Top)
            .ThenBy(word => word.Left)
            .ToArray();

        return new PageData(
            first.PageNumber,
            first.Width,
            first.Height,
            ordered,
            BuildLines(ordered));
    }

    private static PageData? SelectBetterOcrCandidate(
        PageData candidate,
        double meanConfidence,
        PageData? currentBest,
        ref int bestScore)
    {
        var score = OcrCandidateScore(candidate, meanConfidence);

        if (score <= bestScore)
            return currentBest;

        bestScore = score;
        return candidate;
    }

    private static int OcrCandidateScore(PageData candidate, double meanConfidence) =>
        HeaderScore(candidate) * 1_000_000
        + InvoiceDataScore(candidate) * 100_000
        + SemanticDocumentScore(candidate) * 10_000
        + (int)Math.Round(meanConfidence * 100d)
        + Math.Min(candidate.Words.Count, 5000);

    private static Task<byte[]> RenderPdfPageAsync(Windows.Data.Pdf.PdfPage page)
    {
        var destinationWidth = (uint)Math.Clamp(
            Math.Round(page.Size.Width * 3d),
            1600d,
            2600d);
        return RenderPdfPageAsync(page, destinationWidth);
    }

    private static async Task<byte[]> RenderPdfPageAsync(
        Windows.Data.Pdf.PdfPage page,
        uint destinationWidth)
    {
        using var stream = new InMemoryRandomAccessStream();
        var options = new Windows.Data.Pdf.PdfPageRenderOptions
        {
            DestinationWidth = Math.Clamp(destinationWidth, 600u, 2600u)
        };

        await page.RenderToStreamAsync(stream, options);
        stream.Seek(0);

        var length = checked((uint)stream.Size);
        using var reader = new DataReader(stream.GetInputStreamAt(0));
        await reader.LoadAsync(length);
        var bytes = new byte[checked((int)length)];
        reader.ReadBytes(bytes);
        return bytes;
    }

    private static PageData CreateOcrPage(
        int pageNumber,
        TesseractPix image,
        TesseractOCR.Page page)
    {
        var words = new List<WordData>();
        using var layout = page.Layout;
        foreach (var block in layout)
        {
            foreach (var paragraph in block.Paragraphs)
            {
                foreach (var textLine in paragraph.TextLines)
                {
                    foreach (var word in textLine.Words)
                    {
                        var text = word.Text?.Trim();
                        var box = word.BoundingBox;
                        if (string.IsNullOrWhiteSpace(text) || !box.HasValue)
                            continue;

                        var rect = box.Value;
                        words.Add(new WordData(
                            text,
                            rect.X1,
                            rect.X2,
                            rect.Y1,
                            rect.Y2));
                    }
                }
            }
        }

        var ordered = words
            .OrderBy(word => word.Top)
            .ThenBy(word => word.Left)
            .ToArray();

        return new PageData(
            pageNumber,
            image.Width,
            image.Height,
            ordered,
            BuildLines(ordered));
    }

    private static int HeaderScore(PageData page)
    {
        if (TryFindHeaderLayout(page, out var layout))
            return 100 + layout.DetectedRequiredCount;

        var found = new HashSet<ColumnKind>();
        for (var lineIndex = 0; lineIndex < page.Lines.Count; lineIndex++)
        {
            var candidateWords = new List<WordData>(page.Lines[lineIndex].Words);
            if (lineIndex + 1 < page.Lines.Count)
            {
                var nextLine = page.Lines[lineIndex + 1];
                var averageHeight = candidateWords.Count == 0
                    ? 12d
                    : candidateWords.Average(word => word.Height);
                if (nextLine.Top - page.Lines[lineIndex].Top
                    < Math.Max(36d, averageHeight * 2.5d)
                    && !LooksLikeInvoiceDataLine(nextLine))
                    candidateWords.AddRange(nextLine.Words);
            }

            foreach (var pair in HeaderAliases)
            {
                if (!found.Contains(pair.Key)
                    && TryFindAliasCenter(candidateWords, pair.Value, out _))
                    found.Add(pair.Key);
            }
        }

        return found.Count;
    }

    private static int InvoiceDataScore(PageData page)
    {
        if (!TryFindHeaderLayout(page, out var header))
            return 0;

        var score = 0;
        foreach (var line in BuildInvoiceRows(page, header))
        {
            var invoiceDateText = ReadColumn(line.Words, header, ColumnKind.InvoiceDate);
            if (!TryDate(invoiceDateText, out _))
                continue;

            score += 8;
            if (!string.IsNullOrWhiteSpace(ReadColumn(line.Words, header, ColumnKind.Institution)))
                score += 1;
            if (!string.IsNullOrWhiteSpace(DigitsOnly(ReadColumn(line.Words, header, ColumnKind.ServiceNumber))))
                score += 1;
            if (!string.IsNullOrWhiteSpace(ReadInvoiceNumber(line.Words, header)))
                score += 1;

            if (header.Ranges.ContainsKey(ColumnKind.DueDate))
            {
                var dueDateText = ReadColumn(line.Words, header, ColumnKind.DueDate);
                score += TryDate(dueDateText, out _) ? 4 : -6;
            }

            foreach (var column in new[]
                     {
                         ColumnKind.Payable,
                         ColumnKind.Vat,
                         ColumnKind.StampDuty,
                         ColumnKind.Payment
                     })
            {
                if (MoneyCandidates(ReadColumn(line.Words, header, column)).Count > 0)
                    score += 2;
            }

            if (HasConsistentMoneyTuple(line, header))
                score += 20;
        }

        return score;
    }

    private static bool HasConsistentMoneyTuple(
        LineData line,
        HeaderLayout header)
    {
        var payableCandidates = MoneyCandidates(
            ReadColumn(line.Words, header, ColumnKind.Payable));
        var vatCandidates = MoneyCandidates(
            ReadColumn(line.Words, header, ColumnKind.Vat));
        var stampCandidates = MoneyCandidates(
            ReadColumn(line.Words, header, ColumnKind.StampDuty));
        var paymentCandidates = MoneyCandidates(
            ReadColumn(line.Words, header, ColumnKind.Payment));

        foreach (var payable in payableCandidates)
        foreach (var vat in vatCandidates)
        foreach (var stamp in stampCandidates)
        foreach (var payment in paymentCandidates)
        {
            if (payable <= 0m || vat < 0m || vat > payable)
                continue;

            var vatBase = payable - vat;
            var expectedStamp = decimal.Round(
                vatBase * TaxCalculationService.StampDutyRate,
                2,
                MidpointRounding.AwayFromZero);
            var expectedPayment = decimal.Round(
                payable - vatBase * TaxCalculationService.StampDutyRate,
                2,
                MidpointRounding.AwayFromZero);

            if (expectedStamp == stamp && expectedPayment == payment)
                return true;
        }

        return false;
    }

    private static int SemanticDocumentScore(PageData page)
    {
        var text = NormalizeKey(string.Join(" ", page.Lines.Select(line => line.Text)));
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        var score = 0;
        foreach (var phrase in new[]
                 {
                     "odeme emri belgesi",
                     "belge numarasi",
                     "duzenleme",
                     "fatura",
                     "elektrik",
                     "telefon",
                     "internet",
                     "gerceklestirme gorevlisi",
                     "harcama yetkilisi"
                 })
        {
            if (text.Contains(phrase, StringComparison.Ordinal))
                score++;
        }

        return score;
    }

    private static LineData[] BuildLines(IEnumerable<WordData> words)
    {
        var lines = new List<List<WordData>>();
        foreach (var word in words.OrderBy(word => word.Top).ThenBy(word => word.Left))
        {
            var tolerance = Math.Max(3d, word.Height * 0.6d);
            var line = lines.LastOrDefault(candidate =>
                Math.Abs(candidate.Average(item => item.Top) - word.Top) <= tolerance);
            if (line is null)
            {
                line = new List<WordData>();
                lines.Add(line);
            }

            line.Add(word);
        }

        return lines
            .Select(line =>
            {
                var ordered = line.OrderBy(word => word.Left).ToArray();
                return new LineData(
                    ordered.Average(word => word.Top),
                    ordered.Max(word => word.Bottom),
                    string.Join(" ", ordered.Select(word => word.Text)),
                    ordered);
            })
            .OrderBy(line => line.Top)
            .ToArray();
    }

    private static List<decimal> MoneyCandidates(string text)
    {
        var normalized = NormalizeOcrMoneyText(text);
        var chunks = Regex.Matches(normalized, @"[0-9][0-9.,]*")
            .Select(match => match.Value.Trim('.', ','))
            .Where(value => value.Length > 0)
            .ToArray();

        var candidates = new List<decimal>();

        void Add(string value)
        {
            if (NumberParser.TryNonNegativeDecimal(value, out var parsed)
                && !candidates.Contains(parsed))
                candidates.Add(parsed);
        }

        void AddWithImplicitCents(string value)
        {
            var hasExplicitDecimalSeparator = value.Contains('.', StringComparison.Ordinal)
                                              || value.Contains(',', StringComparison.Ordinal);
            var digits = string.Concat(value.Where(char.IsDigit));

            // Eski tarama/OCR çıktılarında PDF'de her zaman 2 ondalık gösterilen
            // para değeri "161730" gibi ayıraçsız gelebilir. Bu durumda kuruş
            // yorumu önce denenir; hesap motoru tutarsızsa ham tam sayı adayına
            // geri dönülür. Açık ayıraç varsa normal para biçimi önceliklidir.
            if (!hasExplicitDecimalSeparator && digits.Length >= 3)
                Add(digits[..^2] + "," + digits[^2..]);

            Add(value);
        }

        foreach (var chunk in chunks)
        {
            AddWithImplicitCents(chunk);

            var decimalSeparator = Math.Max(chunk.LastIndexOf(','), chunk.LastIndexOf('.'));
            var integerLength = decimalSeparator >= 0 ? decimalSeparator : chunk.Length;
            if (integerLength >= 2 && char.IsDigit(chunk[0]))
                AddWithImplicitCents(chunk[1..]);
        }

        if (chunks.Length >= 2 && chunks[0].Length == 1)
            AddWithImplicitCents(chunks[0] + chunks[1]);

        return candidates;
    }

    private static string NormalizeOcrMoneyText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var builder = new StringBuilder(text.Length);
        foreach (var ch in text)
        {
            builder.Append(ch switch
            {
                'O' or 'o' or 'D' or 'Q' => '0',
                'I' or 'İ' or 'ı' or 'l' or '|' or '!' => '1',
                'Z' or 'z' => '2',
                'A' or 'a' => '4',
                'S' or 's' => '5',
                'G' or 'g' => '6',
                'B' or 'b' => '8',
                _ => ch
            });
        }

        return builder.ToString();
    }

    private static bool TryFindDate(string text, out DateTime date) =>
        TryDate(text, out date);

    private static IEnumerable<DateTime> ExtractDates(string text)
    {
        var seen = new HashSet<DateTime>();
        var normalized = (text ?? string.Empty)
            .Replace('O', '0')
            .Replace('o', '0');

        foreach (Match match in Regex.Matches(
                     normalized,
                     @"\b[0-3]?[0-9][.\-/][01]?[0-9][.\-/](?:19|20)[0-9]{2}\b",
                     RegexOptions.CultureInvariant))
        {
            if (TryParseDateExact(match.Value, out var date) && seen.Add(date.Date))
                yield return date.Date;
        }

        foreach (Match match in Regex.Matches(
                     normalized,
                     @"\b[0-3][0-9][01][0-9](?:19|20)[0-9]{2}\b",
                     RegexOptions.CultureInvariant))
        {
            if (DateTime.TryParseExact(
                    match.Value,
                    "ddMMyyyy",
                    Tr,
                    DateTimeStyles.None,
                    out var date)
                && seen.Add(date.Date))
                yield return date.Date;
        }
    }

    private static bool TryDate(string text, out DateTime date)
    {
        var normalized = NormalizeOcrDateText(text);

        if (TryParseDateExact(normalized, out date))
            return true;

        foreach (var candidate in ExtractDates(normalized))
        {
            date = candidate;
            return true;
        }

        date = default;
        return false;
    }

    private static string NormalizeOcrDateText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var builder = new StringBuilder(text.Length);
        foreach (var ch in text.Trim())
        {
            builder.Append(ch switch
            {
                'O' or 'o' or 'Q' or 'D' => '0',
                'I' or 'İ' or 'ı' or 'l' or '|' or '!' => '1',
                'Z' or 'z' => '2',
                'S' or 's' => '5',
                'B' or 'b' => '8',
                _ => ch
            });
        }

        return builder.ToString();
    }

    private static bool TryParseDateExact(string text, out DateTime date) =>
        DateTime.TryParseExact(
            text.Trim(),
            DateFormats,
            Tr,
            DateTimeStyles.AllowWhiteSpaces,
            out date)
        || DateTime.TryParseExact(
            DigitsOnly(text),
            "ddMMyyyy",
            Tr,
            DateTimeStyles.None,
            out date);

    private static string DigitsOnly(string text) =>
        string.Concat((text ?? string.Empty).Where(char.IsDigit));

    private static string InferInstitutionType(string institutionName)
    {
        var normalized = NormalizeKey(institutionName);
        if (normalized.Contains("ortaokulu", StringComparison.Ordinal))
            return "Ortaokul";
        if (normalized.Contains("ilkokulu", StringComparison.Ordinal))
            return "İlkokul";
        if (normalized.Contains("anaokulu", StringComparison.Ordinal)
            || normalized.Contains("anasinifi", StringComparison.Ordinal))
            return "Anasınıfı/Anaokulu";
        return "Kurum";
    }

    private static string[] Tokenize(string text) =>
        NormalizeKey(text)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string NormalizeToken(string text) =>
        NormalizeKey(text).Replace(" ", string.Empty, StringComparison.Ordinal);

    private static string NormalizeKey(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var source = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(source.Length);
        foreach (var ch in source)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                continue;

            var mapped = ch switch
            {
                'ı' or 'İ' or 'I' => 'i',
                'ş' or 'Ş' => 's',
                'ğ' or 'Ğ' => 'g',
                'ü' or 'Ü' => 'u',
                'ö' or 'Ö' => 'o',
                'ç' or 'Ç' => 'c',
                _ => char.ToLowerInvariant(ch)
            };

            builder.Append(char.IsLetterOrDigit(mapped) ? mapped : ' ');
        }

        return Regex.Replace(builder.ToString(), @"\s+", " ").Trim();
    }

    private static HistoricalPdfImportPreview Invalid(
        string sourcePath,
        string fileName,
        string message,
        string hysNumber = "—",
        string invoiceType = "—") => new(
        sourcePath,
        fileName,
        false,
        message,
        hysNumber,
        invoiceType,
        0,
        0m,
        null);

    private enum ColumnKind
    {
        Sequence,
        District,
        Institution,
        ServiceNumber,
        InvoiceNumber,
        InvoiceDate,
        DueDate,
        Payable,
        Vat,
        StampDuty,
        Payment
    }

    private sealed record WordData(
        string Text,
        double Left,
        double Right,
        double Top,
        double Bottom)
    {
        public double CenterX => (Left + Right) / 2d;
        public double CenterY => (Top + Bottom) / 2d;
        public double Height => Math.Max(1d, Bottom - Top);
    }

    private sealed record LineData(
        double Top,
        double Bottom,
        string Text,
        IReadOnlyList<WordData> Words);

    private sealed record PageData(
        int PageNumber,
        double Width,
        double Height,
        IReadOnlyList<WordData> Words,
        IReadOnlyList<LineData> Lines);

    private sealed record ColumnRange(double Left, double Right);

    private sealed record HeaderLayout(
        IReadOnlyDictionary<ColumnKind, ColumnRange> Ranges,
        double Bottom,
        int DetectedRequiredCount);

    private sealed record RowMoneyWord(
        List<decimal> Candidates);

    private sealed record DocumentTotals(
        decimal GrossAmount,
        decimal StampDutyAmount,
        decimal PaymentAmount);

    private sealed record OrientationProbeResult(
        OcrOrientation Orientation,
        PageData? BestPage);

    private sealed record OcrPageResult(
        IReadOnlyList<PageData> Pages,
        string ErrorMessage,
        bool OcrRan);
}
