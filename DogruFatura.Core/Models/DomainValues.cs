namespace DogruFatura.Core.Models;

public enum InvoiceKind
{
    Unknown = 0,
    Electricity,
    Telephone,
    Internet,
    Water
}

public enum InstitutionKind
{
    Unknown = 0,
    PrimarySchool,
    MiddleSchool,
    Preschool,
    Institution
}

public enum ArchiveState
{
    Open = 0,
    Closed
}

public static class DomainValueText
{
    public static InvoiceKind ParseInvoiceKind(string? value) => value?.Trim() switch
    {
        "Elektrik" => InvoiceKind.Electricity,
        "Telefon" => InvoiceKind.Telephone,
        "İnternet" => InvoiceKind.Internet,
        "Su" => InvoiceKind.Water,
        _ => InvoiceKind.Unknown
    };

    public static InstitutionKind ParseInstitutionKind(string? value) => value?.Trim() switch
    {
        "İlkokul" => InstitutionKind.PrimarySchool,
        "Ortaokul" => InstitutionKind.MiddleSchool,
        "Anasınıfı/Anaokulu" => InstitutionKind.Preschool,
        "Kurum" => InstitutionKind.Institution,
        _ => InstitutionKind.Unknown
    };

    public static ArchiveState? ParseArchiveState(string? value) => value?.Trim() switch
    {
        "Açık" => ArchiveState.Open,
        "Kapalı" => ArchiveState.Closed,
        _ => null
    };

    public static ControlIssueSeverity? ParseControlIssueSeverity(string? value) => value?.Trim() switch
    {
        "Hata" => ControlIssueSeverity.Error,
        "Uyarı" => ControlIssueSeverity.Warning,
        _ => null
    };

    public static string ToStorageValue(InvoiceKind value) => value switch
    {
        InvoiceKind.Electricity => "Elektrik",
        InvoiceKind.Telephone => "Telefon",
        InvoiceKind.Internet => "İnternet",
        InvoiceKind.Water => "Su",
        _ => string.Empty
    };

    public static string ToStorageValue(InstitutionKind value) => value switch
    {
        InstitutionKind.PrimarySchool => "İlkokul",
        InstitutionKind.MiddleSchool => "Ortaokul",
        InstitutionKind.Preschool => "Anasınıfı/Anaokulu",
        InstitutionKind.Institution => "Kurum",
        _ => string.Empty
    };

    public static string ToStorageValue(ArchiveState value) => value switch
    {
        ArchiveState.Open => "Açık",
        ArchiveState.Closed => "Kapalı",
        _ => string.Empty
    };

    public static string ToDisplayValue(ControlIssueSeverity value) => value switch
    {
        ControlIssueSeverity.Error => "Hata",
        ControlIssueSeverity.Warning => "Uyarı",
        _ => string.Empty
    };
}
