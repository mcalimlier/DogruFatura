using DogruFatura.Core.Models;
using System.Text;
using System.Text.Json;

namespace DogruFatura.Core.Services;

public sealed record InvoiceLoadResult(
    IReadOnlyList<InvoiceRecord> Invoices,
    string? WarningMessage,
    string DataFilePath);

public sealed class InvoiceDataStore
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public InvoiceDataStore(string? dataFilePath = null)
    {
        DataFilePath = dataFilePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            "DoğruFatura", "Veri", "AktifFaturalar.json");
    }

    public string DataFilePath { get; }

    public InvoiceLoadResult LoadOrCreate()
    {
        if (!File.Exists(DataFilePath))
        {
            var empty = Array.Empty<InvoiceRecord>();
            Save(empty);
            return new InvoiceLoadResult(empty, null, DataFilePath);
        }

        try
        {
            var json = File.ReadAllText(DataFilePath, Encoding.UTF8);
            var schema = DataFileSchemaService.Read(json, DataFileKinds.ActiveInvoices);
            var invoices = schema.Data.Deserialize<List<InvoiceRecord>>(_jsonOptions)
                           ?? new List<InvoiceRecord>();
            return FinalizeLoadedInvoices(invoices, schema.SourceSchemaVersion);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            var recovered = TryRecoverValidInvoices(out var rejectedCount);
            var backupPath = CreateCorruptFileBackup();
            var warning = recovered.Count > 0
                ? $"Aktif fatura veri dosyasında sorun bulundu. {recovered.Count} sağlam kayıt geçici kurtarma modunda açıldı; {rejectedCount} kayıt kullanılamadı. Ana veri dosyasının üzerine yazılmadı."
                : "Aktif fatura veri dosyası okunamadı. Güvenlik amacıyla ana dosyanın üzerine boş veri yazılmadı; bu oturumda boş listeyle kurtarma modunda devam ediliyor.";
            if (backupPath is not null) warning += $" Sorunlu dosyanın koruma kopyası: {backupPath}";
            warning += " Veriyi onarmadan yeni kayıt kaydetmeyin; önce güvenlik yedeği alın.";
            return new InvoiceLoadResult(recovered, warning, DataFilePath);
        }
    }

    private InvoiceLoadResult FinalizeLoadedInvoices(List<InvoiceRecord> invoices, int sourceSchemaVersion)
    {
        var requiresSchemaUpgrade = sourceSchemaVersion != DataFileSchemaService.CurrentSchemaVersion;
        var migratedWaterInvoices = 0;
        var taxService = new TaxCalculationService();
        for (var index = 0; index < invoices.Count; index++)
        {
            var invoice = invoices[index] ?? throw new JsonException("Aktif fatura veri dosyasında boş kayıt var.");
            invoice = Normalize(invoice);
            invoices[index] = invoice;

            if (!TaxCalculationService.IsStampDutyExempt(invoice.InvoiceType)) continue;
            var calculation = taxService.Calculate(invoice.PayableAmount, invoice.VatAmount, invoice.InvoiceType, invoice.InterruptionCompensationAmount);
            if (!calculation.IsValid) continue;
            if (invoice.VatBase == calculation.VatBase && invoice.StampDuty == calculation.StampDuty && invoice.PaymentAmount == calculation.PaymentAmount) continue;
            invoices[index] = invoice with { VatBase = calculation.VatBase, StampDuty = calculation.StampDuty, PaymentAmount = calculation.PaymentAmount };
            migratedWaterInvoices++;
        }

        var warnings = new List<string>();
        var invalidCount = invoices.Count(invoice => !InvoiceValidationService.Validate(invoice).IsValid);
        if (invalidCount > 0) warnings.Add($"Aktif listede {invalidCount} eksik veya hatalı kayıt korunarak açıldı. Ayrıntıları Kontrol Merkezi'nden düzeltin.");

        if (requiresSchemaUpgrade || migratedWaterInvoices > 0)
        {
            try
            {
                string? schemaBackupPath = null;
                if (requiresSchemaUpgrade)
                    schemaBackupPath = DataFileSchemaService.CreatePreUpgradeBackup(DataFilePath, sourceSchemaVersion);

                Save(invoices);
                if (requiresSchemaUpgrade)
                    warnings.Add($"Aktif fatura veri dosyası şema {DataFileSchemaService.CurrentSchemaVersion} biçimine güvenle yükseltildi. Önceki biçim korundu: {schemaBackupPath}");
                if (migratedWaterInvoices > 0)
                    warnings.Add($"{migratedWaterInvoices} su faturası yeni damga vergisi kuralına göre güncellendi.");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                if (requiresSchemaUpgrade)
                    warnings.Add($"Aktif fatura veri dosyasının şema yükseltmesi uygulanamadı; mevcut dosya korunuyor: {ex.Message}");
                if (migratedWaterInvoices > 0)
                    warnings.Add($"Su faturası geçişi bu oturumda uygulandı ancak kalıcı dosya yazılamadı: {ex.Message}");
            }
        }

        var types = InvoiceListTypePolicy.GetDistinctTypes(invoices);
        if (types.Count > 1) warnings.Add($"Eski aktif listede birden fazla fatura türü var ({string.Join(", ", types)}). Kayıtlar korunmuştur.");
        return new InvoiceLoadResult(invoices, warnings.Count == 0 ? null : string.Join(" ", warnings), DataFilePath);
    }

    private List<InvoiceRecord> TryRecoverValidInvoices(out int rejectedCount)
    {
        rejectedCount = 0;
        var recovered = new List<InvoiceRecord>();
        try
        {
            var json = File.ReadAllText(DataFilePath, Encoding.UTF8);
            var schema = DataFileSchemaService.Read(json, DataFileKinds.ActiveInvoices);
            if (schema.Data.ValueKind != JsonValueKind.Array) return recovered;
            foreach (var element in schema.Data.EnumerateArray())
            {
                try
                {
                    var invoice = element.Deserialize<InvoiceRecord>(_jsonOptions);
                    if (invoice is null) { rejectedCount++; continue; }
                    invoice = Normalize(invoice);
                    if (!InvoiceValidationService.Validate(invoice).IsValid) { rejectedCount++; continue; }
                    recovered.Add(invoice);
                }
                catch (JsonException) { rejectedCount++; }
            }
        }
        catch { }
        return recovered;
    }

    private static InvoiceRecord Normalize(InvoiceRecord invoice) => invoice with
    {
        InstitutionType = invoice.InstitutionType ?? string.Empty,
        InstitutionName = invoice.InstitutionName ?? string.Empty,
        Vkn = invoice.Vkn ?? string.Empty,
        InvoiceType = invoice.InvoiceType ?? string.Empty,
        InvoiceNumber = invoice.InvoiceNumber ?? string.Empty,
        ServiceNumber = invoice.ServiceNumber ?? string.Empty
    };

    public void Save(IEnumerable<InvoiceRecord> invoices)
    {
        var folder = Path.GetDirectoryName(DataFilePath) ?? throw new InvalidOperationException("Aktif fatura veri klasörü belirlenemedi.");
        Directory.CreateDirectory(folder);
        var temporaryPath = DataFilePath + ".tmp";
        var json = DataFileSchemaService.SerializeCurrent(
            DataFileKinds.ActiveInvoices,
            invoices.ToList(),
            _jsonOptions);
        File.WriteAllText(temporaryPath, json, new UTF8Encoding(false));
        File.Move(temporaryPath, DataFilePath, true);
    }

    private string? CreateCorruptFileBackup()
    {
        try
        {
            var backupPath = Path.Combine(Path.GetDirectoryName(DataFilePath)!, $"AktifFaturalar_Bozuk_{DateTime.Now:yyyyMMdd_HHmmss_fff}.json");
            File.Copy(DataFilePath, backupPath, false);
            return backupPath;
        }
        catch { return null; }
    }
}
