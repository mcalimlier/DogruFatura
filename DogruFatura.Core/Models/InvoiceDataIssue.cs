namespace DogruFatura.Core.Models;

public enum InvoiceIssueSeverity
{
    Error,
    Warning
}

public enum InvoiceIssueTarget
{
    Invoice,
    School,
    ActiveList
}

public sealed record InvoiceDataIssue(
    string Code,
    InvoiceIssueSeverity Severity,
    string Category,
    string Title,
    string Detail,
    InvoiceIssueTarget Target,
    string InstitutionName,
    string InvoiceType,
    string InvoiceNumber,
    Guid? InvoiceId = null,
    string? SchoolInstitutionType = null,
    string? SchoolName = null)
{
    public string SeverityLabel => DomainValueText.ToDisplayValue(Severity);

    public int SeverityRank => Severity == InvoiceIssueSeverity.Error ? 0 : 1;

    public string TargetLabel => Target switch
    {
        InvoiceIssueTarget.Invoice => "Fatura",
        InvoiceIssueTarget.School => "Okul / Kurum",
        _ => "Aktif Liste"
    };

    public string Reference => Target switch
    {
        InvoiceIssueTarget.Invoice when !string.IsNullOrWhiteSpace(InvoiceNumber) => InvoiceNumber,
        InvoiceIssueTarget.Invoice => "Fatura no yok",
        InvoiceIssueTarget.School when !string.IsNullOrWhiteSpace(SchoolName) => SchoolName,
        _ => "Aktif Fatura Listesi"
    };


}

public sealed record InvoiceConsistencyResult(IReadOnlyList<InvoiceDataIssue> Issues)
{
    public int ErrorCount => Issues.Count(issue => issue.Severity == InvoiceIssueSeverity.Error);
    public int WarningCount => Issues.Count(issue => issue.Severity == InvoiceIssueSeverity.Warning);
    public bool IsClean => Issues.Count == 0;
}
