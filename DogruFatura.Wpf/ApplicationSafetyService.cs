using DogruFatura.Core.Services;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace DogruFatura.Wpf;

public sealed record SafetyCheckResult(
    bool IsHealthy,
    string Message,
    string? BackupPath,
    int ProblemCount,
    int JsonFileCount,
    int PdfFileCount);

public sealed record SafetyRestoreResult(
    bool Success,
    string Message,
    string? SafetyBackupPath);

public sealed class ApplicationSafetyService
{
    private const string BackupFormat = "DogruFaturaBackup/1";
    private const int MaximumArchiveEntryCount = 10_000;
    private const long MaximumUncompressedBytes = 2L * 1024 * 1024 * 1024;

    private static readonly string[] KnownDataFileNames =
    {
        "Okullar.json",
        "AktifFaturalar.json",
        "YetkiliProfili.json",
        "IslemArsivi.json"
    };

    private readonly Func<string> _informationalVersionProvider;
    private readonly string _legacyRootFolder;

    public ApplicationSafetyService(
        string? documentsRoot = null,
        string? desktopRoot = null,
        Func<string>? informationalVersionProvider = null)
    {
        var documents = documentsRoot
                        ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var desktop = desktopRoot
                      ?? Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

        var documentStorage = new DocumentArchiveStorageService(desktop);
        DocumentRootFolder = documentStorage.RootDirectory;
        RootFolder = DocumentRootFolder;
        DataFolder = Path.Combine(RootFolder, "Veri");
        BackupFolder = Path.Combine(RootFolder, "Yedekler");
        LogFolder = Path.Combine(RootFolder, "Loglar");
        _legacyRootFolder = Path.Combine(documents, "DoğruFatura");

        ActivePdfFolder = documentStorage.ActiveOutputDirectory;
        ArchivedPdfFolder = documentStorage.ArchivedOutputDirectory;

        _informationalVersionProvider = informationalVersionProvider ?? ReadInformationalVersion;
    }

    public string RootFolder { get; }
    public string DataFolder { get; }
    public string BackupFolder { get; }
    public string LogFolder { get; }
    public string DocumentRootFolder { get; }
    public string ActivePdfFolder { get; }
    public string ArchivedPdfFolder { get; }

    public string InformationalVersion => _informationalVersionProvider();

    public string RuntimeDescription =>
        $".NET {Environment.Version} • {(Environment.Is64BitProcess ? "64-bit" : "32-bit")} • {Environment.OSVersion.VersionString}";

    public SafetyCheckResult InitializeAndCheck()
    {
        var messages = new List<string>();
        string? startupBackup = null;
        var startupProblemCount = 0;

        try
        {
            var migrationMessage = MigrateLegacyStorageIfNeeded();
            if (!string.IsNullOrWhiteSpace(migrationMessage))
                messages.Add(migrationMessage);
            EnsureFoldersWritable();
        }
        catch (Exception ex)
        {
            return new SafetyCheckResult(
                false,
                "Veri ve belge klasörlerine yazma erişimi doğrulanamadı: " + ex.Message,
                null,
                1,
                0,
                0);
        }

        try
        {
            startupBackup = EnsureVersionBackup();
            if (!string.IsNullOrWhiteSpace(startupBackup))
                messages.Add("Sürüm geçişinden önce otomatik güvenlik yedeği oluşturuldu.");
        }
        catch (Exception ex)
        {
            messages.Add("Sürüm geçişi yedeği oluşturulamadı: " + ex.Message);
            startupProblemCount++;
        }

        var integrity = CheckIntegrity();
        if (!string.IsNullOrWhiteSpace(integrity.Message))
            messages.Add(integrity.Message);

        return integrity with
        {
            IsHealthy = integrity.IsHealthy && startupProblemCount == 0,
            Message = string.Join(" ", messages),
            BackupPath = startupBackup,
            ProblemCount = integrity.ProblemCount + startupProblemCount
        };
    }

