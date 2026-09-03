using DogruFatura.Wpf;
using System.Text;

namespace DogruFatura.Tests;

public sealed class DataSafetyIntegrationTests
{
    [Fact]
    public void ManualBackup_Restore_RoundTripRestoresDataAndPdf()
    {
        using var temp = new TempFolders();
        var service = new ApplicationSafetyService(temp.Documents, temp.Desktop, () => "1.0.0-rc1-test");
        service.InitializeAndCheck();
        SeedCompleteData(service.DataFolder);
        Directory.CreateDirectory(service.ActivePdfFolder);
        var pdfPath = Path.Combine(service.ActivePdfFolder, "AKTIF-TEST.pdf");
        File.WriteAllBytes(pdfPath, Encoding.ASCII.GetBytes("%PDF-1.4\n% test\n"));

        var originalSchools = "[{\"Name\":\"Asıl Kurum\"}]";
        File.WriteAllText(Path.Combine(service.DataFolder, "Okullar.json"), originalSchools, Encoding.UTF8);
        var backup = service.CreateManualSafetyBackup("Test");

        File.WriteAllText(Path.Combine(service.DataFolder, "Okullar.json"), "[{\"Name\":\"DEĞİŞTİ\"}]", Encoding.UTF8);
        File.Delete(pdfPath);

        var result = service.RestoreFromBackup(backup);

        Assert.True(result.Success, result.Message);
        Assert.Equal(originalSchools, File.ReadAllText(Path.Combine(service.DataFolder, "Okullar.json"), Encoding.UTF8));
        Assert.True(File.Exists(pdfPath));
        Assert.NotNull(result.SafetyBackupPath);
        Assert.True(File.Exists(result.SafetyBackupPath));
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

        var service = new ApplicationSafetyService(temp.Documents, temp.Desktop, () => "1.0.0-rc1-test");
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
        File.WriteAllText(Path.Combine(desktopData, "Okullar.json"), "[{\"Name\":\"Yeni\"}]", Encoding.UTF8);

        var service = new ApplicationSafetyService(temp.Documents, temp.Desktop, () => "1.0.0-rc1-test");
        var result = service.InitializeAndCheck();

        Assert.Equal("[{\"Name\":\"Yeni\"}]", File.ReadAllText(Path.Combine(desktopData, "Okullar.json"), Encoding.UTF8));
        Assert.True(File.Exists(Path.Combine(legacyData, "Okullar.json")));
        Assert.Contains("eski konumunda bırakıldı", result.Message, StringComparison.CurrentCultureIgnoreCase);
    }

    private static void SeedCompleteData(string dataFolder)
    {
        Directory.CreateDirectory(dataFolder);
        File.WriteAllText(Path.Combine(dataFolder, "Okullar.json"), "[]", Encoding.UTF8);
        File.WriteAllText(Path.Combine(dataFolder, "AktifFaturalar.json"), "[]", Encoding.UTF8);
        File.WriteAllText(Path.Combine(dataFolder, "YetkiliProfili.json"), "{}", Encoding.UTF8);
        File.WriteAllText(Path.Combine(dataFolder, "IslemArsivi.json"), "[]", Encoding.UTF8);
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
