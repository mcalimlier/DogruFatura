using DogruFatura.Core.Models;
using System.Globalization;

namespace DogruFatura.Core.Services;

public sealed record PdfReviewResult(bool IsValid, string Message, ArchivedInvoiceList? Archive);

/// <summary>A human review may correct OCR fields, but cannot override invoice rules or official total reconciliation.</summary>
public static class PdfReviewService
{
    public static PdfReviewResult Review(string hys, IEnumerable<InvoiceRecord> invoices, decimal officialGross,
        decimal officialStamp, decimal officialPayment, string reason, AuthorityProfile authority)
    {
        var rows = invoices.ToList();
        if (string.IsNullOrWhiteSpace(reason)) return new(false, "Düzeltme ve kaynak karşılaştırması açıklaması zorunludur.", null);
        if (!HysPaymentOrderService.TryNormalize(hys, out var normalized, out var error)) return new(false, error, null);
        var report = FinalReportValidationService.Validate(rows);
        if (!report.IsValid) return new(false, report.Message, null);
        static decimal Round(decimal amount) => decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        if (officialGross <= 0 || officialStamp < 0 || officialPayment <= 0
            || Round(rows.Sum(i => i.PayableAmount)) != Round(officialGross)
            || Round(rows.Sum(i => i.StampDuty)) != Round(officialStamp)
            || Round(rows.Sum(i => i.PaymentAmount)) != Round(officialPayment)
            || Round(officialGross - officialStamp) != Round(officialPayment))
            return new(false, "Satır toplamları ile kaynak belgedeki brüt, damga ve net ödeme tutarları uyuşmuyor. "
                + $"Satır − kaynak farkı: Brüt {NumberParser.Money(Round(rows.Sum(i => i.PayableAmount)) - Round(officialGross))}, "
                + $"damga {NumberParser.Money(Round(rows.Sum(i => i.StampDuty)) - Round(officialStamp))}, "
                + $"net {NumberParser.Money(Round(rows.Sum(i => i.PaymentAmount)) - Round(officialPayment))}. "
                + "Kaynakta brüt − damga = net eşitliği de sağlanmalıdır. Fark giderilmeden kayıt oluşturulamaz.", null);
        var now = DateTime.Now;
        var archive = ClosedArchiveSnapshotService.Seal(new ArchivedInvoiceList(Guid.NewGuid(),
            $"INCELEME-{now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6]}", now, report.InvoiceType!, rows, authority,
            normalized, true, now, LegacyEvidenceNote: $"Kullanıcı kaynak karşılaştırması ({now.ToString("dd.MM.yyyy HH:mm", CultureInfo.GetCultureInfo("tr-TR"))}): {reason.Trim()} · Kaynak toplamları: {officialGross:N2} / {officialStamp:N2} / {officialPayment:N2}"));
        var validation = ArchivedInvoiceListValidationService.Validate(archive);
        return validation.IsValid ? new(true, "Kaynak toplamları ve bütün fatura satırları doğrulandı.", archive) : new(false, validation.Message, null);
    }
}
