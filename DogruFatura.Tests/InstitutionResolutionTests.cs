using DogruFatura.Core.Models;
using DogruFatura.Core.Services;

namespace DogruFatura.Tests;

public sealed class InstitutionResolutionTests
{
    private static readonly SchoolRecord School = SchoolValidationService.Normalize(new("Ortaokul", "31 Ağustos Ortaokulu", "7710327776", "00001036830", "2766181022", "1864001379", "123"));

    [Theory]
    [InlineData("31 Ağustos Ortaokulu")]
    [InlineData("1 | 31 Ağustos Ortaokulu")]
    [InlineData("7 31 Ağustos Ortaokulu")]
    [InlineData("Ağustos Ortaokulu")]
    public void ResolvesHistoricalAliasesWithoutChangingOriginal(string name)
    {
        var invoice = Invoice(name);
        var resolved = InstitutionResolutionService.ForReporting(invoice, new[] { School });
        Assert.Equal(School.Name, resolved.InstitutionName);
        Assert.Equal(School.SchoolId, resolved.SchoolId);
        Assert.Equal(name, invoice.InstitutionName);
        Assert.Equal(invoice.PaymentAmount, resolved.PaymentAmount);
    }

    [Fact]
    public void ResolvesOnlyUnambiguousFullWordNamePrefix()
    {
        var school = School with { Name = "Sivaslı İlçe Milli Eğitim Müdürlüğü" };
        Assert.Equal(school.Name, InstitutionResolutionService.ForReporting(Invoice("Sivaslı İlçe Milli Eğitim"), new[] { school }).InstitutionName);
        Assert.Equal("Sivaslı İlçe Milli Eğitim", InstitutionResolutionService.ForReporting(Invoice("Sivaslı İlçe Milli Eğitim") with { ServiceNumber = "" }, new[] { school, school with { SchoolId = Guid.NewGuid(), Name = "Sivaslı İlçe Milli Eğitim Ek Binası" } }).InstitutionName);
    }

    [Fact]
    public void DoesNotGuessWhenServiceIsSharedAndNameUnknown()
    {
        var other = School with { SchoolId = Guid.NewGuid(), Name = "Başka Ortaokul" };
        Assert.Equal("Bilinmeyen Kurum", InstitutionResolutionService.ForReporting(Invoice("Bilinmeyen Kurum"), new[] { School, other }).InstitutionName);
    }

    [Fact]
    public void StatisticsCombinesAliasesAndFiltersCanonicalNameWithoutLosingInvoices()
    {
        var originals = new[] { Invoice("1 | 31 Ağustos Ortaokulu"), Invoice("3 31 Ağustos Ortaokulu"), Invoice("Ağustos Ortaokulu") };
        var archive = new ArchivedInvoiceList(Guid.NewGuid(), "TEST", DateTime.Now, "Telefon", originals.ToList(), AuthorityProfileDefaults.Create(), "HYS-2026123456", true, DateTime.Now);
        var build = typeof(ArchiveStatisticsService).GetMethods().Single(m => m.Name == "Build" && m.GetParameters().Length == 3);
        var result = (ArchiveStatisticsResult)build.Invoke(null, new object[] { new[] { archive }, new ArchiveStatisticsFilter(InstitutionName: School.Name), new[] { School } })!;
        Assert.Equal(3, result.InvoiceCount);
        Assert.Equal(1, result.InstitutionCount);
        Assert.Equal(300m, result.PaymentTotal);
        Assert.Equal(School.Name, Assert.Single(result.Rows).InstitutionName);
        Assert.StartsWith("1 |", archive.Invoices[0].InstitutionName, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("31 Ağustos Ortaokulu")]
    [InlineData("30 Ağustos İlkokulu")]
    public void ImportDoesNotStripLegitimateNumericSchoolNames(string name)
    {
        var method = typeof(DogruFatura.Wpf.HistoricalPdfImportService).Assembly.GetType("DogruFatura.Wpf.LegacyHysPdfImportService")!
            .GetMethod("NormalizeInstitutionCell", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        Assert.Equal(name, method.Invoke(null, new object[] { name }));
    }

    private static InvoiceRecord Invoice(string name) => new(Guid.NewGuid(), DateTime.Today, "Ortaokul", name, "", "Telefon", Guid.NewGuid().ToString("N"), "2766181022", DateTime.Today, DateTime.Today.AddDays(10), 100, 0, 100, 0, 100);
}
