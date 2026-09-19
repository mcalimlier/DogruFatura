using ClosedXML.Excel;
using DogruFatura.Core.Models;
using Microsoft.Win32;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DogruFatura.Wpf;

public partial class MainWindow
{
    private void ExportStatisticsCsv_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetStatisticsResult(out var result))
            return;

        var dialog = new SaveFileDialog
        {
            Title = "İstatistik CSV çıktısını kaydet",
            Filter = "CSV dosyası (*.csv)|*.csv",
            FileName = $"DogruFatura-Istatistik-{DateTime.Now:yyyyMMdd-HHmm}.csv",
            AddExtension = true,
            DefaultExt = ".csv"
        };
        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            var builder = new StringBuilder();
            builder.AppendLine("sep=;");
            builder.AppendLine($"Rapor;{CsvCell("DoğruFatura İstatistik Raporu")}");
            builder.AppendLine($"Tarih;{CsvCell(DateTime.Now.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR")))}");
            builder.AppendLine($"Filtre;{CsvCell(StatisticsFilterDescription())}");
            builder.AppendLine($"Arşiv;{result.ArchiveCount}");
            builder.AppendLine($"Fatura;{result.InvoiceCount}");
            builder.AppendLine($"Kurum;{result.InstitutionCount}");
            builder.AppendLine($"Fatura Toplamı;{result.PayableTotal.ToString("0.00", CultureInfo.InvariantCulture)}");
            builder.AppendLine($"KDV;{result.VatTotal.ToString("0.00", CultureInfo.InvariantCulture)}");
            builder.AppendLine($"Damga;{result.StampDutyTotal.ToString("0.00", CultureInfo.InvariantCulture)}");
            builder.AppendLine($"Ödeme;{result.PaymentTotal.ToString("0.00", CultureInfo.InvariantCulture)}");
            builder.AppendLine();
            builder.AppendLine("Kurum;Yıl;Tür;Adet;Fatura Tutarı;KDV;Damga;Ödeme");

            foreach (var row in result.Rows)
            {
                builder.Append(CsvCell(row.InstitutionName)).Append(';')
                    .Append(row.Year.ToString(CultureInfo.InvariantCulture)).Append(';')
                    .Append(CsvCell(row.InvoiceType)).Append(';')
                    .Append(row.InvoiceCount.ToString(CultureInfo.InvariantCulture)).Append(';')
                    .Append(row.PayableTotal.ToString("0.00", CultureInfo.InvariantCulture)).Append(';')
                    .Append(row.VatTotal.ToString("0.00", CultureInfo.InvariantCulture)).Append(';')
                    .Append(row.StampDutyTotal.ToString("0.00", CultureInfo.InvariantCulture)).Append(';')
                    .AppendLine(row.PaymentTotal.ToString("0.00", CultureInfo.InvariantCulture));
            }

            File.WriteAllText(
                dialog.FileName,
                builder.ToString(),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

            MessageBox.Show(
                this,
                "CSV istatistik çıktısı kaydedildi.",
                "DoğruFatura - İstatistik",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ShowStatisticsExportError("CSV çıktısı kaydedilemedi", ex);
        }
    }

    private void ExportStatisticsExcel_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetStatisticsResult(out var result))
            return;

        var dialog = new SaveFileDialog
        {
            Title = "İstatistik Excel çıktısını kaydet",
            Filter = "Excel çalışma kitabı (*.xlsx)|*.xlsx",
            FileName = $"DogruFatura-Istatistik-{DateTime.Now:yyyyMMdd-HHmm}.xlsx",
            AddExtension = true,
            DefaultExt = ".xlsx"
        };
        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            using var workbook = new XLWorkbook();

            var summary = workbook.Worksheets.Add("Özet");
            summary.Cell("A1").Value = "DoğruFatura İstatistik Raporu";
            summary.Range("A1:D1").Merge();
            summary.Range("A1:D1").Style.Font.Bold = true;
            summary.Range("A1:D1").Style.Font.FontSize = 16;

            summary.Cell("A3").Value = "Rapor Tarihi";
            summary.Cell("B3").Value = DateTime.Now;
            summary.Cell("B3").Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
            summary.Cell("A4").Value = "Filtre";
            summary.Cell("B4").Value = StatisticsFilterDescription();
            summary.Range("B4:D4").Merge();

            var metrics = new (string Label, decimal Value)[]
            {
                ("Fatura Toplamı", result.PayableTotal),
                ("KDV", result.VatTotal),
                ("Damga", result.StampDutyTotal),
                ("Toplam Ödeme", result.PaymentTotal),
                ("Ortalama Fatura", result.AveragePayableAmount)
            };
            summary.Cell("A6").Value = "Arşiv Sayısı";
            summary.Cell("B6").Value = result.ArchiveCount;
            summary.Cell("C6").Value = "Fatura Sayısı";
            summary.Cell("D6").Value = result.InvoiceCount;
            summary.Cell("A7").Value = "Kurum Sayısı";
            summary.Cell("B7").Value = result.InstitutionCount;

            for (var index = 0; index < metrics.Length; index++)
            {
                var row = 9 + index;
                summary.Cell(row, 1).Value = metrics[index].Label;
                summary.Cell(row, 2).Value = metrics[index].Value;
                summary.Cell(row, 2).Style.NumberFormat.Format = "#,##0.00 [$₺-tr-TR]";
            }
            summary.Columns().AdjustToContents();

            var detail = workbook.Worksheets.Add("Detay");
            var headers = new[]
            {
                "Kurum", "Yıl", "Tür", "Adet", "Fatura Tutarı", "KDV", "Damga", "Ödeme"
            };
            for (var col = 0; col < headers.Length; col++)
            {
                detail.Cell(1, col + 1).Value = headers[col];
                detail.Cell(1, col + 1).Style.Font.Bold = true;
            }

            for (var index = 0; index < result.Rows.Count; index++)
            {
                var rowNumber = index + 2;
                var row = result.Rows[index];
                detail.Cell(rowNumber, 1).Value = row.InstitutionName;
                detail.Cell(rowNumber, 2).Value = row.Year;
                detail.Cell(rowNumber, 3).Value = row.InvoiceType;
                detail.Cell(rowNumber, 4).Value = row.InvoiceCount;
                detail.Cell(rowNumber, 5).Value = row.PayableTotal;
                detail.Cell(rowNumber, 6).Value = row.VatTotal;
                detail.Cell(rowNumber, 7).Value = row.StampDutyTotal;
                detail.Cell(rowNumber, 8).Value = row.PaymentTotal;
            }

            if (result.Rows.Count > 0)
                detail.Range(2, 5, result.Rows.Count + 1, 8).Style.NumberFormat.Format = "#,##0.00 [$₺-tr-TR]";
            detail.SheetView.FreezeRows(1);
            detail.RangeUsed()?.SetAutoFilter();
            detail.Columns().AdjustToContents();

            var monthly = workbook.Worksheets.Add("Aylık Trend");
            monthly.Cell("A1").Value = "Yıl";
            monthly.Cell("B1").Value = "Ay";
            monthly.Cell("C1").Value = "Fatura Sayısı";
            monthly.Cell("D1").Value = "Fatura Tutarı";
            monthly.Cell("E1").Value = "Ödeme";
            monthly.Range("A1:E1").Style.Font.Bold = true;

            for (var index = 0; index < result.MonthlyRows.Count; index++)
            {
                var rowNumber = index + 2;
                var row = result.MonthlyRows[index];
                monthly.Cell(rowNumber, 1).Value = row.Year;
                monthly.Cell(rowNumber, 2).Value = CultureInfo
                    .GetCultureInfo("tr-TR")
                    .DateTimeFormat
                    .GetMonthName(row.Month);
                monthly.Cell(rowNumber, 3).Value = row.InvoiceCount;
                monthly.Cell(rowNumber, 4).Value = row.PayableTotal;
                monthly.Cell(rowNumber, 5).Value = row.PaymentTotal;
            }
            if (result.MonthlyRows.Count > 0)
                monthly.Range(2, 4, result.MonthlyRows.Count + 1, 5).Style.NumberFormat.Format = "#,##0.00 [$₺-tr-TR]";
            monthly.Columns().AdjustToContents();

            workbook.SaveAs(dialog.FileName);

            MessageBox.Show(
                this,
                "Excel istatistik raporu kaydedildi.",
                "DoğruFatura - İstatistik",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ShowStatisticsExportError("Excel çıktısı kaydedilemedi", ex);
        }
    }

    private void ExportStatisticsPng_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetStatisticsResult(out _))
            return;

        var dialog = new SaveFileDialog
        {
            Title = "İstatistik grafiğini PNG olarak kaydet",
            Filter = "PNG görseli (*.png)|*.png",
            FileName = $"DogruFatura-Istatistik-Grafik-{DateTime.Now:yyyyMMdd-HHmm}.png",
            AddExtension = true,
            DefaultExt = ".png"
        };
        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            File.WriteAllBytes(
                dialog.FileName,
                CaptureStatisticsElementPng(StatisticsDonutCard, 2d));

            MessageBox.Show(
                this,
                "İstatistik grafiği PNG olarak kaydedildi.",
                "DoğruFatura - İstatistik",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ShowStatisticsExportError("PNG çıktısı kaydedilemedi", ex);
        }
    }

    private void ExportStatisticsPdf_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetStatisticsResult(out var result))
            return;

        var dialog = new SaveFileDialog
        {
            Title = "İstatistik PDF raporunu kaydet",
            Filter = "PDF dosyası (*.pdf)|*.pdf",
            FileName = $"DogruFatura-Istatistik-{DateTime.Now:yyyyMMdd-HHmm}.pdf",
            AddExtension = true,
            DefaultExt = ".pdf"
        };
        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            SaveStatisticsPdf(result, dialog.FileName);
            MessageBox.Show(
                this,
                "İstatistik PDF raporu kaydedildi.",
                "DoğruFatura - İstatistik",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ShowStatisticsExportError("PDF raporu kaydedilemedi", ex);
        }
    }

    private void PrintStatistics_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetStatisticsResult(out var result))
            return;

        try
        {
            var dialog = new PrintDialog();
            if (dialog.ShowDialog() != true)
                return;

            var document = BuildStatisticsPrintDocument(result);
            document.PageWidth = dialog.PrintableAreaWidth;
            document.PageHeight = dialog.PrintableAreaHeight;
            document.PagePadding = new Thickness(35);
            document.ColumnWidth = double.PositiveInfinity;

            dialog.PrintDocument(
                ((IDocumentPaginatorSource)document).DocumentPaginator,
                "DoğruFatura - İstatistik Raporu");
        }
        catch (Exception ex)
        {
            ShowStatisticsExportError("İstatistik raporu yazdırılamadı", ex);
        }
    }

    private void SaveStatisticsPdf(ArchiveStatisticsResult result, string filePath)
    {
        var donutPng = CaptureStatisticsElementPng(StatisticsDonutChart, 2d);
        var monthlyPng = CaptureStatisticsElementPng(StatisticsMonthlyTrendChart, 1.6d);
        var typePng = CaptureStatisticsElementPng(StatisticsTypeComparisonChart, 1.6d);

        using var document = new PdfDocument();
        document.Info.Title = "DoğruFatura İstatistik Raporu";
        document.Info.Author = "DoğruFatura";
        document.Info.Subject = StatisticsFilterDescription();
        document.Info.Creator = $"DoğruFatura {AppVersionInfo.InformationalVersion}";

        var page = document.AddPage();
        page.Size = PageSize.A4;
        page.Orientation = PageOrientation.Landscape;
        using (var graphics = XGraphics.FromPdfPage(page))
        {
            DrawStatisticsPdfHeader(graphics, page.Width.Point, result);
            DrawStatisticsPdfDonutAndDistribution(graphics, result, donutPng);
        }

        var chartsPage = document.AddPage();
        chartsPage.Size = PageSize.A4;
        chartsPage.Orientation = PageOrientation.Landscape;
        using (var graphics = XGraphics.FromPdfPage(chartsPage))
        {
            DrawStatisticsPdfCharts(
                graphics,
                chartsPage.Width.Point,
                monthlyPng,
                typePng);
        }

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);
        document.Save(filePath);
    }

    private void DrawStatisticsPdfHeader(
        XGraphics graphics,
        double pageWidth,
        ArchiveStatisticsResult result)
    {
        var navy = XColor.FromArgb(3, 28, 56);
        var muted = XColor.FromArgb(82, 98, 119);
        var green = XColor.FromArgb(36, 138, 80);

        graphics.DrawRectangle(new XSolidBrush(navy), 28, 22, pageWidth - 56, 46);
        graphics.DrawString(
            "DOĞRUFATURA İSTATİSTİK RAPORU",
            new XFont("Segoe UI", 17, XFontStyleEx.Bold),
            XBrushes.White,
            new XRect(42, 28, 470, 28),
            XStringFormats.CenterLeft);
        graphics.DrawString(
            DateTime.Now.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR")),
            new XFont("Segoe UI", 9, XFontStyleEx.Regular),
            new XSolidBrush(XColor.FromArgb(216, 228, 239)),
            new XRect(pageWidth - 240, 32, 190, 20),
            XStringFormats.CenterRight);

        graphics.DrawString(
            StatisticsFilterDescription(),
            new XFont("Segoe UI", 9, XFontStyleEx.Regular),
            new XSolidBrush(muted),
            new XRect(35, 76, pageWidth - 70, 22),
            XStringFormats.CenterLeft);

        var summary =
            $"{result.ArchiveCount} arşiv   •   {result.InvoiceCount} fatura   •   {result.InstitutionCount} kurum"
            + $"   •   Ortalama fatura: {NumberParser.Money(result.AveragePayableAmount)}"
            + $"   •   Toplam ödeme: {NumberParser.Money(result.PaymentTotal)}";

        graphics.DrawString(
            summary,
            new XFont("Segoe UI", 10, XFontStyleEx.Bold),
            new XSolidBrush(green),
            new XRect(35, 99, pageWidth - 70, 24),
            XStringFormats.CenterLeft);

        graphics.DrawString(
            $"Fatura: {NumberParser.Money(result.PayableTotal)}   •   KDV: {NumberParser.Money(result.VatTotal)}"
            + $"   •   Damga: {NumberParser.Money(result.StampDutyTotal)}   •   Ödeme: {NumberParser.Money(result.PaymentTotal)}",
            new XFont("Segoe UI", 9.5, XFontStyleEx.Regular),
            new XSolidBrush(navy),
            new XRect(35, 123, pageWidth - 70, 22),
            XStringFormats.CenterLeft);
    }

    private void DrawStatisticsPdfDonutAndDistribution(
        XGraphics graphics,
        ArchiveStatisticsResult result,
        byte[] donutPng)
    {
        using var stream = new MemoryStream(donutPng);
        using var image = XImage.FromStream(stream);
        graphics.DrawImage(image, 35, 157, 360, 360);

        var navy = XColor.FromArgb(3, 28, 56);
        var muted = XColor.FromArgb(82, 98, 119);
        var headingFont = new XFont("Segoe UI", 12, XFontStyleEx.Bold);
        var rowFont = new XFont("Segoe UI", 10, XFontStyleEx.Regular);
        var rowBold = new XFont("Segoe UI", 10, XFontStyleEx.Bold);

        graphics.DrawString(
            "FATURA TÜRÜ DAĞILIMI",
            headingFont,
            new XSolidBrush(navy),
            new XRect(430, 160, 350, 24),
            XStringFormats.CenterLeft);

        var distribution = StatisticsDistribution(result);
        var total = distribution.Sum(item => item.PaymentTotal);
        var y = 198d;
        foreach (var item in distribution)
        {
            var percent = total <= 0m ? 0m : item.PaymentTotal / total * 100m;
            graphics.DrawString(
                item.InvoiceType,
                rowBold,
                new XSolidBrush(navy),
                new XRect(430, y, 150, 20),
                XStringFormats.CenterLeft);
            graphics.DrawString(
                $"{item.InvoiceCount} fatura",
                rowFont,
                new XSolidBrush(muted),
                new XRect(565, y, 90, 20),
                XStringFormats.CenterLeft);
            graphics.DrawString(
                NumberParser.Money(item.PaymentTotal),
                rowBold,
                new XSolidBrush(navy),
                new XRect(640, y, 120, 20),
                XStringFormats.CenterRight);
            graphics.DrawString(
                "%" + percent.ToString("N1", CultureInfo.GetCultureInfo("tr-TR")),
                rowFont,
                new XSolidBrush(muted),
                new XRect(765, y, 45, 20),
                XStringFormats.CenterRight);
            y += 32;
        }

        graphics.DrawString(
            $"PDF sağlığı: {StatisticsPdfHealthText.Text}",
            rowFont,
            new XSolidBrush(muted),
            new XRect(430, 420, 360, 20),
            XStringFormats.CenterLeft);
        graphics.DrawString(
            $"Önceki dönem: {StatisticsPreviousPeriodText.Text}",
            rowFont,
            new XSolidBrush(muted),
            new XRect(430, 445, 360, 20),
            XStringFormats.CenterLeft);
        graphics.DrawString(
            $"Kurum profili: {StatisticsInstitutionProfileText.Text}",
            rowFont,
            new XSolidBrush(muted),
            new XRect(430, 470, 360, 35),
            new XStringFormat
            {
                Alignment = XStringAlignment.Near,
                LineAlignment = XLineAlignment.Near
            });
    }

    private static void DrawStatisticsPdfCharts(
        XGraphics graphics,
        double pageWidth,
        byte[] monthlyPng,
        byte[] typePng)
    {
        var navy = XColor.FromArgb(3, 28, 56);
        graphics.DrawString(
            "AYLIK ÖDEME TRENDİ",
            new XFont("Segoe UI", 13, XFontStyleEx.Bold),
            new XSolidBrush(navy),
            new XRect(35, 30, 350, 22),
            XStringFormats.CenterLeft);
        graphics.DrawString(
            "FATURA TÜRÜ KARŞILAŞTIRMASI",
            new XFont("Segoe UI", 13, XFontStyleEx.Bold),
            new XSolidBrush(navy),
            new XRect(pageWidth / 2 + 10, 30, 360, 22),
            XStringFormats.CenterLeft);

        using var monthlyStream = new MemoryStream(monthlyPng);
        using var monthlyImage = XImage.FromStream(monthlyStream);
        graphics.DrawImage(monthlyImage, 35, 62, 365, 420);

        using var typeStream = new MemoryStream(typePng);
        using var typeImage = XImage.FromStream(typeStream);
        graphics.DrawImage(typeImage, pageWidth / 2 + 10, 62, 365, 420);
    }

    private FlowDocument BuildStatisticsPrintDocument(ArchiveStatisticsResult result)
    {
        var document = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 11
        };

        document.Blocks.Add(new Paragraph(new Run("DoğruFatura İstatistik Raporu"))
        {
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(3, 28, 56))
        });
        document.Blocks.Add(new Paragraph(new Run(
            DateTime.Now.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR"))
            + " • " + StatisticsFilterDescription()))
        {
            Foreground = new SolidColorBrush(Color.FromRgb(82, 98, 119))
        });
        document.Blocks.Add(new Paragraph(new Run(
            $"{result.ArchiveCount} arşiv • {result.InvoiceCount} fatura • {result.InstitutionCount} kurum"
            + $" • Fatura: {NumberParser.Money(result.PayableTotal)}"
            + $" • Ödeme: {NumberParser.Money(result.PaymentTotal)}"))
        {
            FontWeight = FontWeights.SemiBold
        });

        var donutImage = new System.Windows.Controls.Image
        {
            Source = CaptureStatisticsElementBitmap(StatisticsDonutCard, 1.3d),
            Width = 650,
            Stretch = Stretch.Uniform
        };
        document.Blocks.Add(new BlockUIContainer(donutImage));

        var table = new Table { CellSpacing = 0 };
        for (var i = 0; i < 4; i++)
            table.Columns.Add(new TableColumn());
        var group = new TableRowGroup();
        table.RowGroups.Add(group);
        group.Rows.Add(StatisticsPrintRow("Tür", "Adet", "Ödeme", "Pay", true));

        var distribution = StatisticsDistribution(result);
        var total = distribution.Sum(item => item.PaymentTotal);
        foreach (var item in distribution)
        {
            var percent = total <= 0m ? 0m : item.PaymentTotal / total * 100m;
            group.Rows.Add(StatisticsPrintRow(
                item.InvoiceType,
                item.InvoiceCount.ToString(CultureInfo.InvariantCulture),
                NumberParser.Money(item.PaymentTotal),
                "%" + percent.ToString("N1", CultureInfo.GetCultureInfo("tr-TR")),
                false));
        }
        document.Blocks.Add(table);

        return document;
    }

    private static TableRow StatisticsPrintRow(
        string first,
        string second,
        string third,
        string fourth,
        bool header)
    {
        var row = new TableRow();
        foreach (var value in new[] { first, second, third, fourth })
        {
            row.Cells.Add(new TableCell(new Paragraph(new Run(value)))
            {
                Padding = new Thickness(6),
                FontWeight = header ? FontWeights.Bold : FontWeights.Normal,
                BorderBrush = new SolidColorBrush(Color.FromRgb(220, 229, 238)),
                BorderThickness = new Thickness(0.5)
            });
        }
        return row;
    }

    private bool TryGetStatisticsResult(out ArchiveStatisticsResult result)
    {
        ApplyStatisticsFilters();
        result = _statisticsResult!;
        if (result.InvoiceCount > 0)
            return true;

        MessageBox.Show(
            this,
            "Seçili filtrelerde çıktı alınabilecek istatistik verisi bulunmuyor.",
            "DoğruFatura - İstatistik",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        return false;
    }

    private string StatisticsFilterDescription()
    {
        var parts = new List<string>();
        parts.Add(_statisticsFilter.Year.HasValue
            ? $"Yıl: {_statisticsFilter.Year.Value}"
            : "Yıl: Tümü");
        parts.Add(string.IsNullOrWhiteSpace(_statisticsFilter.InstitutionName)
            ? "Kurum: Tümü"
            : $"Kurum: {_statisticsFilter.InstitutionName}");
        parts.Add(string.IsNullOrWhiteSpace(_statisticsFilter.InvoiceType)
            ? "Tür: Tümü"
            : $"Tür: {_statisticsFilter.InvoiceType}");

        if (_statisticsFilter.StartDate.HasValue || _statisticsFilter.EndDate.HasValue)
        {
            var from = _statisticsFilter.StartDate?.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR")) ?? "—";
            var to = _statisticsFilter.EndDate?.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR")) ?? "—";
            parts.Add($"Tarih: {from} – {to}");
        }

        return string.Join(" • ", parts);
    }

    private StatisticsDonutItem[] StatisticsDistribution(ArchiveStatisticsResult result) =>
        result.Rows
            .GroupBy(row => row.InvoiceType, StringComparer.OrdinalIgnoreCase)
            .Select(group => new StatisticsDonutItem(
                group.Key,
                group.Sum(row => row.InvoiceCount),
                group.Sum(row => row.PaymentTotal)))
            .OrderBy(item => StatisticsInvoiceTypeOrder(item.InvoiceType))
            .ThenBy(item => item.InvoiceType, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static byte[] CaptureStatisticsElementPng(
        FrameworkElement element,
        double scale)
    {
        var bitmap = CaptureStatisticsElementBitmap(element, scale);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    private static BitmapSource CaptureStatisticsElementBitmap(
        FrameworkElement element,
        double scale)
    {
        element.UpdateLayout();
        var width = Math.Max(1, (int)Math.Ceiling(element.ActualWidth * scale));
        var height = Math.Max(1, (int)Math.Ceiling(element.ActualHeight * scale));
        var bitmap = new RenderTargetBitmap(
            width,
            height,
            96d * scale,
            96d * scale,
            PixelFormats.Pbgra32);
        bitmap.Render(element);
        bitmap.Freeze();
        return bitmap;
    }

    private static string StatisticsExportTimestamp() =>
        DateTime.Now.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture);

    private static string CsvCell(string value)
    {
        var safe = value ?? string.Empty;
        if (safe.Length > 0 && safe[0] is '=' or '+' or '-' or '@')
            safe = "'" + safe;
        return """ + safe.Replace(""", """", StringComparison.Ordinal) + """;
    }

    private void ShowStatisticsExportError(string title, Exception ex)
    {
        MessageBox.Show(
            this,
            title + ":\n\n" + ex.Message,
            "DoğruFatura - İstatistik",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
