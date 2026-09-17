using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;

namespace DogruFatura.Wpf;

public sealed class FinalReportDocumentService
{
    public const double PageWidth = 1122.24;
    public const double PageHeight = 793.92;

    private static double MarginX => FinalReportLayout.SideMarginDips;
    private static double TableTop => FinalReportLayout.TableTopDips;
    private static double HeaderHeight => FinalReportLayout.HeaderHeightDips;
    private static double RowHeight => FinalReportLayout.RowHeightDips;
    private static double TotalHeight => FinalReportLayout.TotalHeightDips;

    private static readonly Brush Navy = BrushFrom("#031C38");
    private static readonly Brush Blue = BrushFrom("#0D6DA6");
    private static readonly Brush HeaderBlue = BrushFrom("#92CFF2");
    private static readonly Brush PaleBlue = BrushFrom("#DCEFFC");
    private static readonly Brush GridBlue = BrushFrom("#5DB3E8");
    private static readonly Brush Gold = BrushFrom("#FFB41F");
    private static readonly Brush Muted = BrushFrom("#526277");
    private static readonly Brush Light = BrushFrom("#D8E4EF");
    private readonly Size _pageSize = new(PageWidth, PageHeight);

    public FixedDocument Build(FinalReportData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var slices = FinalReportPagePlanner.Plan(data.Invoices.Count);
        var document = new FixedDocument();
        document.DocumentPaginator.PageSize = _pageSize;

        for (var pageIndex = 0; pageIndex < slices.Count; pageIndex++)
        {
            var fixedPage = BuildPage(data, slices[pageIndex], pageIndex + 1, slices.Count);
            var pageContent = new PageContent();
            ((IAddChild)pageContent).AddChild(fixedPage);
            document.Pages.Add(pageContent);
        }

        return document;
    }

    private static FixedPage BuildPage(
        FinalReportData data,
        FinalReportPageSlice slice,
        int pageNumber,
        int pageCount)
    {
        var page = new FixedPage
        {
            Width = PageWidth,
            Height = PageHeight,
            Background = Brushes.White
        };
        var canvas = new Canvas { Width = PageWidth, Height = PageHeight, Background = Brushes.White };
        page.Children.Add(canvas);

        var tableWidth = PageWidth - (MarginX * 2);
        AddRectangle(canvas, MarginX, 20, tableWidth, 50, Navy, 8);
        AddRectangle(canvas, MarginX, 20, 142, 50, Gold, 8);
        AddText(canvas, FinalReportFormat.Upper(data.InvoiceType),
            MarginX + 8, 29, 126, 30, 14.5, FontWeights.Bold, Navy, TextAlignment.Center);
        AddText(canvas, "FATURA ÖDEME LİSTESİ",
            MarginX + 162, 25, 500, 25, 17.5, FontWeights.Bold, Brushes.White, TextAlignment.Left);
        AddText(canvas, "DoğruFatura • Resmî ödeme belgesi",
            MarginX + 162, 49, 430, 15, 8.8, FontWeights.SemiBold, Light, TextAlignment.Left);
        AddText(canvas,
            $"{FinalReportFormat.Date(data.ReportDate)}   •   Sayfa {pageNumber}/{pageCount}",
            PageWidth - MarginX - 315, 32, 295, 24, 9.4, FontWeights.SemiBold, Light, TextAlignment.Right);

        var columnWidths = FinalReportLayout.CalculateColumnWidths(tableWidth);
        var x = MarginX;
        for (var column = 0; column < FinalReportLayout.Headers.Length; column++)
        {
            AddCell(canvas, FinalReportLayout.Headers[column], x, TableTop, columnWidths[column], HeaderHeight,
                HeaderBlue, Navy, 9.0, FontWeights.Bold, TextAlignment.Center, true);
            x += columnWidths[column];
        }

        var y = TableTop + HeaderHeight;
        for (var localIndex = 0; localIndex < slice.Count; localIndex++)
        {
            var invoice = data.Invoices[slice.StartIndex + localIndex];
            var values = RowValues(invoice, slice.StartIndex + localIndex + 1);
            x = MarginX;
            for (var column = 0; column < values.Length; column++)
            {
                var alignment = column >= 6 ? TextAlignment.Right : TextAlignment.Center;
                if (column == 1) alignment = TextAlignment.Left;
                AddCell(canvas, values[column], x, y, columnWidths[column], RowHeight,
                    Brushes.White, Brushes.Black, column is 1 or 3 ? 7.9 : 8.25,
                    FontWeights.Normal, alignment, false);
                x += columnWidths[column];
            }
            y += RowHeight;
        }

        if (slice.IsFinalPage)
        {
            AddTotals(canvas, data, y, columnWidths);
            AddSignatures(canvas, data, y + TotalHeight + 28);
        }

        AddText(canvas, "DoğruFatura • Excel / Office bağımlılığı olmadan oluşturuldu",
            MarginX, PageHeight - 31, 620, 16, 8.4, FontWeights.Normal, Muted, TextAlignment.Left);
        AddText(canvas, $"Sayfa {pageNumber} / {pageCount}",
            PageWidth - MarginX - 180, PageHeight - 31, 180, 16, 8.4,
            FontWeights.SemiBold, Blue, TextAlignment.Right);

        return page;
    }

