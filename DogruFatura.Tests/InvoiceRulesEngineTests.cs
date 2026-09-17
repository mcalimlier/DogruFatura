using DogruFatura.Core.Models;
using DogruFatura.Core.Services;

namespace DogruFatura.Tests;

public sealed class InvoiceRulesEngineTests
{
    [Fact]
    public void Validation_UsesEngineFirstViolationWithoutChangingMessage()
    {
        var invoice = ValidInvoice() with { Vkn = "12A" };

        var evaluation = InvoiceRulesEngine.Evaluate(invoice);
        var validation = InvoiceValidationService.Validate(invoice);

        Assert.True(evaluation.Has(InvoiceRuleCode.InvalidVknCharacters));
        Assert.True(evaluation.Has(InvoiceRuleCode.InvalidVknLength));
        Assert.False(validation.IsValid);
        Assert.Equal("VKN girildiğinde yalnızca rakamlardan oluşmalıdır.", validation.Message);
    }

    [Fact]
    public void Validation_UsesEngineTaxMismatchRule()
    {
        var invoice = ValidInvoice();
        invoice = invoice with { PaymentAmount = invoice.PaymentAmount + 1m };

        var evaluation = InvoiceRulesEngine.Evaluate(invoice);
        var validation = InvoiceValidationService.Validate(invoice);

        Assert.True(evaluation.Has(InvoiceRuleCode.TaxCalculationMismatch));
        Assert.False(validation.IsValid);
        Assert.Equal("Faturanın vergi hesap sonuçları güncel hesap motoruyla uyuşmuyor.", validation.Message);
    }

    [Theory]
    [InlineData("Elektrik")]
    [InlineData("Telefon")]
    [InlineData("İnternet")]
    [InlineData("Su")]
    public void SupportedInvoiceTypes_AreAccepted(string invoiceType)
    {
        Assert.True(InvoiceRulesEngine.IsSupportedInvoiceType(invoiceType));
    }

    [Fact]
    public void UnknownInvoiceType_IsRejected()
    {
        Assert.False(InvoiceRulesEngine.IsSupportedInvoiceType("Doğalgaz"));
    }

    private static InvoiceRecord ValidInvoice()
    {
        var calculation = new TaxCalculationService().Calculate(1000m, 100m, "Elektrik");

        return new InvoiceRecord(
            Guid.NewGuid(),
            new DateTime(2026, 9, 17, 12, 0, 0, DateTimeKind.Local),
            "İlkokul",
            "Test Kurumu",
            "1234567890",
            "Elektrik",
            "FTR-100",
            "12345",
            new DateTime(2026, 9, 1),
            new DateTime(2026, 9, 20),
            1000m,
            100m,
            calculation.VatBase,
            calculation.StampDuty,
            calculation.PaymentAmount);
    }
}
