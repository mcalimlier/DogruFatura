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
            "DoğruFatura",
            "Veri",
            "IslemArsivi.json");
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
            for (var index = 0; index < normalized.Count; index++)
            {
                var validation = ArchivedInvoiceListValidationService.Validate(normalized[index]);
                if (!validation.IsValid)
                    throw new JsonException($"{index + 1}. arşiv kaydı geçersiz: {validation.Message}");
            }
            var duplicateHysNumber = FindDuplicateHysNumber(normalized);
            if (duplicateHysNumber is not null)
                throw new JsonException($"{duplicateHysNumber} birden fazla arşiv kaydında kullanılmış.");

            return new ArchivedInvoiceListLoadResult(normalized, null, DataFilePath);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            var backupPath = CreateCorruptFileBackup();
            var empty = Array.Empty<ArchivedInvoiceList>();
            Save(empty);
            var warning = "İşlem arşivi okunamadı; boş ve güvenli bir arşiv oluşturuldu."
                          + (backupPath is null ? string.Empty : $" Bozuk dosya korundu: {backupPath}");
            return new ArchivedInvoiceListLoadResult(empty, warning, DataFilePath);
        }
    }

    public void Save(IEnumerable<ArchivedInvoiceList> archives)
    {
        ArgumentNullException.ThrowIfNull(archives);
        var normalized = archives.Select(archive => archive.Normalize()).ToList();
        for (var index = 0; index < normalized.Count; index++)
        {
            var validation = ArchivedInvoiceListValidationService.Validate(normalized[index]);
            if (!validation.IsValid)
                throw new InvalidOperationException($"{index + 1}. arşiv kaydı kaydedilemedi: {validation.Message}");
        }
        var duplicateHysNumber = FindDuplicateHysNumber(normalized);
        if (duplicateHysNumber is not null)
            throw new InvalidOperationException($"{duplicateHysNumber} birden fazla arşiv kaydında kullanılamaz.");

        var folder = Path.GetDirectoryName(DataFilePath)
                     ?? throw new InvalidOperationException("İşlem arşivi veri klasörü belirlenemedi.");
        Directory.CreateDirectory(folder);

        var json = JsonSerializer.Serialize(normalized, _jsonOptions);
        var temporaryPath = DataFilePath + ".tmp";
        File.WriteAllText(temporaryPath, json, new UTF8Encoding(false));
        File.Move(temporaryPath, DataFilePath, true);
    }

    private static string? FindDuplicateHysNumber(IEnumerable<ArchivedInvoiceList> archives) => archives
        .Where(archive => !string.IsNullOrWhiteSpace(archive.HysPaymentOrderDocumentNumber))
        .GroupBy(
            archive => archive.HysPaymentOrderDocumentNumber,
            StringComparer.OrdinalIgnoreCase)
        .FirstOrDefault(group => group.Count() > 1)
        ?.Key;

    private string? CreateCorruptFileBackup()
    {
        try
        {
            var backupPath = Path.Combine(
                Path.GetDirectoryName(DataFilePath)!,
                $"IslemArsivi_Bozuk_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            File.Copy(DataFilePath, backupPath, false);
            return backupPath;
        }
        catch
        {
            return null;
        }
    }
}
