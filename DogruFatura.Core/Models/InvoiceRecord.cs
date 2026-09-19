using System.Text.Json.Serialization;

namespace DogruFatura.Core.Models;

public sealed record InvoiceRecord(
    Guid Id,
    DateTime CreatedAt,
    string InstitutionType,
    string InstitutionName,
    string Vkn,
    string InvoiceType,
    string InvoiceNumber,
    string ServiceNumber,
    DateTime InvoiceDate,
    DateTime DueDate,
    decimal PayableAmount,
    decimal VatAmount,
    decimal VatBase,
    decimal StampDuty,
    decimal PaymentAmount,
    decimal InterruptionCompensationAmount = 0m,
    Guid SchoolId = default,
    string TaxRuleVersion = "",
    DateOnly? TaxRuleEffectiveFrom = null,
    decimal AppliedStampDutyRate = 0m)
{
    [JsonIgnore]
    public InvoiceKind Kind => DomainValueText.ParseInvoiceKind(InvoiceType);

    [JsonIgnore]
    public InstitutionKind InstitutionKind => DomainValueText.ParseInstitutionKind(InstitutionType);

    [JsonIgnore]
    public bool HasTaxRuleMetadata =>
        !string.IsNullOrWhiteSpace(TaxRuleVersion) && AppliedStampDutyRate > 0m;
}
