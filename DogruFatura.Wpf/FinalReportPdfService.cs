using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.IO;

namespace DogruFatura.Wpf;

public sealed class FinalReportPdfService
{
    private const double MarginX = 28.5;
    private const double TableTop = 58.5;
    private const double HeaderHeight = 27;
    private const double RowHeight = 20.25;
    private const double TotalHeight = 23.25;
    private static readonly double[] ColumnWeights =
        { 4.43, 24.86, 22.57, 17.86, 13.29, 18.29, 16.57, 22.14, 14.86, 14.29 };
    private static readonly string[] Headers =
        { "No", "Okul Adı", "Hizmet Numarası", "Fatura No", "Fatura Tarihi", "Son Ödeme Tarihi", "Ödenecek Tutar", "KDV Tutarı", "Damga Vergisi", "Ödeme Tutarı" };

    private static readonly XColor Navy = XColor.FromArgb(3, 28, 56);
    private static readonly XColor Blue = XColor.FromArgb(13, 109, 166);
    private static readonly XColor HeaderBlue = XColor.FromArgb(146, 207, 242);
    private static readonly XColor PaleBlue = XColor.FromArgb(220, 239, 252);
    private static readonly XColor GridBlue = XColor.FromArgb(93, 179, 232);
    private static readonly XColor Gold = XColor.FromArgb(255, 180, 31);
    private static readonly XColor Muted = XColor.FromArgb(82, 98, 119);
    private static readonly XColor Light = XColor.FromArgb(216, 228, 239);

    public void Save(FinalReportData data, string filePath)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("PDF dosya yolu boş olamaz.", nameof(filePath));

        var slices = FinalReportPagePlanner.Plan(data.Invoices.Count);
        if (slices.Count == 0)
            throw new InvalidOperationException("Boş aktif listeden PDF oluşturulamaz.");

        var document = new PdfDocument();
        document.Info.Title = $"{data.InvoiceType} Faturaları Ödeme Listesi";
        document.Info.Author = "DoğruFatura";
        document.Info.Subject = "Fatura ödeme ve kontrol listesi";
        document.Info.Creator = "DoğruFatura 1.0";

