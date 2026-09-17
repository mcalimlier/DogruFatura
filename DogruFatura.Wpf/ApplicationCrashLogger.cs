using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace DogruFatura.Wpf;

internal static class ApplicationCrashLogger
{
    private const int RetainedLogCount = 50;

    public static string LogFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
        "DoğruFatura",
        "Loglar");

    public static string Write(Exception exception, string source)
    {
        ArgumentNullException.ThrowIfNull(exception);
        var content = BuildContent(exception, source);

        try
        {
            Directory.CreateDirectory(LogFolder);
            var path = CreateUniqueLogPath(LogFolder);
            File.WriteAllText(path, content, new UTF8Encoding(false));
            PruneLogs(LogFolder);
            return path;
        }
        catch
        {
            try
            {
                var fallbackFolder = Path.Combine(Path.GetTempPath(), "DoğruFatura", "Loglar");
                Directory.CreateDirectory(fallbackFolder);
                var fallbackPath = CreateUniqueLogPath(fallbackFolder);
                File.WriteAllText(fallbackPath, content, new UTF8Encoding(false));
                return fallbackPath;
            }
            catch
            {
                return "Hata kaydı dosyaya yazılamadı.";
            }
        }
    }

    private static string BuildContent(Exception exception, string source)
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "bilinmiyor";

        var builder = new StringBuilder();
        builder.AppendLine("DoğruFatura beklenmeyen hata kaydı");
        builder.AppendLine(new string('=', 54));
        builder.AppendLine(CultureInfo.CurrentCulture, $"Tarih         : {DateTimeOffset.Now:dd.MM.yyyy HH:mm:ss zzz}");
        builder.AppendLine(CultureInfo.CurrentCulture, $"Kaynak        : {source}");
        builder.AppendLine(CultureInfo.CurrentCulture, $"Sürüm         : {informationalVersion}");
        builder.AppendLine(CultureInfo.CurrentCulture, $".NET          : {Environment.Version}");
        builder.AppendLine(CultureInfo.CurrentCulture, $"İşletim Sistemi: {RuntimeInformation.OSDescription}");
        builder.AppendLine(CultureInfo.CurrentCulture, $"İşlem Mimarisi : {RuntimeInformation.ProcessArchitecture}");
        builder.AppendLine(CultureInfo.CurrentCulture, $"Makine        : {Environment.MachineName}");
        builder.AppendLine();
        builder.AppendLine(exception.ToString());
        return builder.ToString();
    }

    private static string CreateUniqueLogPath(string folder)
    {
        var baseName = $"DogruFatura_Hata_{DateTime.Now:yyyyMMdd_HHmmss_fff}";
        var path = Path.Combine(folder, baseName + ".log");
        var suffix = 2;
        while (File.Exists(path))
            path = Path.Combine(folder, $"{baseName}_{suffix++}.log");
        return path;
    }

    private static void PruneLogs(string folder)
    {
        foreach (var file in new DirectoryInfo(folder)
                     .GetFiles("DogruFatura_Hata_*.log")
                     .OrderByDescending(file => file.LastWriteTimeUtc)
                     .Skip(RetainedLogCount))
        {
            try { file.Delete(); } catch { }
        }
    }
}
