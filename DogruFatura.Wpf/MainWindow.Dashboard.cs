using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;

namespace DogruFatura.Wpf;

public partial class MainWindow
{    private void RefreshDashboard()
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

    private static SolidColorBrush DashboardInvoiceTypeBrush(string invoiceType) => invoiceType switch
    {
        "Elektrik" => new SolidColorBrush(Color.FromRgb(255, 180, 31)),
        "Telefon" => new SolidColorBrush(Color.FromRgb(36, 104, 216)),
        "İnternet" => new SolidColorBrush(Color.FromRgb(42, 169, 190)),
        "Su" => new SolidColorBrush(Color.FromRgb(67, 199, 122)),
        _ => new SolidColorBrush(Color.FromRgb(145, 171, 192))
    };
}
