using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using System.Text;
using System.Text.Json;

namespace DogruFatura.Tests;

public sealed class RecoveryModeTests
{
    [Fact]
    public void CorruptInvoiceFileIsNeverOverwrittenAndValidRecordIsRecovered()
    {
        using var temp = new TempFolder();
        var path = Path.Combine(temp.Path, "AktifFaturalar.json");
        var valid = Invoice("FTR-001");
        var original = "[" + JsonSerializer.Serialize(valid) + ",null]";
        File.WriteAllText(path, original, Encoding.UTF8);

        var result = new InvoiceDataStore(path).LoadOrCreate();

        Assert.Single(result.Invoices);
        Assert.Equal(valid.Id, result.Invoices[0].Id);
        Assert.Equal(original, File.ReadAllText(path, Encoding.UTF8));
        Assert.Contains("kurtarma modunda", result.WarningMessage!, StringComparison.CurrentCultureIgnoreCase);
        Assert.Single(Directory.EnumerateFiles(temp.Path, "AktifFaturalar_Bozuk_*.json"));
    }

    [Fact]
    public void CorruptSchoolFileIsNeverOverwrittenAndValidSchoolIsRecovered()
    {
        using var temp = new TempFolder();
        var path = Path.Combine(temp.Path, "Okullar.json");
        var valid = new SchoolRecord(
            "İlkokul", "Kurtarma Test Okulu", "1234567890",
            "1111111111", "2222222222", "3333333333", "4444444444");
        var original = "[" + JsonSerializer.Serialize(valid) + ",null]";
        File.WriteAllText(path, original, Encoding.UTF8);

        var result = new SchoolDataStore(path).LoadOrCreate();

        Assert.Single(result.Schools);
        Assert.Equal(valid.Name, result.Schools[0].Name);
        Assert.Equal(original, File.ReadAllText(path, Encoding.UTF8));
        Assert.Single(Directory.EnumerateFiles(temp.Path, "Okullar_Bozuk_*.json"));
    }

    [Fact]
    public void InvalidAuthorityProfileIsNeverReplacedByDefaultsAutomatically()
    {
        using var temp = new TempFolder();
        var path = Path.Combine(temp.Path, "YetkiliProfili.json");
        const string original = "{}";
        File.WriteAllText(path, original, Encoding.UTF8);

        var result = new AuthorityProfileDataStore(path).LoadOrCreate();

        Assert.True(result.Profile.IsComplete);
        Assert.Equal(original, File.ReadAllText(path, Encoding.UTF8));
        Assert.Contains("üzerine", result.WarningMessage!, StringComparison.CurrentCultureIgnoreCase);
        Assert.Single(Directory.EnumerateFiles(temp.Path, "YetkiliProfili_Bozuk_*.json"));
    }

    [Fact]
    public void ArchiveRecoveryKeepsValidRecordAndRejectsInvalidRecordWithoutOverwritingSource()
    {
        using var temp = new TempFolder();
        var path = Path.Combine(temp.Path, "IslemArsivi.json");
        var validInvoice = Invoice("ARS-001");
        var valid = new ArchivedInvoiceList(
            Guid.NewGuid(), "DF-RECOVERY-001", new DateTime(2026, 9, 1), "Elektrik",
            new List<InvoiceRecord> { validInvoice }, AuthorityProfileDefaults.Create());
        var invalid = valid with
        {
            Id = Guid.NewGuid(),
            ArchiveNumber = "DF-RECOVERY-INVALID",
            Invoices = new List<InvoiceRecord>()
        };
        var original = JsonSerializer.Serialize(new[] { valid, invalid });
        File.WriteAllText(path, original, Encoding.UTF8);

        var result = new ArchivedInvoiceListDataStore(path).LoadOrCreate();

        Assert.Single(result.Archives);
        Assert.Equal(valid.Id, result.Archives[0].Id);
        Assert.Equal(original, File.ReadAllText(path, Encoding.UTF8));
        Assert.Contains("1 sağlam", result.WarningMessage!, StringComparison.CurrentCultureIgnoreCase);
        Assert.Single(Directory.EnumerateFiles(temp.Path, "IslemArsivi_Bozuk_*.json"));
    }

    private static InvoiceRecord Invoice(string number)
    {
        var calculation = new TaxCalculationService().Calculate(1500m, 250m, "Elektrik");
        return new InvoiceRecord(
            Guid.NewGuid(), new DateTime(2026, 9, 1, 10, 0, 0),
            "İlkokul", "Kurtarma Test Okulu", "1234567890",
            "Elektrik", number, "1111111111",
            new DateTime(2026, 9, 1), new DateTime(2026, 9, 15),
            1500m, 250m, calculation.VatBase, calculation.StampDuty, calculation.PaymentAmount);
    }

    private sealed class TempFolder : IDisposable
    {
        public TempFolder()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "DogruFatura_RecoveryTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try { if (Directory.Exists(Path)) Directory.Delete(Path, true); } catch { }
        }
    }
}
