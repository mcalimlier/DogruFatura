using DogruFatura.Core.Models;
using System.Text.Json;

namespace DogruFatura.Core.Services;

/// <summary>Imports a verified personal-install snapshot once, without mixing or replacing user data.</summary>
public static class InitialUserDataService
{
    private static readonly string[] Folders = { "Veri", "Aktif Çıktılar", "Arşivlenenler" };
    private static readonly HashSet<string> DataFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Okullar.json", "AktifFaturalar.json", "YetkiliProfili.json", "IslemArsivi.json",
        "DenetimKaydi.jsonl", "IslemJournal.jsonl", "GorunumTercihleri.json"
    };

    public static bool Initialize(string bundleRoot, string userRoot)
    {
        if (!Directory.Exists(bundleRoot)) return false;
        userRoot = Path.GetFullPath(userRoot);
        // Even partial or unreadable existing records belong to the user and are never overwritten.
        if (Folders.Any(folder => Directory.Exists(Path.Combine(userRoot, folder))
            && Directory.EnumerateFileSystemEntries(Path.Combine(userRoot, folder)).Any())) return false;
        ValidateBundle(bundleRoot);
        var staging = userRoot + ".initial-" + Guid.NewGuid().ToString("N");
        var moved = new List<string>();
        var rootExisted = Directory.Exists(userRoot);
        try
        {
            Directory.CreateDirectory(staging);
            foreach (var source in BundleFiles(bundleRoot))
            {
                var destination = Path.Combine(staging, Path.GetRelativePath(bundleRoot, source));
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Copy(source, destination, false);
            }
            Directory.CreateDirectory(userRoot);
            foreach (var folder in Folders.Where(folder => Directory.Exists(Path.Combine(staging, folder))))
            {
                var target = Path.Combine(userRoot, folder);
                if (Directory.Exists(target)) Directory.Delete(target);
                Directory.Move(Path.Combine(staging, folder), target);
                moved.Add(folder);
            }
            return true;
        }
        catch
        {
            foreach (var folder in moved.AsEnumerable().Reverse())
                Directory.Move(Path.Combine(userRoot, folder), Path.Combine(staging, folder));
            if (!rootExisted && Directory.Exists(userRoot) && !Directory.EnumerateFileSystemEntries(userRoot).Any())
                Directory.Delete(userRoot);
            throw;
        }
        finally { if (Directory.Exists(staging)) Directory.Delete(staging, true); }
    }

    public static void ValidateBundle(string bundleRoot)
    {
        DataDirectoryValidationService.ValidateCompleteDirectory(Path.Combine(bundleRoot, "Veri"));
        var manifest = JsonSerializer.Deserialize<Dictionary<string, string>>(
            File.ReadAllText(Path.Combine(bundleRoot, "Manifest.json"))) ?? throw new InvalidDataException("Başlangıç veri özeti okunamadı.");
        var files = BundleFiles(bundleRoot).ToArray();
        if (files.Length != manifest.Count) throw new InvalidDataException("Başlangıç veri paketi eksik veya fazla dosya içeriyor.");
        foreach (var path in files)
        {
            var relative = Path.GetRelativePath(bundleRoot, path).Replace('\\', '/');
            var parts = relative.Split('/');
            if (parts.Length != 2 || !Folders.Contains(parts[0], StringComparer.Ordinal)
                || (parts[0] == "Veri" ? !DataFiles.Contains(parts[1]) : !parts[1].EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                || !manifest.TryGetValue(relative, out var hash) || !FileIntegrityService.MatchesSha256(path, hash))
                throw new InvalidDataException("Başlangıç veri paketi bütünlük kontrolünü geçemedi: " + relative);
        }
        var archiveSchema = DataFileSchemaService.Read(
            File.ReadAllText(Path.Combine(bundleRoot, "Veri", "IslemArsivi.json")), DataFileKinds.InvoiceArchive);
        var archives = JsonSerializer.Deserialize<List<ArchivedInvoiceList>>(archiveSchema.Data.GetRawText()) ?? new();
        foreach (var archive in archives.Where(a => !string.IsNullOrEmpty(a.PdfFileName)))
        {
            var pdf = Path.Combine(bundleRoot, archive.IsClosed ? "Arşivlenenler" : "Aktif Çıktılar", archive.PdfFileName);
            if (!File.Exists(pdf) || (!string.IsNullOrEmpty(archive.PdfSha256) && !FileIntegrityService.MatchesSha256(pdf, archive.PdfSha256)))
                throw new InvalidDataException("Başlangıç paketinin PDF belgesi eksik veya değişmiş: " + archive.ArchiveNumber);
        }
    }

    private static IEnumerable<string> BundleFiles(string root)
    {
        foreach (var path in Directory.EnumerateFileSystemEntries(root, "*", SearchOption.AllDirectories))
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException("Başlangıç verisi bağlantı içeremez.");
        return Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(path => !string.Equals(Path.GetFullPath(path), Path.GetFullPath(Path.Combine(root, "Manifest.json")), StringComparison.OrdinalIgnoreCase));
    }
}
