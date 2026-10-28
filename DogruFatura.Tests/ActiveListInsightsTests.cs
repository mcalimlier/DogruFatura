using DogruFatura.Core.Models;
using DogruFatura.Core.Services;

namespace DogruFatura.Tests;

public sealed class ActiveListInsightsTests
{
    [Fact]
    public void OverdueExcludesTodayAndSumsInvoiceAmountRatherThanNetPayment()
    {
        var today = new DateTime(2026, 10, 28);
        var result = ActiveListInsights.Build(new[] { Invoice("Işık İlkokulu", today.AddDays(-1)), Invoice("Işık İlkokulu", today), Invoice("Başka Kurum", today.AddDays(1)) }, today);
        Assert.Equal(1, result.OverdueCount);
        Assert.Equal(120m, result.OverdueInvoiceTotal);
        Assert.Equal(2, result.InstitutionCount);
    }

    [Fact]
    public void InstitutionCountIgnoresWhitespaceAndTurkishCasing()
    {
        var result = ActiveListInsights.Build(new[] { Invoice(" Işık İlkokulu ", DateTime.Today), Invoice("ışık ilkokulu", DateTime.Today) }, DateTime.Today);
        Assert.Equal(1, result.InstitutionCount);
    }

    [Fact]
    public void EmptyListHasZeroTotals()
    {
        Assert.Equal(new ActiveListInsights(0, 0m, 0), ActiveListInsights.Build(Array.Empty<InvoiceRecord>(), DateTime.Today));
    }

    private static InvoiceRecord Invoice(string name, DateTime due) => new(Guid.NewGuid(), due.AddDays(-10), "İlkokul", name, "", "Elektrik", "TEST", "123", due.AddDays(-10), due, 120m, 20m, 100m, .948m, 119.052m);
}
