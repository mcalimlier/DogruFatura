using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using System.Text.Json;

namespace DogruFatura.Tests;

public sealed class WaterPaymentTests
{
    [Fact]
    public void MunicipalityPaysFullAmountAndRejectsNonzeroVat()
    {
        var service = new TaxCalculationService();
        var result = service.Calculate(1200m, 0m, "Su", waterRecipient: WaterPaymentRecipient.Municipality);
        Assert.True(result.IsValid);
        Assert.Equal(0m, result.StampDuty);
        Assert.Equal(1200m, result.PaymentAmount);
        Assert.False(service.Calculate(1200m, 100m, "Su", waterRecipient: WaterPaymentRecipient.Municipality).IsValid);
    }

    [Fact]
    public void CompanyUsesStandardCalculationAndSurvivesActiveStoreReload()
    {
        var calc = new TaxCalculationService().Calculate(1200m, 200m, "Su", waterRecipient: WaterPaymentRecipient.Company);
        Assert.Equal(9.48m, calc.StampDuty);
        Assert.Equal(1190.52m, calc.PaymentAmount);
        var invoice = Invoice(calc, WaterPaymentRecipient.Company, 200m);
        Assert.True(InvoiceValidationService.Validate(invoice).IsValid);
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            var store = new InvoiceDataStore(path);
            store.Save(new[] { invoice });
            Assert.Equal(invoice, Assert.Single(store.LoadOrCreate().Invoices));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void LegacyWaterRetainsHistoricalVatAndSerializationShape()
    {
        var calc = new TaxCalculationService().Calculate(1200m, 200m, "Su");
        var invoice = Invoice(calc, WaterPaymentRecipient.Unspecified, 200m);
        var json = JsonSerializer.Serialize(invoice);
        Assert.DoesNotContain("WaterRecipient", json, StringComparison.Ordinal);
        var loaded = JsonSerializer.Deserialize<InvoiceRecord>(json)!;
        Assert.Equal(WaterPaymentRecipient.Unspecified, loaded.WaterRecipient);
        Assert.True(InvoiceValidationService.Validate(loaded).IsValid);
        Assert.Equal(1000m, calc.VatBase);
        Assert.Equal(0m, calc.StampDuty);
    }

    [Fact]
    public void RecipientIsNotAllowedOnOtherInvoiceTypes()
    {
        Assert.False(new TaxCalculationService().Calculate(1200m, 0m, "Elektrik", waterRecipient: WaterPaymentRecipient.Municipality).IsValid);
        Assert.False(new TaxCalculationService().Calculate(1200m, 0m, "Su", waterRecipient: (WaterPaymentRecipient)99).IsValid);
    }

    [Fact]
    public void ClosedArchiveKeepsRecipientAndDetectsChanges()
    {
        var calc = new TaxCalculationService().Calculate(1200m, 200m, "Su", waterRecipient: WaterPaymentRecipient.Company);
        var invoice = Invoice(calc, WaterPaymentRecipient.Company, 200m);
        var archive = ClosedArchiveSnapshotService.Seal(new ArchivedInvoiceList(
            Guid.NewGuid(), "SU-TEST", DateTime.Now, "Su", new() { invoice },
            new AuthorityProfile("Görevli", "Memur", "Yetkili", "Müdür"), "HYS-1", true, DateTime.Now));
        var loaded = JsonSerializer.Deserialize<ArchivedInvoiceList>(JsonSerializer.Serialize(archive))!;
        Assert.True(ClosedArchiveSnapshotService.Matches(loaded));
        Assert.Equal(WaterPaymentRecipient.Company, Assert.Single(loaded.Invoices).WaterRecipient);
        var changed = loaded with { Invoices = new() { invoice with { WaterRecipient = WaterPaymentRecipient.Unspecified } } };
        Assert.False(ClosedArchiveSnapshotService.Matches(changed));
    }

    private static InvoiceRecord Invoice(TaxCalculationResult c, WaterPaymentRecipient recipient, decimal vat) =>
        new(Guid.NewGuid(), DateTime.Now, "Ortaokul", "Örnek Okul", "1234567890", "Su", "SU1", "00123",
            DateTime.Today, DateTime.Today, 1200m, vat, c.VatBase, c.StampDuty, c.PaymentAmount,
            WaterRecipient: recipient);
}
