using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using System.Text;
using System.Text.Json;

namespace DogruFatura.Tests;

public sealed class DataSchemaMigrationTests
{
    [Fact]
    public void LegacyInvoiceFileIsLoadedUpgradedAndOriginalFormatIsBackedUp()
    {
        using var temp = new TempFolder();
        var path = Path.Combine(temp.Path, "AktifFaturalar.json");
        var invoice = Invoice("LEGACY-INV-001");
        var legacy = new[]
        {
            new
            {
                invoice.Id,
                invoice.CreatedAt,
                invoice.InstitutionType,
                invoice.InstitutionName,
                invoice.Vkn,
                invoice.InvoiceType,
                invoice.InvoiceNumber,
                invoice.ServiceNumber,
                invoice.InvoiceDate,
                invoice.DueDate,
                invoice.PayableAmount,
                invoice.VatAmount,
                invoice.VatBase,
                invoice.StampDuty,
                invoice.PaymentAmount,
                invoice.InterruptionCompensationAmount,
                invoice.SchoolId
            }
        };
        var original = JsonSerializer.Serialize(legacy);
        File.WriteAllText(path, original, Encoding.UTF8);

        var result = new InvoiceDataStore(path).LoadOrCreate();

        var loaded = Assert.Single(result.Invoices);
        Assert.Equal(invoice.Id, loaded.Id);
        Assert.False(loaded.HasTaxRuleMetadata);
        Assert.Equal(string.Empty, loaded.TaxRuleVersion);
        Assert.Null(loaded.TaxRuleEffectiveFrom);
        Assert.Equal(0m, loaded.AppliedStampDutyRate);
        AssertCurrentEnvelope(path, DataFileKinds.ActiveInvoices);
        Assert.Contains("şema", result.WarningMessage!, StringComparison.CurrentCultureIgnoreCase);
        var backup = Assert.Single(Directory.EnumerateFiles(temp.Path, "AktifFaturalar_Schema0_Onceki_*.json"));
        Assert.Equal(original, File.ReadAllText(backup, Encoding.UTF8));
    }

    [Fact]
    public void LegacySchoolFileIsLoadedAndUpgradedToCurrentSchema()
    {
        using var temp = new TempFolder();
        var path = Path.Combine(temp.Path, "Okullar.json");
        var school = new SchoolRecord(
            "İlkokul", "Şema Test Okulu", "1234567890",
            "1111111111", "2222222222", "3333333333", "4444444444");
        File.WriteAllText(path, JsonSerializer.Serialize(new[] { school }), Encoding.UTF8);

        var result = new SchoolDataStore(path).LoadOrCreate();

        Assert.Single(result.Schools);
        Assert.Equal(school.Name, result.Schools[0].Name);
        AssertCurrentEnvelope(path, DataFileKinds.Schools);
        Assert.Contains("şema", result.WarningMessage!, StringComparison.CurrentCultureIgnoreCase);
        Assert.Single(Directory.EnumerateFiles(temp.Path, "Okullar_Schema0_Onceki_*.json"));
    }

    [Fact]
    public void LegacyAuthorityProfileIsLoadedAndUpgradedToCurrentSchema()
    {
        using var temp = new TempFolder();
        var path = Path.Combine(temp.Path, "YetkiliProfili.json");
        var profile = AuthorityProfileDefaults.Create();
        File.WriteAllText(path, JsonSerializer.Serialize(profile), Encoding.UTF8);

        var result = new AuthorityProfileDataStore(path).LoadOrCreate();

        Assert.True(result.Profile.IsComplete);
        AssertCurrentEnvelope(path, DataFileKinds.AuthorityProfile);
        Assert.Contains("şema", result.WarningMessage!, StringComparison.CurrentCultureIgnoreCase);
        Assert.Single(Directory.EnumerateFiles(temp.Path, "YetkiliProfili_Schema0_Onceki_*.json"));
    }

    [Fact]
    public void LegacyArchiveFileIsLoadedAndUpgradedToCurrentSchema()
    {
        using var temp = new TempFolder();
        var path = Path.Combine(temp.Path, "IslemArsivi.json");
        var archive = new ArchivedInvoiceList(
            Guid.NewGuid(),
            "DF-SCHEMA-001",
            new DateTime(2026, 9, 1),
            "Elektrik",
            new List<InvoiceRecord> { Invoice("LEGACY-ARCHIVE-001") },
            AuthorityProfileDefaults.Create());
        File.WriteAllText(path, JsonSerializer.Serialize(new[] { archive }), Encoding.UTF8);

        var result = new ArchivedInvoiceListDataStore(path).LoadOrCreate();

        Assert.Single(result.Archives);
        Assert.Equal(archive.Id, result.Archives[0].Id);
        AssertCurrentEnvelope(path, DataFileKinds.InvoiceArchive);
        Assert.Contains("şema", result.WarningMessage!, StringComparison.CurrentCultureIgnoreCase);
        Assert.Single(Directory.EnumerateFiles(temp.Path, "IslemArsivi_Schema0_Onceki_*.json"));
    }

