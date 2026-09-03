using DogruFatura.Core.Models;

namespace DogruFatura.Core.Services;

public sealed record InvoiceDuplicateResult(
    bool IsDuplicate,
    InvoiceRecord? ExistingInvoice,
    string Message);

public static class InvoiceDuplicateService
{
    public static InvoiceDuplicateResult Check(
        IEnumerable<InvoiceRecord> invoices,
        InvoiceRecord candidate,
        Guid? excludedInvoiceId = null)
    {
        ArgumentNullException.ThrowIfNull(invoices);
        ArgumentNullException.ThrowIfNull(candidate);

        if (string.IsNullOrWhiteSpace(candidate.InvoiceNumber))
            return new InvoiceDuplicateResult(false, null, "Fatura numarası boş olduğu için mükerrer numara kontrolü uygulanmadı.");

        var duplicate = invoices.FirstOrDefault(existing =>
            existing.Id != excludedInvoiceId
            && !string.IsNullOrWhiteSpace(existing.InvoiceNumber)
            && Same(existing.InvoiceType, candidate.InvoiceType)
            && Same(existing.ServiceNumber, candidate.ServiceNumber)
            && Same(existing.InvoiceNumber, candidate.InvoiceNumber));

        return duplicate is null
            ? new InvoiceDuplicateResult(false, null, "Mükerrer fatura bulunmadı.")
            : new InvoiceDuplicateResult(
                true,
                duplicate,
                $"{duplicate.InstitutionName} için {duplicate.InvoiceNumber} numaralı fatura aktif listede zaten bulunuyor.");
    }

    private static bool Same(string left, string right) =>
        string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
}
