using DogruFatura.Core.Models;
using DogruFatura.Core.Services;

namespace DogruFatura.Tests;

public sealed class WorkbenchTests
{
    private static SchoolRecord School => new("İlkokul", "Işık İlkokulu", "0012345678", "00012", "0013", "0014", "0015", Guid.NewGuid());
    private static InvoiceRecord Invoice(DateTime due) => new(Guid.NewGuid(), DateTime.Now, "İlkokul", "Işık İlkokulu", "0012345678", "Elektrik", "F1", "00012", due.AddDays(-10), due, 120m, 20m, 100m, .948m, 119.052m);

    [Fact]
    public void DashboardCountsDueTodayAndNextSevenDaysButSeparatesOverdue()
    {
        var today = new DateTime(2026, 10, 27);
        var summary = WorkbenchService.Summarize(new[] { Invoice(today.AddDays(-1)), Invoice(today), Invoice(today.AddDays(7)), Invoice(today.AddDays(8)) }, Array.Empty<ArchivedInvoiceList>(), today);
        Assert.Equal(2, summary.DueSoonCount);
        Assert.Equal(1, summary.OverdueCount);
        Assert.Equal(4, summary.ActiveCount);
    }

    [Fact]
    public void FilterUsesTurkishSearchInclusiveDatesAndAmounts()
    {
        var invoice = Invoice(new DateTime(2026, 10, 27));
        Assert.True(WorkbenchService.Matches(invoice, new InvoiceWorkbenchFilter("ışık", invoice.DueDate, invoice.DueDate, 120, 120)));
        Assert.False(WorkbenchService.Matches(invoice, new InvoiceWorkbenchFilter("", null, null, 121)));
    }

    [Fact]
    public void PasteCalculatesUsingExistingEngineAndPreservesIdentifiers()
    {
        var school = School;
        var result = InvoicePasteService.Preview("Kurum\tTür\tFatura No\tFatura Tarihi\tSon Ödeme\tTutar\tKDV\nIşık İlkokulu\tElektrik\t00042\t20.10.2026\t27.10.2026\t1.200,00\t200,00", new[] { school }, Array.Empty<InvoiceRecord>());
        Assert.True(result.CanImport);
        var invoice = Assert.Single(result.Rows).Invoice!;
        Assert.Equal("00042", invoice.InvoiceNumber);
        Assert.Equal("00012", invoice.ServiceNumber);
        Assert.Equal(1000m, invoice.VatBase);
        Assert.Equal(9.48m, invoice.StampDuty);
    }

    [Theory]
    [InlineData("Bilinmeyen\tElektrik\t1\t20.10.2026\t27.10.2026\t120\t20")]
    [InlineData("Işık İlkokulu\tElektrik\t1\t28.10.2026\t27.10.2026\t120\t20")]
    [InlineData("Işık İlkokulu\tElektrik\t1\t20.10.2026\t27.10.2026\t120\t200")]
    [InlineData("Işık İlkokulu\tElektrik\t1\t20.10.2026\t27.10.2026\t120\t20\textra\textra")]
    public void PasteRejectsInvalidRowsWithoutPartialImport(string text)
    {
        Assert.False(InvoicePasteService.Preview(text, new[] { School }, Array.Empty<InvoiceRecord>()).CanImport);
    }

    [Fact]
    public void PasteRejectsDuplicateWithinBatchAndMixedTypes()
    {
        const string row = "Işık İlkokulu\tElektrik\t42\t20.10.2026\t27.10.2026\t120\t20";
        Assert.False(InvoicePasteService.Preview(row + "\n" + row, new[] { School }, Array.Empty<InvoiceRecord>()).CanImport);
        Assert.False(InvoicePasteService.Preview(row + "\n" + row.Replace("Elektrik", "Su", StringComparison.Ordinal), new[] { School }, Array.Empty<InvoiceRecord>()).CanImport);
    }

    [Fact]
    public void DraftsRoundTripAndRejectCorruptStoreWithoutOverwriting()
    {
        var folder = Path.Combine(Path.GetTempPath(), "DogruFatura-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var path = Path.Combine(folder, "drafts.json");
            var store = new InvoiceDraftStore(path);
            var draft = new InvoiceDraft(Guid.NewGuid(), "Ekim elektrik", DateTimeOffset.Now, new() { Invoice(DateTime.Today) });
            store.Save(new[] { draft });
            Assert.Equal(draft.Invoices[0].Id, Assert.Single(store.Load()).Invoices[0].Id);
            File.WriteAllText(path, "broken");
            Assert.ThrowsAny<Exception>(() => store.Load());
            Assert.Equal("broken", File.ReadAllText(path));
        }
        finally { Directory.Delete(folder, true); }
    }

    [Fact]
    public void ReviewedPdfRequiresMatchingOfficialTotalsAndReason()
    {
        var invoice = Invoice(new DateTime(2026, 10, 27));
        var rows = new[] { invoice };
        Assert.False(PdfReviewService.Review("2026-12345678", rows, 120m, .95m, 119.05m, "", AuthorityProfileDefaults.Create()).IsValid);
        Assert.False(PdfReviewService.Review("2026-12345678", rows, 120m, .95m, 118m, "Kaynak belge ile karşılaştırıldı", AuthorityProfileDefaults.Create()).IsValid);
        var result = PdfReviewService.Review("2026-12345678", rows, 120m, .95m, 119.05m, "OCR fatura numarası düzeltildi", AuthorityProfileDefaults.Create());
        Assert.True(result.IsValid, result.Message);
        Assert.True(result.Archive!.IsClosed);
        Assert.True(ClosedArchiveSnapshotService.Matches(result.Archive));
        Assert.Contains("OCR fatura numarası", result.Archive.LegacyEvidenceNote, StringComparison.Ordinal);
    }
}
