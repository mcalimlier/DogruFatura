using DogruFatura.Core.Models;

namespace DogruFatura.Core.Services;

public sealed record FinalReportValidationResult(
    bool IsValid,
    string Message,
    string? InvoiceType);

public static class FinalReportValidationService
{
    public static FinalReportValidationResult Validate(IEnumerable<InvoiceRecord> invoices)
    {
        ArgumentNullException.ThrowIfNull(invoices);

        var records = invoices.ToArray();
        if (records.Length == 0)
            return Invalid("Nihai çıktı oluşturmak için aktif listede en az bir fatura bulunmalıdır.");

        var types = InvoiceListTypePolicy.GetDistinctTypes(records);
        if (types.Count != 1)
            return Invalid("Nihai çıktı yalnız tek fatura türünden oluşturulabilir. Aktif listeyi tek türe indirin.");

        for (var index = 0; index < records.Length; index++)
        {
            var validation = InvoiceValidationService.Validate(records[index]);
            if (!validation.IsValid)
                return Invalid($"{index + 1}. sıradaki fatura çıktıya hazır değil: {validation.Message}");
        }

        return new FinalReportValidationResult(
            true,
            $"{records.Length} adet {types[0]} faturası nihai çıktıya hazır.",
            types[0]);
    }

    private static FinalReportValidationResult Invalid(string message) => new(false, message, null);
}
