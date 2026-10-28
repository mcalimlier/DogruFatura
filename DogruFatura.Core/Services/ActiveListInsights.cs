using DogruFatura.Core.Models;
using System.Globalization;

namespace DogruFatura.Core.Services;

public sealed record ActiveListInsights(int OverdueCount, decimal OverdueInvoiceTotal, int InstitutionCount)
{
    public static ActiveListInsights Build(IEnumerable<InvoiceRecord> invoices, DateTime today)
    {
        ArgumentNullException.ThrowIfNull(invoices);
        var records = invoices.ToArray();
        var overdue = records.Where(invoice => invoice.DueDate.Date < today.Date).ToArray();
        var names = records.Where(invoice => !string.IsNullOrWhiteSpace(invoice.InstitutionName))
            .Select(invoice => invoice.InstitutionType.Trim() + "|" + invoice.InstitutionName.Trim())
            .Distinct(StringComparer.Create(CultureInfo.GetCultureInfo("tr-TR"), true));
        return new(overdue.Length, overdue.Sum(invoice => invoice.PayableAmount), names.Count());
    }
}
