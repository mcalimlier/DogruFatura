using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace DogruFatura.Wpf;

public sealed record HistoricalPdfImportPreview(
    string SourcePath,
    string FileName,
    bool IsValid,
    string Status,
    string HysNumber,
    string InvoiceType,
    int InvoiceCount,
    decimal PaymentTotal,
    ArchivedInvoiceList? Archive)
{
    public string InvoiceCountText => IsValid ? $"{InvoiceCount} fatura" : "—";
    public string PaymentTotalText => IsValid ? NumberParser.Money(PaymentTotal) : "—";
    public string ResultText => IsValid ? "EKLEMEYE HAZIR" : "HATALI";

    public HistoricalPdfImportPreview WithError(string message) => this with
    {
        IsValid = false,
        Status = message,
        Archive = null
    };
}

public sealed class HistoricalPdfImportService
{
    private const double RowLineTolerance = 2.6;
    private static readonly double[] ColumnWeights =
        { 4.43, 24.86, 22.57, 17.86, 13.29, 18.29, 16.57, 22.14, 14.86, 14.29 };
    private static readonly string[] InvoiceTypes = { "Elektrik", "Telefon", "İnternet", "Su" };
    private static readonly string[] DateFormats = { "dd.MM.yyyy", "d.M.yyyy" };
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    private readonly TaxCalculationService _taxCalculationService;
    private readonly LegacyHysPdfImportService _legacyHysPdfImportService;

    public HistoricalPdfImportService(TaxCalculationService? taxCalculationService = null)
    {
        _taxCalculationService = taxCalculationService ?? new TaxCalculationService();
        _legacyHysPdfImportService = new LegacyHysPdfImportService(_taxCalculationService);
    }

    public HistoricalPdfImportPreview Read(
        string sourcePath,
        IReadOnlyList<SchoolRecord> schools,
        AuthorityProfile fallbackAuthorityProfile,
        CancellationToken cancellationToken = default,
        IProgress<string>? progress = null)
    {
        var fileName = Path.GetFileName(sourcePath);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.Equals(Path.GetExtension(sourcePath), ".pdf", StringComparison.OrdinalIgnoreCase))
                return Invalid(sourcePath, fileName, "Yalnız PDF dosyaları arşive eklenebilir.");
            var hasFileHys = TryExtractHysNumberFromFileName(
                Path.GetFileNameWithoutExtension(sourcePath),
                out var hysNumber,
                out var hysError);

            using var document = PdfDocument.Open(sourcePath);
            cancellationToken.ThrowIfCancellationRequested();
            if (document.NumberOfPages == 0)
                return Invalid(sourcePath, fileName, "PDF sayfa içermiyor.");

            var pageBuffer = new List<PdfPageData>(document.NumberOfPages);
            foreach (var page in document.GetPages())
            {
                cancellationToken.ThrowIfCancellationRequested();
                pageBuffer.Add(ExtractPage(page));
            }
            var pages = pageBuffer.ToArray();
            var allText = string.Join(" ", pages.SelectMany(page => page.Lines).Select(line => line.Text));
            if (!Contains(allText, "FATURA ÖDEME LİSTESİ"))
            {
                return _legacyHysPdfImportService.Read(
                    sourcePath,
                    schools,
                    fallbackAuthorityProfile,
                    cancellationToken,
                    progress);
            }

            if (!hasFileHys)
                return Invalid(
                    sourcePath,
                    fileName,
                    "Dosya adında geçerli bir HYS kodu bulunamadı. " + hysError);

            var invoiceType = InvoiceTypes.FirstOrDefault(type => Contains(allText, type));
            if (invoiceType is null)
                return Invalid(sourcePath, fileName, "PDF içinde fatura türü bulunamadı.", hysNumber);

            var reportDate = FindReportDate(pages);
            if (!reportDate.HasValue)
                return Invalid(sourcePath, fileName, "PDF üst bilgisindeki belge tarihi okunamadı.", hysNumber, invoiceType);

