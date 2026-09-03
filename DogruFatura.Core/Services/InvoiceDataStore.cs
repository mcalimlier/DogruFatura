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
            "DoğruFatura",
            "Veri",
            "AktifFaturalar.json");
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
            var invoices = JsonSerializer.Deserialize<List<InvoiceRecord>>(json, _jsonOptions)
                           ?? new List<InvoiceRecord>();

            var migratedWaterInvoices = 0;
            var taxService = new TaxCalculationService();
            for (var index = 0; index < invoices.Count; index++)
            {
                var invoice = invoices[index];
                if (invoice is null)
                    throw new JsonException("Aktif fatura veri dosyasında boş kayıt var.");

                invoice = invoice with
                {
                    InstitutionType = invoice.InstitutionType ?? string.Empty,
                    InstitutionName = invoice.InstitutionName ?? string.Empty,
                    Vkn = invoice.Vkn ?? string.Empty,
                    InvoiceType = invoice.InvoiceType ?? string.Empty,
                    InvoiceNumber = invoice.InvoiceNumber ?? string.Empty,
                    ServiceNumber = invoice.ServiceNumber ?? string.Empty
                };
                invoices[index] = invoice;

                if (!TaxCalculationService.IsStampDutyExempt(invoice.InvoiceType)) continue;

                var waterCalculation = taxService.Calculate(
                    invoice.PayableAmount,
                    invoice.VatAmount,
                    invoice.InvoiceType,
                    invoice.InterruptionCompensationAmount);
                if (!waterCalculation.IsValid) continue;
                if (invoice.VatBase == waterCalculation.VatBase
                    && invoice.StampDuty == waterCalculation.StampDuty
                    && invoice.PaymentAmount == waterCalculation.PaymentAmount)
                    continue;

                invoices[index] = invoice with
                {
                    VatBase = waterCalculation.VatBase,
                    StampDuty = waterCalculation.StampDuty,
                    PaymentAmount = waterCalculation.PaymentAmount
                };
                migratedWaterInvoices++;
            }

            var warningParts = new List<string>();
            var invalidInvoiceCount = invoices.Count(invoice => !InvoiceValidationService.Validate(invoice).IsValid);
            if (invalidInvoiceCount > 0)
            {
                warningParts.Add(
                    $"Aktif listede {invalidInvoiceCount} eksik veya hatalı kayıt korunarak açıldı. "
                    + "Ayrıntıları görmek ve düzeltmek için Kontrol Merkezi'ni kullanın.");
            }
            if (migratedWaterInvoices > 0)
            {
                try
                {
                    Save(invoices);
                    warningParts.Add($"{migratedWaterInvoices} su faturası yeni damga vergisi kuralına göre otomatik güncellendi.");
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    warningParts.Add($"{migratedWaterInvoices} su faturası bu oturumda yeni kurala göre güncellendi; kalıcı dosya yazılamadı: {ex.Message}");
                }
            }

            var existingTypes = InvoiceListTypePolicy.GetDistinctTypes(invoices);
            if (existingTypes.Count > 1)
            {
                warningParts.Add(
                    $"Eski aktif listede birden fazla fatura türü var ({string.Join(", ", existingTypes)}). "
                    + "Kayıtlar korunmuştur; yeni fatura eklemeden önce listeyi silme veya düzenleme yoluyla tek türe indirin.");
            }

            var warningMessage = warningParts.Count == 0 ? null : string.Join(" ", warningParts);
            return new InvoiceLoadResult(invoices, warningMessage, DataFilePath);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            var backupPath = CreateCorruptFileBackup();
            var empty = Array.Empty<InvoiceRecord>();
            Save(empty);
            var warning = "Aktif fatura veri dosyası okunamadı; boş bir aktif liste oluşturuldu."
                          + (backupPath is null ? string.Empty : $" Eski dosya korundu: {backupPath}");
            return new InvoiceLoadResult(empty, warning, DataFilePath);
        }
    }

    public void Save(IEnumerable<InvoiceRecord> invoices)
    {
        var folder = Path.GetDirectoryName(DataFilePath)
                     ?? throw new InvalidOperationException("Aktif fatura veri klasörü belirlenemedi.");
        Directory.CreateDirectory(folder);

        var json = JsonSerializer.Serialize(invoices.ToList(), _jsonOptions);
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
                $"AktifFaturalar_Bozuk_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            File.Copy(DataFilePath, backupPath, false);
            return backupPath;
        }
        catch
        {
            return null;
        }
    }
}
