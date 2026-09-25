using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace DogruFatura.Wpf;

public partial class MainWindow
{
    private void RefreshDashboard()
    {
        if (HomeMonthLabelText is null) return;

        var today = DateTime.Today;
        var tr = CultureInfo.GetCultureInfo("tr-TR");
        var statistics = DashboardStatisticsService.Build(ArchivedLists, today);
        var closedArchiveCount = ArchivedLists.Count(archive => archive.IsStatisticsEligible);
        var monthName = statistics.PeriodStart.ToString("MMMM yyyy", tr).ToUpper(tr);

        HomeMonthLabelText.Text = monthName;
        HomeMonthPaymentText.Text = NumberParser.Money(statistics.MonthlyPaymentTotal);
        HomeMonthlyPeriodText.Text = $"{monthName} MALİ ÖZETİ";
        HomeMonthlyInvoiceCountText.Text = $"{statistics.MonthlyInvoiceCount} kayıt";
        HomeMonthlyPayableText.Text = NumberParser.Money(statistics.MonthlyPayableTotal);
        HomeMonthlyStampText.Text = NumberParser.Money(statistics.MonthlyStampDutyTotal);
        HomeMonthlyPaymentText.Text = NumberParser.Money(statistics.MonthlyPaymentTotal);
        HomeTotalInvoiceSummaryText.Text = $"{statistics.TotalInvoiceCount} toplam fatura";
        HomeTotalInstitutionSummaryText.Text = $"{statistics.TotalInstitutionCount} farklı kurum";
        HomeArchiveCountText.Text = $"{closedArchiveCount} / {ArchivedLists.Count} KAPALI";

        HomeInvoiceTypeBars.Clear();
        var maxTypePayment = statistics.InvoiceTypeDistribution
            .Select(item => item.PaymentTotal)
            .DefaultIfEmpty(0m)
            .Max();
        foreach (var item in statistics.InvoiceTypeDistribution)
        {
            HomeInvoiceTypeBars.Add(new DashboardBarItem(
                item.Label,
                $"{item.InvoiceCount} fatura",
                NumberParser.Money(item.PaymentTotal),
                CalculateDashboardBarWidth(item.PaymentTotal, maxTypePayment, 430),
                DashboardInvoiceTypeBrush(item.Label)));
        }
        HomeInvoiceTypeEmptyText.Visibility = HomeInvoiceTypeBars.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        HomeMonthlyTrendBars.Clear();
        var maxMonthlyPayment = statistics.MonthlyTrend
            .Select(item => item.PaymentTotal)
            .DefaultIfEmpty(0m)
            .Max();
        foreach (var item in statistics.MonthlyTrend)
        {
            var isCurrentMonth = item.Month == statistics.PeriodStart;
            HomeMonthlyTrendBars.Add(new DashboardBarItem(
                item.Month.ToString("MMM yy", tr).ToUpper(tr),
                $"{item.InvoiceCount} fatura",
                NumberParser.Money(item.PaymentTotal),
                CalculateDashboardBarWidth(item.PaymentTotal, maxMonthlyPayment, 245),
                new SolidColorBrush(isCurrentMonth
                    ? Color.FromRgb(255, 180, 31)
                    : Color.FromRgb(36, 104, 216))));
        }

    }

    private static double CalculateDashboardBarWidth(decimal value, decimal maximum, double availableWidth)
    {
        if (value <= 0m || maximum <= 0m) return 0d;
        return Math.Max(8d, (double)(value / maximum) * availableWidth);
    }

    private static SolidColorBrush DashboardInvoiceTypeBrush(string invoiceType) =>
        DomainValueText.ParseInvoiceKind(invoiceType) switch
        {
            InvoiceKind.Electricity => new SolidColorBrush(Color.FromRgb(255, 180, 31)),
            InvoiceKind.Telephone => new SolidColorBrush(Color.FromRgb(36, 104, 216)),
            InvoiceKind.Internet => new SolidColorBrush(Color.FromRgb(42, 169, 190)),
            InvoiceKind.Water => new SolidColorBrush(Color.FromRgb(67, 199, 122)),
            _ => new SolidColorBrush(Color.FromRgb(145, 171, 192))
        };
}
