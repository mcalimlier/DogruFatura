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

            Assert.StartsWith(root, path);
            Assert.Contains("Seçilen: 24 | Eklenen: 20 | Eklenemeyen: 4", report);
            Assert.Contains("MYS-2025-21.pdf | EKLENEMEDİ", report);
            Assert.Contains("MYS-2025-24.pdf | EKLENEMEDİ", report);
            Assert.Contains("Güçlü tarama • OCR denendi", report);
            Assert.Contains("Tutar alanı okunamadı.", report);
            Assert.Contains("MYS-2025-1.pdf | EKLENDİ", report);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
}