    [Fact]
    public void CurrentSchemaSaveAndReloadRoundTripsWithoutMigrationWarning()
    {
        using var temp = new TempFolder();
        var path = Path.Combine(temp.Path, "AktifFaturalar.json");
        var store = new InvoiceDataStore(path);
        var calculation = new TaxCalculationService().Calculate(1500m, 250m, "Elektrik");
        var invoice = Invoice("CURRENT-001") with
        {
            TaxRuleVersion = calculation.RuleVersion,
            TaxRuleEffectiveFrom = calculation.RuleEffectiveFrom,
            AppliedStampDutyRate = calculation.AppliedStampDutyRate
        };

        store.Save(new[] { invoice });
        var result = store.LoadOrCreate();

        var loaded = Assert.Single(result.Invoices);
        Assert.Equal(invoice.Id, loaded.Id);
        Assert.True(loaded.HasTaxRuleMetadata);
        Assert.Equal(calculation.RuleVersion, loaded.TaxRuleVersion);
        Assert.Equal(calculation.RuleEffectiveFrom, loaded.TaxRuleEffectiveFrom);
        Assert.Equal(calculation.AppliedStampDutyRate, loaded.AppliedStampDutyRate);
        AssertCurrentEnvelope(path, DataFileKinds.ActiveInvoices);
        Assert.True(string.IsNullOrWhiteSpace(result.WarningMessage));
        Assert.Empty(Directory.EnumerateFiles(temp.Path, "*_Schema*_Onceki_*.json"));
    }

    [Fact]
    public void FutureSchemaIsRejectedWithoutOverwritingSource()
    {
        using var temp = new TempFolder();
        var path = Path.Combine(temp.Path, "AktifFaturalar.json");
        var original = JsonSerializer.Serialize(new
        {
            SchemaVersion = DataFileSchemaService.CurrentSchemaVersion + 1,
            DataType = DataFileKinds.ActiveInvoices,
            Data = new[] { Invoice("FUTURE-001") }
        });
        File.WriteAllText(path, original, Encoding.UTF8);

        var result = new InvoiceDataStore(path).LoadOrCreate();

        Assert.Empty(result.Invoices);
        Assert.Equal(original, File.ReadAllText(path, Encoding.UTF8));
        Assert.Contains("kurtarma modunda", result.WarningMessage!, StringComparison.CurrentCultureIgnoreCase);
        Assert.Single(Directory.EnumerateFiles(temp.Path, "AktifFaturalar_Bozuk_*.json"));
    }

    [Fact]
    public void WrongDataTypeIsRejectedWithoutOverwritingSource()
    {
        using var temp = new TempFolder();
        var path = Path.Combine(temp.Path, "AktifFaturalar.json");
        var original = JsonSerializer.Serialize(new
        {
            SchemaVersion = DataFileSchemaService.CurrentSchemaVersion,
            DataType = DataFileKinds.Schools,
            Data = new[] { Invoice("WRONG-TYPE-001") }
        });
        File.WriteAllText(path, original, Encoding.UTF8);

        var result = new InvoiceDataStore(path).LoadOrCreate();

        Assert.Empty(result.Invoices);
        Assert.Equal(original, File.ReadAllText(path, Encoding.UTF8));
        Assert.Single(Directory.EnumerateFiles(temp.Path, "AktifFaturalar_Bozuk_*.json"));
    }

    private static void AssertCurrentEnvelope(string path, string expectedDataType)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path, Encoding.UTF8));
        var root = document.RootElement;
        Assert.Equal(JsonValueKind.Object, root.ValueKind);
        Assert.Equal(DataFileSchemaService.CurrentSchemaVersion, root.GetProperty("SchemaVersion").GetInt32());
        Assert.Equal(expectedDataType, root.GetProperty("DataType").GetString());
        Assert.True(root.TryGetProperty("Data", out _));
    }

    private static InvoiceRecord Invoice(string number)
    {
        var calculation = new TaxCalculationService().Calculate(1500m, 250m, "Elektrik");
        return new InvoiceRecord(
            Guid.NewGuid(), new DateTime(2026, 9, 1, 10, 0, 0),
            "İlkokul", "Şema Test Okulu", "1234567890",
            "Elektrik", number, "1111111111",
            new DateTime(2026, 9, 1), new DateTime(2026, 9, 15),
            1500m, 250m, calculation.VatBase, calculation.StampDuty, calculation.PaymentAmount);
    }

    private sealed class TempFolder : IDisposable
    {
        public TempFolder()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "DogruFatura_SchemaTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try { if (Directory.Exists(Path)) Directory.Delete(Path, true); } catch { }
        }
    }
}