        for (var pageIndex = 0; pageIndex < slices.Count; pageIndex++)
        {
            var page = document.AddPage();
            page.Size = PageSize.A4;
            page.Orientation = PageOrientation.Landscape;
            using var graphics = XGraphics.FromPdfPage(page);
            DrawPage(graphics, page.Width.Point, page.Height.Point, data,
                slices[pageIndex], pageIndex + 1, slices.Count);
        }

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);
        document.Save(filePath);
    }

    private static void DrawPage(
        XGraphics graphics,
        double pageWidth,
        double pageHeight,
        FinalReportData data,
        FinalReportPageSlice slice,
        int pageNumber,
        int pageCount)
    {
        var tableWidth = pageWidth - (MarginX * 2);
        graphics.DrawRectangle(new XSolidBrush(Navy), MarginX, 15, tableWidth, 37.5);
        graphics.DrawRectangle(new XSolidBrush(Gold), MarginX, 15, 106.5, 37.5);
        DrawText(graphics, FinalReportFormat.Upper(data.InvoiceType),
            new XRect(MarginX + 6, 21.75, 94.5, 22.5), 10.8, true, Navy, XStringAlignment.Center);
        DrawText(graphics, "FATURA ÖDEME LİSTESİ",
            new XRect(MarginX + 121.5, 18.75, 375, 18.75), 13.1, true, XColors.White, XStringAlignment.Near);
        DrawText(graphics, "DoğruFatura • Resmî ödeme belgesi",
            new XRect(MarginX + 121.5, 36.75, 322.5, 11.25), 6.6, true, Light, XStringAlignment.Near);
        DrawText(graphics,
            $"{FinalReportFormat.Date(data.ReportDate)}   •   Sayfa {pageNumber}/{pageCount}",
            new XRect(pageWidth - MarginX - 236.25, 24, 221.25, 18), 7.1, true, Light, XStringAlignment.Far);

        var widths = CalculateColumnWidths(tableWidth);
        var x = MarginX;
        for (var column = 0; column < Headers.Length; column++)
        {
            DrawCell(graphics, Headers[column], new XRect(x, TableTop, widths[column], HeaderHeight),
                HeaderBlue, Navy, 6.8, true, XStringAlignment.Center);
            x += widths[column];
        }

        var y = TableTop + HeaderHeight;
        for (var localIndex = 0; localIndex < slice.Count; localIndex++)
        {
            var invoice = data.Invoices[slice.StartIndex + localIndex];
            var values = RowValues(invoice, slice.StartIndex + localIndex + 1);
            x = MarginX;
            for (var column = 0; column < values.Length; column++)
            {
                var alignment = column >= 6 ? XStringAlignment.Far : XStringAlignment.Center;
                if (column == 1) alignment = XStringAlignment.Near;
                DrawCell(graphics, values[column], new XRect(x, y, widths[column], RowHeight),
                    XColors.White, XColors.Black, column is 1 or 3 ? 6.1 : 6.4,
                    false, alignment);
                x += widths[column];
            }
            y += RowHeight;
        }

        if (slice.IsFinalPage)
        {
            DrawTotals(graphics, data, y, widths);
            DrawSignatures(graphics, pageWidth, data, y + TotalHeight + 21);
        }

        DrawText(graphics, "DoğruFatura • Excel / Office bağımlılığı olmadan oluşturuldu",
            new XRect(MarginX, pageHeight - 23, 460, 12), 6.4, false, Muted, XStringAlignment.Near);
        DrawText(graphics, $"Sayfa {pageNumber} / {pageCount}",
            new XRect(pageWidth - MarginX - 135, pageHeight - 23, 135, 12),
            6.4, true, Blue, XStringAlignment.Far);
    }

    private static void DrawTotals(XGraphics graphics, FinalReportData data, double y, double[] widths)
    {
        var labelWidth = widths.Take(6).Sum();
        DrawCell(graphics, "FATURA TOPLAMI:", new XRect(MarginX, y, labelWidth, TotalHeight),
            PaleBlue, Navy, 7.2, true, XStringAlignment.Far);

        var totals = new[] { data.PayableTotal, data.VatTotal, data.StampDutyTotal, data.PaymentTotal };
        var x = MarginX + labelWidth;
        for (var index = 0; index < totals.Length; index++)
        {
            DrawCell(graphics, FinalReportFormat.Money(totals[index]),
                new XRect(x, y, widths[index + 6], TotalHeight),
                PaleBlue, Navy, 7.0, true, XStringAlignment.Far);
            x += widths[index + 6];
        }
    }

    private static void DrawSignatures(XGraphics graphics, double pageWidth, FinalReportData data, double y)
    {
        const double blockWidth = 247.5;
        var leftX = MarginX + 63.75;
        var rightX = pageWidth - MarginX - blockWidth - 63.75;
        DrawSignature(graphics, data.RealizationOfficer, data.ReportDate, leftX, y, blockWidth);
        DrawSignature(graphics, data.SpendingAuthority, data.ReportDate, rightX, y, blockWidth);
    }

    private static void DrawSignature(
        XGraphics graphics,
        ReportSignature signature,
        DateTime reportDate,
        double x,
        double y,
        double width)
    {
        DrawText(graphics, signature.Role, new XRect(x, y, width, 17), 8.2, true, Navy, XStringAlignment.Center);
        DrawText(graphics, FinalReportFormat.Date(reportDate), new XRect(x, y + 17, width, 14), 7.2, false, Muted, XStringAlignment.Center);
        DrawText(graphics, signature.Name, new XRect(x, y + 58, width, 17), 8.2, true, Navy, XStringAlignment.Center);
        DrawText(graphics, signature.Title, new XRect(x, y + 75, width, 17), 7.5, false, Navy, XStringAlignment.Center);
    }

    private static string[] RowValues(InvoiceRecord invoice, int sequenceNumber) =>
        new[]
        {
            sequenceNumber.ToString(),
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

    private static double[] CalculateColumnWidths(double tableWidth)
    {
        var totalWeight = ColumnWeights.Sum();
        return ColumnWeights.Select(weight => tableWidth * weight / totalWeight).ToArray();
    }

    private static void DrawCell(
        XGraphics graphics,
        string text,
        XRect rectangle,
        XColor background,
        XColor foreground,
        double fontSize,
        bool bold,
        XStringAlignment alignment)
    {
        graphics.DrawRectangle(new XSolidBrush(background), rectangle);
        graphics.DrawRectangle(new XPen(GridBlue, 0.45), rectangle);
        var inset = alignment == XStringAlignment.Center ? 2 : 4;
        DrawText(graphics, text,
            new XRect(rectangle.X + inset, rectangle.Y + 1, rectangle.Width - (inset * 2), rectangle.Height - 2),
            fontSize, bold, foreground, alignment);
    }

    private static void DrawText(
        XGraphics graphics,
        string text,
        XRect rectangle,
        double fontSize,
        bool bold,
        XColor color,
        XStringAlignment alignment)
    {
        var font = FitFont(graphics, text, rectangle.Width, fontSize, bold);
        var format = new XStringFormat
        {
            Alignment = alignment,
            LineAlignment = XLineAlignment.Center
        };

        var state = graphics.Save();
        graphics.IntersectClip(rectangle);
        graphics.DrawString(text, font, new XSolidBrush(color), rectangle, format);
        graphics.Restore(state);
    }

    private static XFont FitFont(
        XGraphics graphics,
        string text,
        double availableWidth,
        double requestedSize,
        bool bold)
    {
        var style = bold ? XFontStyleEx.Bold : XFontStyleEx.Regular;
        var size = requestedSize;
        var font = new XFont("Segoe UI", size, style);
        while (size > 4.5 && graphics.MeasureString(text, font).Width > availableWidth)
        {
            size -= 0.25;
            font = new XFont("Segoe UI", size, style);
        }
        return font;
    }
}
