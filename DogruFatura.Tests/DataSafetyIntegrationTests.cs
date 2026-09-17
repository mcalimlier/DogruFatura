using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using DogruFatura.Wpf;
using System.Text;

namespace DogruFatura.Tests;

public sealed class DataSafetyIntegrationTests
{
    [Fact]
    public void ManualBackup_Restore_RoundTripRestoresDataAndPdf()
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
    public void Restore_SemanticallyInvalidBackup_IsRejectedWithoutChangingLiveData()
    {
        using var temp = new TempFolders();
        var service = new ApplicationSafetyService(temp.Documents, temp.Desktop, () => "1.1.0-test");
        service.InitializeAndCheck();
        SeedCompleteData(service.DataFolder);

        File.WriteAllText(
            Path.Combine(service.DataFolder, "YetkiliProfili.json"),
            "{}",
            Encoding.UTF8);
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
    public void Restore_BackupMissingRequiredDataFile_IsRejectedWithoutChangingLiveData()
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
    public void CheckIntegrity_CurrentSchemaCompleteData_IsHealthy()
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
    public void CheckIntegrity_SemanticallyInvalidData_IsReported()
    {
        using var temp = new TempFolders();
        var service = new ApplicationSafetyService(temp.Documents, temp.Desktop, () => "1.1.0-test");
        service.InitializeAndCheck();
        SeedCompleteData(service.DataFolder);
        File.WriteAllText(
            Path.Combine(service.DataFolder, "YetkiliProfili.json"),
            "{}",
            Encoding.UTF8);

        var result = service.CheckIntegrity();

        Assert.False(result.IsHealthy);
        Assert.True(result.ProblemCount > 0);
        Assert.Contains("YetkiliProfili.json", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LegacyDocumentsStorage_MigratesToDesktopWithoutDataLoss()
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
    public void LegacyMigration_DoesNotOverwriteExistingDesktopFile()
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
        "İlkokul",
        name,
        "1234567890",
        "1111111111",
        "2222222222",
        "3333333333",
        "4444444444");

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
