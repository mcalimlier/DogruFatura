using DogruFatura.Core.Models;
using DogruFatura.Core.Services;

namespace DogruFatura.Tests;

public sealed class InvoiceRulesEngineTests
{
    [Fact]
    public void ValidationUsesEngineFirstViolationWithoutChangingMessage()
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
    public void ValidationUsesEngineTaxMismatchRule()
    {
        var invoice = ValidInvoice();
        invoice = invoice with { PaymentAmount = invoice.PaymentAmount + 1m };

        var evaluation = InvoiceRulesEngine.Evaluate(invoice);
        var validation = InvoiceValidationService.Validate(invoice);

        Assert.True(evaluation.Has(InvoiceRuleCode.TaxCalculationMismatch));
        Assert.False(validation.IsValid);
        Assert.Equal("Faturanın vergi hesap sonuçları güncel hesap motoruyla uyuşmuyor.", validation.Message);
    }

    [Fact]
    public void ControlCenterUsesEngineVknRuleWithoutChangingIssueContract()
    {
        var invoice = ValidInvoice() with { Vkn = "12A" };

        var result = InvoiceConsistencyService.Scan(
            new[] { MatchingSchool() },
            new[] { invoice },
            new DateTime(2026, 9, 17));

        var issue = Assert.Single(result.Issues, item => item.Code == "INVOICE_VKN_INVALID");
        Assert.Equal(InvoiceIssueSeverity.Error, issue.Severity);
        Assert.Equal("Faturanın VKN bilgisi geçersiz", issue.Title);
        Assert.Equal("VKN girildiyse 10 haneli ve yalnızca rakamlardan oluşmalıdır.", issue.Detail);
    }

    [Fact]
    public void ControlCenterUsesEngineTaxMismatchWithoutChangingIssueContract()
    {
        var invoice = ValidInvoice();
        invoice = invoice with { PaymentAmount = invoice.PaymentAmount + 1m };

        var result = InvoiceConsistencyService.Scan(
            new[] { MatchingSchool() },
            new[] { invoice },
            new DateTime(2026, 9, 17));

        var issue = Assert.Single(result.Issues, item => item.Code == "INVOICE_TAX_MISMATCH");
        Assert.Equal(InvoiceIssueSeverity.Error, issue.Severity);
        Assert.Equal("Vergi hesabı güncel kuralla uyuşmuyor", issue.Title);
    }

    [Theory]
    [InlineData("Elektrik")]
    [InlineData("Telefon")]
    [InlineData("İnternet")]
    [InlineData("Su")]
    public void SupportedInvoiceTypesAreAccepted(string invoiceType)
    {
        Assert.True(InvoiceRulesEngine.IsSupportedInvoiceType(invoiceType));
    }

    [Fact]
    public void UnknownInvoiceTypeIsRejectedByEngineAndControlCenter()
    {
        var invoice = ValidInvoice() with { InvoiceType = "Doğalgaz" };

        var result = InvoiceConsistencyService.Scan(
            new[] { MatchingSchool() },
            new[] { invoice },
            new DateTime(2026, 9, 17));

        Assert.False(InvoiceRulesEngine.IsSupportedInvoiceType(invoice.InvoiceType));
        Assert.Contains(result.Issues, item => item.Code == "INVOICE_TYPE_INVALID");
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

    private static SchoolRecord MatchingSchool() => new(
        "İlkokul",
        "Test Kurumu",
        string.Empty,
        "12345",
        "1",
        "1",
        "1");
}
