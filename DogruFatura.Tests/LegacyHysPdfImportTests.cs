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
                "İlçe Milli Eğitim Müdürü"),
            scanMode: HistoricalPdfImportScanMode.Fast);

        Assert.True(preview.IsValid, preview.Status);
        Assert.DoesNotContain("OCR kullanıldı", preview.Status, StringComparison.Ordinal);
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

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public void LegacyMysTableWithoutDueDateIsImportedSafely(
        bool useOcrLikeMoney,
        bool damagePaymentHeader,
        bool damageLastResultCells)
    {
        using var temp = new TempFile("1) MYS-2023-4010364.pdf");
        CreateLegacyMysPdf(
            temp.Path,
            useOcrLikeMoney,
            damagePaymentHeader,
            damageLastResultCells);

        var school = new SchoolRecord(
            "İlkokul",
            "Ağaçbeyli İlkokulu",
            "1234567890",
            "00004003970",
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
        Assert.Equal(3, preview.InvoiceCount);
        Assert.Contains(
            "son ödeme tarihi belgede bulunmadığı",
            preview.Status,
            StringComparison.Ordinal);
        Assert.Equal(
            2484.91m,
            decimal.Round(preview.PaymentTotal, 2, MidpointRounding.AwayFromZero));
        Assert.Equal(3, preview.Archive!.Invoices.Count);

        var first = preview.Archive.Invoices[0];
        Assert.Equal("Ağaçbeyli İlkokulu", first.InstitutionName);
        Assert.Equal("00004003970", first.ServiceNumber);
        Assert.Equal("EAH2023000012083", first.InvoiceNumber);
        Assert.Equal(new DateTime(2023, 8, 11), first.InvoiceDate);
        Assert.Equal(first.InvoiceDate, first.DueDate);
        Assert.Equal(1617.30m, first.PayableAmount);
        Assert.Equal(269.55m, first.VatAmount);
        Assert.Equal(
            12.78m,
            decimal.Round(first.StampDuty, 2, MidpointRounding.AwayFromZero));
        Assert.Equal(
            1604.52m,
            decimal.Round(first.PaymentAmount, 2, MidpointRounding.AwayFromZero));

        var second = preview.Archive.Invoices[1];
        Assert.Equal(214.90m, second.PayableAmount);
        Assert.Equal(35.82m, second.VatAmount);
        Assert.Equal(
            1.70m,
            decimal.Round(second.StampDuty, 2, MidpointRounding.AwayFromZero));
        Assert.Equal(
            213.20m,
            decimal.Round(second.PaymentAmount, 2, MidpointRounding.AwayFromZero));

        var third = preview.Archive.Invoices[2];
        Assert.Equal(672.50m, third.PayableAmount);
        Assert.Equal(112.07m, third.VatAmount);
        Assert.Equal(
            5.31m,
            decimal.Round(third.StampDuty, 2, MidpointRounding.AwayFromZero));
        Assert.Equal(
            667.19m,
            decimal.Round(third.PaymentAmount, 2, MidpointRounding.AwayFromZero));
    }

    [Fact]
    public void PaymentOrderWithEmbeddedInvoiceIsImportedWithoutOcr()
    {
        using var temp = new TempFile("24) HYS-2025-21213030.pdf");
        CreateEmbeddedInvoicePdf(temp.Path, includePaymentOrder: true);
        var school = new SchoolRecord(
            "Kurum", "Sivaslı İlçe Milli Eğitim Müdürlüğü", "4710101139",
            "1063715995", "", "", "", Guid.NewGuid());
        var preview = new HistoricalPdfImportService().Read(
            temp.Path, new[] { school },
            AuthorityProfileDefaults.Create(),
            scanMode: HistoricalPdfImportScanMode.Fast);

        Assert.True(preview.IsValid, preview.Status);
        Assert.False(preview.OcrAttempted);
        Assert.Equal("HYS-202521213030", preview.HysNumber);
        var invoice = Assert.Single(preview.Archive!.Invoices);
        Assert.Equal(school.SchoolId, invoice.SchoolId);
        Assert.Equal("EAD2025000026862", invoice.InvoiceNumber);
        Assert.Equal("1063715995", invoice.ServiceNumber);
        Assert.Equal(new DateTime(2025, 4, 15), invoice.InvoiceDate);
        Assert.Equal(new DateTime(2025, 4, 25), invoice.DueDate);
        Assert.Equal(4911.20m, invoice.PayableAmount);
        Assert.Equal(818.53m, invoice.VatAmount);
        Assert.Equal(38.80m, decimal.Round(invoice.StampDuty, 2, MidpointRounding.AwayFromZero));
        Assert.Equal(4872.40m, decimal.Round(invoice.PaymentAmount, 2, MidpointRounding.AwayFromZero));
    }

    [Fact]
    public void MissingServiceNumberUsesOnlyUniqueMatchingInstitution()
    {
        using var temp = new TempFile("24) HYS-2025-21213030.pdf");
        CreateEmbeddedInvoicePdf(
            temp.Path, includePaymentOrder: true, omitServiceNumber: true);
        var school = new SchoolRecord(
            "Kurum", "Sivaslı İlçe Milli Eğitim Müdürlüğü", "4710101139",
            "1063715995", "", "", "", Guid.NewGuid());

        var preview = new HistoricalPdfImportService().Read(
            temp.Path, new[] { school },
            AuthorityProfileDefaults.Create(),
            scanMode: HistoricalPdfImportScanMode.Fast);

        Assert.True(preview.IsValid, preview.Status);
        Assert.Equal("1063715995", Assert.Single(preview.Archive!.Invoices).ServiceNumber);
    }

    [Fact]
    public void EmbeddedInvoiceMustMatchPaymentOrderTotals()
    {
        using var temp = new TempFile("24) HYS-2025-21213030.pdf");
        CreateEmbeddedInvoicePdf(temp.Path, includePaymentOrder: true, vatAmount: "800,00");

        var preview = new HistoricalPdfImportService().Read(
            temp.Path, Array.Empty<SchoolRecord>(),
            AuthorityProfileDefaults.Create(),
            scanMode: HistoricalPdfImportScanMode.Fast);

        Assert.False(preview.IsValid);
        Assert.Null(preview.Archive);
    }

    [Fact]
    public void InvoiceOnlyPdfDoesNotInventOfficialPaymentOrder()
    {
        using var temp = new TempFile("24) HYS-2025-21213030.pdf");
        CreateEmbeddedInvoicePdf(temp.Path, includePaymentOrder: false);

        var preview = new HistoricalPdfImportService().Read(
            temp.Path, Array.Empty<SchoolRecord>(),
            AuthorityProfileDefaults.Create(),
            scanMode: HistoricalPdfImportScanMode.Fast);

        Assert.False(preview.IsValid);
        Assert.Null(preview.Archive);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FourTwoPageInvoicesStayUnderOnePaymentOrder(bool splitHeading)
    {
        using var temp = new TempFile("8) HYS-2025-19794703.pdf");
        CreateFourTwoPageInvoicesPdf(temp.Path, splitHeading, wrongStamp: false);

        var preview = new HistoricalPdfImportService().Read(
            temp.Path, Array.Empty<SchoolRecord>(),
            AuthorityProfileDefaults.Create(),
            scanMode: HistoricalPdfImportScanMode.Fast);
        Assert.True(preview.IsValid, preview.Status);
        Assert.Equal(4, preview.InvoiceCount);
        Assert.Equal(4, preview.Archive!.Invoices.Select(i => i.InvoiceNumber).Distinct().Count());
        Assert.Equal(5386.97m, decimal.Round(preview.PaymentTotal, 2,
            MidpointRounding.AwayFromZero));
    }

    [Fact]
    public void DueDatePrintedBeforeInvoiceDateStillImportsFourInvoices()
    {
        using var temp = new TempFile("8) HYS-2025-19794703.pdf");
        CreateFourTwoPageInvoicesPdf(temp.Path, splitHeading: true,
            wrongStamp: false, dueDateBeforeInvoiceDate: true);

        var preview = new HistoricalPdfImportService().Read(
            temp.Path, Array.Empty<SchoolRecord>(),
            AuthorityProfileDefaults.Create(),
            scanMode: HistoricalPdfImportScanMode.Fast);

        Assert.True(preview.IsValid, preview.Status);
        Assert.False(preview.OcrAttempted);
        Assert.Equal(4, preview.Archive!.Invoices.Count);
        var first = preview.Archive.Invoices[0];
        Assert.Equal(new DateTime(2025, 2, 28), first.InvoiceDate);
        Assert.Equal(new DateTime(2025, 3, 10), first.DueDate);
        Assert.Equal(5386.97m, decimal.Round(preview.PaymentTotal, 2,
            MidpointRounding.AwayFromZero));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void FirstInvoiceHeaderSplitAcrossRowsStillMatchesPaymentOrder(
        bool missingMonthYearSeparator, bool duplicatedDueDate)
    {
        using var temp = new TempFile("8) HYS-2025-19794703.pdf");
        CreateFourTwoPageInvoicesPdf(temp.Path, splitHeading: true,
            wrongStamp: false, splitFirstHeader: true,
            dropFirstInvoiceDateSeparator: missingMonthYearSeparator,
            duplicateFirstDueDate: duplicatedDueDate);

        var preview = new HistoricalPdfImportService().Read(
            temp.Path, Array.Empty<SchoolRecord>(),
            AuthorityProfileDefaults.Create(),
            scanMode: HistoricalPdfImportScanMode.Fast);

        Assert.True(preview.IsValid, preview.Status);
        Assert.Equal(4, preview.Archive!.Invoices.Count);
        Assert.Equal(new DateTime(2025, 2, 28), preview.Archive.Invoices[0].InvoiceDate);
        Assert.Equal(2638.50m, preview.Archive.Invoices[0].PayableAmount);
        Assert.Equal(5386.97m, decimal.Round(preview.PaymentTotal, 2,
            MidpointRounding.AwayFromZero));
    }

    [Theory]
    [InlineData("19) HYS-2025-20585737.pdf", "HYS-2025-20585737",
        "6.472,10", "51,13", "6.420,97",
        "5.819,00", "653,10", "EAC2025000042907", "EAC2025000042365")]
    [InlineData("20) HYS-2025-20588537.pdf", "HYS-2025-20588537",
        "661,40", "5,23", "656,17",
        "571,20", "90,20", "AAC2025001033373", "AAC2025001033430")]
    public void DetailedAllowanceReportReconcilesVatWithRealPaymentOrder(
        string file, string hys, string gross, string stamp, string net,
        string firstAmount, string secondAmount,
        string firstNumber, string secondNumber)
    {
        using var temp = new TempFile(file);
        CreateDetailedAllowancePdf(temp.Path, hys, gross, stamp, net,
            firstAmount, secondAmount, firstNumber, secondNumber);
        var preview = new HistoricalPdfImportService().Read(
            temp.Path, Array.Empty<SchoolRecord>(),
            AuthorityProfileDefaults.Create(),
            scanMode: HistoricalPdfImportScanMode.Fast);
        Assert.True(preview.IsValid, preview.Status);
        Assert.Equal(2, preview.InvoiceCount);
        Assert.Contains("KDV kaynaktan okunmuş değildir", preview.Status,
            StringComparison.Ordinal);
        Assert.Equal(firstNumber, preview.Archive!.Invoices[0].InvoiceNumber);
        Assert.Equal(secondNumber, preview.Archive.Invoices[1].InvoiceNumber);
    }

    [Fact]
    public void DetailedAllowanceWithWrongStampRemainsUnarchived()
    {
        using var temp = new TempFile("20) HYS-2025-20588537.pdf");
        CreateDetailedAllowancePdf(temp.Path, "HYS-2025-20588537",
            "661,40", "5,24", "656,16", "571,20", "90,20",
            "AAC2025001033373", "AAC2025001033430");
        var preview = new HistoricalPdfImportService().Read(
            temp.Path, Array.Empty<SchoolRecord>(),
            AuthorityProfileDefaults.Create(),
            scanMode: HistoricalPdfImportScanMode.Fast);
        Assert.False(preview.IsValid);
        Assert.Null(preview.Archive);
    }

    [Fact]
    public void Clean2024InternetSummaryTableImportsFourInvoices()
    {
        using var temp = new TempFile("16) HYS-2024-11293464.pdf");
        Create2024InternetSummaryPdf(temp.Path);

        var preview = new HistoricalPdfImportService().Read(
            temp.Path,
            Array.Empty<SchoolRecord>(),
            AuthorityProfileDefaults.Create(),
            scanMode: HistoricalPdfImportScanMode.Fast);

        Assert.True(preview.IsValid, preview.Status);
        Assert.False(preview.OcrAttempted);
        Assert.Equal("HYS-202411293464", preview.HysNumber);
        Assert.Equal("İnternet", preview.InvoiceType);
        Assert.Equal(4, preview.InvoiceCount);
        Assert.Equal(1418.44m, decimal.Round(preview.PaymentTotal, 2,
            MidpointRounding.AwayFromZero));
        Assert.Equal("2024044162854", preview.Archive!.Invoices[0].InvoiceNumber);
        Assert.Equal("2024044470604", preview.Archive.Invoices[3].InvoiceNumber);
    }

    [Fact]
    public void TtnetNumericEmbeddedInvoicesImportUnderOne2024PaymentOrder()
    {
        using var temp = new TempFile("23) HYS-2024-13678296.pdf");
        Create2024TtnetEmbeddedInvoicePdf(temp.Path);
        var school = new SchoolRecord(
            "Kurum",
            "Uşak Sivaslı İlçe Milli Eğitim Müdürlüğü",
            "4710101139",
            "111111",
            "222222",
            "1864001378",
            "333333",
            Guid.Parse("33333333-3333-3333-3333-333333333333"));

        var preview = new HistoricalPdfImportService().Read(
            temp.Path,
            new[] { school },
            AuthorityProfileDefaults.Create(),
            scanMode: HistoricalPdfImportScanMode.Fast);

        Assert.True(preview.IsValid, preview.Status);
        Assert.Equal("HYS-202413678296", preview.HysNumber);
        Assert.Equal(2, preview.InvoiceCount);
        Assert.Equal(813.33m, decimal.Round(preview.PaymentTotal, 2,
            MidpointRounding.AwayFromZero));
        Assert.Equal("2024066725124", preview.Archive!.Invoices[0].InvoiceNumber);
        Assert.Equal("2024055390697", preview.Archive.Invoices[1].InvoiceNumber);
        Assert.All(preview.Archive.Invoices,
            invoice => Assert.Equal("1864001378", invoice.ServiceNumber));
    }

    [Fact]
    public void GrossOnlyLegacyAdslListCreatesSourceEvidenceWithoutInventingTaxes()
    {
        using var temp = new TempFile("9) HYS-2024-10150517.pdf");
        CreateGrossOnlyLegacyAdslPdf(temp.Path);
        var school = new SchoolRecord(
            "İlkokul",
            "Budaklar İlkokulu",
            "1234567890",
            "111111",
            "222222",
            "1864035603",
            "333333",
            Guid.Parse("44444444-4444-4444-4444-444444444444"));

        var preview = new HistoricalPdfImportService().Read(
            temp.Path,
            new[] { school },
            AuthorityProfileDefaults.Create(),
            scanMode: HistoricalPdfImportScanMode.Fast);

        Assert.True(preview.IsValid, preview.Status);
        Assert.Equal(3, preview.InvoiceCount);
        Assert.NotNull(preview.Archive);
        Assert.True(preview.Archive!.IsLegacyEvidenceOnly);
        Assert.Empty(preview.Archive.Invoices);
        Assert.Equal(3, preview.Archive.LegacyEvidenceItems!.Count);
        Assert.Equal(118.75m, preview.Archive.LegacyGrossTotal);
        Assert.Equal(0.96m, preview.Archive.LegacyStampDutyTotal);
        Assert.Equal(117.79m, preview.Archive.LegacyPaymentTotal);
        Assert.False(preview.Archive.IsStatisticsEligible);
        Assert.Contains("uydurulmadan", preview.Status, StringComparison.Ordinal);
    }

    private static void CreateDetailedAllowancePdf(
        string path, string hys, string gross, string stamp, string net,
        string firstAmount, string secondAmount, string firstNumber,
        string secondNumber)
    {
        using var document = new PdfDocument();
        var font = new XFont("Segoe UI", 6, XFontStyleEx.Regular);
        var order = document.AddPage();
        using (var g = XGraphics.FromPdfPage(order))
        {
            g.DrawString("ÖDEME EMRİ BELGESİ", font, XBrushes.Black, 40, 35);
            g.DrawString("Düzenleme Tarihi 04-04-2025", font, XBrushes.Black, 40, 55);
            g.DrawString("Belge Numarası " + hys, font, XBrushes.Black, 40, 75);
            g.DrawString("Ödeme Emri Alt Türü Elektrik Alımı", font, XBrushes.Black, 40, 95);
            g.DrawString($"ÖDEME EMRİ TOPLAM TUTARI {gross} {stamp} 0,00 {net} 0,00 {net}",
                font, XBrushes.Black, 40, 400);
        }
        var page = document.AddPage();
        page.Width = XUnit.FromPoint(1152);
        page.Height = XUnit.FromPoint(576);
        using var graphics = XGraphics.FromPdfPage(page);
        graphics.DrawString("AYRINTILI GÖNDERİLEN ÖDENEK/FATURA LİSTESİ",
            font, XBrushes.Black, 400, 60);
        var headers = new[]
        {
            (98, "KURUM"), (121, "ADI"), (159, "KURUM"),
            (537, "GÖNDERİLEN"), (579, "TÜKETİM"),
            (753, "FATURA"), (794, "FATURA"),
            (829, "SON"), (867, "ABONE"), (898, "ÖDEME")
        };
        foreach (var (x, label) in headers)
            graphics.DrawString(label, font, XBrushes.Black, x, 90);
        var rows = new[]
        {
            (1, "Şehit Ruhi Çetin", "Ortaokulu", firstAmount,
                firstNumber, "00001601", "770"),
            (2, "Samatlar İlkokulu", "", secondAmount,
                secondNumber, "1801380", "")
        };
        foreach (var row in rows)
        {
            var y = 117 + (row.Item1 - 1) * 29;
            graphics.DrawString(row.Item1.ToString(System.Globalization.CultureInfo.InvariantCulture),
                font, XBrushes.Black, 2, y);
            graphics.DrawString(row.Item2, font, XBrushes.Black, 98, y);
            if (row.Item3.Length > 0)
                graphics.DrawString(row.Item3, font, XBrushes.Black, 98, y + 8);
            graphics.DrawString(row.Item4, font, XBrushes.Black, 537, y);
            graphics.DrawString(row.Item5[..11], font, XBrushes.Black, 753, y);
            graphics.DrawString(row.Item5[11..], font, XBrushes.Black, 753, y + 8);
            graphics.DrawString("24.03.2025", font, XBrushes.Black, 794, y);
            graphics.DrawString("03.04.2025", font, XBrushes.Black, 829, y);
            graphics.DrawString(row.Item6, font, XBrushes.Black, 867, y);
            if (row.Item7.Length > 0)
                graphics.DrawString(row.Item7, font, XBrushes.Black, 867, y + 8);
        }
        document.Save(path);
    }

    [Fact]
    public void CompleteFourInvoiceTextWithWrongPaymentTotalsSkipsLongOcr()
    {
        using var temp = new TempFile("8) HYS-2025-19794703.pdf");
        CreateFourTwoPageInvoicesPdf(temp.Path, splitHeading: true, wrongStamp: true);
        var preview = new HistoricalPdfImportService().Read(
            temp.Path, Array.Empty<SchoolRecord>(),
            AuthorityProfileDefaults.Create(),
            scanMode: HistoricalPdfImportScanMode.Fast);
        Assert.False(preview.IsValid);
        Assert.True(preview.OcrSkipped);
        Assert.Contains("eşleşmiyor", preview.Status, StringComparison.Ordinal);
    }

    private static void CreateFourTwoPageInvoicesPdf(
        string path, bool splitHeading, bool wrongStamp,
        bool dueDateBeforeInvoiceDate = false, bool splitFirstHeader = false,
        bool dropFirstInvoiceDateSeparator = false,
        bool duplicateFirstDueDate = false)
    {
        using (var document = new PdfDocument())
        {
            var font = new XFont("Segoe UI", 9, XFontStyleEx.Regular);
            var order = document.AddPage();
            using (var g = XGraphics.FromPdfPage(order))
            {
                g.DrawString("ÖDEME EMRİ BELGESİ", font, XBrushes.Black, 40, 35);
                g.DrawString("Düzenleme Tarihi 04-03-2025", font, XBrushes.Black, 40, 55);
                g.DrawString("Belge Numarası HYS-2025-19794703", font, XBrushes.Black, 40, 75);
                g.DrawString("Ödeme Emri Alt Türü Elektrik Alımı", font, XBrushes.Black, 40, 95);
                g.DrawString("Ödeme Emri Detayı", font, XBrushes.Black, 40, 310);
                if (splitHeading)
                {
                    g.DrawString("Ödeme Emri Brüt Tutar Kesinti Toplamı Net Tutar",
                        font, XBrushes.Black, 40, 400);
                    g.DrawString("Toplam Tutarı İndirim Artırım",
                        font, XBrushes.Black, 40, 410);
                    g.DrawString(wrongStamp
                        ? "5.430,50 43,54 0,00 5.386,96 0,00 5.386,96"
                        : "5.430,50 43,53 0,00 5.386,97 0,00 5.386,97",
                        font, XBrushes.Black, 40, 420);
                }
                else
                {
                    g.DrawString(wrongStamp
                        ? "ÖDEME EMRİ TOPLAM TUTARI 5.430,50 43,54 0,00 5.386,96 0,00 5.386,96"
                        : "ÖDEME EMRİ TOPLAM TUTARI 5.430,50 43,53 0,00 5.386,97 0,00 5.386,97",
                        font, XBrushes.Black, 40, 400);
                }
            }
            var items = new[]
            {
                ("EAB2025000055348", "00002604070", "2.638,50", "439,75", "Eldeniz İlkokulu"),
                ("AAB2025000996266", "00002200440", "872,20", "79,30", "Okul Lojmanı"),
                ("AAB2025000996213", "00002201830", "1.733,70", "288,94", "Erice İlkokulu"),
                ("AAB2025001051104", "00002700660", "186,10", "31,01", "Hanoğlu İlkokulu")
            };
            foreach (var item in items)
            {
                var first = document.AddPage();
                using (var g = XGraphics.FromPdfPage(first))
                {
                    g.DrawString("SAYIN", font, XBrushes.Black, 300, 100);
                    g.DrawString(item.Item5, font, XBrushes.Black, 300, 115);
                    g.DrawString("Tüketici No: " + item.Item2, font, XBrushes.Black, 40, 230);
                    g.DrawString("Fatura No: " + item.Item1, font, XBrushes.Black, 300, 280);
                    var isFirst = item.Item1 == "EAB2025000055348";
                    if (splitFirstHeader && isFirst)
                    {
                        g.DrawString(dropFirstInvoiceDateSeparator
                                ? "28-022025" : "28-02-2025",
                            font, XBrushes.Black, 120, 337);
                        g.DrawString("ÖDENECEK", font, XBrushes.Black, 210, 337);
                        g.DrawString("FAT URA T ARİHİ:", font, XBrushes.Black, 40, 350);
                        g.DrawString("SON ÖDEME TARİHİ: "
                                + (duplicateFirstDueDate
                                    ? "10-03-202510-03-2025" : "10-03-2025"),
                            font, XBrushes.Black, 330, 350);
                        g.DrawString("T UT AR:" + item.Item3 + " TL",
                            font, XBrushes.Black, 260, 363);
                    }
                    else
                    {
                        if (dueDateBeforeInvoiceDate && isFirst)
                            g.DrawString("10-03-2025", font, XBrushes.Black, 400, 340);
                        g.DrawString("FAT URA T ARİHİ: "
                            + (isFirst ? "28-02-2025" : "26-02-2025")
                            + " ÖDENECEK T UT AR: "
                            + item.Item3 + " TL", font, XBrushes.Black, 40, 350);
                        g.DrawString(dueDateBeforeInvoiceDate && isFirst
                                ? "SON ÖDEME TARİHİ:"
                                : "SON ÖDEME TARİHİ: 10-03-2025",
                            font, XBrushes.Black, 40, 365);
                    }
                }
                var second = document.AddPage();
                using var details = XGraphics.FromPdfPage(second);
                details.DrawString((item.Item1 == "AAB2025000996266" ? "KDV (%10) " : "KDV (%20) ")
                    + item.Item4 + " TL",
                    font, XBrushes.Black, 300, 600);
            }
            document.Save(path);
        }


    }

    private static void CreateEmbeddedInvoicePdf(
        string path, bool includePaymentOrder, string vatAmount = "818,53",
        bool omitServiceNumber = false)
    {
        using var document = new PdfDocument();
        var font = new XFont("Segoe UI", 9, XFontStyleEx.Regular);
        if (includePaymentOrder)
        {
            var orderPage = document.AddPage();
            orderPage.Size = PageSize.A4;
            using var order = XGraphics.FromPdfPage(orderPage);
            order.DrawString("ÖDEME EMRİ BELGESİ", font, XBrushes.Black, 40, 35);
            order.DrawString("Düzenleme Tarihi 24-04-2025", font, XBrushes.Black, 40, 55);
            order.DrawString("Belge Numarası HYS-2025-21213030", font, XBrushes.Black, 40, 75);
            order.DrawString("Ödeme Emri Alt Türü Elektrik Alımı", font, XBrushes.Black, 40, 95);
            order.DrawString("ÖDEME EMRİ TOPLAM TUTARI 4.911,20 38,80 0,00 4.872,40 0,00 4.872,40",
                font, XBrushes.Black, 40, 400);
        }

        var invoicePage = document.AddPage();
        invoicePage.Size = PageSize.A4;
        using var graphics = XGraphics.FromPdfPage(invoicePage);
        graphics.DrawString("SAYIN", font, XBrushes.Black, 300, 140);
        graphics.DrawString("SİVASLI İLÇE MİLLİ EĞİTİM MÜDÜRLÜĞÜ",
            font, XBrushes.Black, 300, 155);
        if (!omitServiceNumber)
            graphics.DrawString("Sözleşme Hesap No: 1063715995", font, XBrushes.Black, 40, 240);
        graphics.DrawString("Fatura No: EAD2025000026862", font, XBrushes.Black, 300, 300);
        graphics.DrawString("FATURA TARİHİ: 15-04-2025", font, XBrushes.Black, 40, 350);
        graphics.DrawString("ÖDENECEK TUTAR: 4.911,20 TL", font, XBrushes.Black, 200, 370);
        graphics.DrawString("SON ÖDEME TARİHİ: 25-04-2025", font, XBrushes.Black, 300, 390);
        graphics.DrawString($"Elektrik KDV (%20) {vatAmount} TL", font, XBrushes.Black, 300, 650);
        document.Save(path);
    }

    private static void CreateLegacyMysPdf(
        string path,
        bool useOcrLikeMoney,
        bool damagePaymentHeader,
        bool damageLastResultCells)
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
            "FATURA_TARİHİ", "TOPLAM_TUTAR", "KDV TUTARI", "DAMGA VERGİSİ",
            damagePaymentHeader ? "E.G.M." : "ELE GEÇEN MİKTAR"
        };
        for (var i = 0; i < headers.Length; i++)
            graphics.DrawString(headers[i], bold, XBrushes.Black, x[i], 88);

        var rows = useOcrLikeMoney
            ? new[]
            {
                new[] { "1", "Sivaslı", "Ağaçbeyli İlkokulu", "00004003970", "EAH2023000012083",
                    "11.08.2023", "161730", "26955", "1278", "160AS2" },
                new[] { "2", "Sivaslı", "Ağaçbeyli Anaokulu", "00004001720", "EAH2023000012082",
                    "11.08.2023", "21490", "3582", "170", "21320" },
                new[] { "3", "Sivaslı", "Ağaçbeyli Ortaokulu", "00004002490", "EAH2023000012078",
                    "11.08.2023", "67250", "11207", "531", "66719" }
            }
            : new[]
            {
                new[] { "1", "Sivaslı", "Ağaçbeyli İlkokulu", "00004003970", "EAH2023000012083",
                    "11.08.2023", "1.617,30", "269,55", "12,78", "1.604,52" },
                new[] { "2", "Sivaslı", "Ağaçbeyli Anaokulu", "00004001720", "EAH2023000012082",
                    "11.08.2023", "214,90", "35,82", "1,70", "213,20" },
                new[] { "3", "Sivaslı", "Ağaçbeyli Ortaokulu", "00004002490", "EAH2023000012078",
                    "11.08.2023", "672,50", "112,07", "5,31", "667,19" }
            };

        var ocrLikeOffsets = new[] { 0d, 7d, -5d, 5d, -4d, 0d, -8d, 4d, -3d, 2d };
        for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
        {
            for (var columnIndex = 0; columnIndex < rows[rowIndex].Length; columnIndex++)
            {
                if (damageLastResultCells
                    && rowIndex == rows.Length - 1
                    && columnIndex >= 8)
                    continue;

                var yOffset = useOcrLikeMoney ? ocrLikeOffsets[columnIndex] : 0d;
                var xOffset = damagePaymentHeader
                              && damageLastResultCells
                              && columnIndex >= 6
                    ? 48d
                    : 0d;
                graphics.DrawString(
                    rows[rowIndex][columnIndex],
                    body,
                    XBrushes.Black,
                    x[columnIndex] + xOffset,
                    110 + rowIndex * 20 + yOffset);
            }
        }

        graphics.DrawString(
            "ÖDEME EMRİ TOPLAM TUTARI 2.504,70 19,79 0,00 2.484,91 0,00 2.484,91",
            body,
            XBrushes.Black,
            250,
            455);

        graphics.DrawString("Gerçekleştirme Görevlisi", bold, XBrushes.Black, 110, 500);
        graphics.DrawString("Ömerali ŞİMŞEK", body, XBrushes.Black, 140, 518);
        graphics.DrawString("Şube Müdürü", body, XBrushes.Black, 140, 534);
        graphics.DrawString("Harcama Yetkilisi", bold, XBrushes.Black, 590, 500);
        graphics.DrawString("Mehmet SAKAL", body, XBrushes.Black, 610, 518);
        graphics.DrawString("İlçe Milli Eğitim Müdürü", body, XBrushes.Black, 590, 534);

        document.Save(path);
    }

    private static void Create2024InternetSummaryPdf(string path)
    {
        using var document = new PdfDocument();
        var body = new XFont("Segoe UI", 8, XFontStyleEx.Regular);
        var order = document.AddPage();
        using (var g = XGraphics.FromPdfPage(order))
        {
            g.DrawString("ÖDEME EMRİ BELGESİ", body, XBrushes.Black, 40, 35);
            g.DrawString("Düzenleme Tarihi 13-05-2024", body, XBrushes.Black, 40, 55);
            g.DrawString("Belge Numarası HYS-2024-11293464", body, XBrushes.Black, 40, 75);
            g.DrawString("Ödeme Emri Alt Türü İnternet Kullanım Ücreti", body, XBrushes.Black, 40, 95);
            g.DrawString("ÖDEME EMRİ TOPLAM TUTARI 1.430,00 11,56 0,00 1.418,44 0,00 1.418,44",
                body, XBrushes.Black, 40, 400);
        }

        var page = document.AddPage();
        page.Size = PageSize.A4;
        page.Orientation = PageOrientation.Landscape;
        using (var g = XGraphics.FromPdfPage(page))
        {
            var x = new[] { 35d, 80d, 205d, 280d, 350d, 420d, 500d, 595d, 685d, 745d };
            var headers = new[]
            {
                "No:", "Okul Adı", "Hizmet Numarası", "Fatura No", "Fatura Tarihi",
                "Son Ödeme Tarihi", "Ödenecek Tutar", "KDV Tutarı", "Damga Vergisi", "Ödeme Tutarı"
            };
            for (var i = 0; i < headers.Length; i++)
                g.DrawString(headers[i], body, XBrushes.Black, x[i], 60);

            var rows = new[]
            {
                new[] { "1", "Muammer Yazgan İlkokulu", "1864001383", "2024044162854", "30.04.2024", "16.05.2024", "410,00", "53,85", "3,38", "406,62" },
                new[] { "2", "Cumhuriyet İlkokulu", "8847516331", "2024044701450", "30.04.2024", "16.05.2024", "350,00", "53,85", "2,81", "347,19" },
                new[] { "3", "Atatürk İlkokulu", "1864001380", "2024044567172", "30.04.2024", "16.05.2024", "350,00", "53,85", "2,81", "347,19" },
                new[] { "4", "Şehit Ruhi Çetin Ortaokulu", "8851932065", "2024044470604", "30.04.2024", "16.05.2024", "320,00", "49,23", "2,57", "317,43" }
            };
            for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
            {
                var y = 82 + rowIndex * 17;
                for (var i = 0; i < rows[rowIndex].Length; i++)
                    g.DrawString(rows[rowIndex][i], body, XBrushes.Black, x[i], y);
            }
        }

        document.Save(path);
    }

    private static void Create2024TtnetEmbeddedInvoicePdf(string path)
    {
        using var document = new PdfDocument();
        var body = new XFont("Segoe UI", 8, XFontStyleEx.Regular);
        var order = document.AddPage();
        using (var g = XGraphics.FromPdfPage(order))
        {
            g.DrawString("ÖDEME EMRİ BELGESİ", body, XBrushes.Black, 40, 35);
            g.DrawString("Düzenleme Tarihi 07-08-2024", body, XBrushes.Black, 40, 55);
            g.DrawString("Belge Numarası HYS-2024-13678296", body, XBrushes.Black, 40, 75);
            g.DrawString("Ödeme Emri Alt Türü İnternet Kullanım Ücreti", body, XBrushes.Black, 40, 95);
            g.DrawString("ÖDEME EMRİ TOPLAM TUTARI 820,00 6,67 0,00 813,33 0,00 813,33",
                body, XBrushes.Black, 40, 400);
        }

        void AddInvoice(string number, string date, string due)
        {
            var page = document.AddPage();
            using var g = XGraphics.FromPdfPage(page);
            g.DrawString($"Fatura ID A16202400{number[^7..]}", body, XBrushes.Black, 40, 50);
            g.DrawString($"Fatura Tarih {date} 23:59", body, XBrushes.Black, 40, 70);
            g.DrawString("TTNET ANONIM SIRKETI", body, XBrushes.Black, 40, 90);
            g.DrawString("HİZMET NO : 8821991202", body, XBrushes.Black, 340, 90);
            g.DrawString($"FATURA NO : {number}", body, XBrushes.Black, 340, 110);
            g.DrawString("SAYIN UŞAK SİVASLI İLÇE MİLLİ EĞİTİM MÜDÜRLÜĞÜ",
                body, XBrushes.Black, 40, 145);
            g.DrawString($"SON ÖDEME TARİHİ : {due}", body, XBrushes.Black, 340, 170);
            g.DrawString("ÖDENECEK TUTAR : 410,00 TL", body, XBrushes.Black, 340, 190);
            g.DrawString("HESAP NUMARASI : 1864001378", body, XBrushes.Black, 340, 210);
            g.DrawString("KDV %20 (Matrah 292.31 ) 58,46", body, XBrushes.Black, 40, 350);
        }

        AddInvoice("2024066725124", "30-06-2024", "16/07/2024");
        AddInvoice("2024055390697", "31-05-2024", "17/06/2024");
        document.Save(path);
    }

    private static void CreateGrossOnlyLegacyAdslPdf(string path)
    {
        using var document = new PdfDocument();
        var body = new XFont("Segoe UI", 8, XFontStyleEx.Regular);
        var order = document.AddPage();
        using (var g = XGraphics.FromPdfPage(order))
        {
            g.DrawString("ÖDEME EMRİ BELGESİ", body, XBrushes.Black, 40, 35);
            g.DrawString("Düzenleme Tarihi 26-03-2024", body, XBrushes.Black, 40, 55);
            g.DrawString("Belge Numarası HYS-2024-10150517", body, XBrushes.Black, 40, 75);
            g.DrawString("Ödeme Emri Alt Türü İnternet Kullanım Ücreti", body, XBrushes.Black, 40, 95);
            g.DrawString("ÖDEME EMRİ TOPLAM TUTARI 118,75 0,96 0,00 117,79 0,00 117,79",
                body, XBrushes.Black, 40, 400);
        }

        var page = document.AddPage();
        page.Size = PageSize.A4;
        using (var g = XGraphics.FromPdfPage(page))
        {
            g.DrawString("BUDAKLAR İLKOKULU GEÇMİŞ DÖNEM ADSL FATURA BİLGİLERİ",
                body, XBrushes.Black, 60, 100);
            var x = new[] { 55d, 100d, 190d, 285d, 400d, 505d };
            var headers = new[] { "SIRANO", "HİZMETNO", "FATURANO", "DÖNEMİ", "SONÖDEMETARİHİ", "TUTAR" };
            for (var i = 0; i < headers.Length; i++)
                g.DrawString(headers[i], body, XBrushes.Black, x[i], 135);
            var rows = new[]
            {
                new[] { "1", "1864035603", "2020020012939", "01/02/2020-29/02/2020", "20/03/2020", "34,00" },
                new[] { "2", "1864035603", "2021031053234", "01/02/2021-28/02/2021", "16/03/2021", "34,75" },
                new[] { "3", "1864035603", "2022010840585", "01/01/2022-31/01/2022", "16/02/2022", "50,00" }
            };
            for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)
            {
                var y = 155 + rowIndex * 18;
                for (var i = 0; i < rows[rowIndex].Length; i++)
                    g.DrawString(rows[rowIndex][i], body, XBrushes.Black, x[i], y);
            }
        }

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

        var row = alternative
            ? new[]
            {
                "1", "Tatar Ortaokulu", "12330", "AAA2025000417018", "13.O1.2O25",
                "23.O1.2O25", "9.806,90", "1.634,48", "77,47", "9.729,43"
            }
            : new[]
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
