using DogruFatura.Core.Models;

namespace DogruFatura.Core.Services;

public sealed record InvoiceDuplicateResult(
    bool IsDuplicate,
    InvoiceRecord? ExistingInvoice,
    string Message);

public sealed record InvoicePossibleDuplicateResult(
    bool IsPossibleDuplicate,
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

    public static InvoicePossibleDuplicateResult CheckPossibleWhenNumberMissing(
        IEnumerable<InvoiceRecord> invoices,
        InvoiceRecord candidate,
        Guid? excludedInvoiceId = null)
    {
        ArgumentNullException.ThrowIfNull(invoices);
        ArgumentNullException.ThrowIfNull(candidate);

        if (!string.IsNullOrWhiteSpace(candidate.InvoiceNumber))
        {
            return new InvoicePossibleDuplicateResult(
                false,
                null,
                "Fatura numarası mevcut olduğu için olası mükerrer kontrolü uygulanmadı.");
        }

        var possibleDuplicate = invoices.FirstOrDefault(existing =>
            existing.Id != excludedInvoiceId
            && string.IsNullOrWhiteSpace(existing.InvoiceNumber)
            && SameInstitution(existing, candidate)
            && Same(existing.InvoiceType, candidate.InvoiceType)
            && Same(existing.ServiceNumber, candidate.ServiceNumber)
            && existing.InvoiceDate.Date == candidate.InvoiceDate.Date
            && existing.DueDate.Date == candidate.DueDate.Date
            && existing.PayableAmount == candidate.PayableAmount
            && existing.VatAmount == candidate.VatAmount);

        return possibleDuplicate is null
            ? new InvoicePossibleDuplicateResult(false, null, "Olası mükerrer fatura bulunmadı.")
            : new InvoicePossibleDuplicateResult(
                true,
                possibleDuplicate,
                $"{possibleDuplicate.InstitutionName} için fatura numarası boş olan benzer bir kayıt aktif listede bulunuyor.");
    }

    private static bool SameInstitution(InvoiceRecord left, InvoiceRecord right)
    {
        if (left.SchoolId != Guid.Empty && right.SchoolId != Guid.Empty)
            return left.SchoolId == right.SchoolId;

        return Same(left.Vkn, right.Vkn)
            && Same(left.InstitutionName, right.InstitutionName);
    }

    private static bool Same(string left, string right) =>
        string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
}