            var invoices = new List<InvoiceRecord>();
            var inferredCompensationCount = 0;
            foreach (var page in pages)
            {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var row in ExtractRows(page, invoiceType, schools, reportDate.Value))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!row.IsValid || row.Invoice is null)
                        return Invalid(
                            sourcePath,
                            fileName,
                            $"{page.PageNumber}. sayfa, {row.SequenceNumber}. satır: {row.ErrorMessage}",
                            hysNumber,
                            invoiceType);
                    invoices.Add(row.Invoice);
                    if (row.Invoice.InterruptionCompensationAmount > 0m)
                        inferredCompensationCount++;
                }
            }

            if (invoices.Count == 0)
                return Invalid(sourcePath, fileName, "PDF tablosunda okunabilir fatura satırı bulunamadı.", hysNumber, invoiceType);

            cancellationToken.ThrowIfCancellationRequested();
            var sequenceNumbers = pages
                .SelectMany(page => ExtractSequenceNumbers(page))
                .ToArray();
            var expectedSequence = Enumerable.Range(1, invoices.Count).ToArray();
            if (!sequenceNumbers.SequenceEqual(expectedSequence))
                return Invalid(sourcePath, fileName, "PDF fatura sıra numaraları kesintisiz okunamadı.", hysNumber, invoiceType);

            var authorityProfile = ExtractAuthorityProfile(pages[^1], fallbackAuthorityProfile);
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
                return Invalid(sourcePath, fileName, validation.Message, hysNumber, invoiceType);

            var compensationNote = inferredCompensationCount > 0
                ? $" • {inferredCompensationCount} elektrik kaydında kesinti tazminatı hesaplamadan çıkarıldı"
                : string.Empty;
            return new HistoricalPdfImportPreview(
                sourcePath,
                fileName,
                true,
                $"{document.NumberOfPages} sayfa ve {invoices.Count} fatura doğrulandı{compensationNote}.",
                hysNumber,
                invoiceType,
                invoices.Count,
                invoices.Sum(invoice => invoice.PaymentAmount),
                archive);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return Invalid(sourcePath, fileName, "PDF okunamadı: " + ex.Message);
        }
    }

    internal static bool TryExtractHysNumberFromFileName(
        string? fileNameWithoutExtension,
        out string hysNumber,
        out string errorMessage)
    {
        hysNumber = string.Empty;
        errorMessage = string.Empty;
        var value = (fileNameWithoutExtension ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            errorMessage = "Dosya adı boş.";
            return false;
        }

        // Eski kullanıcı arşivlerinde HYS öncesi MYS numaraları da bulunur.
        // "48) HYS-2026-34546180" / "1) MYS-2023-4010364" gibi sıra numarası
        // ve belge sayısının içinde ayırıcı tire bulunabilir.
        var match = Regex.Match(
            value,
            @"(?:^|[^A-Z0-9])(?<prefix>HYS|MYS)\s*-?\s*(?<number>[0-9][0-9\s-]*)$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!match.Success)
        {
            errorMessage = "Örnek geçerli adlar: HYS-2026146253.pdf, 48) HYS-2026-34546180.pdf veya 1) MYS-2023-4010364.pdf.";
            return false;
        }

        var numericPart = Regex.Replace(match.Groups["number"].Value, @"[\s-]", string.Empty);
        var prefix = match.Groups["prefix"].Value.ToUpperInvariant();
        if (!HysPaymentOrderService.TryNormalize(
                prefix + "-" + numericPart,
                out hysNumber,
                out var normalizeError))
        {
            errorMessage = normalizeError;
            return false;
        }

        return true;
    }

    private static PdfPageData ExtractPage(Page page)
    {
        var pageHeight = Convert.ToDouble(page.Height, CultureInfo.InvariantCulture);
        var words = page.GetWords()
            .Where(word => !string.IsNullOrWhiteSpace(word.Text))
            .Select(word => new PdfWordData(
                word.Text.Trim(),
                Convert.ToDouble(word.BoundingBox.Left, CultureInfo.InvariantCulture),
                Convert.ToDouble(word.BoundingBox.Right, CultureInfo.InvariantCulture),
                pageHeight - Convert.ToDouble(word.BoundingBox.Top, CultureInfo.InvariantCulture)))
            .OrderBy(word => word.Top)
            .ThenBy(word => word.Left)
            .ToArray();
        return new PdfPageData(
            page.Number,
            Convert.ToDouble(page.Width, CultureInfo.InvariantCulture),
            pageHeight,
            words,
            BuildLines(words));
    }

    private static PdfLineData[] BuildLines(IEnumerable<PdfWordData> words)
    {
        var lines = new List<List<PdfWordData>>();
        foreach (var word in words)
        {
            var line = lines.LastOrDefault(candidate =>
                Math.Abs(candidate.Average(item => item.Top) - word.Top) <= RowLineTolerance);
            if (line is null)
            {
                line = new List<PdfWordData>();
                lines.Add(line);
            }
            line.Add(word);
        }

        return lines
            .Select(line =>
            {
                var ordered = line.OrderBy(word => word.Left).ToArray();
                return new PdfLineData(
                    ordered.Average(word => word.Top),
                    string.Join(" ", ordered.Select(word => word.Text)),
                    ordered);
            })
            .OrderBy(line => line.Top)
            .ToArray();
    }

    private IEnumerable<ParsedInvoiceRow> ExtractRows(
        PdfPageData page,
        string invoiceType,
        IReadOnlyList<SchoolRecord> schools,
        DateTime reportDate)
    {
        var boundaries = ColumnBoundaries(page.Width);
        foreach (var line in page.Lines)
        {
            var columnText = Enumerable.Range(0, 10)
                .Select(column => JoinColumn(line.Words, boundaries[column], boundaries[column + 1]))
                .ToArray();
            if (!int.TryParse(columnText[0], NumberStyles.None, CultureInfo.InvariantCulture, out var sequenceNumber))
                continue;

            if (!TryDate(columnText[4], out var invoiceDate)
                || !TryDate(columnText[5], out var dueDate))
            {
                yield return ParsedInvoiceRow.Invalid(sequenceNumber, "Fatura veya son ödeme tarihi okunamadı.");
                continue;
            }
            if (!NumberParser.TryNonNegativeDecimal(columnText[6], out var payableAmount)
                || !NumberParser.TryNonNegativeDecimal(columnText[7], out var vatAmount)
                || !NumberParser.TryNonNegativeDecimal(columnText[8], out var printedStampDuty)
                || !NumberParser.TryNonNegativeDecimal(columnText[9], out var printedPaymentAmount))
            {
                yield return ParsedInvoiceRow.Invalid(sequenceNumber, "Tutar sütunlarından biri okunamadı.");
                continue;
            }

            var institutionName = columnText[1].Trim();
            var serviceNumber = DigitsOnly(columnText[2]);
            var invoiceNumber = columnText[3].Trim();
            if (string.IsNullOrWhiteSpace(institutionName) || string.IsNullOrWhiteSpace(serviceNumber))
            {
                yield return ParsedInvoiceRow.Invalid(sequenceNumber, "Kurum adı veya hizmet numarası eksik.");
                continue;
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
                yield return ParsedInvoiceRow.Invalid(sequenceNumber, "PDF tutarları güncel vergi hesabıyla doğrulanamadı.");
                continue;
            }

            var school = schools.FirstOrDefault(candidate =>
                TurkishTextComparison.EqualsIgnoreCase(candidate.Name.Trim(), institutionName));
            var institutionType = school?.InstitutionType ?? InferInstitutionType(institutionName);

            // Tarihsel PDF güncel hesapla doğrulanabilir; ancak PDF'nin üretildiği özgün
            // vergi kural sürümü kanıtlanamadığı için rule metadata bilinçli olarak boş bırakılır.
            var invoice = new InvoiceRecord(
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
                compensationAmount);
            var validation = InvoiceValidationService.Validate(invoice);
            yield return validation.IsValid
                ? ParsedInvoiceRow.Valid(sequenceNumber, invoice)
                : ParsedInvoiceRow.Invalid(sequenceNumber, validation.Message);
        }
    }

    private static IEnumerable<int> ExtractSequenceNumbers(PdfPageData page)
    {
        var boundaries = ColumnBoundaries(page.Width);
        foreach (var line in page.Lines)
        {
            var value = JoinColumn(line.Words, boundaries[0], boundaries[1]);
            if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var sequenceNumber))
                yield return sequenceNumber;
        }
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
        if (MatchesPrinted(calculation, printedStampDuty, printedPaymentAmount)) return true;
        if (!TaxCalculationService.IsInterruptionCompensationAllowed(invoiceType)) return false;

        var estimate = printedStampDuty / TaxCalculationService.StampDutyRate + vatAmount - payableAmount;
        var centerInCents = decimal.ToInt32(decimal.Round(estimate * 100m, 0, MidpointRounding.AwayFromZero));
        for (var distance = 0; distance <= 110; distance++)
        {
            foreach (var candidateInCents in distance == 0
                         ? new[] { centerInCents }
                         : new[] { centerInCents - distance, centerInCents + distance })
            {
                if (candidateInCents <= 0) continue;
                var candidate = candidateInCents / 100m;
                var candidateCalculation = _taxCalculationService.Calculate(
                    payableAmount, vatAmount, invoiceType, candidate);
                if (!MatchesPrinted(candidateCalculation, printedStampDuty, printedPaymentAmount)) continue;
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

    private static AuthorityProfile ExtractAuthorityProfile(
        PdfPageData page,
        AuthorityProfile fallback)
    {
        var left = ExtractSignature(page, true);
        var right = ExtractSignature(page, false);
        return new AuthorityProfile(
            left.Name ?? fallback.RealizationOfficerName,
            left.Title ?? fallback.RealizationOfficerTitle,
            right.Name ?? fallback.SpendingAuthorityName,
            right.Title ?? fallback.SpendingAuthorityTitle).Normalize();
    }

    private static (string? Name, string? Title) ExtractSignature(PdfPageData page, bool leftSide)
    {
        var sideWords = page.Words
            .Where(word => leftSide ? word.CenterX < page.Width / 2 : word.CenterX >= page.Width / 2)
            .ToArray();
        var lines = BuildLines(sideWords);
        var role = lines.FirstOrDefault(line => Contains(
            line.Text,
            leftSide ? "Gerçekleştirme Görevlisi" : "Harcama Yetkilisi"));
        if (role is null) return (null, null);

        var candidates = lines
            .Where(line => line.Top > role.Top + 8 && line.Top < role.Top + 125)
            .Where(line => !TryDate(line.Text, out _))
            .Where(line => !Contains(line.Text, "Sayfa"))
            .OrderBy(line => line.Top)
            .ToArray();
        return candidates.Length switch
        {
            >= 2 => (candidates[0].Text.Trim(), candidates[1].Text.Trim()),
            1 => (candidates[0].Text.Trim(), null),
            _ => (null, null)
        };
    }

    private static DateTime? FindReportDate(IEnumerable<PdfPageData> pages)
    {
        foreach (var word in pages.SelectMany(page => page.Words).Where(word => word.Top < 58))
        {
            if (TryDate(word.Text, out var date)) return date;
        }
        return null;
    }

    private static double[] ColumnBoundaries(double pageWidth)
    {
        const double margin = 28.5;
        var tableWidth = pageWidth - margin * 2;
        var totalWeight = ColumnWeights.Sum();
        var boundaries = new double[ColumnWeights.Length + 1];
        boundaries[0] = margin;
        for (var index = 0; index < ColumnWeights.Length; index++)
            boundaries[index + 1] = boundaries[index] + tableWidth * ColumnWeights[index] / totalWeight;
        return boundaries;
    }

    private static string JoinColumn(
        IEnumerable<PdfWordData> words,
        double left,
        double right) => string.Join(
        " ",
        words
            .Where(word => word.CenterX >= left && word.CenterX < right)
            .OrderBy(word => word.Left)
            .Select(word => word.Text));

    private static bool TryDate(string text, out DateTime date) =>
        DateTime.TryParseExact(
            text.Trim(),
            DateFormats,
            Tr,
            DateTimeStyles.AllowWhiteSpaces,
            out date);

    private static string DigitsOnly(string text) =>
        string.Concat((text ?? string.Empty).Where(char.IsDigit));

    private static string InferInstitutionType(string institutionName)
    {
        if (Contains(institutionName, "Ortaokulu")) return "Ortaokul";
        if (Contains(institutionName, "İlkokulu")) return "İlkokul";
        if (Contains(institutionName, "Anaokulu") || Contains(institutionName, "Anasınıfı"))
            return "Anasınıfı/Anaokulu";
        return "Kurum";
    }

    private static bool Contains(string source, string value) =>
        Tr.CompareInfo.IndexOf(source, value, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;

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

    private sealed record PdfWordData(string Text, double Left, double Right, double Top)
    {
        public double CenterX => (Left + Right) / 2d;
    }

    private sealed record PdfLineData(double Top, string Text, IReadOnlyList<PdfWordData> Words);

    private sealed record PdfPageData(
        int PageNumber,
        double Width,
        double Height,
        IReadOnlyList<PdfWordData> Words,
        IReadOnlyList<PdfLineData> Lines);

    private sealed record ParsedInvoiceRow(
        int SequenceNumber,
        bool IsValid,
        InvoiceRecord? Invoice,
        string ErrorMessage)
    {
        public static ParsedInvoiceRow Valid(int sequenceNumber, InvoiceRecord invoice) =>
            new(sequenceNumber, true, invoice, string.Empty);

        public static ParsedInvoiceRow Invalid(int sequenceNumber, string message) =>
            new(sequenceNumber, false, null, message);
    }
}
