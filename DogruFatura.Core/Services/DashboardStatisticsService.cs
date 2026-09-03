using DogruFatura.Core.Models;

namespace DogruFatura.Core.Services;

public static class DashboardStatisticsService
{
    private static readonly string[] InvoiceTypeOrder = { "Elektrik", "Telefon", "İnternet", "Su" };

    public static DashboardStatistics Build(
        IEnumerable<ArchivedInvoiceList> archivedLists,
        DateTime today)
    {
        ArgumentNullException.ThrowIfNull(archivedLists);

        var archives = archivedLists
            .Where(archive => archive.Invoices is not null && archive.IsStatisticsEligible)
            .ToArray();
        var archivedInvoices = archives
            .SelectMany(archive => archive.Invoices)
            .ToArray();
        var allInvoiceBuffer = new List<InvoiceRecord>();
        var knownIds = new HashSet<Guid>();
        foreach (var invoice in archivedInvoices)
        {
            if (invoice.Id != Guid.Empty && !knownIds.Add(invoice.Id)) continue;
            allInvoiceBuffer.Add(invoice);
        }
        var allInvoices = allInvoiceBuffer.ToArray();

        var referenceDate = today.Date;
        var periodStart = new DateTime(referenceDate.Year, referenceDate.Month, 1);
        var nextPeriodStart = periodStart.AddMonths(1);
        var monthlyInvoices = allInvoices
            .Where(invoice => invoice.InvoiceDate >= periodStart && invoice.InvoiceDate < nextPeriodStart)
            .ToArray();

        var typeDistribution = InvoiceTypeOrder
            .Select(invoiceType => new DashboardDistributionItem(
                invoiceType,
                allInvoices.Count(invoice => string.Equals(invoice.InvoiceType, invoiceType, StringComparison.Ordinal)),
                allInvoices
                    .Where(invoice => string.Equals(invoice.InvoiceType, invoiceType, StringComparison.Ordinal))
                    .Sum(invoice => invoice.PaymentAmount)))
            .Where(item => item.InvoiceCount > 0)
            .ToArray();

        var monthlyTrend = Enumerable.Range(-5, 6)
            .Select(monthOffset => periodStart.AddMonths(monthOffset))
            .Select(month =>
            {
                var nextMonth = month.AddMonths(1);
                var invoices = allInvoices
                    .Where(invoice => invoice.InvoiceDate >= month && invoice.InvoiceDate < nextMonth)
                    .ToArray();
                return new DashboardMonthlyItem(
                    month,
                    invoices.Length,
                    invoices.Sum(invoice => invoice.PaymentAmount));
            })
            .ToArray();

        return new DashboardStatistics(
            periodStart,
            monthlyInvoices.Length,
            monthlyInvoices.Sum(invoice => invoice.PayableAmount),
            monthlyInvoices.Sum(invoice => invoice.StampDuty),
            monthlyInvoices.Sum(invoice => invoice.PaymentAmount),
            allInvoices.Length,
            allInvoices
                .Where(invoice => !string.IsNullOrWhiteSpace(invoice.InstitutionName))
                .Select(invoice => invoice.InstitutionName.Trim())
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .Count(),
            typeDistribution,
            monthlyTrend);
    }
}
