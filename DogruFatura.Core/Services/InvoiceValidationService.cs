using DogruFatura.Core.Models;

namespace DogruFatura.Core.Services;

public sealed record InvoiceValidationResult(bool IsValid, string Message);

public static class InvoiceValidationService
{
    public static InvoiceValidationResult Validate(InvoiceRecord invoice)
    {
        var evaluation = InvoiceRulesEngine.Evaluate(invoice);
        var firstViolation = evaluation.Violations.FirstOrDefault();

        return firstViolation is null
            ? new InvoiceValidationResult(true, "Fatura kaydı geçerli.")
            : Invalid(firstViolation.Message);
    }

    private static InvoiceValidationResult Invalid(string message) => new(false, message);
}
