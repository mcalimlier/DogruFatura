using DogruFatura.Core.Models;

namespace DogruFatura.Core.Services;

public sealed record ArchivedInvoiceListValidationResult(bool IsValid, string Message);

public static class ArchivedInvoiceListValidationService
{
    public static ArchivedInvoiceListValidationResult Validate(ArchivedInvoiceList archive)
    {
        ArgumentNullException.ThrowIfNull(archive);

        if (archive.Id == Guid.Empty) return Invalid("Arşiv kayıt kimliği geçersiz.");
        if (string.IsNullOrWhiteSpace(archive.ArchiveNumber)) return Invalid("Arşiv numarası eksik.");
        if (archive.ArchivedAt == default) return Invalid("Arşiv tarihi eksik.");
        if (archive.Invoices is null || archive.Invoices.Count == 0) return Invalid("Arşiv listesi boş olamaz.");
        if (archive.AuthorityProfile is null || !archive.AuthorityProfile.IsComplete)
            return Invalid("Arşivdeki yetkili/imza bilgileri eksik.");
        if (!string.IsNullOrWhiteSpace(archive.PdfFileName)
            && (!string.Equals(Path.GetFileName(archive.PdfFileName), archive.PdfFileName, StringComparison.Ordinal)
                || !string.Equals(Path.GetExtension(archive.PdfFileName), ".pdf", StringComparison.OrdinalIgnoreCase)))
            return Invalid("Arşiv PDF dosya adı geçersiz.");
        if (!string.IsNullOrWhiteSpace(archive.PdfSha256)
            && !FileIntegrityService.IsValidSha256(archive.PdfSha256))
            return Invalid("Arşiv PDF SHA-256 bilgisi geçersiz.");
        if (!string.IsNullOrWhiteSpace(archive.ClosedSnapshotSha256)
            && !FileIntegrityService.IsValidSha256(archive.ClosedSnapshotSha256))
            return Invalid("Kapalı arşiv mühür SHA-256 bilgisi geçersiz.");

        var reportValidation = FinalReportValidationService.Validate(archive.Invoices);
        if (!reportValidation.IsValid || reportValidation.InvoiceType is null)
            return Invalid(reportValidation.Message);
        if (!string.Equals(reportValidation.InvoiceType, archive.InvoiceType, StringComparison.Ordinal))
            return Invalid("Arşiv liste türü, fatura kayıtlarıyla uyuşmuyor.");

        if (archive.HasClosedSnapshot && !archive.IsClosed)
            return Invalid("Mühürlü HYS arşivi kapalı durumda kalmalıdır.");

        if (archive.IsClosed)
        {
            if (!HysPaymentOrderService.TryNormalize(
                    archive.HysPaymentOrderDocumentNumber,
                    out var normalizedHysNumber,
                    out var hysError))
                return Invalid(hysError);
            if (!string.Equals(
                    archive.HysPaymentOrderDocumentNumber,
                    normalizedHysNumber,
                    StringComparison.Ordinal))
                return Invalid("HYS Ödeme Emri Belge No standart biçimde saklanmalıdır.");
            if (!archive.ClosedAt.HasValue)
                return Invalid("Kapalı arşiv kaydının kapanış tarihi eksik.");
        }

        if (archive.HasClosedSnapshot && !ClosedArchiveSnapshotService.Matches(archive))
            return Invalid("Kapalı arşiv mühür SHA-256 özeti kayıt içeriğiyle uyuşmuyor.");

        return new ArchivedInvoiceListValidationResult(true, "Arşiv kaydı geçerli.");
    }

    private static ArchivedInvoiceListValidationResult Invalid(string message) => new(false, message);
}
