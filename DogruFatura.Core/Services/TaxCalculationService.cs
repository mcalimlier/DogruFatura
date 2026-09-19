using DogruFatura.Core.Models;

namespace DogruFatura.Core.Services;

public sealed class TaxCalculationService
{
    public const decimal StampDutyRate = TaxRuleCatalog.LegacyApprovedStampDutyRate;

    private readonly TaxRuleDefinition _activeRule;

    public TaxCalculationService(TaxRuleDefinition? activeRule = null)
    {
        _activeRule = activeRule ?? TaxRuleCatalog.Current;
    }

    public TaxRuleDefinition ActiveRule => _activeRule;

    public TaxCalculationResult Calculate(
        decimal payableAmount,
        decimal vatAmount,
        string? invoiceType = null,
        decimal interruptionCompensationAmount = 0m)
    {
        if (payableAmount <= 0m)
            return Invalid("Ödenecek tutar sıfırdan büyük olmalıdır.");
        if (vatAmount < 0m)
            return Invalid("KDV tutarı negatif olamaz.");
        if (interruptionCompensationAmount < 0m)
            return Invalid("Kesinti tazminat tutarı negatif olamaz.");
        if (interruptionCompensationAmount > 0m && !IsInterruptionCompensationAllowed(invoiceType))
            return Invalid("Kesinti tazminat tutarı yalnız elektrik faturalarında kullanılabilir.");

        var adjustedVatAmountLimit = payableAmount + interruptionCompensationAmount;
        if (vatAmount > adjustedVatAmountLimit)
            return Invalid(interruptionCompensationAmount > 0m
                ? "KDV tutarı, fatura tutarı ile kesinti tazminatı toplamından büyük olamaz."
                : "KDV tutarı ödenecek tutardan büyük olamaz.");

        // Excel'deki onaylı işleyiş gibi ara değerler tam hassasiyetle korunur.
        // İki ondalık gösterim/yuvarlama yalnız kullanıcı görünümünde ve nihai çıktıda yapılır.
        var vatBase = adjustedVatAmountLimit - vatAmount;
        var stampDutyExempt = IsStampDutyExempt(invoiceType);
        var stampDuty = stampDutyExempt ? 0m : vatBase * _activeRule.StampDutyRate;
        var paymentAmount = payableAmount - stampDuty;
        if (paymentAmount < 0m)
            return Invalid("Hesaplanan damga vergisi fatura tutarından büyük olamaz.");

        return new TaxCalculationResult(
            true,
            vatBase,
            stampDuty,
            paymentAmount,
            interruptionCompensationAmount > 0m
                ? "Elektrik kesinti tazminatı KDV matrahına eklenerek hesaplandı."
                : stampDutyExempt
                ? "Su faturası damga vergisi istisnasıyla hesaplandı."
                : "Hesaplama 0,00948 damga vergisi oranıyla yapıldı.",
            _activeRule.Version,
            _activeRule.EffectiveFrom,
            _activeRule.StampDutyRate);
    }

    private TaxCalculationResult Invalid(string message) =>
        TaxCalculationResult.Empty(
            message,
            _activeRule.Version,
            _activeRule.EffectiveFrom,
            _activeRule.StampDutyRate);

    public static bool IsStampDutyExempt(string? invoiceType) =>
        string.Equals(invoiceType?.Trim(), "Su", StringComparison.OrdinalIgnoreCase);

    public static bool IsInterruptionCompensationAllowed(string? invoiceType) =>
        string.Equals(invoiceType?.Trim(), "Elektrik", StringComparison.OrdinalIgnoreCase);
}
