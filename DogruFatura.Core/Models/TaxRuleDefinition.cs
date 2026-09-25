namespace DogruFatura.Core.Models;

/// <summary>
/// Bir vergi hesap kural setinin izlenebilir kimliğini ve yürürlük bilgisini taşır.
/// EffectiveFrom null ise legacy uygulamada doğrulanmış tarih metadata'sı bulunmadığı anlamına gelir;
/// hukuki bir tarih varsayılmaz.
/// </summary>
public sealed record TaxRuleDefinition(
    string Version,
    DateOnly? EffectiveFrom,
    decimal StampDutyRate)
{
    public bool IsEffectiveOn(DateOnly date) =>
        !EffectiveFrom.HasValue || date >= EffectiveFrom.Value;
}
