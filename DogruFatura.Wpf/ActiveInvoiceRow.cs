using DogruFatura.Core.Models;

namespace DogruFatura.Wpf;

public sealed record ActiveInvoiceRow(int SequenceNumber, InvoiceRecord Invoice)
{
    public string DueStatus => Invoice.DueDate.Date < DateTime.Today ? "⚠ Vadesi geçmiş"
        : Invoice.DueDate.Date == DateTime.Today ? "● Bugün"
        : Invoice.DueDate.Date <= DateTime.Today.AddDays(7) ? "● 7 gün içinde" : "Planlı";
}
