using DogruFatura.Core.Models;
using DogruFatura.Wpf;
using PdfSharp.Pdf;

namespace DogruFatura.Tests;

public sealed class HistoricalPdfImportCancellationTests
{
    [Fact]
    public void ReadThrowsWhenCancellationWasRequestedBeforeParsing()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var service = new HistoricalPdfImportService();

        Assert.Throws<OperationCanceledException>(() =>
            service.Read(
                "HYS-2026123456.pdf",
                Array.Empty<SchoolRecord>(),
                AuthorityProfileDefaults.Create(),
                cancellationToken: cancellation.Token));
    }

    [Fact]
    public void FastScanDefersUnrecognizedPdfWithoutStartingStrongOcr()
    {
        var folder = Path.Combine(
            Path.GetTempPath(),
            "DogruFatura_FastScanTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "1) MYS-2023-4010364.pdf");

        try
        {
            using (var document = new PdfDocument())
            {
                document.AddPage();
                document.Save(path);
            }

            var preview = new HistoricalPdfImportService().Read(
                path,
                Array.Empty<SchoolRecord>(),
                AuthorityProfileDefaults.Create(),
                scanMode: HistoricalPdfImportScanMode.Fast);

            Assert.False(preview.IsValid);
            Assert.Contains("Güçlü taramada yeniden denenecek", preview.Status, StringComparison.Ordinal);
        }
        finally
        {
            try
            {
                if (Directory.Exists(folder))
                    Directory.Delete(folder, true);
            }
            catch
            {
                // Test geçici dosya temizliği best-effort.
            }
        }
    }
}
