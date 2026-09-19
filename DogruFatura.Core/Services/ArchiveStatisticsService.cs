using DogruFatura.Core.Models;

namespace DogruFatura.Core.Services;

public static class ArchiveStatisticsService
{
    public static ArchiveStatisticsResult Build(
        IEnumerable<ArchivedInvoiceList> archivedLists,
        ArchiveStatisticsFilter? filter = null)
    {
        ArgumentNullException.ThrowIfNull(archivedLists);
        filter ??= new ArchiveStatisticsFilter();

        var eligibleArchives = archivedLists
            .Where(archive => archive.IsStatisticsEligible && archive.Invoices is not null)
            .ToArray();

        var source = eligibleArchives
            .SelectMany(archive => archive.Invoices.Select(invoice => new ArchiveInvoice(archive.Id, invoice)))
            .GroupBy(item => item.Invoice.Id)
            .Select(group => group.First())
            .ToArray();

        var availableYears = source
            .Select(item => item.Invoice.InvoiceDate.Year)
            .Distinct()
            .OrderByDescending(year => year)
            .ToArray();

        var availableInstitutions = source
            .Select(item => (item.Invoice.InstitutionName ?? string.Empty).Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

        var availableInvoiceTypes = source
            .Select(item => (item.Invoice.InvoiceType ?? string.Empty).Trim())
            .Where(type => !string.IsNullOrWhiteSpace(type))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(type => type, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

        var filtered = source
            .Where(item => !filter.Year.HasValue || item.Invoice.InvoiceDate.Year == filter.Year.Value)
            .Where(item => string.IsNullOrWhiteSpace(filter.InstitutionName)
                || string.Equals(
                    item.Invoice.InstitutionName?.Trim(),
                    filter.InstitutionName.Trim(),
                    StringComparison.CurrentCultureIgnoreCase))
            .Where(item => string.IsNullOrWhiteSpace(filter.InvoiceType)
                || string.Equals(
                    item.Invoice.InvoiceType?.Trim(),
                    filter.InvoiceType.Trim(),
                    StringComparison.CurrentCultureIgnoreCase))
            .ToArray();

        var rows = filtered
            .GroupBy(item => new
            {
                InstitutionName = (item.Invoice.InstitutionName ?? string.Empty).Trim(),
                Year = item.Invoice.InvoiceDate.Year,
                InvoiceType = (item.Invoice.InvoiceType ?? string.Empty).Trim()
            })
            .Select(group => new ArchiveStatisticsRow(
                group.Key.InstitutionName,
                group.Key.Year,
                group.Key.InvoiceType,
                group.Count(),
                group.Sum(item => item.Invoice.PayableAmount),
                group.Sum(item => item.Invoice.VatAmount),
                group.Sum(item => item.Invoice.StampDuty),
                group.Sum(item => item.Invoice.PaymentAmount)))
            .OrderBy(row => row.InstitutionName, StringComparer.CurrentCultureIgnoreCase)
            .ThenByDescending(row => row.Year)
            .ThenBy(row => row.InvoiceType, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

        return new ArchiveStatisticsResult(
            filtered.Select(item => item.ArchiveId).Distinct().Count(),
            filtered.Length,
            filtered
                .Select(item => (item.Invoice.InstitutionName ?? string.Empty).Trim())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .Count(),
            filtered.Sum(item => item.Invoice.PayableAmount),
            filtered.Sum(item => item.Invoice.VatAmount),
            filtered.Sum(item => item.Invoice.StampDuty),
            filtered.Sum(item => item.Invoice.PaymentAmount),
            availableYears,
            availableInstitutions,
            availableInvoiceTypes,
            rows);
    }

    private sealed record ArchiveInvoice(Guid ArchiveId, InvoiceRecord Invoice);
}
