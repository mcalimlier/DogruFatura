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
    decimal InterruptionCompensationAmount = 0m);