    private static void AddTotals(Canvas canvas, FinalReportData data, double y, double[] widths)
    {
        var labelWidth = widths.Take(6).Sum();
        AddCell(canvas, "FATURA TOPLAMI:", MarginX, y, labelWidth, TotalHeight,
            PaleBlue, Navy, 9.5, FontWeights.Bold, TextAlignment.Right, false);

        var totals = new[] { data.PayableTotal, data.VatTotal, data.StampDutyTotal, data.PaymentTotal };
        var x = MarginX + labelWidth;
        for (var index = 0; index < totals.Length; index++)
        {
            AddCell(canvas, FinalReportFormat.Money(totals[index]), x, y, widths[index + 6], TotalHeight,
                PaleBlue, Navy, 9.3, FontWeights.Bold, TextAlignment.Right, false);
            x += widths[index + 6];
        }
    }

    private static void AddSignatures(Canvas canvas, FinalReportData data, double y)
    {
        var blockWidth = 330.0;
        var leftX = MarginX + 85;
        var rightX = PageWidth - MarginX - blockWidth - 85;
        AddSignature(canvas, data.RealizationOfficer, data.ReportDate, leftX, y, blockWidth);
        AddSignature(canvas, data.SpendingAuthority, data.ReportDate, rightX, y, blockWidth);
    }

    private static void AddSignature(
        Canvas canvas,
        ReportSignature signature,
        DateTime reportDate,
        double x,
        double y,
        double width)
    {
        AddText(canvas, signature.Role, x, y, width, 22, 11, FontWeights.SemiBold, Navy, TextAlignment.Center);
        AddText(canvas, FinalReportFormat.Date(reportDate), x, y + 23, width, 18, 9.5, FontWeights.Normal, Muted, TextAlignment.Center);
        AddText(canvas, signature.Name, x, y + 77, width, 22, 11, FontWeights.SemiBold, Navy, TextAlignment.Center);
        AddText(canvas, signature.Title, x, y + 100, width, 22, 10, FontWeights.Normal, Navy, TextAlignment.Center);
    }

    private static string[] RowValues(InvoiceRecord invoice, int sequenceNumber) =>
        new[]
        {
            sequenceNumber.ToString(CultureInfo.CurrentCulture),
            invoice.InstitutionName,
            invoice.ServiceNumber,
            invoice.InvoiceNumber,
            FinalReportFormat.Date(invoice.InvoiceDate),
            FinalReportFormat.Date(invoice.DueDate),
            FinalReportFormat.Money(invoice.PayableAmount),
            FinalReportFormat.Money(invoice.VatAmount),
            FinalReportFormat.Money(invoice.StampDuty),
            FinalReportFormat.Money(invoice.PaymentAmount)
        };

    private static void AddCell(
        Canvas canvas,
        string text,
        double x,
        double y,
        double width,
        double height,
        Brush background,
        Brush foreground,
        double fontSize,
        FontWeight fontWeight,
        TextAlignment alignment,
        bool wrap)
    {
        var border = new Border
        {
            Width = width,
            Height = height,
            Background = background,
            BorderBrush = GridBlue,
            BorderThickness = new Thickness(0.55),
            Padding = new Thickness(alignment == TextAlignment.Left ? 5 : 3, 1,
                alignment == TextAlignment.Right ? 5 : 3, 1),
            Child = new TextBlock
            {
                Text = text,
                Foreground = foreground,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = fontSize,
                FontWeight = fontWeight,
                TextAlignment = alignment,
                TextWrapping = wrap ? TextWrapping.Wrap : TextWrapping.NoWrap,
                TextTrimming = wrap ? TextTrimming.None : TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
                LineHeight = fontSize + 1.0
            }
        };
        Canvas.SetLeft(border, x);
        Canvas.SetTop(border, y);
        canvas.Children.Add(border);
    }

    private static void AddText(
        Canvas canvas,
        string text,
        double x,
        double y,
        double width,
        double height,
        double fontSize,
        FontWeight fontWeight,
        Brush foreground,
        TextAlignment alignment)
    {
        var block = new TextBlock
        {
            Text = text,
            Width = width,
            Height = height,
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = fontSize,
            FontWeight = fontWeight,
            Foreground = foreground,
            TextAlignment = alignment,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        Canvas.SetLeft(block, x);
        Canvas.SetTop(block, y);
        canvas.Children.Add(block);
    }

    private static void AddRectangle(
        Canvas canvas,
        double x,
        double y,
        double width,
        double height,
        Brush fill,
        double cornerRadius = 0)
    {
        var border = new Border
        {
            Width = width,
            Height = height,
            Background = fill,
            CornerRadius = new CornerRadius(cornerRadius)
        };
        Canvas.SetLeft(border, x);
        Canvas.SetTop(border, y);
        canvas.Children.Add(border);
    }

    private static SolidColorBrush BrushFrom(string color) =>
        new((Color)ColorConverter.ConvertFromString(color));
}
