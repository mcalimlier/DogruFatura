using DogruFatura.Core.Models;

namespace DogruFatura.Wpf;

public sealed record ActiveInvoiceRow(int SequenceNumber, InvoiceRecord Invoice);
