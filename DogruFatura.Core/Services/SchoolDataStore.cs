using DogruFatura.Core.Models;
using System.Text;
using System.Text.Json;

namespace DogruFatura.Core.Services;

public sealed record SchoolLoadResult(
    IReadOnlyList<SchoolRecord> Schools,
    string? WarningMessage,
    string DataFilePath);

public sealed class SchoolDataStore
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public SchoolDataStore(string? dataFilePath = null)
    {
        DataFilePath = dataFilePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            "DoğruFatura", "Veri", "Okullar.json");
    }

    public string DataFilePath { get; }

    public SchoolLoadResult LoadOrCreate()
    {
        if (!File.Exists(DataFilePath))
        {
            var seedSchools = SchoolSeedData.Create();
            Save(seedSchools);
            return new SchoolLoadResult(seedSchools, null, DataFilePath);
        }

        try
        {
            var json = File.ReadAllText(DataFilePath, Encoding.UTF8);
            var schema = DataFileSchemaService.Read(json, DataFileKinds.Schools);
            var schools = schema.Data.Deserialize<List<SchoolRecord>>(_jsonOptions)
                          ?? new List<SchoolRecord>();
            if (schools.Any(IsStructurallyInvalid))
                throw new JsonException("Okul/kurum veri dosyasında eksik alanlar var.");

            string? warning = null;
            if (schema.RequiresUpgrade)
            {
                try
                {
                    Save(schools);
                    warning = $"Okul/kurum veri dosyası şema {DataFileSchemaService.CurrentSchemaVersion} biçimine güvenle yükseltildi.";
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    warning = $"Okul/kurum veri dosyasının şema yükseltmesi uygulanamadı; mevcut dosya korunuyor: {ex.Message}";
                }
            }

            return new SchoolLoadResult(schools, warning, DataFilePath);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            var recovered = TryRecoverSchools(out var rejectedCount);
            var backupPath = CreateCorruptFileBackup();
            IReadOnlyList<SchoolRecord> sessionSchools = recovered.Count > 0 ? recovered : SchoolSeedData.Create();
            var warning = recovered.Count > 0
                ? $"Okul/kurum veri dosyasında sorun bulundu. {recovered.Count} sağlam kayıt geçici kurtarma modunda açıldı; {rejectedCount} kayıt kullanılamadı. Ana dosyanın üzerine yazılmadı."
                : "Okul/kurum veri dosyası okunamadı. Ana dosya korunarak bu oturumda başlangıç kayıtları kullanılıyor; bozuk dosyanın üzerine yazılmadı.";
            if (backupPath is not null) warning += $" Sorunlu dosyanın koruma kopyası: {backupPath}";
            return new SchoolLoadResult(sessionSchools, warning, DataFilePath);
        }
    }

    private IReadOnlyList<SchoolRecord> TryRecoverSchools(out int rejectedCount)
    {
        rejectedCount = 0;
        var recovered = new List<SchoolRecord>();
        try
        {
            var json = File.ReadAllText(DataFilePath, Encoding.UTF8);
            var schema = DataFileSchemaService.Read(json, DataFileKinds.Schools);
            if (schema.Data.ValueKind != JsonValueKind.Array) return recovered;
            foreach (var element in schema.Data.EnumerateArray())
            {
                try
                {
                    var school = element.Deserialize<SchoolRecord>(_jsonOptions);
                    if (school is null || IsStructurallyInvalid(school)) { rejectedCount++; continue; }
                    var normalized = SchoolValidationService.Normalize(school);
                    if (!SchoolValidationService.Validate(normalized, recovered).IsValid) { rejectedCount++; continue; }
                    recovered.Add(normalized);
                }
                catch (JsonException) { rejectedCount++; }
            }
        }
        catch { }
        return recovered;
    }

    private static bool IsStructurallyInvalid(SchoolRecord? school) => school is null
        || school.InstitutionType is null
        || school.Name is null
        || school.Vkn is null
        || school.ElectricityServiceNumber is null
        || school.TelephoneServiceNumber is null
        || school.InternetServiceNumber is null
        || school.WaterServiceNumber is null;

    public void Save(IEnumerable<SchoolRecord> schools)
    {
        var folder = Path.GetDirectoryName(DataFilePath) ?? throw new InvalidOperationException("Okul/kurum veri klasörü belirlenemedi.");
        Directory.CreateDirectory(folder);
        var temporaryPath = DataFilePath + ".tmp";
        var json = DataFileSchemaService.SerializeCurrent(
            DataFileKinds.Schools,
            schools.ToList(),
            _jsonOptions);
        File.WriteAllText(temporaryPath, json, new UTF8Encoding(false));
        File.Move(temporaryPath, DataFilePath, true);
    }

    private string? CreateCorruptFileBackup()
    {
        try
        {
            var backupPath = Path.Combine(Path.GetDirectoryName(DataFilePath)!, $"Okullar_Bozuk_{DateTime.Now:yyyyMMdd_HHmmss_fff}.json");
            File.Copy(DataFilePath, backupPath, false);
            return backupPath;
        }
        catch { return null; }
    }
}
