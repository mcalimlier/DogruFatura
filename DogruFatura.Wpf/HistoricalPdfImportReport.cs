using DogruFatura.Core.Services;
using System.Globalization;
using System.IO;
using System.Text;

namespace DogruFatura.Wpf;

/// <summary>Bir toplu PDF seçiminin sonuçlarını arşivi yeniden taramadan dosyaya yazar.</summary>
public static class HistoricalPdfImportReport
{
    public static string Write(
        string rootDirectory,
        IReadOnlyList<HistoricalPdfImportPreview> previews,
        IReadOnlyList<HistoricalPdfImportPreview> imported,
        string outcome)
    {
        ArgumentNullException.ThrowIfNull(rootDirectory);
        ArgumentNullException.ThrowIfNull(previews);
        ArgumentNullException.ThrowIfNull(imported);

        Directory.CreateDirectory(rootDirectory);
        var importedPaths = imported.Select(item => item.SourcePath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var addedCount = previews.Count(item => item.IsValid && importedPaths.Contains(item.SourcePath));
        var alreadyArchived = previews.Where(item => item.AlreadyArchived).ToArray();
        var failed = previews.Where(item => !item.IsValid && !item.AlreadyArchived).ToArray();
        var pendingCount = previews.Count - addedCount - alreadyArchived.Length - failed.Length;
        var lines = new List<string>
        {
            "DOĞRUFATURA - ARŞİV PDF AKTARIM RAPORU",
            "Tarih: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.GetCultureInfo("tr-TR")),
            "İşlem sonucu: " + Clean(outcome),
            $"Seçilen: {previews.Count} | Eklenen: {addedCount} | Zaten arşivde: {alreadyArchived.Length} | İnceleme gereken: {failed.Length} | Eklenmesi bekleyen/vazgeçilen: {pendingCount}",
            "",
            "İNCELEME GEREKEN DOSYALAR"
        };

        if (failed.Length == 0)
            lines.Add("Yok.");
        foreach (var item in failed)
        {
            lines.Add("- " + Clean(item.FileName));
            lines.Add("  Kaynak: " + Clean(item.SourcePath));
            lines.Add("  Tarama: " + Clean(item.ScanDetail));
            lines.Add("  OCR denendi: " + (item.OcrAttempted ? "EVET" : "HAYIR"));
            lines.Add("  OCR atlandı: " + (item.OcrSkipped ? "EVET" : "HAYIR"));
            lines.Add("  Neden: " + Clean(item.Status));
        }

        lines.Add("");
        lines.Add("ZATEN ARŞİVDE OLAN DOSYALAR");
        if (alreadyArchived.Length == 0)
            lines.Add("Yok.");
        foreach (var item in alreadyArchived)
            lines.Add("- " + Clean(item.FileName) + " | " + Clean(item.Status));

        lines.Add("");
        lines.Add("TÜM SEÇİLEN DOSYALAR");
        foreach (var item in previews)
        {
            var result = item.AlreadyArchived ? "ZATEN ARŞİVDE"
                : !item.IsValid ? "İNCELEME GEREKİYOR"
                : importedPaths.Contains(item.SourcePath) ? "EKLENDİ"
                : "EKLENMEDİ (onay verilmedi veya işlem geri alındı)";
            lines.Add("- " + Clean(item.FileName) + " | " + result);
            lines.Add("  Kaynak: " + Clean(item.SourcePath));
            lines.Add("  Tarama: " + Clean(item.ScanDetail));
            lines.Add("  OCR denendi: " + (item.OcrAttempted ? "EVET" : "HAYIR"));
            lines.Add("  OCR atlandı: " + (item.OcrSkipped ? "EVET" : "HAYIR"));
            lines.Add("  Açıklama: " + Clean(item.Status));
            if (item.Archive is { } archive)
            {
                lines.Add("  Ödeme emri: " + Clean(item.HysNumber));
                if (archive.Invoices.Count > 0)
                {
                    for (var index = 0; index < archive.Invoices.Count; index++)
                    {
                        var invoice = archive.Invoices[index];
                        lines.Add($"    {index + 1}. Fatura: {Clean(invoice.InvoiceNumber)} | Kurum: {Clean(invoice.InstitutionName)}"
                            + $" | Abone: {Clean(invoice.ServiceNumber)}"
                            + $" | Fatura tarihi: {invoice.InvoiceDate.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR"))}"
                            + $" | Vade: {invoice.DueDate.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR"))}"
                            + $" | Brüt: {MoneyRounded(invoice.PayableAmount)}"
                            + $" | KDV: {MoneyRounded(invoice.VatAmount)}"
                            + $" | Damga: {MoneyRounded(invoice.StampDuty)}"
                            + $" | Net: {MoneyRounded(invoice.PaymentAmount)}");
                    }

                    lines.Add("  Doğrulanan toplam: "
                        + $"Brüt {MoneyRounded(archive.Invoices.Sum(invoice => invoice.PayableAmount))} | "
                        + $"KDV {MoneyRounded(archive.Invoices.Sum(invoice => invoice.VatAmount))} | "
                        + $"Damga {MoneyRounded(archive.Invoices.Sum(invoice => invoice.StampDuty))} | "
                        + $"Net {MoneyRounded(archive.Invoices.Sum(invoice => invoice.PaymentAmount))}");
                }
                else if (archive.IsLegacyEvidenceOnly)
                {
                    lines.Add("  Kaynak-delil modu: satır düzeyinde olmayan vergi/fatura tarihi alanları üretilmedi.");
                    foreach (var evidence in archive.LegacyEvidenceItems ?? new List<DogruFatura.Core.Models.LegacyInvoiceEvidenceRecord>())
                    {
                        lines.Add($"    Kaynak satır: {Clean(evidence.InvoiceNumber)} | Kurum: {Clean(evidence.InstitutionName)}"
                            + $" | Hizmet: {Clean(evidence.ServiceNumber)}"
                            + $" | Dönem: {evidence.PeriodStart.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR"))}"
                            + $"-{evidence.PeriodEnd.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR"))}"
                            + $" | Vade: {evidence.DueDate.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR"))}"
                            + $" | Brüt: {MoneyRounded(evidence.GrossAmount)}");
                    }

                    lines.Add("  Resmî HYS toplamı: "
                        + $"Brüt {MoneyRounded(archive.LegacyGrossTotal)} | "
                        + $"Damga {MoneyRounded(archive.LegacyStampDutyTotal)} | "
                        + $"Net {MoneyRounded(archive.LegacyPaymentTotal)}");
                    lines.Add("  Not: " + Clean(archive.LegacyEvidenceNote));
                }
            }
        }

        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture);
        var path = Path.Combine(rootDirectory,
            $"Arsiv_Aktarim_Raporu_{stamp}_{Guid.NewGuid():N}.txt");
        var tempPath = path + ".tmp";
        try
        {
            File.WriteAllLines(tempPath, lines, new UTF8Encoding(true));
            File.Move(tempPath, path);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }

        return path;
    }

    private static string MoneyRounded(decimal value) => NumberParser.Money(
        decimal.Round(value, 2, MidpointRounding.AwayFromZero));

    private static string Clean(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? "Belirtilmedi"
            : value.Replace('\r', ' ').Replace('\n', ' ').Trim();
}
