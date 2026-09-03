using DogruFatura.Core.Models;
using System.Text;
using System.Text.Json;

namespace DogruFatura.Core.Services;

public sealed record AuthorityProfileLoadResult(
    AuthorityProfile Profile,
    string? WarningMessage,
    string DataFilePath);

public sealed class AuthorityProfileDataStore
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public AuthorityProfileDataStore(string? dataFilePath = null)
    {
        DataFilePath = dataFilePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            "DoğruFatura",
            "Veri",
            "YetkiliProfili.json");
    }

    public string DataFilePath { get; }

    public AuthorityProfileLoadResult LoadOrCreate()
    {
        if (!File.Exists(DataFilePath))
        {
            var defaultProfile = AuthorityProfileDefaults.Create();
            Save(defaultProfile);
            return new AuthorityProfileLoadResult(defaultProfile, null, DataFilePath);
        }

        try
        {
            var json = File.ReadAllText(DataFilePath, Encoding.UTF8);
            var profile = JsonSerializer.Deserialize<AuthorityProfile>(json, _jsonOptions);
            if (profile is null
                || profile.RealizationOfficerName is null
                || profile.RealizationOfficerTitle is null
                || profile.SpendingAuthorityName is null
                || profile.SpendingAuthorityTitle is null
                || !profile.IsComplete)
                throw new JsonException("Yetkili profilinde eksik alanlar var.");

            return new AuthorityProfileLoadResult(profile.Normalize(), null, DataFilePath);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            var backupPath = CreateCorruptFileBackup();
            var defaultProfile = AuthorityProfileDefaults.Create();
            Save(defaultProfile);
            var warning = "Yetkili/imza profili okunamadı; varsayılan bilgiler yeniden yüklendi."
                          + (backupPath is null ? string.Empty : $" Bozuk dosya korundu: {backupPath}");
            return new AuthorityProfileLoadResult(defaultProfile, warning, DataFilePath);
        }
    }

    public void Save(AuthorityProfile profile)
    {
        var normalized = profile.Normalize();
        if (!normalized.IsComplete)
            throw new InvalidOperationException("Yetkili profilindeki bütün alanlar zorunludur.");

        var folder = Path.GetDirectoryName(DataFilePath)
                     ?? throw new InvalidOperationException("Yetkili profil klasörü belirlenemedi.");
        Directory.CreateDirectory(folder);

        var json = JsonSerializer.Serialize(normalized, _jsonOptions);
        var temporaryPath = DataFilePath + ".tmp";
        File.WriteAllText(temporaryPath, json, new UTF8Encoding(false));
        File.Move(temporaryPath, DataFilePath, true);
    }

    private string? CreateCorruptFileBackup()
    {
        try
        {
            var backupPath = Path.Combine(
                Path.GetDirectoryName(DataFilePath)!,
                $"YetkiliProfili_Bozuk_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            File.Copy(DataFilePath, backupPath, false);
            return backupPath;
        }
        catch
        {
            return null;
        }
    }
}
