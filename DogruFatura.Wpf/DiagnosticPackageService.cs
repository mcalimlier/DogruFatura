using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace DogruFatura.Wpf;

public sealed class DiagnosticPackageService
{
    private const int MaximumLogFiles = 5;
    private const int MaximumLogCharacters = 250_000;
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(250);

    private static readonly Regex SensitiveLabelRegex = new(
        @"(?im)\b(?<label>Kurum(?: Adı| Bilgisi)?|Okul(?: Adı)?|Yetkili(?: Adı)?|Ad Soyad|Gerçekleştirme Görevlisi|Harcama Yetkilisi|VKN|Vergi Kimlik(?: Numarası| No)?|Hizmet(?: Numarası| No)?|Fatura(?: Numarası| No)|Belge(?: Numarası| No)|HYS(?: Ödeme Emri Belge(?: Numarası| No)?|(?: Numarası| No))?)\s*[:=]\s*[^;\r\n]+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        RegexTimeout);

    private static readonly Regex EmailRegex = new(
        @"(?i)\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        RegexTimeout);

    private static readonly Regex GuidRegex = new(
        @"(?i)\b[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        RegexTimeout);

    private static readonly Regex LongNumberRegex = new(
        @"(?<![\d.])\d{8,}(?![\d.])",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        RegexTimeout);

    private static readonly Regex IpV4Regex = new(
        @"(?<!\d)(?:25[0-5]|2[0-4]\d|1?\d?\d)(?:\.(?:25[0-5]|2[0-4]\d|1?\d?\d)){3}(?!\d)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        RegexTimeout);

    private static readonly Regex WindowsPathRegex = new(
        @"(?im)\b[A-Z]:\\.*?(?=:\s*line\s+\d+|$)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
        RegexTimeout);

    private static readonly Regex UncPathRegex = new(
        @"(?im)\\\\[^\r\n]+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant,
        RegexTimeout);

    public static string Create(ApplicationSafetyService safetyService)
    {
        ArgumentNullException.ThrowIfNull(safetyService);
        var integrity = safetyService.CheckIntegrity();
        var folder = Path.Combine(safetyService.BackupFolder, "Tanılama");
        Directory.CreateDirectory(folder);
        var path = UniquePath(folder, $"DogruFatura_Tanilama_{DateTime.Now:yyyyMMdd_HHmmss}", ".zip");

        using var archive = ZipFile.Open(path, ZipArchiveMode.Create, Encoding.UTF8);
        WriteEntry(archive, "Tanilama.txt", BuildSummary(safetyService, integrity));

        if (Directory.Exists(safetyService.LogFolder))
        {
            foreach (var log in new DirectoryInfo(safetyService.LogFolder)
                         .GetFiles("*.log")
                         .OrderByDescending(file => file.LastWriteTimeUtc)
                         .Take(MaximumLogFiles))
            {
                try
                {
                    var text = File.ReadAllText(log.FullName, Encoding.UTF8);
                    if (text.Length > MaximumLogCharacters)
                        text = text[^MaximumLogCharacters..];
                    WriteEntry(archive, "Loglar/" + log.Name, Sanitize(text, safetyService));
                }
                catch
                {
                    // Tek bir log okunamıyorsa tanılama paketinin tamamı başarısız olmasın.
                }
            }
        }

        return path;
    }

    private static string BuildSummary(ApplicationSafetyService service, SafetyCheckResult integrity)
    {
        var builder = new StringBuilder();
        builder.AppendLine("DoğruFatura Tanılama Paketi");
        builder.AppendLine(new string('=', 48));
        builder.AppendLine(CultureInfo.CurrentCulture, $"Oluşturma       : {DateTimeOffset.Now:dd.MM.yyyy HH:mm:ss zzz}");
        builder.AppendLine(CultureInfo.CurrentCulture, $"Sürüm           : {service.InformationalVersion}");
        builder.AppendLine(CultureInfo.CurrentCulture, $"Çalışma ortamı  : {service.RuntimeDescription}");
        builder.AppendLine(CultureInfo.CurrentCulture, $"Veri bütünlüğü  : {(integrity.IsHealthy ? "Sağlıklı" : "Sorun bulundu")}");
        builder.AppendLine(CultureInfo.CurrentCulture, $"Kontrol edilen  : {integrity.JsonFileCount} JSON, {integrity.PdfFileCount} PDF");
        builder.AppendLine(CultureInfo.CurrentCulture, $"Sorun sayısı    : {integrity.ProblemCount}");
        builder.AppendLine(CultureInfo.CurrentCulture, $"Açıklama        : {integrity.Message}");
        builder.AppendLine();
        builder.AppendLine("Gizlilik notu: Bu paket fatura JSON verilerini veya PDF belgelerini içermez.");
        builder.AppendLine("Yalnız uygulama/ortam özeti ve en fazla son 5 hata günlüğü eklenir.");
        return Sanitize(builder.ToString(), service);
    }

    private static string Sanitize(string value, ApplicationSafetyService service)
    {
        var sanitized = value ?? string.Empty;

        sanitized = ReplaceKnownValue(sanitized, service.ActivePdfFolder, "%ACTIVE_PDF_DIR%");
        sanitized = ReplaceKnownValue(sanitized, service.ArchivedPdfFolder, "%ARCHIVED_PDF_DIR%");
        sanitized = ReplaceKnownValue(sanitized, service.DataFolder, "%DATA_DIR%");
        sanitized = ReplaceKnownValue(sanitized, service.BackupFolder, "%BACKUP_DIR%");
        sanitized = ReplaceKnownValue(sanitized, service.LogFolder, "%LOG_DIR%");
        sanitized = ReplaceKnownValue(sanitized, service.DocumentRootFolder, "%DOCUMENT_ROOT%");

        sanitized = ReplaceKnownValue(
            sanitized,
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "%USERPROFILE%");
        sanitized = ReplaceKnownValue(sanitized, Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar), "%TEMP%");
        sanitized = ReplaceKnownValue(sanitized, Environment.UserName, "%USERNAME%");
        sanitized = ReplaceKnownValue(sanitized, Environment.MachineName, "%COMPUTERNAME%");

        using var reader = new StringReader(sanitized);
        var builder = new StringBuilder(sanitized.Length);
        string? line;
        while ((line = reader.ReadLine()) is not null)
            builder.AppendLine(SanitizeLine(line));

        return builder.ToString();
    }

    private static string SanitizeLine(string line)
    {
        try
        {
            var sanitized = SensitiveLabelRegex.Replace(
                line,
                match => match.Groups["label"].Value + ": [GİZLENDİ]");
            sanitized = EmailRegex.Replace(sanitized, "%EMAIL%");
            sanitized = GuidRegex.Replace(sanitized, "%GUID%");
            sanitized = IpV4Regex.Replace(sanitized, "%IP%");
            sanitized = LongNumberRegex.Replace(sanitized, "%NUMBER%");
            sanitized = WindowsPathRegex.Replace(sanitized, "%PATH%");
            sanitized = UncPathRegex.Replace(sanitized, "%PATH%");
            return sanitized;
        }
        catch (RegexMatchTimeoutException)
        {
            // Tanılama paketi bir regex yüzünden tamamen başarısız olmamalı.
            // Timeout olan satırı olduğu gibi bırakmak kişisel veri sızdırabilir;
            // bu nedenle satırın tamamını güvenli biçimde kapat.
            return "[GİZLENDİ: satır güvenli biçimde işlenemedi]";
        }
    }

    private static string ReplaceKnownValue(string source, string? value, string replacement)
    {
        if (string.IsNullOrWhiteSpace(value))
            return source;

        return source.Replace(value, replacement, StringComparison.OrdinalIgnoreCase);
    }

    private static void WriteEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private static string UniquePath(string folder, string baseName, string extension)
    {
        var path = Path.Combine(folder, baseName + extension);
        var suffix = 2;
        while (File.Exists(path))
            path = Path.Combine(folder, $"{baseName}_{suffix++}{extension}");
        return path;
    }
}
