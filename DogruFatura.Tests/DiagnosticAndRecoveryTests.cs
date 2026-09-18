using DogruFatura.Wpf;
using System.IO.Compression;
using System.Text;

namespace DogruFatura.Tests;

public sealed class DiagnosticAndRecoveryTests
{
    [Fact]
    public void DiagnosticPackageContainsSummaryAndLogsButNeverUserJsonOrPdf()
    {
        using var temp = new TempEnvironment();
        var safety = new ApplicationSafetyService(temp.Documents, temp.Desktop, () => "1.0.0-rc1-test");
        safety.InitializeAndCheck();
        Directory.CreateDirectory(safety.DataFolder);
        Directory.CreateDirectory(safety.ActivePdfFolder);
        Directory.CreateDirectory(safety.LogFolder);
        File.WriteAllText(Path.Combine(safety.DataFolder, "Okullar.json"), "[{\"secret\":true}]", Encoding.UTF8);
        File.WriteAllBytes(Path.Combine(safety.ActivePdfFolder, "secret.pdf"), Encoding.ASCII.GetBytes("%PDF-1.4\nsecret"));
        File.WriteAllText(Path.Combine(safety.LogFolder, "test.log"), "C:\\Users\\TestUser\\DoğruFatura hata", Encoding.UTF8);

        var package = DiagnosticPackageService.Create(safety);

        Assert.True(File.Exists(package));
        using var zip = ZipFile.OpenRead(package);
        var names = zip.Entries.Select(entry => entry.FullName).ToArray();
        Assert.Contains("Tanilama.txt", names);
        Assert.Contains("Loglar/test.log", names);
        Assert.DoesNotContain(names, name => name.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, name => name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void StaleSessionMarkerTriggersRecoveryBackupAndCleanExitRemovesMarker()
    {
        using var temp = new TempEnvironment();
        var markerRoot = Path.Combine(temp.Root, "Session");
        Directory.CreateDirectory(markerRoot);
        var marker = Path.Combine(markerRoot, "oturum_acik.marker");
        File.WriteAllText(marker, "PID: 999999\n", Encoding.UTF8);

        var safety = new ApplicationSafetyService(temp.Documents, temp.Desktop, () => "1.0.0-rc1-test");
        safety.InitializeAndCheck();
        Directory.CreateDirectory(safety.DataFolder);
        File.WriteAllText(Path.Combine(safety.DataFolder, "test.txt"), "korunacak", Encoding.UTF8);

        RecoverySessionService.Begin(markerRoot, () => safety);

        Assert.NotNull(RecoverySessionService.PendingNotice);
        Assert.Contains("güvenlik yedeği", RecoverySessionService.PendingNotice, StringComparison.CurrentCultureIgnoreCase);
        Assert.True(File.Exists(marker));
        Assert.NotEmpty(Directory.EnumerateFiles(Path.Combine(safety.BackupFolder, "Güvenlik"), "*.zip"));

        RecoverySessionService.EndCleanly();
        RecoverySessionService.ClearPendingNotice();
        Assert.False(File.Exists(marker));
    }

    private sealed class TempEnvironment : IDisposable
    {
        public TempEnvironment()
        {
            Root = Path.Combine(Path.GetTempPath(), "DogruFatura_DiagnosticTests_" + Guid.NewGuid().ToString("N"));
            Documents = Path.Combine(Root, "Documents");
            Desktop = Path.Combine(Root, "Desktop");
            Directory.CreateDirectory(Documents);
            Directory.CreateDirectory(Desktop);
        }

        public string Root { get; }
        public string Documents { get; }
        public string Desktop { get; }

        public void Dispose()
        {
            try { RecoverySessionService.EndCleanly(); } catch { }
            RecoverySessionService.ClearPendingNotice();
            try { if (Directory.Exists(Root)) Directory.Delete(Root, true); } catch { }
        }
    }
}
