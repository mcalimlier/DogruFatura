using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace DogruFatura.Wpf;

public static class RecoverySessionService
{
    private static readonly object Sync = new();
    private static string? _activeMarkerPath;

    public static string? PendingNotice { get; private set; }

    public static void Begin(
        string? markerRoot = null,
        Func<ApplicationSafetyService>? safetyServiceFactory = null)
    {
        lock (Sync)
        {
            var root = markerRoot
                       ?? Path.Combine(
                           Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                           "DoğruFatura");
            Directory.CreateDirectory(root);
            var marker = Path.Combine(root, "oturum_acik.marker");
            PendingNotice = null;

            if (File.Exists(marker))
            {
                try
                {
                    var safety = safetyServiceFactory?.Invoke() ?? new ApplicationSafetyService();
                    var backup = safety.CreateManualSafetyBackup("BeklenmeyenKapanmaSonrasi");
                    PendingNotice =
                        "Önceki DoğruFatura oturumu normal kapanmamış görünüyor. " +
                        "Mevcut veriler açılışta korunarak güvenlik yedeği alındı:\n" + backup;
                }
                catch (Exception ex)
                {
                    PendingNotice =
                        "Önceki DoğruFatura oturumu normal kapanmamış görünüyor. " +
                        "Otomatik güvenlik yedeği oluşturulamadı; Hakkında / Güvenlik ekranından veri bütünlüğünü kontrol edin. " +
                        "Ayrıntı: " + ex.Message;
                }
            }

            var content = new StringBuilder()
                .AppendLine("DoğruFatura çalışma oturumu")
                .AppendLine(CultureInfo.InvariantCulture, $"Başlangıç: {DateTimeOffset.Now:O}")
                .AppendLine(CultureInfo.InvariantCulture, $"PID: {Environment.ProcessId}")
                .ToString();
            File.WriteAllText(marker, content, new UTF8Encoding(false));
            _activeMarkerPath = marker;
        }
    }

    public static void EndCleanly()
    {
        lock (Sync)
        {
            if (string.IsNullOrWhiteSpace(_activeMarkerPath)) return;
            try
            {
                if (File.Exists(_activeMarkerPath)) File.Delete(_activeMarkerPath);
            }
            finally
            {
                _activeMarkerPath = null;
            }
        }
    }

    public static void ClearPendingNotice() => PendingNotice = null;

    public static bool IsMarkerProcessStillRunning(string markerPath)
    {
        if (!File.Exists(markerPath)) return false;
        try
        {
            var pidLine = File.ReadLines(markerPath)
                .FirstOrDefault(line => line.StartsWith("PID:", StringComparison.OrdinalIgnoreCase));
            if (pidLine is null || !int.TryParse(pidLine[4..].Trim(), out var pid) || pid <= 0)
                return false;
            using var process = Process.GetProcessById(pid);
            return !process.HasExited;
        }
        catch
        {
            return false;
        }
    }
}
