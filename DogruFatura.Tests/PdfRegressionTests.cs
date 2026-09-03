using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using DogruFatura.Wpf;
using System.Globalization;
using UglyToad.PdfPig;

namespace DogruFatura.Tests;

public sealed class PdfRegressionTests
{
    [Fact]
    public void ThirtyTwoInvoices_CreateThreeLandscapeA4Pages_InsideSideMargins()
    {
        using var temp = new TempFile("report.pdf");
        var data = ReportData(32);

        new FinalReportPdfService().Save(data, temp.Path);

        using var document = PdfDocument.Open(temp.Path);
        Assert.Equal(3, document.NumberOfPages);
        foreach (var page in document.GetPages())
        {
            var width = Convert.ToDouble(page.Width, CultureInfo.InvariantCulture);
            var height = Convert.ToDouble(page.Height, CultureInfo.InvariantCulture);
            Assert.True(width > height, "PDF A4 yatay olmalıdır.");
            Assert.InRange(width, 840.0, 843.0);
            Assert.InRange(height, 594.0, 597.0);

            var words = page.GetWords().Where(word => !string.IsNullOrWhiteSpace(word.Text)).ToArray();
            Assert.NotEmpty(words);
            var left = words.Min(word => Convert.ToDouble(word.BoundingBox.Left, CultureInfo.InvariantCulture));
            var right = words.Max(word => Convert.ToDouble(word.BoundingBox.Right, CultureInfo.InvariantCulture));
            Assert.True(left >= FinalReportLayout.SideMarginPoints - 4.0,
                $"Sol kenar taşması: {left:0.00} pt");
            Assert.True(right <= width - FinalReportLayout.SideMarginPoints + 4.0,
                $"Sağ kenar taşması: {right:0.00} pt");
        }
    }

    [Fact]
    public void GeneratedPdf_CanBeReadBackByHistoricalArchiveImporter()
    {
        using var temp = new TempFile("48) HYS-2026-34546180.pdf");
        var data = ReportData(3);
        new FinalReportPdfService().Save(data, temp.Path);

        var school = new SchoolRecord(
            "İlkokul", "Çağdaş Türkçe İlkokulu", "1234567890",
            "123456789012345678", "2222222222", "3333333333", "4444444444");
        var preview = new HistoricalPdfImportService().Read(
            temp.Path,
            new[] { school },
            AuthorityProfileDefaults.Create());

        Assert.True(preview.IsValid, preview.Status);
        Assert.Equal("HYS-202634546180", preview.HysNumber);
        Assert.Equal("Elektrik", preview.InvoiceType);
        Assert.Equal(3, preview.InvoiceCount);
        Assert.NotNull(preview.Archive);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(12, 1)]
    [InlineData(13, 2)]
    [InlineData(31, 2)]
    [InlineData(32, 3)]
    [InlineData(100, 6)]
    public void ActualPdfPageCount_MatchesPlanner(int invoiceCount, int expectedPages)
    {
        using var temp = new TempFile($"page-{invoiceCount}.pdf");
        new FinalReportPdfService().Save(ReportData(invoiceCount), temp.Path);

        using var document = PdfDocument.Open(temp.Path);
        Assert.Equal(expectedPages, document.NumberOfPages);
        Assert.Equal(FinalReportPagePlanner.Plan(invoiceCount).Count, document.NumberOfPages);
    }

    private static FinalReportData ReportData(int count)
    {
        var invoices = Enumerable.Range(1, count).Select(Invoice).ToArray();
        return new FinalReportData(
            invoices,
            "Elektrik",
            new DateTime(2026, 9, 3),
            new ReportSignature("Gerçekleştirme Görevlisi", "Halil ÖZENÇ", "Şube Müdürü"),
            new ReportSignature("Harcama Yetkilisi", "Özgür BAYSAL", "İlçe Milli Eğitim Müdürü"));
    }

    private static InvoiceRecord Invoice(int sequence)
    {
        const decimal payable = 1500m;
        const decimal vat = 250m;
        var calculation = new TaxCalculationService().Calculate(payable, vat, "Elektrik");
        return new InvoiceRecord(
            Guid.Parse($"00000000-0000-0000-0000-{sequence:000000000000}"),
            new DateTime(2026, 9, 3, 10, 0, 0),
            "İlkokul", "Çağdaş Türkçe İlkokulu", "1234567890", "Elektrik",
            $"FTR-{sequence:000000}", "123456789012345678",
            new DateTime(2026, 9, 1), new DateTime(2026, 9, 15),
            payable, vat, calculation.VatBase, calculation.StampDuty, calculation.PaymentAmount);
    }

    private sealed class TempFile : IDisposable
    {
        private readonly string _folder;

        public TempFile(string fileName)
        {
            _folder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "DogruFatura_PdfTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            Path = System.IO.Path.Combine(_folder, fileName);
        }

        public string Path { get; }

        public void Dispose()
        {
            try { if (Directory.Exists(_folder)) Directory.Delete(_folder, true); } catch { }
        }
    }
}
