using DogruFatura.Core.Models;

namespace DogruFatura.Core.Services;

public sealed record InstitutionPeriodRow(string InstitutionName, decimal PreviousTotal, decimal CurrentTotal)
{
    public decimal Difference => CurrentTotal - PreviousTotal;
    public decimal? PercentChange => PreviousTotal == 0 ? null : Difference / PreviousTotal * 100m;
}

public sealed record InstitutionPeriodComparison(ArchiveStatisticsFilter CurrentFilter,
    ArchiveStatisticsFilter PreviousFilter, IReadOnlyList<InstitutionPeriodRow> Rows);

public static class InstitutionPeriodComparisonService
{
    public static InstitutionPeriodComparison Build(IEnumerable<ArchivedInvoiceList> archives, ArchiveStatisticsFilter filter, IEnumerable<SchoolRecord>? schools = null)
    {
        ArgumentNullException.ThrowIfNull(archives);
        ArgumentNullException.ThrowIfNull(filter);
        var data = archives.ToArray();
        var catalog = schools?.ToArray() ?? Array.Empty<SchoolRecord>();
        ArchiveStatisticsFilter current = filter;
        ArchiveStatisticsFilter previous;
        if (filter.StartDate.HasValue && filter.EndDate.HasValue)
        {
            var start = filter.StartDate.Value.Date;
            var end = filter.EndDate.Value.Date;
            if (end < start) throw new ArgumentException("Bitiş tarihi başlangıçtan önce olamaz.", nameof(filter));
            current = filter with { Year = null };
            previous = current with { StartDate = start.AddDays(-(end - start).Days - 1), EndDate = start.AddDays(-1) };
        }
        else
        {
            var year = filter.Year ?? ArchiveStatisticsService.Build(data, filter with { Year = null, StartDate = null, EndDate = null }, catalog)
                .Rows.Select(row => (int?)row.Year).Max() ?? DateTime.Today.Year;
            current = filter with { Year = year, StartDate = null, EndDate = null };
            previous = current with { Year = year - 1 };
        }
        var now = Totals(data, current, catalog);
        var before = Totals(data, previous, catalog);
        var rows = now.Keys.Concat(before.Keys).Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(name => new InstitutionPeriodRow(name, before.GetValueOrDefault(name), now.GetValueOrDefault(name)))
            .OrderByDescending(row => row.Difference).ThenBy(row => row.InstitutionName, StringComparer.CurrentCultureIgnoreCase).ToArray();
        return new(current, previous, rows);
    }

    private static Dictionary<string, decimal> Totals(ArchivedInvoiceList[] data, ArchiveStatisticsFilter filter, SchoolRecord[] schools) =>
        ArchiveStatisticsService.Build(data, filter, schools).Rows.GroupBy(row => row.InstitutionName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Sum(row => row.PaymentTotal), StringComparer.OrdinalIgnoreCase);
}
