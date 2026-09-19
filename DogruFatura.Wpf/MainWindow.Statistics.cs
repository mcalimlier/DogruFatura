using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
                options.AvailableYears.Select(year => year.ToString(CultureInfo.InvariantCulture)),
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
        int? year = int.TryParse(yearText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedYear) ? parsedYear : null;

        var institution = StatisticsInstitutionFilterBox.SelectedItem as string;
        if (string.Equals(institution, "Tümü", StringComparison.OrdinalIgnoreCase))
            institution = string.Empty;

        var invoiceType = StatisticsTypeFilterBox.SelectedItem as string;
        if (string.Equals(invoiceType, "Tümü", StringComparison.OrdinalIgnoreCase))
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

        RefreshStatisticsDonut(result);

        StatisticsGrid.ItemsSource = result.Rows;
        StatisticsEmptyState.Visibility =
            result.Rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RefreshStatisticsDonut(ArchiveStatisticsResult result)
    {
        StatisticsDonutLegendPanel.Children.Clear();
        StatisticsDonutTotalText.Text = NumberParser.Money(result.PaymentTotal);

        var distribution = result.Rows
            .GroupBy(row => row.InvoiceType, StringComparer.OrdinalIgnoreCase)
            .Select(group => new StatisticsDonutItem(
                group.Key,
                group.Sum(row => row.InvoiceCount),
                group.Sum(row => row.PaymentTotal)))
            .Where(item => item.PaymentTotal > 0m)
            .OrderBy(item => StatisticsInvoiceTypeOrder(item.InvoiceType))
            .ThenBy(item => item.InvoiceType, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var total = distribution.Sum(item => item.PaymentTotal);
        if (distribution.Length == 0 || total <= 0m)
        {
            StatisticsDonutChart.Series = Array.Empty<ISeries>();
            StatisticsDonutEmptyState.Visibility = Visibility.Visible;
            return;
        }

        StatisticsDonutEmptyState.Visibility = Visibility.Collapsed;

        var series = new List<ISeries>(distribution.Length);
        foreach (var item in distribution)
        {
            var percentage = item.PaymentTotal / total;
            var skColor = StatisticsSkColor(item.InvoiceType);
            var wpfBrush = StatisticsBrush(item.InvoiceType);
            var percentageText = "%" + (percentage * 100m).ToString(
                "N0",
                CultureInfo.GetCultureInfo("tr-TR"));

            series.Add(new PieSeries<double>
            {
                Name = item.InvoiceType,
                Values = new[] { (double)item.PaymentTotal },
                InnerRadius = 92,
                HoverPushout = 8,
                Fill = new SolidColorPaint(skColor),
                Stroke = new SolidColorPaint(SKColors.White) { StrokeThickness = 2 },
                DataLabelsPaint = new SolidColorPaint(SKColors.White),
                DataLabelsSize = 16,
                DataLabelsPosition = PolarLabelsPosition.Middle,
                DataLabelsFormatter = _ => percentage >= 0.06m ? percentageText : string.Empty,
                TooltipLabelFormatter = _ =>
                    $"{item.InvoiceType}: {NumberParser.Money(item.PaymentTotal)} ({percentageText})"
            });

            AddStatisticsLegendItem(
                item,
                percentage * 100m,
                wpfBrush);
        }

        StatisticsDonutChart.Series = series;
    }

    private void AddStatisticsLegendItem(
        StatisticsDonutItem item,
        decimal percentage,
        SolidColorBrush brush)
    {
        var row = new Grid
        {
            Margin = new Thickness(0, 0, 0, 12)
        };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var marker = new Border
        {
            Width = 13,
            Height = 13,
            CornerRadius = new CornerRadius(3),
            Background = brush,
            VerticalAlignment = VerticalAlignment.Center
        };

        var label = new StackPanel
        {
            Margin = new Thickness(9, 0, 10, 0)
        };
        label.Children.Add(new TextBlock
        {
            Text = item.InvoiceType,
            Foreground = new SolidColorBrush(Color.FromRgb(3, 28, 56)),
            FontSize = 15,
            FontWeight = FontWeights.SemiBold
        });
        label.Children.Add(new TextBlock
        {
            Text = $"{item.InvoiceCount} fatura",
            Foreground = new SolidColorBrush(Color.FromRgb(100, 119, 139)),
            FontSize = 13
        });

        var value = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Right
        };
        value.Children.Add(new TextBlock
        {
            Text = NumberParser.Money(item.PaymentTotal),
            Foreground = new SolidColorBrush(Color.FromRgb(3, 28, 56)),
            FontSize = 15,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Right
        });
        value.Children.Add(new TextBlock
        {
            Text = "%" + percentage.ToString("N1", CultureInfo.GetCultureInfo("tr-TR")),
            Foreground = new SolidColorBrush(Color.FromRgb(84, 112, 141)),
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Right
        });

        Grid.SetColumn(label, 1);
        Grid.SetColumn(value, 2);
        row.Children.Add(marker);
        row.Children.Add(label);
        row.Children.Add(value);
        StatisticsDonutLegendPanel.Children.Add(row);
    }

    private static int StatisticsInvoiceTypeOrder(string invoiceType) => invoiceType switch
    {
        "Elektrik" => 0,
        "Su" => 1,
        "Telefon" => 2,
        "İnternet" => 3,
        _ => 4
    };

    private static SolidColorBrush StatisticsBrush(string invoiceType) => invoiceType switch
    {
        "Elektrik" => Brushes.Goldenrod,
        "Su" => Brushes.SteelBlue,
        "Telefon" => Brushes.MediumPurple,
        "İnternet" => Brushes.MediumSeaGreen,
        _ => Brushes.SlateGray
    };

    private static SKColor StatisticsSkColor(string invoiceType) => invoiceType switch
    {
        "Elektrik" => new SKColor(218, 165, 32),
        "Su" => new SKColor(70, 130, 180),
        "Telefon" => new SKColor(147, 112, 219),
        "İnternet" => new SKColor(60, 179, 113),
        _ => new SKColor(112, 128, 144)
    };

    private sealed record StatisticsDonutItem(
        string InvoiceType,
        int InvoiceCount,
        decimal PaymentTotal);

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
                StringComparison.OrdinalIgnoreCase));

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
