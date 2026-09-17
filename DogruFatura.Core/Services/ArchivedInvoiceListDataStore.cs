using DogruFatura.Core.Models;
using System.Text;
using System.Text.Json;

namespace DogruFatura.Core.Services;

public sealed record ArchivedInvoiceListLoadResult(
    IReadOnlyList<ArchivedInvoiceList> Archives,
    string? WarningMessage,
    string DataFilePath);

public sealed class ArchivedInvoiceListDataStore
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public ArchivedInvoiceListDataStore(string? dataFilePath = null)
    {
        DataFilePath = dataFilePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            "DoğruFatura", "Veri", "IslemArsivi.json");
    }

    public string DataFilePath { get; }

    public ArchivedInvoiceListLoadResult LoadOrCreate()
    {
        if (!File.Exists(DataFilePath))
        {
            var empty = Array.Empty<ArchivedInvoiceList>();
            Save(empty);
            return new ArchivedInvoiceListLoadResult(empty, null, DataFilePath);
        }

        try
        {
            var json = File.ReadAllText(DataFilePath, Encoding.UTF8);
            var archives = JsonSerializer.Deserialize<List<ArchivedInvoiceList>>(json, _jsonOptions)
                           ?? new List<ArchivedInvoiceList>();
            if (archives.Any(archive => archive is null))
                throw new JsonException("İşlem arşivinde boş kayıt var.");

            var normalized = archives.Select(archive => archive.Normalize()).ToList();
            ValidateArchiveSet(normalized);
            return new ArchivedInvoiceListLoadResult(normalized, null, DataFilePath);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            var recovered = TryRecoverValidArchives(out var rejectedCount);
            var backupPath = CreateCorruptFileBackup();
            var warning = recovered.Count > 0
                ? $"İşlem arşivinde sorun bulundu. {recovered.Count} sağlam arşiv kaydı geçici kurtarma modunda açıldı; {rejectedCount} hatalı/çakışan kayıt dışarıda bırakıldı. Ana arşiv dosyasının üzerine yazılmadı."
                : "İşlem arşivi okunamadı. Güvenlik amacıyla ana arşiv dosyasının üzerine boş arşiv yazılmadı; bu oturumda boş görünümle kurtarma modunda devam ediliyor.";
            if (backupPath is not null) warning += $" Sorunlu dosyanın koruma kopyası: {backupPath}";
            warning += " Arşivi onarmadan kaydetme işlemi yapmayın; önce güvenlik yedeği alın.";
            return new ArchivedInvoiceListLoadResult(recovered, warning, DataFilePath);
        }
    }

    private IReadOnlyList<ArchivedInvoiceList> TryRecoverValidArchives(out int rejectedCount)
    {
        rejectedCount = 0;
        var recovered = new List<ArchivedInvoiceList>();
        var knownHys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(DataFilePath, Encoding.UTF8));
            if (document.RootElement.ValueKind != JsonValueKind.Array) return recovered;
            foreach (var element in document.RootElement.EnumerateArray())
            {
                try
                {
                    var archive = element.Deserialize<ArchivedInvoiceList>(_jsonOptions)?.Normalize();
                    if (archive is null) { rejectedCount++; continue; }
                    var validation = ArchivedInvoiceListValidationService.Validate(archive);
                    if (!validation.IsValid) { rejectedCount++; continue; }
                    if (!string.IsNullOrWhiteSpace(archive.HysPaymentOrderDocumentNumber)
                        && !knownHys.Add(archive.HysPaymentOrderDocumentNumber))
                    {
                        rejectedCount++;
                        continue;
                    }
                    recovered.Add(archive);
                }
                catch (Exception ex) when (ex is JsonException or InvalidOperationException) { rejectedCount++; }
            }
        }
        catch { }
        return recovered;
    }

    public void Save(IEnumerable<ArchivedInvoiceList> archives)
    {
        ArgumentNullException.ThrowIfNull(archives);
        var normalized = archives.Select(archive => archive.Normalize()).ToList();
        ValidateArchiveSet(normalized, saving: true);

        var folder = Path.GetDirectoryName(DataFilePath) ?? throw new InvalidOperationException("İşlem arşivi veri klasörü belirlenemedi.");
        Directory.CreateDirectory(folder);
        var temporaryPath = DataFilePath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(normalized, _jsonOptions), new UTF8Encoding(false));
        File.Move(temporaryPath, DataFilePath, true);
    }

    private static void ValidateArchiveSet(IReadOnlyList<ArchivedInvoiceList> archives, bool saving = false)
    {
        for (var index = 0; index < archives.Count; index++)
        {
            var validation = ArchivedInvoiceListValidationService.Validate(archives[index]);
            if (!validation.IsValid)
                throw saving
                    ? new InvalidOperationException($"{index + 1}. arşiv kaydı kaydedilemedi: {validation.Message}")
                    : new JsonException($"{index + 1}. arşiv kaydı geçersiz: {validation.Message}");
        }
        var duplicate = FindDuplicateHysNumber(archives);
        if (duplicate is null) return;
        if (saving) throw new InvalidOperationException($"{duplicate} birden fazla arşiv kaydında kullanılamaz.");
        throw new JsonException($"{duplicate} birden fazla arşiv kaydında kullanılmış.");
    }

    private static string? FindDuplicateHysNumber(IEnumerable<ArchivedInvoiceList> archives) => archives
        .Where(archive => !string.IsNullOrWhiteSpace(archive.HysPaymentOrderDocumentNumber))
        .GroupBy(archive => archive.HysPaymentOrderDocumentNumber, StringComparer.OrdinalIgnoreCase)
        .FirstOrDefault(group => group.Count() > 1)?.Key;

    private string? CreateCorruptFileBackup()
    {
        try
        {
            var backupPath = Path.Combine(Path.GetDirectoryName(DataFilePath)!, $"IslemArsivi_Bozuk_{DateTime.Now:yyyyMMdd_HHmmss_fff}.json");
            File.Copy(DataFilePath, backupPath, false);
            return backupPath;
        }
        catch { return null; }
    }
}
