using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using DogruFatura.Wpf;
using System.IO.Compression;
using System.Text.Json;

namespace DogruFatura.Tests;

public sealed class BackupManifestIntegrityTests
{
    [Fact]
    public void ManualBackup_WritesV2ManifestWithLengthAndSha256ForEveryStoredFile()
    {
        using var temp = new TempFolders();
        var service = new ApplicationSafetyService(temp.Documents, temp.Desktop, () => "1.1.0-test");
        SeedCompleteData(service.DataFolder);

        var backup = service.CreateManualSafetyBackup("ManifestV2");

        using var archive = ZipFile.OpenRead(backup);
        var manifestEntry = archive.GetEntry("DogruFatura_Yedegi.json");
        Assert.NotNull(manifestEntry);
        using var manifestStream = manifestEntry!.Open();
        using var document = JsonDocument.Parse(manifestStream);
        var root = document.RootElement;
        Assert.Equal("DogruFaturaBackup/2", root.GetProperty("Format").GetString());

        var manifestFiles = root.GetProperty("Files").EnumerateArray().ToArray();
        var zipFiles = archive.Entries
            .Where(entry => !entry.FullName.EndsWith('/')
                            && entry.FullName != "DogruFatura_Yedegi.json")
            .ToArray();
        Assert.Equal(zipFiles.Length, manifestFiles.Length);

        foreach (var item in manifestFiles)
        {
            var path = item.GetProperty("RelativePath").GetString();
            var byteLength = item.GetProperty("ByteLength").GetInt64();
            var sha256 = item.GetProperty("Sha256").GetString();
            var entry = archive.GetEntry(path!);
            Assert.NotNull(entry);
            Assert.Equal(entry!.Length, byteLength);
            Assert.True(FileIntegrityService.IsValidSha256(sha256));
        }
    }

    private static void SeedCompleteData(string dataFolder)
    {
        Directory.CreateDirectory(dataFolder);
        new SchoolDataStore(Path.Combine(dataFolder, "Okullar.json")).Save(Array.Empty<SchoolRecord>());
        new InvoiceDataStore(Path.Combine(dataFolder, "AktifFaturalar.json")).Save(Array.Empty<InvoiceRecord>());
        new AuthorityProfileDataStore(Path.Combine(dataFolder, "YetkiliProfili.json"))
            .Save(AuthorityProfileDefaults.Create());
        new ArchivedInvoiceListDataStore(Path.Combine(dataFolder, "IslemArsivi.json"))
            .Save(Array.Empty<ArchivedInvoiceList>());
    }

    private sealed class TempFolders : IDisposable
    {
        private readonly string _root = Path.Combine(
            Path.GetTempPath(),
            "DogruFatura_BackupManifestTests_" + Guid.NewGuid().ToString("N"));

        public TempFolders()
        {
            Documents = Path.Combine(_root, "Documents");
            Desktop = Path.Combine(_root, "Desktop");
            Directory.CreateDirectory(Documents);
            Directory.CreateDirectory(Desktop);
        }

        public string Documents { get; }
        public string Desktop { get; }

        public void Dispose()
        {
            try { if (Directory.Exists(_root)) Directory.Delete(_root, true); } catch { }
        }
    }
}
