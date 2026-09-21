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
        Assert.Equal(77.47m, decimal.Round(invoice.StampDuty, 2, MidpointRounding.AwayFromZero));
        Assert.Equal(9729.43m, decimal.Round(invoice.PaymentAmount, 2, MidpointRounding.AwayFromZero));
        Assert.Equal(school.SchoolId, invoice.SchoolId);
    }

    [Fact]
    public void LegacyMysTableWithoutDueDateIsImportedSafely()
    {
        using var temp = new TempFile("1) MYS-2023-4010364.pdf");
        CreateLegacyMysPdf(temp.Path);

        var school = new SchoolRecord(
            "İlkokul",
            "Ağaçbeyli İlkokulu",
            "1234567890",
            "00004091370",
            "22222",
            "33333",
            "44444",
            Guid.Parse("22222222-2222-2222-2222-222222222222"));

        var preview = new HistoricalPdfImportService().Read(
            temp.Path,
            new[] { school },
            new AuthorityProfile(
                "Ömerali ŞİMŞEK",
                "Şube Müdürü",
                "Mehmet SAKAL",
                "İlçe Milli Eğitim Müdürü"));

        Assert.True(preview.IsValid, preview.Status);
        Assert.Equal("MYS-20234010364", preview.HysNumber);
        Assert.Equal("Elektrik", preview.InvoiceType);
        Assert.Equal(1, preview.InvoiceCount);
        Assert.Contains(
            "son ödeme tarihi belgede bulunmadığı",
            preview.Status,
            StringComparison.Ordinal);

        var invoice = Assert.Single(preview.Archive!.Invoices);
        Assert.Equal("Ağaçbeyli İlkokulu", invoice.InstitutionName);
        Assert.Equal("00004091370", invoice.ServiceNumber);
        Assert.Equal("EAH202300012083", invoice.InvoiceNumber);
        Assert.Equal(new DateTime(2023, 8, 11), invoice.InvoiceDate);
        Assert.Equal(invoice.InvoiceDate, invoice.DueDate);
        Assert.Equal(1617.30m, invoice.PayableAmount);
        Assert.Equal(269.55m, invoice.VatAmount);
        Assert.Equal(12.78m, decimal.Round(invoice.StampDuty, 2, MidpointRounding.AwayFromZero));
        Assert.Equal(1604.52m, decimal.Round(invoice.PaymentAmount, 2, MidpointRounding.AwayFromZero));
    }

    private static void CreateLegacyMysPdf(string path)
    {
        using var document = new PdfDocument();
        var page = document.AddPage();
        page.Size = PageSize.A4;
        page.Orientation = PageOrientation.Landscape;

        using var graphics = XGraphics.FromPdfPage(page);
        var body = new XFont("Segoe UI", 7.5, XFontStyleEx.Regular);
        var bold = new XFont("Segoe UI", 7.5, XFontStyleEx.Bold);

        graphics.DrawString("ÖDEME EMRİ BELGESİ", bold, XBrushes.Black, 320, 28);
        graphics.DrawString("Düzenleme Tarihi 15-08-2023", body, XBrushes.Black, 560, 28);
        graphics.DrawString("Belge Numarası MYS-2023-4010364", body, XBrushes.Black, 560, 42);
        graphics.DrawString("Ödeme Emri Alt Türü Elektrik Alımı", body, XBrushes.Black, 40, 42);

        var x = new[] { 25d, 65d, 125d, 250d, 340d, 435d, 520d, 605d, 685d, 760d };
        var headers = new[]
        {
            "S.NO", "İLÇE ADI", "KURUM_ADI", "Tüketici No:", "FATURA_NO",
            "FATURA_TARİHİ", "TOPLAM_TUTAR", "KDV TUTARI", "DAMGA VERGİSİ", "ELE GEÇEN MİKTAR"
        };
        for (var i = 0; i < headers.Length; i++)
            graphics.DrawString(headers[i], bold, XBrushes.Black, x[i], 88);

        var row = new[]
        {
            "1", "Sivaslı", "Ağaçbeyli İlkokulu", "00004091370", "EAH202300012083",
            "11.08.2023", "1.617,30", "269,55", "12,78", "1.604,52"
        };
        for (var i = 0; i < row.Length; i++)
            graphics.DrawString(row[i], body, XBrushes.Black, x[i], 110);

        graphics.DrawString("Gerçekleştirme Görevlisi", bold, XBrushes.Black, 110, 500);
        graphics.DrawString("Ömerali ŞİMŞEK", body, XBrushes.Black, 140, 518);
        graphics.DrawString("Şube Müdürü", body, XBrushes.Black, 140, 534);
        graphics.DrawString("Harcama Yetkilisi", bold, XBrushes.Black, 590, 500);
        graphics.DrawString("Mehmet SAKAL", body, XBrushes.Black, 610, 518);
        graphics.DrawString("İlçe Milli Eğitim Müdürü", body, XBrushes.Black, 590, 534);

        document.Save(path);
    }

    private static void CreateLegacyHysPdf(string path, bool alternative)
    {
        using var document = new PdfDocument();
        var page = document.AddPage();
        page.Size = PageSize.A4;
        page.Orientation = PageOrientation.Landscape;

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
