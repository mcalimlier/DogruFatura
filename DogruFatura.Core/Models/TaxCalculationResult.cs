namespace DogruFatura.Core.Models;

public sealed record TaxCalculationResult(
    bool IsValid,
    decimal VatBase,
    decimal StampDuty,
    decimal PaymentAmount,
    string Message)
{
    public static TaxCalculationResult Empty(string message = "Tutar ve KDV girildiğinde hesaplama yapılır.") =>
        new(false, 0m, 0m, 0m, message);
}