    public SafetyCheckResult CheckIntegrity()
    {
        var problems = new List<string>();
        var checkedJson = 0;
        var checkedPdf = 0;

        try
        {
            EnsureFoldersWritable();
        }
        catch (Exception ex)
        {
            problems.Add("Klasör erişimi: " + ex.Message);
        }

        var knownFiles = KnownDataFileNames
            .Select(fileName => Path.Combine(DataFolder, fileName))
            .ToArray();
        var existingKnownFiles = knownFiles.Where(File.Exists).ToArray();

        if (existingKnownFiles.Length > 0 && existingKnownFiles.Length != knownFiles.Length)
        {
            var missing = knownFiles
                .Where(path => !File.Exists(path))
                .Select(Path.GetFileName);
            problems.Add("Eksik veri dosyaları: " + string.Join(", ", missing));
        }

        foreach (var file in existingKnownFiles)
        {
            checkedJson++;
            try
            {
                using var stream = File.OpenRead(file);
                using var document = JsonDocument.Parse(stream);
                if (string.Equals(Path.GetFileName(file), "IslemArsivi.json", StringComparison.OrdinalIgnoreCase)
                    && document.RootElement.ValueKind != JsonValueKind.Array)
                    throw new JsonException("Arşiv kökü liste biçiminde değil.");
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                problems.Add($"{Path.GetFileName(file)} okunamadı ({ex.Message})");
            }
        }

        foreach (var folder in new[] { ActivePdfFolder, ArchivedPdfFolder })
        {
            if (!Directory.Exists(folder)) continue;
            foreach (var file in Directory.EnumerateFiles(folder, "*.pdf", SearchOption.TopDirectoryOnly))
            {
                checkedPdf++;
                try
                {
                    ValidatePdfHeader(file);
                }
                catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException)
                {
                    problems.Add($"{Path.GetFileName(file)} geçerli PDF değil ({ex.Message})");
                }
            }
        }

        if (problems.Count == 0)
        {
            return new SafetyCheckResult(
                true,
                $"Veri bütünlüğü sağlıklı. {checkedJson} JSON dosyası ve {checkedPdf} PDF kontrol edildi.",
                null,
                0,
                checkedJson,
                checkedPdf);
        }

