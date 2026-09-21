using DogruFatura.Core.Models;
using DogruFatura.Wpf;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace DogruFatura.Tests;

public sealed class LegacyHysPdfImportTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GovernmentPaymentAttachmentIsReadBySemanticHeaders(bool useAlternativeHeaders)
    {
        using var temp = new TempFile("1) HYS-2025-18581984.pdf");
        CreateLegacyHysPdf(temp.Path, useAlternativeHeaders);

        var school = new SchoolRecord(
            "Ortaokul",
            "Tatar Ortaokulu",
            "1234567890",
            "12330",
            "22222",
            "33333",
            "44444",
            Guid.Parse("11111111-1111-1111-1111-111111111111"));

        var preview = new HistoricalPdfImportService().Read(
            temp.Path,
            new[] { school },
            new AuthorityProfile(
                "Halil ÖZENÇ",
                "Şube Müdürü",
                "Özgür BAYSAL",
                "İlçe Milli Eğitim Müdürü"));

        Assert.True(preview.IsValid, preview.Status);
        Assert.Equal("HYS-202518581984", preview.HysNumber);
        Assert.Equal("Elektrik", preview.InvoiceType);
        Assert.Equal(1, preview.InvoiceCount);
        Assert.NotNull(preview.Archive);

        var invoice = Assert.Single(preview.Archive!.Invoices);
        Assert.Equal("Tatar Ortaokulu", invoice.InstitutionName);
        Assert.Equal("12330", invoice.ServiceNumber);
        Assert.Equal("AAA2025000417018", invoice.InvoiceNumber);
        Assert.Equal(new DateTime(2025, 1, 13), invoice.InvoiceDate);
        Assert.Equal(new DateTime(2025, 1, 23), invoice.DueDate);
        Assert.Equal(9806.90m, invoice.PayableAmount);
        Assert.Equal(1634.48m, invoice.VatAmount);
        Assert.Equal(77.47m, invoice.StampDuty);
        Assert.Equal(9729.43m, invoice.PaymentAmount);
        Assert.Equal(school.SchoolId, invoice.SchoolId);
    }

    private static void CreateLegacyHysPdf(string path, bool alternative)
    {
        using var document = new PdfDocument();
        var page = document.AddPage();
        page.Orientation = PageOrientation.Landscape;
        page.Size = PageSize.A4;

        using var graphics = XGraphics.FromPdfPage(page);
        var body = new XFont("Segoe UI", 8, XFontStyleEx.Regular);
        var bold = new XFont("Segoe UI", 8, XFontStyleEx.Bold);
        var title = new XFont("Segoe UI", 11, XFontStyleEx.Bold);

        graphics.DrawString("ÖDEME EMRİ BELGESİ", title, XBrushes.Black, 320, 28);
        graphics.DrawString("Düzenleme Tarihi 16-01-2025", body, XBrushes.Black, 560, 28);
        graphics.DrawString("Belge Numarası HYS-2025-18581984", body, XBrushes.Black, 560, 42);
        graphics.DrawString("Ödeme Emri Alt Türü Elektrik Alımı", body, XBrushes.Black, 40, 42);

        var x = new[] { 35d, 80d, 185d, 260d, 345d, 420d, 500d, 590d, 675d, 755d };
        var headers = alternative
            ? new[]
            {
                "Sıra No", "Kurum Adı", "Abone No", "Fatura Numarası", "Belge Tarihi",
                "Vade Tarihi", "Fatura Tutarı", "KDV", "Damga Tutarı", "Net Tutar"
            }
            : new[]
            {
                "No", "Okul Adı", "Hizmet Numarası", "Fatura No", "Fatura Tarihi",
                "Son Ödeme Tarihi", "Ödenecek Tutar", "KDV Tutarı", "Damga Vergisi", "Ödeme Tutarı"
            };

        for (var i = 0; i < headers.Length; i++)
            graphics.DrawString(headers[i], bold, XBrushes.Black, x[i], 88);

        var row = new[]
        {
            "1", "Tatar Ortaokulu", "12330", "AAA2025000417018", "13.01.2025",
            "23.01.2025", "9.806,90", "1.634,48", "77,47", "9.729,43"
        };

        for (var i = 0; i < row.Length; i++)
            graphics.DrawString(row[i], body, XBrushes.Black, x[i], 110);

        graphics.DrawString("Gerçekleştirme Görevlisi", bold, XBrushes.Black, 110, 500);
        graphics.DrawString("Halil ÖZENÇ", body, XBrushes.Black, 140, 518);
        graphics.DrawString("Şube Müdürü", body, XBrushes.Black, 140, 534);
        graphics.DrawString("Harcama Yetkilisi", bold, XBrushes.Black, 590, 500);
        graphics.DrawString("Özgür BAYSAL", body, XBrushes.Black, 610, 518);
        graphics.DrawString("İlçe Milli Eğitim Müdürü", body, XBrushes.Black, 590, 534);

        document.Save(path);
    }

    private sealed class TempFile : IDisposable
    {
        private readonly string _folder;

        public TempFile(string fileName)
        {
            _folder = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "DogruFatura_LegacyPdfTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_folder);
            Path = System.IO.Path.Combine(_folder, fileName);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_folder))
                    Directory.Delete(_folder, true);
            }
            catch
            {
                // Test geçici dosya temizliği best-effort.
            }
        }
    }
}
