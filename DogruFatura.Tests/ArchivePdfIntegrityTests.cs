using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using DogruFatura.Wpf;
using System.Text;

namespace DogruFatura.Tests;

public sealed class ArchivePdfIntegrityTests
{
    [Fact]
    public void Synchronize_LegacyPdfWithoutHash_StoresInitialSha256()
    {
        using var temp = new TempFolder();
        var storage = new DocumentArchiveStorageService(temp.Path);
        var archive = Archive();
        var path = storage.GetActiveOutputPath(archive.Invoices, archive.InvoiceType, archive.ArchivedAt.Date);
        File.WriteAllBytes(path, Encoding.ASCII.GetBytes("%PDF-1.4\nLEGACY-PDF\n"));
        archive = archive with { PdfFileName = Path.GetFileName(path) };
        var service = Service(storage);

        var result = service.Synchronize(new[] { archive });

        Assert.Empty(result.Errors);
        var updated = Assert.Single(result.UpdatedArchives);
        Assert.Equal(FileIntegrityService.ComputeSha256(path), updated.PdfSha256);
        Assert.True(FileIntegrityService.IsValidSha256(updated.PdfSha256));
    }

    [Fact]
    public void Synchronize_ExistingPdfWithDifferentHash_ReportsIntegrityErrorAndKeepsRecordedHash()
    {
        using var temp = new TempFolder();
        var storage = new DocumentArchiveStorageService(temp.Path);
        var archive = Archive();
        var path = storage.GetActiveOutputPath(archive.Invoices, archive.InvoiceType, archive.ArchivedAt.Date);
        File.WriteAllBytes(path, Encoding.ASCII.GetBytes("%PDF-1.4\nORIGINAL-CONTENT\n"));
        var recordedHash = FileIntegrityService.ComputeSha256(path);
        archive = archive with
        {
            PdfFileName = Path.GetFileName(path),
            PdfSha256 = recordedHash
        };
        File.WriteAllBytes(path, Encoding.ASCII.GetBytes("%PDF-1.4\nCHANGED-CONTENT\n"));
        var service = Service(storage);

        var result = service.Synchronize(new[] { archive });

        Assert.Single(result.Errors);
        Assert.Contains("bütünlüğü", result.Errors[0], StringComparison.CurrentCultureIgnoreCase);
        Assert.Empty(result.UpdatedArchives);
        Assert.Equal(recordedHash, archive.PdfSha256);
    }

    private static ArchivePdfSynchronizationService Service(DocumentArchiveStorageService storage) =>
        new(storage, (_, path) =>
            File.WriteAllBytes(path, Encoding.ASCII.GetBytes("%PDF-1.4\nGENERATED\n")));

    private static ArchivedInvoiceList Archive()
    {
        var calculation = new TaxCalculationService().Calculate(1500m, 250m, "Elektrik");
        var invoice = new InvoiceRecord(
            Guid.NewGuid(), new DateTime(2026, 9, 17, 10, 0, 0),
            "İlkokul", "PDF Hash Test Okulu", "1234567890",
            "Elektrik", "PDF-HASH-001", "1111111111",
            new DateTime(2026, 9, 1), new DateTime(2026, 9, 15),
            1500m, 250m, calculation.VatBase, calculation.StampDuty, calculation.PaymentAmount);

        return new ArchivedInvoiceList(
            Guid.NewGuid(),
            "DF-PDF-HASH-001",
            new DateTime(2026, 9, 17),
            "Elektrik",
            new List<InvoiceRecord> { invoice },
            AuthorityProfileDefaults.Create());
    }

    private sealed class TempFolder : IDisposable
    {
        public TempFolder()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "DogruFatura_PdfIntegrityTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try { if (Directory.Exists(Path)) Directory.Delete(Path, true); } catch { }
        }
    }
}
