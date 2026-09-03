using DogruFatura.Core.Models;

namespace DogruFatura.Core.Services;

public sealed record InvoiceListTypeResult(
    bool IsAllowed,
    string? LockedType,
    string Message);

public static class InvoiceListTypePolicy
{
    public static InvoiceListTypeResult Check(
        IEnumerable<InvoiceRecord> invoices,
        string? candidateType,
        Guid? excludedInvoiceId = null)
    {
        ArgumentNullException.ThrowIfNull(invoices);

        if (string.IsNullOrWhiteSpace(candidateType))
            return new InvoiceListTypeResult(false, null, "Fatura türü seçilmelidir.");

        var existingTypes = GetDistinctTypes(invoices, excludedInvoiceId);
        if (existingTypes.Count == 0)
            return new InvoiceListTypeResult(
                true,
                candidateType.Trim(),
                $"Yeni aktif liste {candidateType.Trim()} faturası olarak başlatılabilir.");

        if (existingTypes.Count > 1)
            return new InvoiceListTypeResult(
                false,
                null,
                "Aktif listede birden fazla fatura türü bulunuyor. Yeni kayıt eklemeden önce listeyi tek türe indirin.");

        var lockedType = existingTypes[0];
        return string.Equals(lockedType, candidateType.Trim(), StringComparison.OrdinalIgnoreCase)
            ? new InvoiceListTypeResult(true, lockedType, $"Aktif liste {lockedType} faturalarına kilitli.")
            : new InvoiceListTypeResult(
                false,
                lockedType,
                $"Aktif liste yalnız {lockedType} faturalarından oluşabilir; {candidateType.Trim()} faturası bu listeye eklenemez.");
    }

    public static IReadOnlyList<string> GetDistinctTypes(
        IEnumerable<InvoiceRecord> invoices,
        Guid? excludedInvoiceId = null)
    {
        ArgumentNullException.ThrowIfNull(invoices);

        return invoices
            .Where(invoice => invoice.Id != excludedInvoiceId)
            .Select(invoice => invoice.InvoiceType.Trim())
            .Where(type => type.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(type => type, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    public static string GetListTypeLabel(IEnumerable<InvoiceRecord> invoices)
    {
        var types = GetDistinctTypes(invoices);
        return types.Count switch
        {
            0 => "Henüz belirlenmedi",
            1 => types[0],
            _ => "Karma liste — düzeltilmeli"
        };
    }
}
