namespace DogruFatura.Core.Models;

public sealed record LegacyInvoiceEvidenceRecord(
    string InstitutionName,
    string ServiceNumber,
    string InvoiceNumber,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    DateTime DueDate,
    decimal GrossAmount)
{
    public LegacyInvoiceEvidenceRecord Normalize() => this with
    {
        InstitutionName = (InstitutionName ?? string.Empty).Trim(),
        ServiceNumber = (ServiceNumber ?? string.Empty).Trim(),
        InvoiceNumber = (InvoiceNumber ?? string.Empty).Trim()
    };
}
