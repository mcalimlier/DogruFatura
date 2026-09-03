using DogruFatura.Core.Models;
using System.Globalization;

namespace DogruFatura.Wpf;

public sealed record ReportSignature(
    string Role,
    string Name,
    string Title);

public sealed record FinalReportData(
    IReadOnlyList<InvoiceRecord> Invoices,
    string InvoiceType,
    DateTime ReportDate,
    ReportSignature RealizationOfficer,
    ReportSignature SpendingAuthority)
{
    public decimal PayableTotal => Invoices.Sum(invoice => invoice.PayableAmount);
    public decimal VatTotal => Invoices.Sum(invoice => invoice.VatAmount);
    public decimal StampDutyTotal => Invoices.Sum(invoice => invoice.StampDuty);
    public decimal PaymentTotal => Invoices.Sum(invoice => invoice.PaymentAmount);
}

public static class FinalReportFormat
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    public static string Money(decimal value) =>
        "₺ " + Math.Round(value, 2, MidpointRounding.AwayFromZero).ToString("N2", Tr);

    public static string Date(DateTime value) => value.ToString("dd.MM.yyyy", Tr);

    public static string Upper(string value) => value.ToUpper(Tr);
}
