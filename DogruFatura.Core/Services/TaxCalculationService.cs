using DogruFatura.Core.Models;

namespace DogruFatura.Core.Services;

public sealed class TaxCalculationService
{
    public const decimal StampDutyRate = 0.00948m;
    private readonly decimal _activeStampDutyRate = StampDutyRate;

    public TaxCalculationResult Calculate(
        decimal payableAmount,
        decimal vatAmount,
        string? invoiceType = null,
        decimal interruptionCompensationAmount = 0m)
    {
        if (payableAmount <= 0m)
            return TaxCalculationResult.Empty("Ödenecek tutar sıfırdan büyük olmalıdır.");
        if (vatAmount < 0m)
            return TaxCalculationResult.Empty("KDV tutarı negatif olamaz.");
        if (interruptionCompensationAmount < 0m)
            return TaxCalculationResult.Empty("Kesinti tazminat tutarı negatif olamaz.");
        if (interruptionCompensationAmount > 0m && !IsInterruptionCompensationAllowed(invoiceType))
            return TaxCalculationResult.Empty("Kesinti tazminat tutarı yalnız elektrik faturalarında kullanılabilir.");

        var adjustedVatAmountLimit = payableAmount + interruptionCompensationAmount;
        if (vatAmount > adjustedVatAmountLimit)
            return TaxCalculationResult.Empty(interruptionCompensationAmount > 0m
                ? "KDV tutarı, fatura tutarı ile kesinti tazminatı toplamından büyük olamaz."
                : "KDV tutarı ödenecek tutardan büyük olamaz.");

        // Excel'deki onaylı işleyiş gibi ara değerler tam hassasiyetle korunur.
        // İki ondalık gösterim/yuvarlama yalnız kullanıcı görünümünde ve nihai çıktıda yapılır.
        var vatBase = adjustedVatAmountLimit - vatAmount;
        var stampDutyExempt = IsStampDutyExempt(invoiceType);
        var stampDuty = stampDutyExempt ? 0m : vatBase * _activeStampDutyRate;
        var paymentAmount = payableAmount - stampDuty;
        if (paymentAmount < 0m)
            return TaxCalculationResult.Empty("Hesaplanan damga vergisi fatura tutarından büyük olamaz.");

        return new TaxCalculationResult(
            true,
            vatBase,
            stampDuty,
            paymentAmount,
            interruptionCompensationAmount > 0m
                ? "Elektrik kesinti tazminatı KDV matrahına eklenerek hesaplandı."
                : stampDutyExempt
                ? "Su faturası damga vergisi istisnasıyla hesaplandı."
                : "Hesaplama 0,00948 damga vergisi oranıyla yapıldı.");
    }

    public static bool IsStampDutyExempt(string? invoiceType) =>
        string.Equals(invoiceType?.Trim(), "Su", StringComparison.OrdinalIgnoreCase);

    public static bool IsInterruptionCompensationAllowed(string? invoiceType) =>
        string.Equals(invoiceType?.Trim(), "Elektrik", StringComparison.OrdinalIgnoreCase);
}
