using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
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
                "ele gecen miktar", "ele geçen miktar"
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
        IProgress<string>? progress = null)
    {
        var fileName = Path.GetFileName(sourcePath);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report("PDF metin katmanı okunuyor");

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
            progress?.Report("Eski/bozuk PDF için OCR hazırlanıyor");
            var ocrResult = TryExtractOcrPages(
                sourcePath,
                extractedPages,
                cancellationToken,
                progress);
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
            return Invalid(sourcePath, fileName, "Eski ödeme emri PDF'si okunamadı: " + ex.Message);
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

        var invoices = new List<InvoiceRecord>();
        var missingDueDateCount = 0;
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
                        out var dueDateWasMissing,
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

        var dueDateNote = missingDueDateCount > 0
            ? $" • {missingDueDateCount} kayıtta son ödeme tarihi belgede bulunmadığı için fatura tarihi kullanıldı."
            : string.Empty;

        preview = new HistoricalPdfImportPreview(
            sourcePath,
            fileName,
            true,
            $"{pages.Count} sayfa ve {invoices.Count} fatura semantik sütun başlıklarıyla doğrulandı"
            + (usedOcr ? " • bozuk/eski PDF metin katmanı için OCR kullanıldı." : ".")
            + dueDateNote,
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
        out bool dueDateWasMissing,
        out string errorMessage)
    {
        invoice = null;
        dueDateWasMissing = false;
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
                out var payableAmount,
                out var vatAmount,
                out var printedStampDuty,
                out var printedPaymentAmount,
                out var calculation,
                out var compensationAmount))
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
        out decimal payableAmount,
        out decimal vatAmount,
        out decimal printedStampDuty,
        out decimal printedPaymentAmount,
        out TaxCalculationResult calculation,
        out decimal compensationAmount)
    {
        payableAmount = 0m;
        vatAmount = 0m;
        printedStampDuty = 0m;
        printedPaymentAmount = 0m;
        calculation = default!;
        compensationAmount = 0m;

        var payableCandidates = MoneyCandidates(payableText);
        var vatCandidates = MoneyCandidates(vatText);
        var stampCandidates = MoneyCandidates(stampDutyText);
        var paymentCandidates = MoneyCandidates(paymentText);
        if (payableCandidates.Count == 0
            || vatCandidates.Count == 0
            || stampCandidates.Count == 0
            || paymentCandidates.Count == 0)
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

    private static bool LooksLikeInvoiceDataLine(LineData line)
    {
        if (line.Words.Count == 0)
            return false;

        var dateCount = ExtractDates(line.Text).Take(2).Count();
        if (dateCount >= 1)
            return true;

        var numericTokenCount = line.Words.Count(word =>
        {
            var token = word.Text
                .Replace(".", string.Empty, StringComparison.Ordinal)
                .Replace(",", string.Empty, StringComparison.Ordinal)
                .Replace("-", string.Empty, StringComparison.Ordinal)
                .Replace("/", string.Empty, StringComparison.Ordinal)
                .Trim();
            return token.Length >= 3 && token.All(char.IsDigit);
        });

        return numericTokenCount >= 4;
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
        CancellationToken cancellationToken,
        IProgress<string>? progress)
    {
        try
        {
            return ExtractOcrPagesAsync(
                    sourcePath,
                    extractedPages,
                    cancellationToken,
                    progress)
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
                "OCR motorunun yerel bileşenleri yüklenemedi: " + ex.Message);
        }
        catch (Exception ex)
        {
            return new OcrPageResult(Array.Empty<PageData>(), ex.Message);
        }
    }

    private static async Task<OcrPageResult> ExtractOcrPagesAsync(
        string sourcePath,
        IReadOnlyList<PageData> extractedPages,
        CancellationToken cancellationToken,
        IProgress<string>? progress)
    {
        var tessdataDirectory = Path.Combine(AppContext.BaseDirectory, "tessdata");
        var tessdataFile = Path.Combine(tessdataDirectory, "tur.traineddata");
        if (!File.Exists(tessdataFile))
        {
            return new OcrPageResult(
                Array.Empty<PageData>(),
                "Türkçe OCR modeli kurulumda bulunamadı: " + tessdataFile);
        }

        using var engine = new TesseractEngine(
            tessdataDirectory,
            Language.Turkish,
            EngineMode.LstmOnly);

        var file = await StorageFile.GetFileFromPathAsync(sourcePath);
        var document = await Windows.Data.Pdf.PdfDocument.LoadFromFileAsync(file);
        var pages = new List<PageData>((int)document.PageCount);

        for (uint pageIndex = 0; pageIndex < document.PageCount; pageIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var pageNumber = checked((int)pageIndex + 1);
            var extractedPage = extractedPages.FirstOrDefault(page => page.PageNumber == pageNumber);
            if (extractedPage is not null && SemanticDocumentScore(extractedPage) >= 2)
            {
                progress?.Report($"OCR {pageNumber}/{document.PageCount}: okunabilir belge sayfası atlandı");
                continue;
            }

            progress?.Report($"OCR {pageNumber}/{document.PageCount}: sayfa hazırlanıyor");
            using var pdfPage = document.GetPage(pageIndex);
            var pngBytes = await RenderPdfPageAsync(pdfPage);
            using var sourceImage = TesseractPix.LoadFromMemory(pngBytes);

            PageData? bestPage = null;
            var bestScore = int.MinValue;

            progress?.Report($"OCR {pageNumber}/{document.PageCount}: normal yön taranıyor");
            bestPage = SelectBetterOcrPage(
                engine,
                sourceImage,
                PageSegMode.Auto,
                (int)pageIndex + 1,
                bestPage,
                ref bestScore,
                cancellationToken);

            using var gray = sourceImage.ConvertRGBToGray();
            using var cleaned = gray.RemoveLines(150, 150);
            progress?.Report($"OCR {pageNumber}/{document.PageCount}: tablo temizleniyor");
            bestPage = SelectBetterOcrPage(
                engine,
                cleaned,
                PageSegMode.SingleBlock,
                (int)pageIndex + 1,
                bestPage,
                ref bestScore,
                cancellationToken);

            if (bestScore < 100_000_000)
            {
                progress?.Report($"OCR {pageNumber}/{document.PageCount}: 90° yön deneniyor");
                using var clockwise = cleaned.Rotate90(RotationDirection.Clockwise);
                bestPage = SelectBetterOcrPage(
                    engine,
                    clockwise,
                    PageSegMode.SingleBlock,
                    (int)pageIndex + 1,
                    bestPage,
                    ref bestScore,
                    cancellationToken);
            }

            if (bestScore < 100_000_000)
            {
                progress?.Report($"OCR {pageNumber}/{document.PageCount}: ters 90° yön deneniyor");
                using var counterClockwise = cleaned.Rotate90(RotationDirection.CounterClockwise);
                bestPage = SelectBetterOcrPage(
                    engine,
                    counterClockwise,
                    PageSegMode.SingleBlock,
                    (int)pageIndex + 1,
                    bestPage,
                    ref bestScore,
                    cancellationToken);
            }

            if (bestPage is not null)
            {
                pages.Add(bestPage);
                progress?.Report($"OCR {pageNumber}/{document.PageCount}: sayfa tamamlandı");
            }
        }

        return new OcrPageResult(pages, string.Empty);
    }

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
        var score = HeaderScore(candidate) * 1_000_000
                    + SemanticDocumentScore(candidate) * 10_000
                    + (int)Math.Round(page.MeanConfidence * 100d)
                    + Math.Min(candidate.Words.Count, 5000);

        if (score <= bestScore)
            return currentBest;

        bestScore = score;
        return candidate;
    }

    private static async Task<byte[]> RenderPdfPageAsync(Windows.Data.Pdf.PdfPage page)
    {
        using var stream = new InMemoryRandomAccessStream();
        var destinationWidth = (uint)Math.Clamp(
            Math.Round(page.Size.Width * 3d),
            1600d,
            2600d);
        var options = new Windows.Data.Pdf.PdfPageRenderOptions
        {
            DestinationWidth = destinationWidth
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
        var normalized = (text ?? string.Empty)
            .Replace('O', '0')
            .Replace('o', '0');
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

        foreach (var chunk in chunks)
        {
            Add(chunk);

            var decimalSeparator = Math.Max(chunk.LastIndexOf(','), chunk.LastIndexOf('.'));
            var integerLength = decimalSeparator >= 0 ? decimalSeparator : chunk.Length;
            if (integerLength >= 2 && char.IsDigit(chunk[0]))
                Add(chunk[1..]);
        }

        if (chunks.Length >= 2 && chunks[0].Length == 1)
            Add(chunks[0] + chunks[1]);

        return candidates;
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
        var normalized = (text ?? string.Empty)
            .Replace('O', '0')
            .Replace('o', '0')
            .Trim();

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
