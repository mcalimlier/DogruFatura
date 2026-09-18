using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using DogruFatura.Wpf;
using System.IO.Compression;
using System.Text;

namespace DogruFatura.Tests;

public sealed class DataSafetyIntegrationTests
{
    [Fact]
    public void ManualBackupRestoreRoundTripRestoresDataAndPdf()
    {
        using var temp = new TempFolders();
        var service = new ApplicationSafetyService(temp.Documents, temp.Desktop, () => "1.1.0-test");
        service.InitializeAndCheck();
        SeedCompleteData(service.DataFolder);
        Directory.CreateDirectory(service.ActivePdfFolder);
        var pdfPath = Path.Combine(service.ActivePdfFolder, "AKTIF-TEST.pdf");
        File.WriteAllBytes(pdfPath, Encoding.ASCII.GetBytes("%PDF-1.4\n% test\n"));

        var schoolsPath = Path.Combine(service.DataFolder, "Okullar.json");
        new SchoolDataStore(schoolsPath).Save(new[] { School("Asıl Kurum") });
        var originalSchools = File.ReadAllText(schoolsPath, Encoding.UTF8);
        var backup = service.CreateManualSafetyBackup("Test");

        new SchoolDataStore(schoolsPath).Save(new[] { School("Değişen Kurum") });
        File.Delete(pdfPath);

        var result = service.RestoreFromBackup(backup);

        Assert.True(result.Success, result.Message);
        Assert.Equal(originalSchools, File.ReadAllText(schoolsPath, Encoding.UTF8));
        Assert.True(File.Exists(pdfPath));
        Assert.NotNull(result.SafetyBackupPath);
        Assert.True(File.Exists(result.SafetyBackupPath));
    }

    [Fact]
    public void RestoreTamperedV2BackupIsRejectedWithoutChangingLiveData()
    {
        using var temp = new TempFolders();
        var service = new ApplicationSafetyService(temp.Documents, temp.Desktop, () => "1.1.0-test");
        service.InitializeAndCheck();
        SeedCompleteData(service.DataFolder);
        var backup = service.CreateManualSafetyBackup("HashTest");

        using (var archive = ZipFile.Open(backup, ZipArchiveMode.Update, Encoding.UTF8))
        {
            var entry = archive.GetEntry("Veri/Okullar.json")!;
            entry.Delete();
            var replacement = archive.CreateEntry("Veri/Okullar.json", CompressionLevel.Optimal);
            using var writer = new StreamWriter(replacement.Open(), new UTF8Encoding(false));
            writer.Write("[]");
        }

        var schoolsPath = Path.Combine(service.DataFolder, "Okullar.json");
        new SchoolDataStore(schoolsPath).Save(new[] { School("CANLI HASH KORUMA") });
        var before = File.ReadAllText(schoolsPath, Encoding.UTF8);

        var result = service.RestoreFromBackup(backup);

        Assert.False(result.Success);
        Assert.Null(result.SafetyBackupPath);
        Assert.Contains("manifest", result.Message, StringComparison.CurrentCultureIgnoreCase);
        Assert.Equal(before, File.ReadAllText(schoolsPath, Encoding.UTF8));
    }

    [Fact]
    public void RestoreSemanticallyInvalidBackupIsRejectedWithoutChangingLiveData()
    {
        using var temp = new TempFolders();
        var service = new ApplicationSafetyService(temp.Documents, temp.Desktop, () => "1.1.0-test");
        service.InitializeAndCheck();
        SeedCompleteData(service.DataFolder);

        File.WriteAllText(Path.Combine(service.DataFolder, "YetkiliProfili.json"), "{}", Encoding.UTF8);
        var invalidBackup = service.CreateManualSafetyBackup("SemantikGecersiz");

        SeedCompleteData(service.DataFolder);
        var schoolsPath = Path.Combine(service.DataFolder, "Okullar.json");
        new SchoolDataStore(schoolsPath).Save(new[] { School("CANLI VERI KORUNACAK") });
        var liveSchoolsBefore = File.ReadAllText(schoolsPath, Encoding.UTF8);

        var result = service.RestoreFromBackup(invalidBackup);

        Assert.False(result.Success);
        Assert.Null(result.SafetyBackupPath);
        Assert.Contains("semantik", result.Message, StringComparison.CurrentCultureIgnoreCase);
        Assert.Equal(liveSchoolsBefore, File.ReadAllText(schoolsPath, Encoding.UTF8));
    }

