using DogruFatura.Core.Models;

namespace DogruFatura.Core.Services;

public sealed record InvoiceValidationResult(bool IsValid, string Message);

public static class InvoiceValidationService
{
    public static InvoiceValidationResult Validate(InvoiceRecord invoice)
    {
        var evaluation = InvoiceRulesEngine.Evaluate(invoice);

        return evaluation.Violations.Count == 0
            ? new InvoiceValidationResult(true, "Fatura kaydı geçerli.")
            : Invalid(evaluation.Violations[0].Message);
    }

    private static InvoiceValidationResult Invalid(string message) => new(false, message);
}
