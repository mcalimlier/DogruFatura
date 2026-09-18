using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using System.Text;
using System.Text.Json;

namespace DogruFatura.Tests;

public sealed class DomainIdentityTests
{
    [Fact]
    public void LegacySchoolNormalizationCreatesStableNonEmptyId()
    {
        var legacy = School("Eski Kimlik Okulu");

        var first = SchoolValidationService.Normalize(legacy);
        var second = SchoolValidationService.Normalize(legacy);

        Assert.NotEqual(Guid.Empty, first.SchoolId);
        Assert.Equal(first.SchoolId, second.SchoolId);
    }

    [Fact]
    public void ExplicitSchoolIdSurvivesNormalization()
    {
        var id = Guid.NewGuid();
        var school = School("Kimlik Koruma Okulu") with { SchoolId = id };

        var normalized = SchoolValidationService.Normalize(school);

        Assert.Equal(id, normalized.SchoolId);
    }

    [Fact]
    public void LegacySchoolJsonWithoutIdLoadsWithCompatibleIdentity()
    {
        using var temp = new TempFolder();
        var path = Path.Combine(temp.Path, "Okullar.json");
        var legacy = new[]
        {
            new
            {
                InstitutionType = "İlkokul",
                Name = "Legacy JSON Okulu",
                Vkn = "1234567890",
                ElectricityServiceNumber = "1111111111",
                TelephoneServiceNumber = "2222222222",
                InternetServiceNumber = "3333333333",
                WaterServiceNumber = "4444444444"
            }
        };
        File.WriteAllText(path, JsonSerializer.Serialize(legacy), Encoding.UTF8);

        var result = new SchoolDataStore(path).LoadOrCreate();

        var school = Assert.Single(result.Schools);
        Assert.NotEqual(Guid.Empty, school.SchoolId);
        Assert.Contains(""SchoolId"", File.ReadAllText(path, Encoding.UTF8), StringComparison.Ordinal);
    }

    [Fact]
    public void TypedDomainViewsPreserveExistingStorageText()
    {
        var school = SchoolValidationService.Normalize(School("Typed Okul"));
        var invoice = Invoice("TYPED-001", school, "Elektrik");
        var archive = new ArchivedInvoiceList(
            Guid.NewGuid(),
            "DF-TYPED-001",
            new DateTime(2026, 9, 18),
            "Elektrik",
            new List<InvoiceRecord> { invoice },
            AuthorityProfileDefaults.Create(),
            "HYS-12345",
            true,
            new DateTime(2026, 9, 18));

        Assert.Equal(InstitutionKind.PrimarySchool, school.Kind);
        Assert.Equal(InvoiceKind.Electricity, invoice.Kind);
        Assert.Equal(InstitutionKind.PrimarySchool, invoice.InstitutionKind);
        Assert.Equal(InvoiceKind.Electricity, archive.Kind);
        Assert.Equal(ArchiveState.Closed, archive.State);
        Assert.Equal("Elektrik", DomainValueText.ToStorageValue(invoice.Kind));
        Assert.Equal("İlkokul", DomainValueText.ToStorageValue(school.Kind));
        Assert.Equal(school.ElectricityServiceNumber, school.GetServiceNumber(InvoiceKind.Electricity));
    }

    [Fact]
    public void ControlCenterUsesSchoolIdBeforeLegacyNameMatching()
    {
        var school = SchoolValidationService.Normalize(School("Yeni Kurum Adı"));
        var invoice = Invoice("ID-LINK-001", school, "Elektrik") with
        {
            InstitutionName = "Eski Kurum Adı"
        };

        var result = ControlCenterService.Scan(
            new[] { school },
            new[] { invoice },
            new DateTime(2026, 9, 10));

        Assert.DoesNotContain(result.Issues, item => item.Code == "INVOICE_SCHOOL_MISSING");
    }

    [Fact]
    public void LegacyInvoiceWithoutSchoolIdStillUsesNameMatching()
    {
        var school = SchoolValidationService.Normalize(School("Legacy Bağ Okulu"));
        var invoice = Invoice("LEGACY-LINK-001", school, "Elektrik") with { SchoolId = Guid.Empty };

        var result = ControlCenterService.Scan(
            new[] { school },
            new[] { invoice },
            new DateTime(2026, 9, 10));

        Assert.DoesNotContain(result.Issues, item => item.Code == "INVOICE_SCHOOL_MISSING");
    }

    private static SchoolRecord School(string name) => new(
        "İlkokul",
        name,
        "1234567890",
        "1111111111",
        "2222222222",
        "3333333333",
        "4444444444");

    private static InvoiceRecord Invoice(string number, SchoolRecord school, string invoiceType)
    {
        var calculation = new TaxCalculationService().Calculate(1500m, 250m, invoiceType);
        return new InvoiceRecord(
            Guid.NewGuid(),
            new DateTime(2026, 9, 18, 10, 0, 0),
            school.InstitutionType,
            school.Name,
            school.Vkn,
            invoiceType,
            number,
            school.GetServiceNumber(invoiceType),
            new DateTime(2026, 9, 1),
            new DateTime(2026, 9, 15),
            1500m,
            250m,
            calculation.VatBase,
            calculation.StampDuty,
            calculation.PaymentAmount,
            0m,
            school.SchoolId);
    }

    private sealed class TempFolder : IDisposable
    {
        private readonly string _path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "DogruFatura_DomainIdentityTests_" + Guid.NewGuid().ToString("N"));

        public TempFolder() => Directory.CreateDirectory(_path);

        public string Path => _path;

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_path))
                    Directory.Delete(_path, true);
            }
            catch
            {
            }
        }
    }
}