    [Fact]
    public void RestoreBackupMissingRequiredDataFileIsRejectedWithoutChangingLiveData()
    {
        using var temp = new TempFolders();
        var service = new ApplicationSafetyService(temp.Documents, temp.Desktop, () => "1.1.0-test");
        service.InitializeAndCheck();

        Directory.CreateDirectory(service.DataFolder);
        foreach (var fileName in DataDirectoryValidationService.RequiredFileNames)
        {
            var path = Path.Combine(service.DataFolder, fileName);
            if (File.Exists(path)) File.Delete(path);
        }
        new SchoolDataStore(Path.Combine(service.DataFolder, "Okullar.json"))
            .Save(new[] { School("Eksik Yedek Kurumu") });
        var incompleteBackup = service.CreateManualSafetyBackup("EksikVeri");

        SeedCompleteData(service.DataFolder);
        var authorityPath = Path.Combine(service.DataFolder, "YetkiliProfili.json");
        var liveAuthorityBefore = File.ReadAllText(authorityPath, Encoding.UTF8);

        var result = service.RestoreFromBackup(incompleteBackup);

        Assert.False(result.Success);
        Assert.Null(result.SafetyBackupPath);
        Assert.Contains("eksik", result.Message, StringComparison.CurrentCultureIgnoreCase);
        Assert.Equal(liveAuthorityBefore, File.ReadAllText(authorityPath, Encoding.UTF8));
    }

    [Fact]
    public void CheckIntegrityCurrentSchemaCompleteDataIsHealthy()
    {
        using var temp = new TempFolders();
        var service = new ApplicationSafetyService(temp.Documents, temp.Desktop, () => "1.1.0-test");
        service.InitializeAndCheck();
        SeedCompleteData(service.DataFolder);

        var result = service.CheckIntegrity();

        Assert.True(result.IsHealthy, result.Message);
        Assert.Equal(4, result.JsonFileCount);
        Assert.Equal(0, result.ProblemCount);
    }

