using System.Diagnostics;
using System.IO;
using System.Text;

namespace DogruFatura.Wpf;

public static class StartupPerformanceService
{
    private static readonly Stopwatch Watch = new();

    public static TimeSpan? LastStartupDuration { get; private set; }

    public static void Begin()
    {
        LastStartupDuration = null;
        Watch.Restart();
    }

    public static TimeSpan Complete()
    {
        if (!Watch.IsRunning) return LastStartupDuration ?? TimeSpan.Zero;
        Watch.Stop();
        LastStartupDuration = Watch.Elapsed;
        if (Watch.Elapsed >= TimeSpan.FromSeconds(5))
            WriteSlowStartupLog(Watch.Elapsed);
        return Watch.Elapsed;
    }

    private static void WriteSlowStartupLog(TimeSpan duration)
    {
        try
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                "DoğruFatura",
                "Loglar");
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, $"DogruFatura_AcilisPerformansi_{DateTime.Now:yyyyMMdd_HHmmss}.log");
            File.WriteAllText(
                path,
                $"DoğruFatura açılış süresi: {duration.TotalSeconds:0.00} saniye{Environment.NewLine}" +
                $"Sürüm: {AppVersionInfo.InformationalVersion}{Environment.NewLine}" +
                $"Çalışma ortamı: .NET {Environment.Version}, {(Environment.Is64BitProcess ? "64-bit" : "32-bit")}{Environment.NewLine}",
                new UTF8Encoding(false));
        }
        catch
        {
            // Performans ölçümü uygulamanın açılmasını asla engellemez.
        }
    }
}
