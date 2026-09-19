using DogruFatura.Core.Models;

namespace DogruFatura.Core.Services;

/// <summary>
/// DoğruFatura vergi hesap kurallarının tek metadata kaynağı.
/// Legacy onaylı kuralın hukuki yürürlük tarihi eski veride tutulmadığı için
/// EffectiveFrom bilinçli olarak null bırakılır; doğrulanmamış tarih üretilmez.
/// </summary>
public static class TaxRuleCatalog
{
    public const decimal LegacyApprovedStampDutyRate = 0.00948m;
    public const string CurrentVersion = "DV-0.00948-R1";

    public static TaxRuleDefinition Current { get; } = new(
        CurrentVersion,
        EffectiveFrom: null,
        LegacyApprovedStampDutyRate);

    public static TaxRuleDefinition Resolve(DateOnly? calculationDate = null)
    {
        _ = calculationDate;
        return Current;
    }
}