    [Fact]
    public void CheckIntegrityChangedClosedArchivePdfIsReportedBySha256()
    {
        using var temp = new TempFolders();
        var service = new ApplicationSafetyService(temp.Documents, temp.Desktop, () => "1.1.0-test");
        service.InitializeAndCheck();
        SeedCompleteData(service.DataFolder);
        Directory.CreateDirectory(service.ArchivedPdfFolder);

        const string pdfFileName = "HYS-12345.pdf";
        var pdfPath = Path.Combine(service.ArchivedPdfFolder, pdfFileName);
        File.WriteAllBytes(pdfPath, Encoding.ASCII.GetBytes("%PDF-1.4\nORIGINAL\n"));
        var invoice = Invoice("ARCH-HASH-001");
        var archive = new ArchivedInvoiceList(
            Guid.NewGuid(),
            "DF-HASH-001",
            new DateTime(2026, 9, 17),
            "Elektrik",
            new List<InvoiceRecord> { invoice },
            AuthorityProfileDefaults.Create(),
            "HYS-12345",
            true,
            new DateTime(2026, 9, 17),
            pdfFileName,
            FileIntegrityService.ComputeSha256(pdfPath));
        new ArchivedInvoiceListDataStore(Path.Combine(service.DataFolder, "IslemArsivi.json"))
            .Save(new[] { archive });

        var healthy = service.CheckIntegrity();
        Assert.True(healthy.IsHealthy, healthy.Message);

        File.WriteAllBytes(pdfPath, Encoding.ASCII.GetBytes("%PDF-1.4\nCHANGED\n"));
        var changed = service.CheckIntegrity();

        Assert.False(changed.IsHealthy);
        Assert.Contains("SHA-256", changed.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CheckIntegritySemanticallyInvalidDataIsReported()
    {
        using var temp = new TempFolders();
        var service = new ApplicationSafetyService(temp.Documents, temp.Desktop, () => "1.1.0-test");
        service.InitializeAndCheck();
        SeedCompleteData(service.DataFolder);
        File.WriteAllText(Path.Combine(service.DataFolder, "YetkiliProfili.json"), "{}", Encoding.UTF8);

        var result = service.CheckIntegrity();

        Assert.False(result.IsHealthy);
        Assert.True(result.ProblemCount > 0);
        Assert.Contains("YetkiliProfili.json", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LegacyDocumentsStorageMigratesToDesktopWithoutDataLoss()
    {
        using var temp = new TempFolders();
        var legacyRoot = Path.Combine(temp.Documents, "DoğruFatura");
        SeedCompleteData(Path.Combine(legacyRoot, "Veri"));
        Directory.CreateDirectory(Path.Combine(legacyRoot, "Yedekler"));
        Directory.CreateDirectory(Path.Combine(legacyRoot, "Loglar"));
        File.WriteAllText(Path.Combine(legacyRoot, "Yedekler", "eski.zip"), "backup", Encoding.UTF8);
        File.WriteAllText(Path.Combine(legacyRoot, "Loglar", "eski.log"), "log", Encoding.UTF8);

        var service = new ApplicationSafetyService(temp.Documents, temp.Desktop, () => "1.1.0-test");
        var result = service.InitializeAndCheck();

        Assert.True(File.Exists(Path.Combine(service.DataFolder, "Okullar.json")));
        Assert.True(File.Exists(Path.Combine(service.BackupFolder, "eski.zip")));
        Assert.True(File.Exists(Path.Combine(service.LogFolder, "eski.log")));
        Assert.Contains("Masaüstü", result.Message, StringComparison.CurrentCultureIgnoreCase);
        Assert.False(File.Exists(Path.Combine(legacyRoot, "Veri", "Okullar.json")));
    }

    [Fact]
    public void LegacyMigrationDoesNotOverwriteExistingDesktopFile()
    {
        using var temp = new TempFolders();
        var legacyData = Path.Combine(temp.Documents, "DoğruFatura", "Veri");
        SeedCompleteData(legacyData);

        var desktopData = Path.Combine(temp.Desktop, "DoğruFatura", "Veri");
        Directory.CreateDirectory(desktopData);
        File.WriteAllText(Path.Combine(desktopData, "Okullar.json"), "DESKTOP-KORUNACAK", Encoding.UTF8);

        var service = new ApplicationSafetyService(temp.Documents, temp.Desktop, () => "1.1.0-test");
        var result = service.InitializeAndCheck();

        Assert.Equal("DESKTOP-KORUNACAK", File.ReadAllText(Path.Combine(desktopData, "Okullar.json"), Encoding.UTF8));
        Assert.True(File.Exists(Path.Combine(legacyData, "Okullar.json")));
        Assert.Contains("eski konumunda bırakıldı", result.Message, StringComparison.CurrentCultureIgnoreCase);
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

    private static SchoolRecord School(string name) => new(
        "İlkokul", name, "1234567890",
        "1111111111", "2222222222", "3333333333", "4444444444");

    private static InvoiceRecord Invoice(string number)
    {
        var calculation = new TaxCalculationService().Calculate(1500m, 250m, "Elektrik");
        return new InvoiceRecord(
            Guid.NewGuid(), new DateTime(2026, 9, 17, 10, 0, 0),
            "İlkokul", "Hash Test Okulu", "1234567890",
            "Elektrik", number, "1111111111",
            new DateTime(2026, 9, 1), new DateTime(2026, 9, 15),
            1500m, 250m, calculation.VatBase, calculation.StampDuty, calculation.PaymentAmount);
    }

    private sealed class TempFolders : IDisposable
    {
        private readonly string _root = Path.Combine(Path.GetTempPath(), "DogruFatura_Tests_" + Guid.NewGuid().ToString("N"));

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
