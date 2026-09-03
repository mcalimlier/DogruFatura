using DogruFatura.Core.Models;

namespace DogruFatura.Core.Services;

public sealed record InvoiceValidationResult(bool IsValid, string Message);

public static class InvoiceValidationService
{
    private static readonly HashSet<string> InvoiceTypes = new(StringComparer.Ordinal)
    {
        "Elektrik",
        "Telefon",
        "İnternet",
        "Su"
    };

    public static InvoiceValidationResult Validate(InvoiceRecord invoice)
    {
        if (invoice.Id == Guid.Empty) return Invalid("Fatura kayıt kimliği geçersiz.");
        if (string.IsNullOrWhiteSpace(invoice.InstitutionType)) return Invalid("Kurum türü seçilmelidir.");
        if (string.IsNullOrWhiteSpace(invoice.InstitutionName)) return Invalid("Okul / kurum seçilmelidir.");
        if (!string.IsNullOrWhiteSpace(invoice.Vkn) && !invoice.Vkn.All(char.IsDigit))
            return Invalid("VKN girildiğinde yalnızca rakamlardan oluşmalıdır.");
        if (!string.IsNullOrWhiteSpace(invoice.Vkn) && invoice.Vkn.Length != 10)
            return Invalid("VKN girildiğinde 10 haneli olmalıdır.");
        if (!InvoiceTypes.Contains(invoice.InvoiceType)) return Invalid("Geçerli bir fatura türü seçilmelidir.");
        if (string.IsNullOrWhiteSpace(invoice.ServiceNumber)) return Invalid("Seçilen fatura türünün hizmet numarası eksik.");
        if (!invoice.ServiceNumber.All(char.IsDigit)) return Invalid("Hizmet numarası yalnızca rakamlardan oluşmalıdır.");
        if (invoice.ServiceNumber.Length > 50) return Invalid("Hizmet numarası en fazla 50 haneli olabilir.");
        if (invoice.InvoiceNumber.Length > 100) return Invalid("Fatura numarası en fazla 100 karakter olabilir.");
        if (invoice.CreatedAt == default || invoice.InvoiceDate == default || invoice.DueDate == default)
            return Invalid("Fatura tarih bilgileri eksik.");
        if (invoice.DueDate.Date < invoice.InvoiceDate.Date) return Invalid("Son ödeme tarihi fatura tarihinden önce olamaz.");
        if (invoice.PayableAmount <= 0m) return Invalid("Ödenecek tutar sıfırdan büyük olmalıdır.");
        if (invoice.VatAmount < 0m) return Invalid("KDV tutarı negatif olamaz.");

        var calculation = new TaxCalculationService().Calculate(
            invoice.PayableAmount,
            invoice.VatAmount,
            invoice.InvoiceType,
            invoice.InterruptionCompensationAmount);
        if (!calculation.IsValid) return Invalid(calculation.Message);
        if (invoice.VatBase != calculation.VatBase
            || invoice.StampDuty != calculation.StampDuty
            || invoice.PaymentAmount != calculation.PaymentAmount)
            return Invalid("Faturanın vergi hesap sonuçları güncel hesap motoruyla uyuşmuyor.");

        return new InvoiceValidationResult(true, "Fatura kaydı geçerli.");
    }

    private static InvoiceValidationResult Invalid(string message) => new(false, message);
}
