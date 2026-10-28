using DogruFatura.Core.Models;
using DogruFatura.Core.Services;

namespace DogruFatura.Tests;

public sealed class InstitutionPeriodComparisonTests
{
    [Fact]
    public void DefaultPeriodProducesInstitutionRowsForLatestAvailableYear()
    {
        var result = InstitutionPeriodComparisonService.Build(Data(), new());
        Assert.Equal(2026, result.CurrentFilter.Year);
        var row = Assert.Single(result.Rows);
        Assert.Equal("Örnek Kurum", row.InstitutionName);
        Assert.Equal(200m, row.CurrentTotal);
        Assert.Equal(100m, row.PreviousTotal);
        Assert.Equal(100m, row.Difference);
        Assert.Equal(100m, row.PercentChange);
    }

    [Fact]
    public void DefaultPeriodRespectsSelectedInstitutionWhenOtherInstitutionsHaveNewerData()
    {
        var other = Packet(2028, 300m);
        other = other with { Invoices = new() { other.Invoices[0] with { InstitutionName = "Başka Kurum" } } };
        var result = InstitutionPeriodComparisonService.Build(Data().Append(other), new(InstitutionName: "Örnek Kurum"));
        Assert.Equal(2026, result.CurrentFilter.Year);
        Assert.Equal(200m, Assert.Single(result.Rows).CurrentTotal);
    }

    [Fact]
    public void DateRangeUsesImmediatelyPrecedingEqualLengthPeriod()
    {
        var result = InstitutionPeriodComparisonService.Build(Data(), new(StartDate: new(2026, 1, 1), EndDate: new(2026, 1, 31)));
        Assert.Equal(new DateTime(2025, 12, 1), result.PreviousFilter.StartDate);
        Assert.Equal(new DateTime(2025, 12, 31), result.PreviousFilter.EndDate);
    }

    [Fact]
    public void OpenPacketsAreExcludedAndZeroBaselineDoesNotFabricatePercentage()
    {
        var data = Data();
        var result = InstitutionPeriodComparisonService.Build(data.Select(a => a.ArchivedAt.Year == 2025 ? a with { IsClosed = false } : a), new(2026));
        Assert.Null(Assert.Single(result.Rows).PercentChange);
        Assert.Equal(200m, result.Rows[0].Difference);
    }

    private static ArchivedInvoiceList[] Data() => new[] { Packet(2025, 100m), Packet(2026, 200m) };
    private static ArchivedInvoiceList Packet(int year, decimal payment)
    {
        var date = new DateTime(year, 1, 15);
        var invoice = new InvoiceRecord(Guid.NewGuid(), date, "İlkokul", "Örnek Kurum", "", "Su", year.ToString(System.Globalization.CultureInfo.InvariantCulture), "123", date, date.AddDays(10), payment, 0, payment, 0, payment);
        return new(Guid.NewGuid(), "P-" + year, date, "Su", new() { invoice }, AuthorityProfileDefaults.Create(), "HYS-" + year, true, date);
    }
}
