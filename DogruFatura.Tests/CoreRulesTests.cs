using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using DogruFatura.Wpf;

namespace DogruFatura.Tests;

public sealed class CoreRulesTests
{
    [Fact]
    public void ElectricityCalculationUsesApprovedStampDutyFormula()
    {
        var service = new TaxCalculationService();
        var result = service.Calculate(4965.10m, 827.53m, "Elektrik");

        Assert.True(result.IsValid);
        Assert.Equal(4137.57m, result.VatBase);
        Assert.Equal(4137.57m * TaxCalculationService.StampDutyRate, result.StampDuty);
        Assert.Equal(4965.10m - result.StampDuty, result.PaymentAmount);
        Assert.Equal(TaxRuleCatalog.CurrentVersion, result.RuleVersion);
        Assert.Null(result.RuleEffectiveFrom);
        Assert.Equal(TaxCalculationService.StampDutyRate, result.AppliedStampDutyRate);
        Assert.Equal(TaxRuleCatalog.Current, service.ActiveRule);
    }

    [Fact]
    public void WaterCalculationIsStampDutyExempt()
    {
        var result = new TaxCalculationService().Calculate(1250m, 125m, "Su");

        Assert.True(result.IsValid);
        Assert.Equal(1125m, result.VatBase);
        Assert.Equal(0m, result.StampDuty);
        Assert.Equal(1250m, result.PaymentAmount);
    }

    [Fact]
    public void ElectricityCompensationIsAddedToVatBaseOnly()
    {
        var result = new TaxCalculationService().Calculate(9921.79m, 1653.63m, "Elektrik", 250m);

        Assert.True(result.IsValid);
        Assert.Equal(9921.79m + 250m - 1653.63m, result.VatBase);
        Assert.Equal(result.VatBase * TaxCalculationService.StampDutyRate, result.StampDuty);
        Assert.Equal(9921.79m - result.StampDuty, result.PaymentAmount);
    }

    [Theory]
    [InlineData("HYS-202614586", "HYS-202614586")]
    [InlineData("hys202614586", "HYS-202614586")]
    [InlineData(" HYS-202614586 ", "HYS-202614586")]
    [InlineData("202614586", "HYS-202614586")]
    public void HysNumberIsNormalized(string input, string expected)
    {
        var success = HysPaymentOrderService.TryNormalize(input, out var normalized, out _);

        Assert.True(success);
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("")]
    [InlineData("HYS-0123")]
    [InlineData("HYS-ABC")]
    public void InvalidHysNumberIsRejected(string input)
    {
        Assert.False(HysPaymentOrderService.TryNormalize(input, out _, out _));
    }

    [Fact]
    public void DuplicateInvoiceIsDetectedIgnoringWhitespaceAndCase()
    {
        var existing = Invoice("Elektrik", "12345", "ABC-100");
        var candidate = Invoice("elektrik", " 12345 ", " abc-100 ");

        var result = InvoiceDuplicateService.Check(new[] { existing }, candidate);

        Assert.True(result.IsDuplicate);
        Assert.Equal(existing.Id, result.ExistingInvoice?.Id);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(12, 1)]
    [InlineData(13, 2)]
    [InlineData(31, 2)]
    [InlineData(32, 3)]
    public void PagePlannerProducesExpectedPageCount(int itemCount, int expectedPages)
    {
        var pages = FinalReportPagePlanner.Plan(itemCount);

        Assert.Equal(expectedPages, pages.Count);
        Assert.Equal(itemCount, pages.Sum(x => x.Count));
        Assert.True(pages[^1].IsFinalPage);
        Assert.True(pages[^1].Count <= FinalReportPagePlanner.FinalPageCapacity);
        Assert.All(pages.Take(pages.Count - 1), page => Assert.True(page.Count <= FinalReportPagePlanner.RegularPageCapacity));
    }

    [Fact]
    public void ReportSideMarginsAreExactlyPointSeventyFiveCentimeters()
    {
        var expectedPoints = 0.75 / 2.54 * 72.0;
        var expectedDips = 0.75 / 2.54 * 96.0;

        Assert.Equal(expectedPoints, FinalReportLayout.SideMarginPoints, 8);
        Assert.Equal(expectedDips, FinalReportLayout.SideMarginDips, 8);
        Assert.Equal(4.0 / 3.0, FinalReportLayout.SideMarginDips / FinalReportLayout.SideMarginPoints, 8);
    }

    private static InvoiceRecord Invoice(string type, string serviceNo, string invoiceNo)
    {
        var calculation = new TaxCalculationService().Calculate(1000m, 100m, "Elektrik");
        return new InvoiceRecord(
            Guid.NewGuid(),
            DateTime.Now,
            "İlkokul",
            "Test Kurumu",
            string.Empty,
            type,
            invoiceNo,
            serviceNo,
            new DateTime(2026, 9, 1),
            new DateTime(2026, 9, 10),
            1000m,
            100m,
            calculation.VatBase,
            calculation.StampDuty,
            calculation.PaymentAmount);
    }
}
