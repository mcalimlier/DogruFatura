namespace DogruFatura.Core.Models;

public sealed record ArchiveStatisticsFilter(
    int? Year = null,
    string InstitutionName = "",
    string InvoiceType = "",
    DateTime? StartDate = null,
    DateTime? EndDate = null);

public sealed record ArchiveStatisticsRow(
    string InstitutionName,
    int Year,
    string InvoiceType,
    int InvoiceCount,
    decimal PayableTotal,
    decimal VatTotal,
    decimal StampDutyTotal,
    decimal PaymentTotal);

public sealed record ArchiveStatisticsMonthlyRow(
    int Year,
    int Month,
    int InvoiceCount,
    decimal PayableTotal,
    decimal PaymentTotal);

public sealed record ArchiveStatisticsResult(
    int ArchiveCount,
    int InvoiceCount,
    int InstitutionCount,
    decimal PayableTotal,
    decimal VatTotal,
    decimal StampDutyTotal,
    decimal PaymentTotal,
    decimal AveragePayableAmount,
    decimal AveragePaymentAmount,
    IReadOnlyList<int> AvailableYears,
    IReadOnlyList<string> AvailableInstitutions,
    IReadOnlyList<string> AvailableInvoiceTypes,
    IReadOnlyList<ArchiveStatisticsRow> Rows,
    IReadOnlyList<ArchiveStatisticsMonthlyRow> MonthlyRows);
