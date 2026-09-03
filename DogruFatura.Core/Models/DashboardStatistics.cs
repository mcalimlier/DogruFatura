namespace DogruFatura.Core.Models;

public sealed record DashboardDistributionItem(
    string Label,
    int InvoiceCount,
    decimal PaymentTotal);

public sealed record DashboardMonthlyItem(
    DateTime Month,
    int InvoiceCount,
    decimal PaymentTotal);

public sealed record DashboardStatistics(
    DateTime PeriodStart,
    int MonthlyInvoiceCount,
    decimal MonthlyPayableTotal,
    decimal MonthlyStampDutyTotal,
    decimal MonthlyPaymentTotal,
    int TotalInvoiceCount,
    int TotalInstitutionCount,
    IReadOnlyList<DashboardDistributionItem> InvoiceTypeDistribution,
    IReadOnlyList<DashboardMonthlyItem> MonthlyTrend);
