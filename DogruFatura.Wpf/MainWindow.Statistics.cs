using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using System.Windows;
using System.Windows.Controls;

namespace DogruFatura.Wpf;

public partial class MainWindow
{
    private bool _suppressStatisticsEvents;

    private void RefreshStatistics()
    {
        var currentYear = StatisticsYearFilterBox.SelectedItem as string ?? "Tümü";
        var currentInstitution = StatisticsInstitutionFilterBox.SelectedItem as string ?? "Tümü";
        var currentType = StatisticsTypeFilterBox.SelectedItem as string ?? "Tümü";

        var options = ArchiveStatisticsService.Build(ArchivedLists);

        _suppressStatisticsEvents = true;
        try
        {
            ReplaceStatisticsItems(
                StatisticsYearFilterBox,
                options.AvailableYears.Select(year => year.ToString()),
                currentYear);
            ReplaceStatisticsItems(
                StatisticsInstitutionFilterBox,
                options.AvailableInstitutions,
                currentInstitution);
            ReplaceStatisticsItems(
                StatisticsTypeFilterBox,
                options.AvailableInvoiceTypes,
                currentType);
        }
        finally
        {
            _suppressStatisticsEvents = false;
        }

        ApplyStatisticsFilters();
    }

    private void ApplyStatisticsFilters()
    {
        var yearText = StatisticsYearFilterBox.SelectedItem as string;
        int? year = int.TryParse(yearText, out var parsedYear) ? parsedYear : null;

        var institution = StatisticsInstitutionFilterBox.SelectedItem as string;
        if (string.Equals(institution, "Tümü", StringComparison.CurrentCultureIgnoreCase))
            institution = string.Empty;

        var invoiceType = StatisticsTypeFilterBox.SelectedItem as string;
        if (string.Equals(invoiceType, "Tümü", StringComparison.CurrentCultureIgnoreCase))
            invoiceType = string.Empty;

        var result = ArchiveStatisticsService.Build(
            ArchivedLists,
            new ArchiveStatisticsFilter(
                year,
                institution ?? string.Empty,
                invoiceType ?? string.Empty));

        StatisticsArchiveCountText.Text = $"{result.ArchiveCount} arşiv";
        StatisticsInvoiceCountText.Text = $"{result.InvoiceCount} fatura";
        StatisticsInstitutionCountText.Text = $"{result.InstitutionCount} kurum";
        StatisticsPaymentTotalText.Text = NumberParser.Money(result.PaymentTotal);
        StatisticsTotalsText.Text =
            $"Fatura toplamı: {NumberParser.Money(result.PayableTotal)}"
            + $" • KDV: {NumberParser.Money(result.VatTotal)}"
            + $" • Damga: {NumberParser.Money(result.StampDutyTotal)}"
            + $" • Ödeme: {NumberParser.Money(result.PaymentTotal)}";

        StatisticsGrid.ItemsSource = result.Rows;
        StatisticsEmptyState.Visibility =
            result.Rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static void ReplaceStatisticsItems(
        ComboBox comboBox,
        IEnumerable<string> values,
        string preferredSelection)
    {
        comboBox.Items.Clear();
        comboBox.Items.Add("Tümü");
        foreach (var value in values)
            comboBox.Items.Add(value);

        var selected = comboBox.Items
            .Cast<object>()
            .Select(item => item?.ToString() ?? string.Empty)
            .FirstOrDefault(item => string.Equals(
                item,
                preferredSelection,
                StringComparison.CurrentCultureIgnoreCase));

        comboBox.SelectedItem = selected ?? "Tümü";
    }

    private void StatisticsFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressStatisticsEvents)
            return;

        ApplyStatisticsFilters();
    }

    private void ClearStatisticsFilters_Click(object sender, RoutedEventArgs e)
    {
        _suppressStatisticsEvents = true;
        try
        {
            StatisticsYearFilterBox.SelectedIndex = 0;
            StatisticsInstitutionFilterBox.SelectedIndex = 0;
            StatisticsTypeFilterBox.SelectedIndex = 0;
        }
        finally
        {
            _suppressStatisticsEvents = false;
        }

        ApplyStatisticsFilters();
    }

    private void RefreshStatistics_Click(object sender, RoutedEventArgs e) =>
        RefreshStatistics();
}
