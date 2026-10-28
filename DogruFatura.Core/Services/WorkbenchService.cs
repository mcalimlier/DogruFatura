using DogruFatura.Core.Models;
using System.Globalization;

namespace DogruFatura.Core.Services;

public sealed record InvoiceWorkbenchFilter(string Query = "", DateTime? DueFrom = null, DateTime? DueTo = null,
    decimal? MinimumAmount = null, decimal? MaximumAmount = null, bool DueSoonOnly = false);

public sealed record WorkbenchSummary(int ActiveCount, int DueSoonCount, int OverdueCount, int PendingHysCount,
    int IssueCount, decimal ActivePaymentTotal, string NextAction);

public static class WorkbenchService
{
    public static WorkbenchSummary Summarize(IEnumerable<InvoiceRecord> invoices, IEnumerable<ArchivedInvoiceList> archives, DateTime today,
        int issueCount = 0)
    {
        var active = invoices.ToArray();
        var pending = archives.Count(a => !a.IsClosed);
        var next = active.Length == 0
            ? pending > 0 ? "HYS kapanışı bekleyen listeleri tamamlayın veya yeni fatura ekleyin." : "İlk faturayı ekleyerek başlayın."
            : issueCount > 0 ? "Kontrol bulgularını inceleyin; listeyi çıktı önizlemesinde kontrol edin." : "Listeyi kontrol edin ve çıktı önizlemesine geçin.";
        return new(active.Length, active.Count(i => i.DueDate.Date >= today.Date && i.DueDate.Date <= today.Date.AddDays(7)),
            active.Count(i => i.DueDate.Date < today.Date), pending, issueCount, active.Sum(i => i.PaymentAmount), next);
    }

    public static bool Matches(InvoiceRecord invoice, InvoiceWorkbenchFilter filter, DateTime? today = null)
    {
        if (filter.DueFrom.HasValue && invoice.DueDate.Date < filter.DueFrom.Value.Date) return false;
        if (filter.DueTo.HasValue && invoice.DueDate.Date > filter.DueTo.Value.Date) return false;
        if (filter.MinimumAmount.HasValue && invoice.PayableAmount < filter.MinimumAmount.Value) return false;
        if (filter.MaximumAmount.HasValue && invoice.PayableAmount > filter.MaximumAmount.Value) return false;
        if (filter.DueSoonOnly && invoice.DueDate.Date > (today ?? DateTime.Today).Date.AddDays(7)) return false;
        var text = $"{invoice.InstitutionName} {invoice.InvoiceType} {invoice.InvoiceNumber} {invoice.ServiceNumber} {invoice.Vkn}";
        return string.IsNullOrWhiteSpace(filter.Query) || CultureInfo.GetCultureInfo("tr-TR").CompareInfo.IndexOf(
            text, filter.Query.Trim(), CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;
    }
}
