namespace DogruFatura.Core.Models;

public sealed record TaxCalculationResult(
    bool IsValid,
    decimal VatBase,
    decimal StampDuty,
    decimal PaymentAmount,
    string Message,
    string RuleVersion = "",
    DateOnly? RuleEffectiveFrom = null,
    decimal AppliedStampDutyRate = 0m)
{
    public static TaxCalculationResult Empty(
        string message = "Tutar ve KDV girildiğinde hesaplama yapılır.",
        string ruleVersion = "",
        DateOnly? ruleEffectiveFrom = null,
        decimal appliedStampDutyRate = 0m) =>
        new(
            false,
            0m,
            0m,
            0m,
            message,
            ruleVersion,
            ruleEffectiveFrom,
            appliedStampDutyRate);
}
