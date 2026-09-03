namespace DogruFatura.Core.Models;

public sealed record SchoolRecord(
    string InstitutionType,
    string Name,
    string Vkn,
    string ElectricityServiceNumber,
    string TelephoneServiceNumber,
    string InternetServiceNumber,
    string WaterServiceNumber)
{
    public override string ToString() => Name;

    public string GetServiceNumber(string? invoiceType) => invoiceType switch
    {
        "Elektrik" => ElectricityServiceNumber,
        "Telefon" => TelephoneServiceNumber,
        "İnternet" => InternetServiceNumber,
        "Su" => WaterServiceNumber,
        _ => string.Empty
    };
}
