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

        var bytes = JsonSerializer.SerializeToUtf8Bytes(snapshot, JsonOptions);
        return Convert.ToHexString(SHA256.HashData(bytes));
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
}
