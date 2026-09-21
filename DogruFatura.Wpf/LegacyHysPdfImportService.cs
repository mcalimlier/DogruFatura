using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage;
using Windows.Storage.Streams;

namespace DogruFatura.Wpf;

/// <summary>
/// Eski HYS / Ödeme Emri PDF'lerindeki fatura eklerini semantik sütun başlıklarıyla okur.
/// Sabit koordinat veya tek bir başlık yazımına bağımlı değildir. PDF metin katmanı bozuksa
/// Windows OCR son çare olarak kullanılır.
/// </summary>
internal sealed class LegacyHysPdfImportService
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly string[] DateFormats =
    {
        "dd.MM.yyyy", "d.M.yyyy",
        "dd-MM-yyyy", "d-M-yyyy",
        "dd/MM/yyyy", "d/M/yyyy"
    };

    private static readonly IReadOnlyDictionary<ColumnKind, string[]> HeaderAliases =
        new Dictionary<ColumnKind, string[]>
        {
            [ColumnKind.Sequence] = new[] { "no", "sira no", "sıra no" },
            [ColumnKind.Institution] = new[]
            {
                "okul adi", "okul adı", "kurum adi", "kurum adı",
                "okul kurum adi", "okul / kurum adi", "okul/kurum adi"
            },
            [ColumnKind.ServiceNumber] = new[]
            {
                "hizmet numarasi", "hizmet numarası", "hizmet no",
                "abone numarasi", "abone numarası", "abone no",
                "tesisat numarasi", "tesisat numarası", "tesisat no"
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
                "brut tutar", "brüt tutar"
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
                "odeme tutari", "ödeme tutarı", "net odeme", "net ödeme", "net tutar"
            }
        };

    private static readonly ColumnKind[] RequiredColumns =
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

    private readonly TaxCalculationService _taxCalculationService;

    public LegacyHysPdfImportService(TaxCalculationService taxCalculationService)
    {
        _taxCalculationService = taxCalculationService;
    }

    public HistoricalPdfImportPreview Read(
        string sourcePath,
        IReadOnlyList<SchoolRecord> schools,
        AuthorityProfile fallbackAuthorityProfile,
        CancellationToken cancellationToken)
    {
        var fileName = Path.GetFileName(sourcePath);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var extractedPages = ExtractPdfTextPages(sourcePath, cancellationToken);
            if (TryParsePages(
                    sourcePath,
                    fileName,
                    extractedPages,
                    schools,
                    fallbackAuthorityProfile,
                    usedOcr: false,
                    out var extractedPreview))
                return extractedPreview;

            cancellationToken.ThrowIfCancellationRequested();
            var ocrResult = TryExtractOcrPages(sourcePath, cancellationToken);
            if (ocrResult.Pages.Count > 0
                && TryParsePages(
                    sourcePath,
                    fileName,
                    ocrResult.Pages,
                    schools,
                    fallbackAuthorityProfile,
                    usedOcr: true,
                    out var ocrPreview))
                return ocrPreview;

            var reason = string.IsNullOrWhiteSpace(ocrResult.ErrorMessage)
                ? "Belgede tanınan fatura tablosu bulunamadı."
                : "Belgede tanınan fatura tablosu bulunamadı. OCR: " + ocrResult.ErrorMessage;
            return Invalid(
                sourcePath,
                fileName,
                reason
                + " Beklenen temel alanlar: Okul/Kurum Adı, Hizmet/Abone No, Fatura No, "
                + "Fatura Tarihi, Son Ödeme/Vade Tarihi, Tutar, KDV, Damga Vergisi ve Ödeme/Net Tutar.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Invalid(sourcePath, fileName, "Eski HYS PDF'si okunamadı: " + ex.Message);
        }
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

        var invoices = new List<InvoiceRecord>();
        HeaderLayout? usedHeader = null;
        foreach (var page in pages)
        {
            if (!TryFindHeaderLayout(page, out var header))
                continue;

            usedHeader ??= header;
            foreach (var line in page.Lines.Where(line => line.Top > header.Bottom + 1d))
            {
                if (!TryParseInvoiceLine(
                        line,
                        header,
                        invoiceType,
                        schools,
                        reportDate.Value,
                        out var invoice,
                        out var lineLooksLikeInvoice,
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

        preview = new HistoricalPdfImportPreview(
            sourcePath,
            fileName,
            true,
            $"{pages.Count} sayfa ve {invoices.Count} fatura semantik sütun başlıklarıyla doğrulandı"
            + (usedOcr ? " • bozuk/eski PDF metin katmanı için OCR kullanıldı." : "."),
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
        out InvoiceRecord? invoice,
        out bool lineLooksLikeInvoice,
        out string errorMessage)
    {
        invoice = null;
        errorMessage = string.Empty;

        var invoiceDateText = ReadColumn(line.Words, header, ColumnKind.InvoiceDate);
        var dueDateText = ReadColumn(line.Words, header, ColumnKind.DueDate);
        var hasInvoiceDate = TryDate(invoiceDateText, out var invoiceDate);
        var hasDueDate = TryDate(dueDateText, out var dueDate);

        lineLooksLikeInvoice = hasInvoiceDate || hasDueDate;
        if (!hasInvoiceDate || !hasDueDate)
            return false;

        var institutionText = ReadColumn(line.Words, header, ColumnKind.Institution).Trim();
        var serviceNumber = DigitsOnly(ReadColumn(line.Words, header, ColumnKind.ServiceNumber));
        var invoiceNumber = ReadColumn(line.Words, header, ColumnKind.InvoiceNumber)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Trim();

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

        if (!TryMoney(ReadColumn(line.Words, header, ColumnKind.Payable), out var payableAmount)
            || !TryMoney(ReadColumn(line.Words, header, ColumnKind.Vat), out var vatAmount)
            || !TryMoney(ReadColumn(line.Words, header, ColumnKind.StampDuty), out var printedStampDuty)
            || !TryMoney(ReadColumn(line.Words, header, ColumnKind.Payment), out var printedPaymentAmount))
        {
            errorMessage = "Tutar/KDV/Damga Vergisi/Ödeme sütunlarından biri okunamadı.";
            return false;
        }

        if (!TryResolveCalculation(
                invoiceType,
                payableAmount,
                vatAmount,
                printedStampDuty,
                printedPaymentAmount,
                out var calculation,
                out var compensationAmount))
        {
            errorMessage = "PDF tutarları vergi hesabıyla doğrulanamadı.";
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

    private static SchoolRecord? ResolveSchool(
        string institutionText,
        IReadOnlyList<SchoolRecord> schools)
    {
        var key = NormalizeKey(institutionText);
        return schools.FirstOrDefault(school =>
            string.Equals(NormalizeKey(school.Name), key, StringComparison.Ordinal));
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
            if (lineIndex + 1 < page.Lines.Count
                && page.Lines[lineIndex + 1].Top - page.Lines[lineIndex].Top < 28d)
            {
                candidateWords.AddRange(page.Lines[lineIndex + 1].Words);
                bottom = Math.Max(bottom, page.Lines[lineIndex + 1].Bottom);
            }

            var centers = new Dictionary<ColumnKind, double>();
            foreach (var pair in HeaderAliases)
            {
                if (TryFindAliasCenter(candidateWords, pair.Value, out var center))
                    centers[pair.Key] = center;
            }

            var requiredCount = RequiredColumns.Count(centers.ContainsKey);
            if (requiredCount < 7)
                continue;

            var candidate = BuildHeaderLayout(page.Width, centers, bottom);
            if (candidate is null)
                continue;

            if (best is null || requiredCount > best.DetectedRequiredCount)
                best = candidate with { DetectedRequiredCount = requiredCount };
        }

        if (best is null || RequiredColumns.Any(column => !best.Ranges.ContainsKey(column)))
            return false;

        layout = best;
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

    private static bool TryFindAliasCenter(
        IReadOnlyList<WordData> words,
        IEnumerable<string> aliases,
        out double center)
    {
        center = 0d;
        var ordered = words.OrderBy(word => word.Left).ToArray();

        foreach (var alias in aliases.OrderByDescending(alias => NormalizeKey(alias).Length))
        {
            var aliasTokens = Tokenize(alias);
            if (aliasTokens.Length == 0)
                continue;

            for (var start = 0; start < ordered.Length; start++)
            {
                var matched = new List<WordData>();
                var aliasIndex = 0;
                for (var wordIndex = start; wordIndex < ordered.Length && aliasIndex < aliasTokens.Length; wordIndex++)
                {
                    var token = NormalizeToken(ordered[wordIndex].Text);
                    if (string.IsNullOrEmpty(token))
                        continue;

                    if (!string.Equals(token, aliasTokens[aliasIndex], StringComparison.Ordinal))
                        break;

                    matched.Add(ordered[wordIndex]);
                    aliasIndex++;
                }

                if (aliasIndex != aliasTokens.Length || matched.Count == 0)
                    continue;

                center = (matched.Min(word => word.Left) + matched.Max(word => word.Right)) / 2d;
                return true;
            }
        }

        return false;
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
            foreach (var line in page.Lines)
            {
                var normalized = NormalizeKey(line.Text);
                if (!normalized.Contains("duzenleme tarihi", StringComparison.Ordinal)
                    && !normalized.Contains("belge tarihi", StringComparison.Ordinal))
                    continue;

                if (TryFindDate(line.Text, out var date))
                    return date;
            }
        }

        // Etiket ile değer ayrı OCR satırlarına düştüyse ilk sayfanın üst bölümündeki tarihi kullan.
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
        var hasFileHys = HistoricalPdfImportService.TryExtractHysNumberFromFileName(
            Path.GetFileNameWithoutExtension(sourcePath),
            out var fileHys,
            out var fileError);
        var hasDocumentHys = TryExtractHysNumberFromText(documentText, out var documentHys);

        if (hasFileHys && hasDocumentHys
            && !string.Equals(fileHys, documentHys, StringComparison.Ordinal))
        {
            hysNumber = string.Empty;
            errorMessage = $"Dosya adındaki HYS ({fileHys}) ile PDF içindeki HYS ({documentHys}) uyuşmuyor.";
            return false;
        }

        if (hasDocumentHys)
        {
            hysNumber = documentHys;
            errorMessage = string.Empty;
            return true;
        }

        if (hasFileHys)
        {
            hysNumber = fileHys;
            errorMessage = string.Empty;
            return true;
        }

        hysNumber = string.Empty;
        errorMessage = "HYS Ödeme Emri Belge No ne dosya adından ne de PDF içeriğinden okunabildi. " + fileError;
        return false;
    }

    private static bool TryExtractHysNumberFromText(string text, out string hysNumber)
    {
        hysNumber = string.Empty;
        var normalized = NormalizeKey(text);
        var match = Regex.Match(
            normalized,
            @"\bhys\s*[-:]?\s*(?<year>20[0-9]{2})\s*[- ]?\s*(?<number>[0-9]{4,24})\b",
            RegexOptions.CultureInvariant);
        if (!match.Success)
            return false;

        return HysPaymentOrderService.TryNormalize(
            match.Groups["year"].Value + match.Groups["number"].Value,
            out hysNumber,
            out _);
    }

    private static IReadOnlyList<PageData> ExtractPdfTextPages(
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
        CancellationToken cancellationToken)
    {
        try
        {
            return ExtractOcrPagesAsync(sourcePath, cancellationToken)
                .GetAwaiter()
                .GetResult();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new OcrPageResult(Array.Empty<PageData>(), ex.Message);
        }
    }

    private static async Task<OcrPageResult> ExtractOcrPagesAsync(
        string sourcePath,
        CancellationToken cancellationToken)
    {
        var engine = OcrEngine.TryCreateFromUserProfileLanguages();
        if (engine is null)
        {
            var fallbackLanguage = OcrEngine.AvailableRecognizerLanguages.FirstOrDefault();
            if (fallbackLanguage is not null)
                engine = OcrEngine.TryCreateFromLanguage(fallbackLanguage);
        }

        if (engine is null)
            return new OcrPageResult(
                Array.Empty<PageData>(),
                "Windows OCR dili kullanılamıyor. Windows Dil ayarlarında OCR desteği olan bir dil kurulmalıdır.");

        var file = await StorageFile.GetFileFromPathAsync(sourcePath);
        var document = await Windows.Data.Pdf.PdfDocument.LoadFromFileAsync(file);
        var pages = new List<PageData>((int)document.PageCount);

        for (uint pageIndex = 0; pageIndex < document.PageCount; pageIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var pdfPage = document.GetPage(pageIndex);
            using var stream = new InMemoryRandomAccessStream();

            var destinationWidth = (uint)Math.Clamp(
                Math.Round(pdfPage.Size.Width * 2.5d),
                1200d,
                Math.Min(2200d, OcrEngine.MaxImageDimension));
            var renderOptions = new Windows.Data.Pdf.PdfPageRenderOptions
            {
                DestinationWidth = destinationWidth
            };

            await pdfPage.RenderToStreamAsync(stream, renderOptions);
            stream.Seek(0);
            var decoder = await BitmapDecoder.CreateAsync(stream);

            PageData? bestPage = null;
            var bestScore = int.MinValue;
            foreach (var rotation in new[]
                     {
                         BitmapRotation.None,
                         BitmapRotation.Clockwise90Degrees,
                         BitmapRotation.Clockwise270Degrees
                     })
            {
                cancellationToken.ThrowIfCancellationRequested();
                var transform = new BitmapTransform { Rotation = rotation };
                using var bitmap = await decoder.GetSoftwareBitmapAsync(
                    BitmapPixelFormat.Bgra8,
                    BitmapAlphaMode.Premultiplied,
                    transform,
                    ExifOrientationMode.IgnoreExifOrientation,
                    ColorManagementMode.DoNotColorManage);

                var result = await engine.RecognizeAsync(bitmap);
                var pageData = CreateOcrPage((int)pageIndex + 1, bitmap, result);
                var score = HeaderScore(pageData) * 1000 + pageData.Words.Count;
                if (score <= bestScore)
                    continue;

                bestScore = score;
                bestPage = pageData;
            }

            if (bestPage is not null)
                pages.Add(bestPage);
        }

        return new OcrPageResult(pages, string.Empty);
    }

    private static PageData CreateOcrPage(
        int pageNumber,
        SoftwareBitmap bitmap,
        OcrResult result)
    {
        var words = result.Lines
            .SelectMany(line => line.Words)
            .Where(word => !string.IsNullOrWhiteSpace(word.Text))
            .Select(word => new WordData(
                word.Text.Trim(),
                word.BoundingRect.X,
                word.BoundingRect.X + word.BoundingRect.Width,
                word.BoundingRect.Y,
                word.BoundingRect.Y + word.BoundingRect.Height))
            .OrderBy(word => word.Top)
            .ThenBy(word => word.Left)
            .ToArray();

        return new PageData(
            pageNumber,
            bitmap.PixelWidth,
            bitmap.PixelHeight,
            words,
            BuildLines(words));
    }

    private static int HeaderScore(PageData page)
    {
        var found = new HashSet<ColumnKind>();
        foreach (var line in page.Lines)
        {
            foreach (var pair in HeaderAliases)
            {
                if (!found.Contains(pair.Key)
                    && TryFindAliasCenter(line.Words, pair.Value, out _))
                    found.Add(pair.Key);
            }
        }

        return found.Count;
    }

    private static IReadOnlyList<LineData> BuildLines(IEnumerable<WordData> words)
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

    private static bool TryMoney(string text, out decimal value)
    {
        var cleaned = (text ?? string.Empty)
            .Replace("₺", string.Empty, StringComparison.Ordinal)
            .Replace("TL", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("TRY", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        return NumberParser.TryNonNegativeDecimal(cleaned, out value);
    }

    private static bool TryFindDate(string text, out DateTime date)
    {
        foreach (Match match in Regex.Matches(
                     text ?? string.Empty,
                     @"\b[0-3]?[0-9][.\-/][01]?[0-9][.\-/](?:19|20)[0-9]{2}\b",
                     RegexOptions.CultureInvariant))
        {
            if (TryDate(match.Value, out date))
                return true;
        }

        date = default;
        return false;
    }

    private static bool TryDate(string text, out DateTime date) =>
        DateTime.TryParseExact(
            (text ?? string.Empty).Trim(),
            DateFormats,
            Tr,
            DateTimeStyles.AllowWhiteSpaces,
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

    private sealed record OcrPageResult(
        IReadOnlyList<PageData> Pages,
        string ErrorMessage);
}
