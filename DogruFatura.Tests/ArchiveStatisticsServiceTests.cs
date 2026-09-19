using DogruFatura.Core.Models;
using DogruFatura.Core.Services;

namespace DogruFatura.Tests;

public sealed class ArchiveStatisticsServiceTests
{
    [Fact]
    public void BuildFiltersByInstitutionYearAndInvoiceType()
    {
        var archive = new ArchivedInvoiceList(
            Guid.NewGuid(),
            "STAT-001",
            new DateTime(2025, 12, 31),
            "Elektrik",
            new List<InvoiceRecord>
            {
                Invoice("A Kurumu", "Elektrik", new DateTime(2025, 1, 10), 1000m, 180m, 7.77m, 992.23m),
                Invoice("A Kurumu", "Elektrik", new DateTime(2025, 2, 10), 2000m, 360m, 15.55m, 1984.45m),
                Invoice("A Kurumu", "Su", new DateTime(2025, 3, 10), 500m, 90m, 0m, 500m),
                Invoice("B Kurumu", "Elektrik", new DateTime(2024, 4, 10), 750m, 135m, 5m, 745m)
            },
            AuthorityProfileDefaults.Create(),
            "HYS-2025123456",
            true,
            new DateTime(2025, 12, 31));

        var result = ArchiveStatisticsService.Build(
            new[] { archive },
            new ArchiveStatisticsFilter(
                2025,
                "A Kurumu",
                "Elektrik"));

        Assert.Equal(1, result.ArchiveCount);
        Assert.Equal(2, result.InvoiceCount);
        Assert.Equal(1, result.InstitutionCount);
        Assert.Equal(3000m, result.PayableTotal);
        Assert.Equal(540m, result.VatTotal);
        Assert.Equal(23.32m, result.StampDutyTotal);
        Assert.Equal(2976.68m, result.PaymentTotal);

        var row = Assert.Single(result.Rows);
        Assert.Equal("A Kurumu", row.InstitutionName);
        Assert.Equal(2025, row.Year);
        Assert.Equal("Elektrik", row.InvoiceType);
        Assert.Equal(2, row.InvoiceCount);

        Assert.Contains(2025, result.AvailableYears);
        Assert.Contains("A Kurumu", result.AvailableInstitutions);
        Assert.Contains("Elektrik", result.AvailableInvoiceTypes);
    }

    [Fact]
    public void BuildAppliesDateRangeAndCreatesMonthlyBreakdown()
    {
        var archive = new ArchivedInvoiceList(
            Guid.NewGuid(),
            "STAT-DATE-001",
            new DateTime(2025, 12, 31),
            "Elektrik",
            new List<InvoiceRecord>
            {
                Invoice("A Kurumu", "Elektrik", new DateTime(2025, 1, 10), 1000m, 180m, 7m, 993m),
                Invoice("A Kurumu", "Elektrik", new DateTime(2025, 2, 10), 2000m, 360m, 14m, 1986m),
                Invoice("A Kurumu", "Elektrik", new DateTime(2025, 3, 10), 3000m, 540m, 21m, 2979m)
            },
            AuthorityProfileDefaults.Create(),
            "HYS-2025987654",
            true,
            new DateTime(2025, 12, 31));

        var result = ArchiveStatisticsService.Build(
            new[] { archive },
            new ArchiveStatisticsFilter(
                InstitutionName: "A Kurumu",
                InvoiceType: "Elektrik",
                StartDate: new DateTime(2025, 2, 1),
                EndDate: new DateTime(2025, 3, 31)));

        Assert.Equal(2, result.InvoiceCount);
        Assert.Equal(2500m, result.AveragePayableAmount);
        Assert.Equal(2482.5m, result.AveragePaymentAmount);
        Assert.Equal(2, result.MonthlyRows.Count);
        Assert.Equal(2, result.MonthlyRows[0].Month);
        Assert.Equal(1986m, result.MonthlyRows[0].PaymentTotal);
        Assert.Equal(3, result.MonthlyRows[1].Month);
        Assert.Equal(2979m, result.MonthlyRows[1].PaymentTotal);
    }

    private static InvoiceRecord Invoice(
        string institutionName,
        string invoiceType,
        DateTime invoiceDate,
        decimal payable,
        decimal vat,
        decimal stamp,
        decimal payment) =>
        new(
            Guid.NewGuid(),
            invoiceDate,
            "İlkokul",
            institutionName,
            "1234567890",
            invoiceType,
            Guid.NewGuid().ToString("N"),
            "1111111111",
            invoiceDate,
            invoiceDate.AddDays(10),
            payable,
            vat,
            payable - vat,
            stamp,
            payment);
}
