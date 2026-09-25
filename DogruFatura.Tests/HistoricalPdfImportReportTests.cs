using DogruFatura.Core.Models;
using DogruFatura.Wpf;

namespace DogruFatura.Tests;

public sealed class HistoricalPdfImportReportTests
{
    [Fact]
    public void ReportListsFailedFilesAndOcrStagesWithoutScanningArchive()
    {
        var root = Path.Combine(Path.GetTempPath(), "DogruFatura_Report_" + Guid.NewGuid().ToString("N"));
        try
        {
            var previews = Enumerable.Range(1, 24)
                .Select(index => new HistoricalPdfImportPreview(
                    Path.Combine(root, $"MYS-2025-{index}.pdf"),
                    $"MYS-2025-{index}.pdf",
                    index <= 20,
                    index <= 20 ? "Fatura doğrulandı." : "Tutar alanı okunamadı.",
                    $"MYS-2025-{index}",
                    "Elektrik",
                    1,
                    100m,
                    null)
                {
                    OcrAttempted = index > 20,
                    ScanDetail = index > 20
                        ? "Güçlü tarama • OCR denendi"
                        : "Hızlı metin taraması"
                })
                .ToArray();

            var path = HistoricalPdfImportReport.Write(root, previews, previews[..20], "Aktarım tamamlandı");
            var report = File.ReadAllText(path);

            Assert.StartsWith(root, path, StringComparison.Ordinal);
            Assert.Contains("Seçilen: 24 | Eklenen: 20 | Zaten arşivde: 0 | İnceleme gereken: 4", report, StringComparison.Ordinal);
            Assert.Contains("MYS-2025-21.pdf | İNCELEME GEREKİYOR", report, StringComparison.Ordinal);
            Assert.Contains("MYS-2025-24.pdf | İNCELEME GEREKİYOR", report, StringComparison.Ordinal);
            Assert.Contains("Güçlü tarama • OCR denendi", report, StringComparison.Ordinal);
            Assert.Contains("Tutar alanı okunamadı.", report, StringComparison.Ordinal);
            Assert.Contains("MYS-2025-1.pdf | EKLENDİ", report, StringComparison.Ordinal);
            var rerun = previews
                .Select(item => item.IsValid
                    ? (item.WithError("İşlem arşivinde zaten kayıtlı.")
                        with { AlreadyArchived = true })
                    : item)
                .ToArray();
            var rerunPath = HistoricalPdfImportReport.Write(
                root, rerun, Array.Empty<HistoricalPdfImportPreview>(), "Tekrar seçim");
            var rerunReport = File.ReadAllText(rerunPath);

            Assert.Contains("Eklenen: 0 | Zaten arşivde: 20 | İnceleme gereken: 4", rerunReport, StringComparison.Ordinal);
            Assert.Contains("MYS-2025-1.pdf | ZATEN ARŞİVDE", rerunReport, StringComparison.Ordinal);
            Assert.Contains("MYS-2025-21.pdf | İNCELEME GEREKİYOR", rerunReport, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
    [Fact]
    public void ReportListsInvoicesAndReconciledPaymentOrderTotals()
    {
        var root = Path.Combine(Path.GetTempPath(),
            "DogruFatura_Report_" + Guid.NewGuid().ToString("N"));
        try
        {
            var invoice = new InvoiceRecord(
                Guid.NewGuid(), new DateTime(2025, 3, 4),
                "İlkokul", "Hanoğlu İlkokulu", "",
                "Elektrik", "AAB2025001051104", "00002700660",
                new DateTime(2025, 2, 27), new DateTime(2025, 3, 10),
                186.10m, 31.01m, 155.09m, 1.4702532m, 184.6297468m);
            var archive = new ArchivedInvoiceList(
                Guid.NewGuid(), "DF-PDF-202519794703",
                new DateTime(2025, 3, 4), "Elektrik",
                new List<InvoiceRecord> { invoice },
                AuthorityProfileDefaults.Create(),
                "HYS-202519794703", true,
                new DateTime(2025, 3, 4), "HYS-202519794703.pdf");
            var preview = new HistoricalPdfImportPreview(
                Path.Combine(root, "8) HYS-2025-19794703.pdf"),
                "8) HYS-2025-19794703.pdf", true, "Doğrulandı.",
                "HYS-202519794703", "Elektrik", 1,
                invoice.PaymentAmount, archive);

            var path = HistoricalPdfImportReport.Write(
                root, new[] { preview }, new[] { preview }, "Aktarım tamamlandı");
            var report = File.ReadAllText(path);
            Assert.Contains("Fatura: AAB2025001051104", report, StringComparison.Ordinal);
            Assert.Contains("Abone: 00002700660", report, StringComparison.Ordinal);
            Assert.Contains("KDV: 31,01 TL", report, StringComparison.Ordinal);
            Assert.Contains("Damga: 1,47 TL", report, StringComparison.Ordinal);
            Assert.Contains("Doğrulanan toplam: Brüt 186,10 TL", report, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
}
