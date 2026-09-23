using DogruFatura.Core.Models;
using System.Security.Cryptography;
using System.Text.Json;

namespace DogruFatura.Core.Services;

/// <summary>
/// Kapalı HYS arşivinin iş/veri içeriğini deterministik bir SHA-256 özetiyle mühürler.
/// PDF dosya adı ve PDF SHA-256 bu özete bilinçli olarak dahil edilmez; PDF bütünlüğü
/// ayrı PdfSha256 mekanizmasıyla korunur ve eksik PDF sonradan güvenle üretilebilir.
/// </summary>
public static class ClosedArchiveSnapshotService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    public static ArchivedInvoiceList Seal(ArchivedInvoiceList archive)
    {
        ArgumentNullException.ThrowIfNull(archive);

        var normalized = archive.Normalize();
        if (!normalized.IsClosed)
            throw new InvalidOperationException("Yalnız kapalı arşiv kaydı mühürlenebilir.");
        if (string.IsNullOrWhiteSpace(normalized.HysPaymentOrderDocumentNumber))
            throw new InvalidOperationException("Mühürlenecek arşivde HYS Ödeme Emri Belge No bulunmalıdır.");
        if (!normalized.ClosedAt.HasValue)
            throw new InvalidOperationException("Mühürlenecek arşivde kapanış tarihi bulunmalıdır.");

        return normalized with
        {
            ClosedSnapshotSha256 = ComputeSha256(normalized)
        };
    }

    public static string ComputeSha256(ArchivedInvoiceList archive)
    {
        ArgumentNullException.ThrowIfNull(archive);

        var normalized = archive.Normalize();
        // Normal arşivler için eski snapshot şemasını aynen koru. Böylece
        // mevcut kapalı arşivlerin daha önce yazılmış SHA-256 mühürleri geçerli kalır.
        if (!normalized.IsLegacyEvidenceOnly)
        {
            var snapshot = new ClosedArchiveSnapshot(
                normalized.Id,
                normalized.ArchiveNumber,
                normalized.ArchivedAt,
                normalized.InvoiceType,
                normalized.Invoices,
                normalized.AuthorityProfile,
                normalized.HysPaymentOrderDocumentNumber,
                normalized.IsClosed,
                normalized.ClosedAt);

            var legacyBytes = JsonSerializer.SerializeToUtf8Bytes(snapshot, JsonOptions);
            return Convert.ToHexString(SHA256.HashData(legacyBytes));
        }

        var evidenceSnapshot = new LegacyEvidenceClosedArchiveSnapshot(
            normalized.Id,
            normalized.ArchiveNumber,
            normalized.ArchivedAt,
            normalized.InvoiceType,
            normalized.LegacyEvidenceItems ?? new List<LegacyInvoiceEvidenceRecord>(),
            normalized.LegacyGrossTotal,
            normalized.LegacyStampDutyTotal,
            normalized.LegacyPaymentTotal,
            normalized.LegacyEvidenceNote,
            normalized.AuthorityProfile,
            normalized.HysPaymentOrderDocumentNumber,
            normalized.IsClosed,
            normalized.ClosedAt);

        var evidenceBytes = JsonSerializer.SerializeToUtf8Bytes(evidenceSnapshot, JsonOptions);
        return Convert.ToHexString(SHA256.HashData(evidenceBytes));
    }

    public static bool Matches(ArchivedInvoiceList archive) =>
        archive.HasClosedSnapshot
        && string.Equals(
            archive.ClosedSnapshotSha256,
            ComputeSha256(archive),
            StringComparison.OrdinalIgnoreCase);

    private sealed record ClosedArchiveSnapshot(
        Guid Id,
        string ArchiveNumber,
        DateTime ArchivedAt,
        string InvoiceType,
        IReadOnlyList<InvoiceRecord> Invoices,
        AuthorityProfile AuthorityProfile,
        string HysPaymentOrderDocumentNumber,
        bool IsClosed,
        DateTime? ClosedAt);

    private sealed record LegacyEvidenceClosedArchiveSnapshot(
        Guid Id,
        string ArchiveNumber,
        DateTime ArchivedAt,
        string InvoiceType,
        IReadOnlyList<LegacyInvoiceEvidenceRecord> EvidenceItems,
        decimal GrossTotal,
        decimal StampDutyTotal,
        decimal PaymentTotal,
        string EvidenceNote,
        AuthorityProfile AuthorityProfile,
        string HysPaymentOrderDocumentNumber,
        bool IsClosed,
        DateTime? ClosedAt);
}
