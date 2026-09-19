namespace DogruFatura.Core.Models;

public sealed record ArchiveStatisticsFilter(
    int? Year = null,
    string InstitutionName = "",
    string InvoiceType = "");

public sealed record ArchiveStatisticsRow(
    string InstitutionName,
    int Year,
    string InvoiceType,
    int InvoiceCount,
    decimal PayableTotal,
    decimal VatTotal,
    decimal StampDutyTotal,
    decimal PaymentTotal);

public sealed record ArchiveStatisticsResult(
    int ArchiveCount,
    int InvoiceCount,
    int InstitutionCount,
    decimal PayableTotal,
    decimal VatTotal,
    decimal StampDutyTotal,
    decimal PaymentTotal,
    IReadOnlyList<int> AvailableYears,
    IReadOnlyList<string> AvailableInstitutions,
    IReadOnlyList<string> AvailableInvoiceTypes,
    IReadOnlyList<ArchiveStatisticsRow> Rows);
