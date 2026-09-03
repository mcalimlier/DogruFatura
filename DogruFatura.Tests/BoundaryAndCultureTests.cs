using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using DogruFatura.Wpf;

namespace DogruFatura.Tests;

public sealed class BoundaryAndCultureTests
{
    [Theory]
    [InlineData("1.234,56", 1234.56)]
    [InlineData("1,234.56", 1234.56)]
    [InlineData("1234,56 TL", 1234.56)]
    [InlineData("₺ 1 234,56", 1234.56)]
    [InlineData("1234.56", 1234.56)]
    public void MoneyParser_AcceptsCommonTurkishAndInternationalForms(string text, double expected)
    {
        Assert.True(NumberParser.TryNonNegativeDecimal(text, out var value));
        Assert.Equal((decimal)expected, value);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("")]
    [InlineData("abc")]
    public void MoneyParser_RejectsInvalidOrNegativeValues(string text)
    {
        Assert.False(NumberParser.TryNonNegativeDecimal(text, out _));
    }

    [Fact]
    public void TurkishUppercase_PreservesTurkishLetters()
    {
        Assert.Equal("İSTANBUL ŞEHİR ÇAĞDAŞ ÖĞRENCİ", FinalReportFormat.Upper("istanbul şehir çağdaş öğrenci"));
    }

    [Fact]
    public void MaximumAllowedServiceAndInvoiceNumbers_AreAccepted()
    {
        var invoice = ValidInvoice(new string('7', 50), new string('F', 100));

        var result = InvoiceValidationService.Validate(invoice);

        Assert.True(result.IsValid, result.Message);
    }

    [Fact]
    public void OverlongServiceNumber_IsRejected()
    {
        var invoice = ValidInvoice(new string('7', 51), "INV-1");

        var result = InvoiceValidationService.Validate(invoice);

        Assert.False(result.IsValid);
        Assert.Contains("50", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void OverlongInvoiceNumber_IsRejected()
    {
        var invoice = ValidInvoice("123456", new string('F', 101));

        var result = InvoiceValidationService.Validate(invoice);

        Assert.False(result.IsValid);
        Assert.Contains("100", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DueDateBeforeInvoiceDate_IsRejected()
    {
        var invoice = ValidInvoice("123456", "INV-1") with
        {
            DueDate = new DateTime(2026, 8, 31)
        };

        Assert.False(InvoiceValidationService.Validate(invoice).IsValid);
    }

    [Fact]
    public void LargeButRealisticAmounts_RemainDeterministic()
    {
        const decimal payable = 999_999_999_999.99m;
        const decimal vat = 166_666_666_666.66m;
        var first = new TaxCalculationService().Calculate(payable, vat, "Elektrik");
        var second = new TaxCalculationService().Calculate(payable, vat, "Elektrik");

        Assert.True(first.IsValid);
        Assert.Equal(first, second);
        Assert.True(first.PaymentAmount > 0m);
    }

    private static InvoiceRecord ValidInvoice(string serviceNumber, string invoiceNumber)
    {
        const decimal payable = 1500m;
        const decimal vat = 250m;
        var calculation = new TaxCalculationService().Calculate(payable, vat, "Elektrik");
        return new InvoiceRecord(
            Guid.NewGuid(), new DateTime(2026, 9, 3, 12, 0, 0),
            "İlkokul", "İstanbul Şehit Öğretmen Çağdaş İlkokulu", "1234567890",
            "Elektrik", invoiceNumber, serviceNumber,
            new DateTime(2026, 9, 1), new DateTime(2026, 9, 15),
            payable, vat, calculation.VatBase, calculation.StampDuty, calculation.PaymentAmount);
    }
}
