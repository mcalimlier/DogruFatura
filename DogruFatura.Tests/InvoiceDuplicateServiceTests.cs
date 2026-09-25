using DogruFatura.Core.Models;
using DogruFatura.Core.Services;

namespace DogruFatura.Tests;

public sealed class InvoiceDuplicateServiceTests
{
    [Fact]
    public void BlankInvoiceNumberWarnsOnlyForMatchingInvoice()
    {
        var schoolId = Guid.NewGuid();
        var existing = Invoice(
            schoolId,
            string.Empty,
            new DateTime(2026, 9, 1),
            new DateTime(2026, 9, 15),
            12000m,
            2000m);

        var candidate = existing with
        {
            Id = Guid.NewGuid(),
            CreatedAt = new DateTime(2026, 9, 20, 10, 0, 0)
        };

        var possible = InvoiceDuplicateService.CheckPossibleWhenNumberMissing(
            new[] { existing },
            candidate);

        Assert.True(possible.IsPossibleDuplicate);
        Assert.Equal(existing.Id, possible.ExistingInvoice?.Id);

        var changedAmount = candidate with
        {
            Id = Guid.NewGuid(),
            PayableAmount = 12001m
        };

        var different = InvoiceDuplicateService.CheckPossibleWhenNumberMissing(
            new[] { existing },
            changedAmount);

        Assert.False(different.IsPossibleDuplicate);
        Assert.Null(different.ExistingInvoice);
    }

    private static InvoiceRecord Invoice(
        Guid schoolId,
        string invoiceNumber,
        DateTime invoiceDate,
        DateTime dueDate,
        decimal payableAmount,
        decimal vatAmount)
    {
        var calculation = new TaxCalculationService().Calculate(
            payableAmount,
            vatAmount,
            "Elektrik");

        return new InvoiceRecord(
            Guid.NewGuid(),
            new DateTime(2026, 9, 18, 10, 0, 0),
            "İlkokul",
            "Atatürk İlkokulu",
            "1234567890",
            "Elektrik",
            invoiceNumber,
            "1111111111",
            invoiceDate,
            dueDate,
            payableAmount,
            vatAmount,
            calculation.VatBase,
            calculation.StampDuty,
            calculation.PaymentAmount,
            0m,
            schoolId,
            calculation.RuleVersion,
            calculation.RuleEffectiveFrom,
            calculation.AppliedStampDutyRate);
    }
}
