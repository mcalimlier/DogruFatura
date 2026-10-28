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
            RuntimeInstallationProfile.RootDirectory, "Veri", "YetkiliProfili.json");
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
            var schema = DataFileSchemaService.Read(json, DataFileKinds.AuthorityProfile);
            var profile = schema.Data.Deserialize<AuthorityProfile>(_jsonOptions);
            if (profile is null
                || profile.RealizationOfficerName is null
                || profile.RealizationOfficerTitle is null
                || profile.SpendingAuthorityName is null
                || profile.SpendingAuthorityTitle is null
                || (!profile.IsComplete && !profile.IsEmpty))
                throw new JsonException("Yetkili profilinde eksik alanlar var.");

            var normalized = profile.Normalize();
            string? warning = null;
            if (schema.RequiresUpgrade)
            {
                try
                {
                    var backupPath = DataFileSchemaService.CreatePreUpgradeBackup(DataFilePath, schema.SourceSchemaVersion);
                    Save(normalized);
                    warning = $"Yetkili profili veri dosyası şema {DataFileSchemaService.CurrentSchemaVersion} biçimine güvenle yükseltildi. Önceki biçim korundu: {backupPath}";
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    warning = $"Yetkili profili veri dosyasının şema yükseltmesi uygulanamadı; mevcut dosya korunuyor: {ex.Message}";
                }
            }

            return new AuthorityProfileLoadResult(normalized, warning, DataFilePath);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            var backupPath = CreateCorruptFileBackup();
            var defaultProfile = AuthorityProfileDefaults.Create();
            var warning = "Yetkili/imza profili okunamadı. Ana veri dosyasının üzerine varsayılan profil yazılmadı; bu oturumda yalnız geçici olarak varsayılan bilgiler kullanılıyor.";
            if (backupPath is not null) warning += $" Sorunlu dosyanın koruma kopyası: {backupPath}";
            warning += " Profili kaydetmeden önce mevcut sorunlu dosyayı inceleyin veya güvenli bir yedekten geri yükleyin.";
            return new AuthorityProfileLoadResult(defaultProfile, warning, DataFilePath);
        }
    }

    public void Save(AuthorityProfile profile)
    {
        var normalized = profile.Normalize();
        if (!normalized.IsComplete && !normalized.IsEmpty)
            throw new InvalidOperationException("Yetkili profilindeki bütün alanlar zorunludur.");

        var folder = Path.GetDirectoryName(DataFilePath) ?? throw new InvalidOperationException("Yetkili profil klasörü belirlenemedi.");
        Directory.CreateDirectory(folder);
        var temporaryPath = DataFilePath + ".tmp";
        var json = DataFileSchemaService.SerializeCurrent(
            DataFileKinds.AuthorityProfile,
            normalized,
            _jsonOptions);
        File.WriteAllText(temporaryPath, json, new UTF8Encoding(false));
        File.Move(temporaryPath, DataFilePath, true);
    }

    private string? CreateCorruptFileBackup()
    {
        try
        {
            var backupPath = Path.Combine(Path.GetDirectoryName(DataFilePath)!, $"YetkiliProfili_Bozuk_{DateTime.Now:yyyyMMdd_HHmmss_fff}.json");
            File.Copy(DataFilePath, backupPath, false);
            return backupPath;
        }
        catch { return null; }
    }
}
