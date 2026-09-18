using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using System.Text;

namespace DogruFatura.Tests;

public sealed class ArchiveStorageIntegrationTests
{
    [Fact]
    public void CloseAndReopenArchiveMovesPdfBothDirections()
    {
        using var temp = new TempFolder();
        var storage = new DocumentArchiveStorageService(temp.Path);
        var invoices = new List<InvoiceRecord> { Invoice() };
        var archivedAt = new DateTime(2026, 9, 3);
        var activePath = storage.GetActiveOutputPath(invoices, "Elektrik", archivedAt);
        File.WriteAllBytes(activePath, Encoding.ASCII.GetBytes("%PDF-1.4\narchive-test"));

        var archive = new ArchivedInvoiceList(
            Guid.NewGuid(), "ARS-0001", archivedAt, "Elektrik", invoices,
            AuthorityProfileDefaults.Create(), PdfFileName: Path.GetFileName(activePath));

        const string hys = "HYS-2026123456";
        var closeMove = storage.MoveActiveOutputToArchive(archive, hys);

        Assert.True(closeMove.Moved);
        Assert.False(File.Exists(activePath));
        Assert.True(File.Exists(closeMove.DestinationPath));
        Assert.Equal(hys + ".pdf", closeMove.PdfFileName);

        var closedArchive = archive with
        {
            HysPaymentOrderDocumentNumber = hys,
            IsClosed = true,
            ClosedAt = archivedAt,
            PdfFileName = closeMove.PdfFileName
        };
        var reopenMove = storage.MoveArchivedOutputToActive(closedArchive);

        Assert.True(reopenMove.Moved);
        Assert.False(File.Exists(closeMove.DestinationPath));
        Assert.True(File.Exists(reopenMove.DestinationPath));
        Assert.StartsWith("AKTIF-ELEKTRIK-", reopenMove.PdfFileName, StringComparison.Ordinal);
    }

    [Fact]
    public void ImportedHistoricalPdfIsCopiedAndOriginalRemains()
    {
        using var temp = new TempFolder();
        var storage = new DocumentArchiveStorageService(temp.Path);
        var sourceFolder = System.IO.Path.Combine(temp.Path, "Kaynak");
        Directory.CreateDirectory(sourceFolder);
        var source = System.IO.Path.Combine(sourceFolder, "48) HYS-2026-34546180.pdf");
        File.WriteAllBytes(source, Encoding.ASCII.GetBytes("%PDF-1.4\nimport-test"));

        var fileName = storage.CopyImportedPdf(source, "HYS-202634546180");

        Assert.True(File.Exists(source));
        Assert.Equal("HYS-202634546180.pdf", fileName);
        Assert.True(File.Exists(System.IO.Path.Combine(storage.ArchivedOutputDirectory, fileName)));
    }

    [Fact]
    public void ExistingArchivePdfIsNeverOverwritten()
    {
        using var temp = new TempFolder();
        var storage = new DocumentArchiveStorageService(temp.Path);
        var source = System.IO.Path.Combine(temp.Path, "source.pdf");
        File.WriteAllBytes(source, Encoding.ASCII.GetBytes("%PDF-1.4\nsource"));
        var destination = storage.GetArchivedOutputPath("HYS-2026999999");
        File.WriteAllBytes(destination, Encoding.ASCII.GetBytes("%PDF-1.4\nexisting"));

        Assert.Throws<IOException>(() => storage.CopyImportedPdf(source, "HYS-2026999999"));
        Assert.Contains("existing", Encoding.ASCII.GetString(File.ReadAllBytes(destination)), StringComparison.Ordinal);
    }

    private static InvoiceRecord Invoice()
    {
        var calculation = new TaxCalculationService().Calculate(1500m, 250m, "Elektrik");
        return new InvoiceRecord(
            Guid.NewGuid(), new DateTime(2026, 9, 3, 10, 0, 0),
            "İlkokul", "Şehit Öğretmen Çağdaş Türkçe İlkokulu", "1234567890",
            "Elektrik", "TR-ÇĞİÖŞÜ-2026/000001", "ELK-000000000000000001",
            new DateTime(2026, 9, 1), new DateTime(2026, 9, 15),
            1500m, 250m, calculation.VatBase, calculation.StampDuty, calculation.PaymentAmount);
    }

    private sealed class TempFolder : IDisposable
    {
        public TempFolder()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "DogruFatura_ArchiveTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try { if (Directory.Exists(Path)) Directory.Delete(Path, true); } catch { }
        }
    }
}
