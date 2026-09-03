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
            "DoğruFatura",
            "Veri",
            "Okullar.json");
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
            var schools = JsonSerializer.Deserialize<List<SchoolRecord>>(json, _jsonOptions)
                          ?? new List<SchoolRecord>();
            if (schools.Any(s => s is null
                                 || s.InstitutionType is null
                                 || s.Name is null
                                 || s.Vkn is null
                                 || s.ElectricityServiceNumber is null
                                 || s.TelephoneServiceNumber is null
                                 || s.InternetServiceNumber is null
                                 || s.WaterServiceNumber is null))
                throw new JsonException("Okul/kurum veri dosyasında eksik alanlar var.");
            return new SchoolLoadResult(schools, null, DataFilePath);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            var backupPath = CreateCorruptFileBackup();
            var seedSchools = SchoolSeedData.Create();
            Save(seedSchools);
            var warning = "Okul/kurum veri dosyası okunamadı. 27 başlangıç kaydı yeniden yüklendi."
                          + (backupPath is null ? string.Empty : $" Bozuk dosya korundu: {backupPath}");
            return new SchoolLoadResult(seedSchools, warning, DataFilePath);
        }
    }

    public void Save(IEnumerable<SchoolRecord> schools)
    {
        var folder = Path.GetDirectoryName(DataFilePath)
                     ?? throw new InvalidOperationException("Okul/kurum veri klasörü belirlenemedi.");
        Directory.CreateDirectory(folder);

        var json = JsonSerializer.Serialize(schools.ToList(), _jsonOptions);
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
                $"Okullar_Bozuk_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            File.Copy(DataFilePath, backupPath, false);
            return backupPath;
        }
        catch
        {
            return null;
        }
    }
}
