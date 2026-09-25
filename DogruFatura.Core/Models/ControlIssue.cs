namespace DogruFatura.Core.Models;

public enum ControlIssueSeverity
{
    Error,
    Warning
}

public enum ControlIssueTarget
{
    Invoice,
    School,
    ActiveList
}

public sealed record ControlIssue(
    string Code,
    ControlIssueSeverity Severity,
    string Category,
    string Title,
    string Detail,
    ControlIssueTarget Target,
    string InstitutionName,
    string InvoiceType,
    string InvoiceNumber,
    Guid? InvoiceId = null,
    string? SchoolInstitutionType = null,
    string? SchoolName = null)
{
    public string SeverityLabel => DomainValueText.ToDisplayValue(Severity);

    public int SeverityRank => Severity == ControlIssueSeverity.Error ? 0 : 1;

    public string TargetLabel => Target switch
    {
        ControlIssueTarget.Invoice => "Fatura",
        ControlIssueTarget.School => "Okul / Kurum",
        _ => "Aktif Liste"
    };

    public string Reference => Target switch
    {
        ControlIssueTarget.Invoice when !string.IsNullOrWhiteSpace(InvoiceNumber) => InvoiceNumber,
        ControlIssueTarget.Invoice => "Fatura no yok",
        ControlIssueTarget.School when !string.IsNullOrWhiteSpace(SchoolName) => SchoolName,
        _ => "Aktif Fatura Listesi"
    };

    public string ActionLabel => Target switch
    {
        ControlIssueTarget.Invoice => "FATURAYA GİT",
        ControlIssueTarget.School => "KURUMA GİT",
        _ => "LİSTEYE GİT"
    };
}

public sealed record ControlScanResult(IReadOnlyList<ControlIssue> Issues)
{
    public int ErrorCount => Issues.Count(issue => issue.Severity == ControlIssueSeverity.Error);
    public int WarningCount => Issues.Count(issue => issue.Severity == ControlIssueSeverity.Warning);
    public bool IsClean => Issues.Count == 0;
}