        return new SafetyCheckResult(
            false,
            $"{problems.Count} güvenlik sorunu bulundu: {string.Join(" | ", problems.Take(3))}",
            null,
            problems.Count,
            checkedJson,
            checkedPdf);
    }

    public string CreateManualSafetyBackup(string reason = "Manuel")
    {
        var safetyFolder = Path.Combine(BackupFolder, "Güvenlik");
        Directory.CreateDirectory(safetyFolder);
        var safeReason = SafeFileToken(reason);
        if (string.IsNullOrWhiteSpace(safeReason)) safeReason = "Manuel";

        var path = BuildUniquePath(
            safetyFolder,
            $"DogruFatura_{safeReason}_{DateTime.Now:yyyyMMdd_HHmmss}",
            ".zip");
        CreateCompleteBackup(path);
        PruneZipBackups(safetyFolder, 25);
        return path;
    }

    public SafetyRestoreResult RestoreFromBackup(string zipPath)
    {
        if (string.IsNullOrWhiteSpace(zipPath) || !File.Exists(zipPath))
            return new SafetyRestoreResult(false, "Seçilen yedek dosyası bulunamadı.", null);

        var tempRoot = Path.Combine(Path.GetTempPath(), "DogruFatura_Restore_" + Guid.NewGuid().ToString("N"));
        var extractedRoot = Path.Combine(tempRoot, "Extracted");
        Directory.CreateDirectory(extractedRoot);

        string? beforeRestore = null;
        try
        {
            ValidateAndExtractBackup(zipPath, extractedRoot);
            ValidateExtractedBackup(extractedRoot);

            var beforeRestoreFolder = Path.Combine(BackupFolder, "Geri_Yukleme_Oncesi");
            Directory.CreateDirectory(beforeRestoreFolder);
            beforeRestore = BuildUniquePath(
                beforeRestoreFolder,
                $"DogruFatura_GeriYuklemeOncesi_{DateTime.Now:yyyyMMdd_HHmmss}",
                ".zip");
            CreateCompleteBackup(beforeRestore);
            PruneZipBackups(beforeRestoreFolder, 20);

            RestoreExtractedDirectories(extractedRoot);
            return new SafetyRestoreResult(
                true,
                "Veriler ve kayıtlı PDF belgeleri yedekten başarıyla geri yüklendi.",
                beforeRestore);
        }
        catch (Exception ex)
        {
            return new SafetyRestoreResult(false, "Yedek geri yüklenemedi: " + ex.Message, beforeRestore);
        }
        finally
        {
            TryDeleteDirectory(tempRoot);
        }
    }

    private string? EnsureVersionBackup()
    {
        Directory.CreateDirectory(DataFolder);
        var markerPath = Path.Combine(DataFolder, "uygulama_surumu.txt");
        var previousVersion = File.Exists(markerPath)
            ? File.ReadAllText(markerPath, Encoding.UTF8).Trim()
            : string.Empty;
        var currentVersion = InformationalVersion;

        var hasExistingData = Directory.EnumerateFiles(DataFolder, "*", SearchOption.AllDirectories)
            .Any(path => !path.Equals(markerPath, StringComparison.OrdinalIgnoreCase));
        var hasExistingPdf = Directory.EnumerateFiles(ActivePdfFolder, "*.pdf", SearchOption.TopDirectoryOnly).Any()
                             || Directory.EnumerateFiles(ArchivedPdfFolder, "*.pdf", SearchOption.TopDirectoryOnly).Any();

        string? backup = null;
        if ((hasExistingData || hasExistingPdf)
            && !string.Equals(previousVersion, currentVersion, StringComparison.OrdinalIgnoreCase))
        {
            var updateFolder = Path.Combine(BackupFolder, "Güncelleme_Öncesi");
            Directory.CreateDirectory(updateFolder);
            var from = string.IsNullOrWhiteSpace(previousVersion) ? "EskiSurum" : SafeFileToken(previousVersion);
            backup = BuildUniquePath(
                updateFolder,
                $"DogruFatura_{from}_to_{SafeFileToken(currentVersion)}_{DateTime.Now:yyyyMMdd_HHmmss}",
                ".zip");
            CreateCompleteBackup(backup);
            PruneZipBackups(updateFolder, 15);
        }

        File.WriteAllText(markerPath, currentVersion, new UTF8Encoding(false));
        return backup;
    }

    private string? MigrateLegacyStorageIfNeeded()
    {
        if (string.Equals(
                Path.GetFullPath(_legacyRootFolder).TrimEnd(Path.DirectorySeparatorChar),
                Path.GetFullPath(RootFolder).TrimEnd(Path.DirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase)
            || !Directory.Exists(_legacyRootFolder))
            return null;

        var movedFiles = 0;
        var conflictFiles = 0;
        foreach (var folderName in new[] { "Veri", "Yedekler", "Loglar" })
        {
            var source = Path.Combine(_legacyRootFolder, folderName);
            if (!Directory.Exists(source)) continue;
            var destination = Path.Combine(RootFolder, folderName);

            MoveDirectoryContentsWithoutOverwrite(source, destination, ref movedFiles, ref conflictFiles);
            try
            {
                if (Directory.Exists(source) && !Directory.EnumerateFileSystemEntries(source).Any())
                    Directory.Delete(source, false);
            }
            catch
            {
                // Kaynak klasör silinemezse içerik güvenli hedefte kullanılmaya devam eder.
            }
        }

        try
        {
            if (Directory.Exists(_legacyRootFolder)
                && !Directory.EnumerateFileSystemEntries(_legacyRootFolder).Any())
                Directory.Delete(_legacyRootFolder, false);
        }
        catch
        {
            // Eski kök boş olsa bile silinememesi veri kullanımını etkilemez.
        }

        if (movedFiles == 0 && conflictFiles == 0) return null;
        var message = $"Belgeler\\DoğruFatura içindeki {movedFiles} kullanıcı dosyası Masaüstü\\DoğruFatura altına taşındı.";
        if (conflictFiles > 0)
            message += $" {conflictFiles} aynı adlı dosya güvenlik amacıyla eski konumunda bırakıldı.";
        return message;
    }

    private static void MoveDirectoryContentsWithoutOverwrite(
        string source,
        string destination,
        ref int movedFiles,
        ref int conflictFiles)
    {
        Directory.CreateDirectory(destination);
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, directory);
            Directory.CreateDirectory(Path.Combine(destination, relative));
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories).ToArray())
        {
            var relative = Path.GetRelativePath(source, file);
            var target = Path.Combine(destination, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            if (File.Exists(target))
            {
                conflictFiles++;
                continue;
            }

            File.Copy(file, target, false);
            File.Delete(file);
            movedFiles++;
        }

        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories)
                     .OrderByDescending(path => path.Length))
        {
            try
            {
                if (!Directory.EnumerateFileSystemEntries(directory).Any())
                    Directory.Delete(directory, false);
            }
            catch
            {
                // Çakışan veya kilitli içerikler eski klasörde korunur.
            }
        }
    }

    private void EnsureFoldersWritable()
    {
        foreach (var folder in new[]
                 {
                     RootFolder,
                     DataFolder,
                     BackupFolder,
                     LogFolder,
                     DocumentRootFolder,
                     ActivePdfFolder,
                     ArchivedPdfFolder
                 })
        {
            Directory.CreateDirectory(folder);
            var probe = Path.Combine(folder, $".dogrufatura_write_test_{Guid.NewGuid():N}.tmp");
            try
            {
                File.WriteAllText(probe, "ok", new UTF8Encoding(false));
            }
            finally
            {
                try { if (File.Exists(probe)) File.Delete(probe); } catch { }
            }
        }
    }

    private void CreateCompleteBackup(string destinationZip)
    {
        EnsureFoldersWritable();
        var destinationFolder = Path.GetDirectoryName(destinationZip)
                                ?? throw new InvalidOperationException("Yedek klasörü belirlenemedi.");
        Directory.CreateDirectory(destinationFolder);
        if (File.Exists(destinationZip))
            throw new IOException($"{Path.GetFileName(destinationZip)} zaten var.");

        var temporaryPath = destinationZip + ".tmp_" + Guid.NewGuid().ToString("N");
        try
        {
            using (var archive = ZipFile.Open(temporaryPath, ZipArchiveMode.Create, Encoding.UTF8))
            {
                var manifestEntry = archive.CreateEntry("DogruFatura_Yedegi.json", CompressionLevel.Optimal);
                using (var writer = new StreamWriter(manifestEntry.Open(), new UTF8Encoding(false)))
                {
                    var manifest = new BackupManifest(
                        BackupFormat,
                        DateTimeOffset.Now,
                        InformationalVersion,
                        Environment.MachineName);
                    writer.Write(JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
                }

                AddDirectoryToArchive(archive, DataFolder, "Veri", _ => true);
                AddDirectoryToArchive(archive, ActivePdfFolder, "Belgeler/Aktif Çıktılar", IsPdfFile);
                AddDirectoryToArchive(archive, ArchivedPdfFolder, "Belgeler/Arşivlenenler", IsPdfFile);
            }
            File.Move(temporaryPath, destinationZip, false);
        }
        catch
        {
            try { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); } catch { }
            throw;
        }
    }

    private static void AddDirectoryToArchive(
        ZipArchive archive,
        string sourceFolder,
        string entryRoot,
        Func<string, bool> includeFile)
    {
        var normalizedRoot = entryRoot.Trim('/');
        archive.CreateEntry(normalizedRoot + "/");
        if (!Directory.Exists(sourceFolder)) return;

        foreach (var file in Directory.EnumerateFiles(sourceFolder, "*", SearchOption.AllDirectories))
        {
            if (!includeFile(file)) continue;
            var relative = Path.GetRelativePath(sourceFolder, file).Replace('\\', '/');
            archive.CreateEntryFromFile(file, normalizedRoot + "/" + relative, CompressionLevel.Optimal);
        }
    }

    private static bool IsPdfFile(string path) =>
        string.Equals(Path.GetExtension(path), ".pdf", StringComparison.OrdinalIgnoreCase);

    private static void ValidateAndExtractBackup(string zipPath, string destination)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        if (archive.Entries.Count == 0)
            throw new InvalidDataException("ZIP yedeği boş.");
        if (archive.Entries.Count > MaximumArchiveEntryCount)
            throw new InvalidDataException("ZIP yedeğinde güvenli sınırdan fazla dosya var.");

        var totalLength = archive.Entries.Sum(entry => entry.Length);
        if (totalLength > MaximumUncompressedBytes)
            throw new InvalidDataException("ZIP yedeği açıldığında güvenli boyut sınırını aşıyor.");

        var manifestEntry = archive.GetEntry("DogruFatura_Yedegi.json")
                            ?? throw new InvalidDataException("Bu ZIP, DoğruFatura güvenlik yedeği değil.");
        using (var stream = manifestEntry.Open())
        {
            var manifest = JsonSerializer.Deserialize<BackupManifest>(stream)
                           ?? throw new InvalidDataException("Yedek kimlik bilgisi okunamadı.");
            if (!string.Equals(manifest.Format, BackupFormat, StringComparison.Ordinal))
                throw new InvalidDataException("Yedek biçimi bu DoğruFatura sürümüyle uyumlu değil.");
        }

        var basePath = Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar)
                       + Path.DirectorySeparatorChar;
        foreach (var entry in archive.Entries)
        {
            var normalizedName = entry.FullName.Replace('\\', '/');
            if (normalizedName.StartsWith('/')
                || normalizedName.Contains(':')
                || normalizedName.Split('/', StringSplitOptions.RemoveEmptyEntries)
                    .Any(part => part is "." or ".."))
                throw new InvalidDataException("Yedek içinde geçersiz dosya yolu bulundu.");

            var allowed = normalizedName.Equals("DogruFatura_Yedegi.json", StringComparison.Ordinal)
                          || normalizedName.StartsWith("Veri/", StringComparison.Ordinal)
                          || normalizedName.StartsWith("Belgeler/Aktif Çıktılar/", StringComparison.Ordinal)
                          || normalizedName.StartsWith("Belgeler/Arşivlenenler/", StringComparison.Ordinal);
            if (!allowed)
                throw new InvalidDataException("Yedek içinde tanınmayan içerik bulundu.");

            var targetPath = Path.GetFullPath(Path.Combine(destination, normalizedName));
            if (!targetPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Yedek içinde güvenli olmayan dosya yolu bulundu.");
        }

        archive.ExtractToDirectory(destination, false);
    }

    private static void ValidateExtractedBackup(string extractedRoot)
    {
        var dataRoot = Path.Combine(extractedRoot, "Veri");
        if (!Directory.Exists(dataRoot))
            throw new InvalidDataException("Yedekte Veri klasörü bulunamadı.");

        var knownFiles = KnownDataFileNames
            .Select(fileName => Path.Combine(dataRoot, fileName))
            .Where(File.Exists)
            .ToArray();
        if (knownFiles.Length == 0)
            throw new InvalidDataException("Yedekte tanınan DoğruFatura veri dosyası bulunamadı.");

        foreach (var file in knownFiles)
        {
            using var stream = File.OpenRead(file);
            using var _ = JsonDocument.Parse(stream);
        }

        var documentRoot = Path.Combine(extractedRoot, "Belgeler");
        if (!Directory.Exists(documentRoot)) return;
        foreach (var file in Directory.EnumerateFiles(documentRoot, "*.pdf", SearchOption.AllDirectories))
            ValidatePdfHeader(file);
    }

    private void RestoreExtractedDirectories(string extractedRoot)
    {
        var token = Guid.NewGuid().ToString("N");
        var stagedData = Path.Combine(RootFolder, "Veri_yeni_" + token);
        var stagedActive = Path.Combine(RootFolder, "AktifCiktilar_yeni_" + token);
        var stagedArchived = Path.Combine(RootFolder, "Arsivlenenler_yeni_" + token);
        var oldData = Path.Combine(RootFolder, "Veri_onceki_" + token);
        var oldActive = Path.Combine(RootFolder, "AktifCiktilar_onceki_" + token);
        var oldArchived = Path.Combine(RootFolder, "Arsivlenenler_onceki_" + token);

        CopyDirectory(Path.Combine(extractedRoot, "Veri"), stagedData);
        Directory.CreateDirectory(stagedActive);
        Directory.CreateDirectory(stagedArchived);
        CopyDirectoryIfExists(
            Path.Combine(extractedRoot, "Belgeler", "Aktif Çıktılar"),
            stagedActive);
        CopyDirectoryIfExists(
            Path.Combine(extractedRoot, "Belgeler", "Arşivlenenler"),
            stagedArchived);

        var dataSwapped = false;
        var activeSwapped = false;
        var archivedSwapped = false;
        try
        {
            SwapDirectory(stagedData, DataFolder, oldData);
            dataSwapped = true;
            SwapDirectory(stagedActive, ActivePdfFolder, oldActive);
            activeSwapped = true;
            SwapDirectory(stagedArchived, ArchivedPdfFolder, oldArchived);
            archivedSwapped = true;
        }
        catch
        {
            if (archivedSwapped) RollbackDirectory(ArchivedPdfFolder, oldArchived);
            if (activeSwapped) RollbackDirectory(ActivePdfFolder, oldActive);
            if (dataSwapped) RollbackDirectory(DataFolder, oldData);
            throw;
        }
        finally
        {
            TryDeleteDirectory(stagedData);
            TryDeleteDirectory(stagedActive);
            TryDeleteDirectory(stagedArchived);
        }

        TryDeleteDirectory(oldData);
        TryDeleteDirectory(oldActive);
        TryDeleteDirectory(oldArchived);
    }

    private static void SwapDirectory(string staging, string target, string old)
    {
        if (!Directory.Exists(staging))
            throw new DirectoryNotFoundException("Geri yükleme hazırlık klasörü bulunamadı.");
        if (Directory.Exists(old))
            throw new IOException("Geri yükleme güvenlik klasörü zaten var.");

        if (Directory.Exists(target)) Directory.Move(target, old);
        try
        {
            Directory.Move(staging, target);
        }
        catch
        {
            if (!Directory.Exists(target) && Directory.Exists(old))
                Directory.Move(old, target);
            throw;
        }
    }

    private static void RollbackDirectory(string target, string old)
    {
        try
        {
            TryDeleteDirectory(target);
            if (Directory.Exists(old)) Directory.Move(old, target);
        }
        catch
        {
            // Geri yükleme öncesi ZIP yedeği ayrıca korunduğu için ikincil hata bastırılır.
        }
    }

    private static void CopyDirectoryIfExists(string source, string destination)
    {
        if (Directory.Exists(source))
            CopyDirectory(source, destination);
        else
            Directory.CreateDirectory(destination);
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.EnumerateFiles(source))
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
        foreach (var directory in Directory.EnumerateDirectories(source))
            CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
    }

    private static void ValidatePdfHeader(string path)
    {
        using var stream = File.OpenRead(path);
        Span<byte> header = stackalloc byte[5];
        if (stream.Read(header) != header.Length
            || !header.SequenceEqual("%PDF-"u8))
            throw new InvalidDataException("PDF imzası bulunamadı.");
    }

    private static void PruneZipBackups(string folder, int keep)
    {
        if (!Directory.Exists(folder)) return;
        foreach (var file in new DirectoryInfo(folder)
                     .GetFiles("*.zip")
                     .OrderByDescending(file => file.LastWriteTimeUtc)
                     .Skip(keep))
        {
            try { file.Delete(); } catch { }
        }
    }

    private static string BuildUniquePath(string folder, string baseName, string extension)
    {
        var candidate = Path.Combine(folder, baseName + extension);
        var suffix = 2;
        while (File.Exists(candidate))
            candidate = Path.Combine(folder, $"{baseName}_{suffix++}{extension}");
        return candidate;
    }

    private static string SafeFileToken(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Concat((value ?? string.Empty)
            .Where(character => !char.IsWhiteSpace(character))
            .Select(character => invalid.Contains(character) ? '_' : character));
    }

    private static void TryDeleteDirectory(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, true); } catch { }
    }

    private static string ReadInformationalVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
               ?? assembly.GetName().Version?.ToString(3)
               ?? "1.0.0";
    }

    private sealed record BackupManifest(
        string Format,
        DateTimeOffset CreatedAt,
        string ApplicationVersion,
        string MachineName);
}
