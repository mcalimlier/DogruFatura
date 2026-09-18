using System.Text.Json.Serialization;

namespace DogruFatura.Core.Models;

public sealed record SchoolRecord(
    string InstitutionType,
    string Name,
    string Vkn,
    string ElectricityServiceNumber,
    string TelephoneServiceNumber,
    string InternetServiceNumber,
    string WaterServiceNumber,
    Guid SchoolId = default)
{
    [JsonIgnore]
    public InstitutionKind Kind => DomainValueText.ParseInstitutionKind(InstitutionType);

    public override string ToString() => Name;

    public string GetServiceNumber(string? invoiceType) =>
        GetServiceNumber(DomainValueText.ParseInvoiceKind(invoiceType));

    public string GetServiceNumber(InvoiceKind invoiceKind) => invoiceKind switch
    {
        InvoiceKind.Electricity => ElectricityServiceNumber,
        InvoiceKind.Telephone => TelephoneServiceNumber,
        InvoiceKind.Internet => InternetServiceNumber,
        InvoiceKind.Water => WaterServiceNumber,
        _ => string.Empty
    };
}
