using DogruFatura.Core.Models;
using System.Text;
using System.Text.Json;

namespace DogruFatura.Core.Services;

/// <summary>
/// DoğruFatura kalıcı veri dosyalarını yalnız JSON sözdizimi açısından değil,
/// uygulamanın mevcut iş kuralları açısından da doğrular.
/// Restore ve bütünlük kontrolü aynı doğrulama kurallarını kullanır.
/// </summary>
public static class DataDirectoryValidationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly IReadOnlyDictionary<string, string> KnownDataFiles =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Okullar.json"] = DataFileKinds.Schools,
            ["AktifFaturalar.json"] = DataFileKinds.ActiveInvoices,
            ["YetkiliProfili.json"] = DataFileKinds.AuthorityProfile,
            ["IslemArsivi.json"] = DataFileKinds.InvoiceArchive
        };

    public static IReadOnlyCollection<string> RequiredFileNames => KnownDataFiles.Keys.ToArray();

    public static void ValidateCompleteDirectory(string dataRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataRoot);
        if (!Directory.Exists(dataRoot))
            throw new InvalidDataException("Veri klasörü bulunamadı.");

        var missing = RequiredFileNames
            .Where(fileName => !File.Exists(Path.Combine(dataRoot, fileName)))
            .ToArray();
        if (missing.Length > 0)
            throw new InvalidDataException(
                "Zorunlu veri dosyaları eksik: " + string.Join(", ", missing));

        foreach (var fileName in RequiredFileNames)
            ValidateFile(Path.Combine(dataRoot, fileName));
    }

    public static void ValidateFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
            throw new FileNotFoundException("Doğrulanacak veri dosyası bulunamadı.", filePath);

        var fileName = Path.GetFileName(filePath);
        if (!KnownDataFiles.TryGetValue(fileName, out var dataType))
            throw new InvalidDataException($"Tanınmayan DoğruFatura veri dosyası: {fileName}");

        try
        {
            var json = File.ReadAllText(filePath, Encoding.UTF8);
            var readResult = DataFileSchemaService.Read(json, dataType);

            if (string.Equals(fileName, "Okullar.json", StringComparison.OrdinalIgnoreCase))
                ValidateSchools(readResult.Data);
            else if (string.Equals(fileName, "AktifFaturalar.json", StringComparison.OrdinalIgnoreCase))
                ValidateActiveInvoices(readResult.Data);
            else if (string.Equals(fileName, "YetkiliProfili.json", StringComparison.OrdinalIgnoreCase))
                ValidateAuthorityProfile(readResult.Data);
            else if (string.Equals(fileName, "IslemArsivi.json", StringComparison.OrdinalIgnoreCase))
                ValidateArchives(readResult.Data);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or ArgumentException)
        {
            throw new InvalidDataException($"{fileName} semantik olarak geçersiz: {ex.Message}", ex);
        }
    }

    private static void ValidateSchools(JsonElement data)
    {
        var schools = data.Deserialize<List<SchoolRecord>>(JsonOptions)
                      ?? throw new JsonException("Okul/kurum listesi okunamadı.");
        var validated = new List<SchoolRecord>();

        for (var index = 0; index < schools.Count; index++)
        {
            var school = schools[index]
                         ?? throw new JsonException($"{index + 1}. okul/kurum kaydı boş.");
            if (school.InstitutionType is null
                || school.Name is null
                || school.Vkn is null
                || school.ElectricityServiceNumber is null
                || school.TelephoneServiceNumber is null
                || school.InternetServiceNumber is null
                || school.WaterServiceNumber is null)
                throw new JsonException($"{index + 1}. okul/kurum kaydında eksik alan var.");

            var normalized = SchoolValidationService.Normalize(school);
            var validation = SchoolValidationService.Validate(normalized, validated);
            if (!validation.IsValid)
                throw new InvalidOperationException(
                    $"{index + 1}. okul/kurum kaydı geçersiz: {validation.Message}");
            validated.Add(normalized);
        }
    }

    private static void ValidateActiveInvoices(JsonElement data)
    {
        var invoices = data.Deserialize<List<InvoiceRecord>>(JsonOptions)
                       ?? throw new JsonException("Aktif fatura listesi okunamadı.");
        var validated = new List<InvoiceRecord>();
        var ids = new HashSet<Guid>();

        for (var index = 0; index < invoices.Count; index++)
        {
            var invoice = invoices[index]
                          ?? throw new JsonException($"{index + 1}. aktif fatura kaydı boş.");
            invoice = NormalizeInvoice(invoice);

            if (invoice.Id == Guid.Empty || !ids.Add(invoice.Id))
                throw new InvalidOperationException($"{index + 1}. aktif fatura kaydının kimliği geçersiz veya mükerrer.");

            var validation = InvoiceValidationService.Validate(invoice);
            if (!validation.IsValid)
                throw new InvalidOperationException(
                    $"{index + 1}. aktif fatura kaydı geçersiz: {validation.Message}");

            var duplicate = InvoiceDuplicateService.Check(validated, invoice);
            if (duplicate.IsDuplicate)
                throw new InvalidOperationException(
                    $"{index + 1}. aktif fatura mükerrer: {duplicate.Message}");

            validated.Add(invoice);
        }

        var distinctTypes = InvoiceListTypePolicy.GetDistinctTypes(validated);
        if (distinctTypes.Count > 1)
            throw new InvalidOperationException(
                "Aktif fatura listesi birden fazla fatura türü içeriyor: "
                + string.Join(", ", distinctTypes));
    }

    private static void ValidateAuthorityProfile(JsonElement data)
    {
        var profile = data.Deserialize<AuthorityProfile>(JsonOptions)
                      ?? throw new JsonException("Yetkili/imza profili okunamadı.");
        if (profile.RealizationOfficerName is null
            || profile.RealizationOfficerTitle is null
            || profile.SpendingAuthorityName is null
            || profile.SpendingAuthorityTitle is null)
            throw new JsonException("Yetkili/imza profilinde eksik alan var.");

        var normalized = profile.Normalize();
        if (!normalized.IsComplete && !normalized.IsEmpty)
            throw new InvalidOperationException("Yetkili/imza profilindeki bütün alanlar zorunludur.");
    }

    private static void ValidateArchives(JsonElement data)
    {
        var archives = data.Deserialize<List<ArchivedInvoiceList>>(JsonOptions)
                       ?? throw new JsonException("İşlem arşivi okunamadı.");
        var archiveIds = new HashSet<Guid>();
        var archiveNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var hysNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < archives.Count; index++)
        {
            var archive = archives[index]?.Normalize()
                          ?? throw new JsonException($"{index + 1}. arşiv kaydı boş.");

            if (archive.Id == Guid.Empty || !archiveIds.Add(archive.Id))
                throw new InvalidOperationException($"{index + 1}. arşiv kaydının kimliği geçersiz veya mükerrer.");

            if (!archiveNumbers.Add(archive.ArchiveNumber))
                throw new InvalidOperationException($"{archive.ArchiveNumber} arşiv numarası birden fazla kayıtta kullanılmış.");

            var validation = ArchivedInvoiceListValidationService.Validate(archive);
            if (!validation.IsValid)
                throw new InvalidOperationException(
                    $"{index + 1}. arşiv kaydı geçersiz: {validation.Message}");

            if (!string.IsNullOrWhiteSpace(archive.HysPaymentOrderDocumentNumber)
                && !hysNumbers.Add(archive.HysPaymentOrderDocumentNumber))
                throw new InvalidOperationException(
                    $"{archive.HysPaymentOrderDocumentNumber} birden fazla arşiv kaydında kullanılmış.");
        }
    }

    private static InvoiceRecord NormalizeInvoice(InvoiceRecord invoice) => invoice with
    {
        InstitutionType = invoice.InstitutionType ?? string.Empty,
        InstitutionName = invoice.InstitutionName ?? string.Empty,
        Vkn = invoice.Vkn ?? string.Empty,
        InvoiceType = invoice.InvoiceType ?? string.Empty,
        InvoiceNumber = invoice.InvoiceNumber ?? string.Empty,
        ServiceNumber = invoice.ServiceNumber ?? string.Empty
    };
}
