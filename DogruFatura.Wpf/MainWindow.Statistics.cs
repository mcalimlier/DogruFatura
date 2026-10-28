using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System.IO;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DogruFatura.Wpf;

public partial class MainWindow
{
    private bool _suppressStatisticsEvents;
    private ArchiveStatisticsFilter _statisticsFilter = new();
    private ArchiveStatisticsResult? _statisticsResult;

    private void RefreshStatistics()
    {
        EnsureStatisticsTemplates();

        var currentYear = StatisticsYearFilterBox.SelectedItem as string ?? "Tümü";
        var currentInstitution = StatisticsInstitutionFilterBox.SelectedItem as string ?? "Tümü";
        var currentType = StatisticsTypeFilterBox.SelectedItem as string ?? "Tümü";

        var options = ArchiveStatisticsService.Build(ArchivedLists, schools: AllSchools);

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
        _statisticsFilter = ReadStatisticsFilter();
        var result = ArchiveStatisticsService.Build(ArchivedLists, _statisticsFilter, AllSchools);
        _statisticsResult = result;

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
    }

    private ArchiveStatisticsFilter ReadStatisticsFilter()
    {
        var yearText = StatisticsYearFilterBox.SelectedItem as string;
        int? year = int.TryParse(
            yearText,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var parsedYear)
            ? parsedYear
            : null;

        var institution = StatisticsInstitutionFilterBox.SelectedItem as string;
        if (string.Equals(institution, "Tümü", StringComparison.OrdinalIgnoreCase))
            institution = string.Empty;

        var invoiceType = StatisticsTypeFilterBox.SelectedItem as string;
        if (string.Equals(invoiceType, "Tümü", StringComparison.OrdinalIgnoreCase))
            invoiceType = string.Empty;

        return new ArchiveStatisticsFilter(
            year,
            institution ?? string.Empty,
            invoiceType ?? string.Empty,
            StatisticsDateFromPicker.SelectedDate?.Date,
            StatisticsDateToPicker.SelectedDate?.Date);
    }



    private void RefreshStatisticsDonut(ArchiveStatisticsResult result)
    {
        StatisticsDonutLegendPanel.Children.Clear();
        StatisticsDonutLabelsCanvas.Children.Clear();
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
            var percentageText = DonutPercentageLayout.FormatPercent(percentage * 100m);

            series.Add(new PieSeries<double>
            {
                Name = item.InvoiceType,
                Values = new[] { (double)item.PaymentTotal },
                InnerRadius = 82,
                OuterRadiusOffset = 0,
                HoverPushout = 0,
                Fill = new SolidColorPaint(skColor),
                Stroke = new SolidColorPaint(SKColors.White)
                {
                    StrokeThickness = 2.4f,
                    IsAntialias = true
                },
                ShowDataLabels = false,
                ToolTipLabelFormatter = _ =>
                    $"{item.InvoiceType}: {NumberParser.Money(item.PaymentTotal)} ({percentageText})"
            });

            AddStatisticsLegendItem(
                item,
                percentage * 100m,
                wpfBrush);
        }

        StatisticsDonutChart.Series = series;
        RefreshStatisticsDonutLabels(distribution, total);
        InformationCopyService.Apply(this);
    }

    private void RefreshStatisticsDonutLabels(IReadOnlyList<StatisticsDonutItem> distribution, decimal total)
    {
        StatisticsDonutLabelsCanvas.Children.Clear();
        if (total <= 0) return;
        foreach (var label in DonutPercentageLayout.Plan(distribution.Select(item => item.PaymentTotal).ToArray()))
        {
            var sliceColor = StatisticsSkColor(distribution[label.Index].InvoiceType);
            var sliceBrush = StatisticsBrush(distribution[label.Index].InvoiceType);
            if (label.IsCallout)
            {
                var edgeX = label.Left < DonutPercentageLayout.CanvasWidth / 2 ? label.Left + DonutPercentageLayout.LabelWidth : label.Left;
                var middleX = edgeX + (label.Left < DonutPercentageLayout.CanvasWidth / 2 ? 2 : -2);
                var radialX = DonutPercentageLayout.CanvasWidth / 2 + (label.AnchorX - DonutPercentageLayout.CanvasWidth / 2) * 172 / 157;
                var radialY = DonutPercentageLayout.CanvasHeight / 2 + (label.AnchorY - DonutPercentageLayout.CanvasHeight / 2) * 172 / 157;
                var leader = new System.Windows.Shapes.Polyline { Stroke = sliceBrush, StrokeThickness = 1.8,
                    StrokeLineJoin = PenLineJoin.Round, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round,
                    Points = new PointCollection { new(label.AnchorX,label.AnchorY), new(radialX,radialY), new(middleX,radialY), new(middleX,label.Top+12), new(edgeX,label.Top+12) }, IsHitTestVisible = false };
                StatisticsDonutLabelsCanvas.Children.Add(leader);
                var dot = new System.Windows.Shapes.Ellipse { Width = 6, Height = 6, Fill = sliceBrush, Stroke = Brushes.White, StrokeThickness = .7, IsHitTestVisible = false };
                Canvas.SetLeft(dot, label.AnchorX - 3); Canvas.SetTop(dot, label.AnchorY - 3); StatisticsDonutLabelsCanvas.Children.Add(dot);
            }
            var text = new TextBlock { Text = label.Text, FontFamily = new FontFamily("Segoe UI"), FontSize = 12,
                FontWeight = FontWeights.SemiBold, TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                Foreground = StatisticsDataLabelBrush(sliceColor) };
            var badge = new Border { Width = DonutPercentageLayout.LabelWidth, Height = DonutPercentageLayout.LabelHeight, Child = text,
                CornerRadius = new CornerRadius(5), Background = label.IsCallout ? sliceBrush : Brushes.Transparent,
                BorderBrush = label.IsCallout ? sliceBrush : Brushes.Transparent, BorderThickness = new Thickness(label.IsCallout ? .7 : 0) };
            Canvas.SetLeft(badge,label.Left); Canvas.SetTop(badge,label.Top); StatisticsDonutLabelsCanvas.Children.Add(badge);
        }
    }

    private static SolidColorBrush StatisticsDataLabelBrush(SKColor background)
    {
        var color = StatisticsDataLabelColor(background);
        return new SolidColorBrush(Color.FromRgb(color.Red, color.Green, color.Blue));
    }

    private void AddStatisticsLegendItem(
        StatisticsDonutItem item,
        decimal percentage,
        SolidColorBrush brush)
    {
        var row = new Grid
        {
            Margin = new Thickness(0, 0, 0, 16)
        };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(22) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var marker = new Border
        {
            Width = 15,
            Height = 15,
            CornerRadius = new CornerRadius(4),
            Background = brush,
            VerticalAlignment = VerticalAlignment.Center
        };

        var label = new StackPanel
        {
            Margin = new Thickness(10, 0, 12, 0)
        };
        label.Children.Add(new TextBlock
        {
            Text = item.InvoiceType,
            Foreground = new SolidColorBrush(Color.FromRgb(239, 244, 250)),
            FontSize = 14,
            FontWeight = FontWeights.SemiBold
        });
        label.Children.Add(new TextBlock
        {
            Text = $"{item.InvoiceCount} fatura",
            Foreground = new SolidColorBrush(Color.FromRgb(185, 203, 218)),
            FontSize = 13.5
        });

        var value = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Right
        };
        value.Children.Add(new TextBlock
        {
            Text = NumberParser.Money(item.PaymentTotal),
            Foreground = new SolidColorBrush(Color.FromRgb(239, 244, 250)),
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Right
        });
        value.Children.Add(new TextBlock
        {
            Text = DonutPercentageLayout.FormatPercent(percentage),
            Foreground = new SolidColorBrush(Color.FromRgb(232, 199, 120)),
            FontSize = 12.5,
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

    private static SKColor StatisticsDataLabelColor(SKColor background)
    {
        var luminance =
            (0.299d * background.Red)
            + (0.587d * background.Green)
            + (0.114d * background.Blue);

        return luminance >= 150d
            ? new SKColor(3, 28, 56)
            : SKColors.White;
    }

    private sealed record StatisticsDonutItem(
        string InvoiceType,
        int InvoiceCount,
        decimal PaymentTotal);

    private void EnsureStatisticsTemplates()
    {
        if (StatisticsTemplateBox.Items.Count > 0)
            return;

        StatisticsTemplateBox.ItemsSource = new[]
        {
            "Seçiniz",
            "Yıllık Genel Rapor",
            "Kurum Bazlı Rapor",
            "Elektrik Giderleri",
            "Tüm Fatura Türleri"
        };
        StatisticsTemplateBox.SelectedIndex = 0;
    }

    private void ApplyStatisticsTemplate_Click(object sender, RoutedEventArgs e)
    {
        var template = StatisticsTemplateBox.SelectedItem as string ?? "Seçiniz";
        _suppressStatisticsEvents = true;
        try
        {
            switch (template)
            {
                case "Yıllık Genel Rapor":
                    var availableYears = ArchiveStatisticsService.Build(ArchivedLists, schools: AllSchools).AvailableYears;
                    var latestYear = availableYears.Count > 0 ? availableYears[0] : 0;
                    StatisticsYearFilterBox.SelectedItem = latestYear > 0
                        ? latestYear.ToString(CultureInfo.InvariantCulture)
                        : "Tümü";
                    StatisticsInstitutionFilterBox.SelectedItem = "Tümü";
                    StatisticsTypeFilterBox.SelectedItem = "Tümü";
                    StatisticsDateFromPicker.SelectedDate = null;
                    StatisticsDateToPicker.SelectedDate = null;
                    break;

                case "Kurum Bazlı Rapor":
                    if (string.Equals(
                        StatisticsInstitutionFilterBox.SelectedItem as string,
                        "Tümü",
                        StringComparison.OrdinalIgnoreCase))
                    {
                        MessageBox.Show(
                            this,
                            "Kurum bazlı rapor için önce Kurum filtresinden bir kurum seçin.",
                            "DoğruFatura - İstatistik",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    StatisticsTypeFilterBox.SelectedItem = "Tümü";
                    StatisticsDateFromPicker.SelectedDate = null;
                    StatisticsDateToPicker.SelectedDate = null;
                    break;

                case "Elektrik Giderleri":
                    StatisticsTypeFilterBox.SelectedItem = "Elektrik";
                    break;

                case "Tüm Fatura Türleri":
                    StatisticsTypeFilterBox.SelectedItem = "Tümü";
                    break;
            }
        }
        finally
        {
            _suppressStatisticsEvents = false;
        }

        ApplyStatisticsFilters();
    }

    private void StatisticsDateFilter_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (_suppressStatisticsEvents)
            return;

        ApplyStatisticsFilters();
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
                StringComparison.OrdinalIgnoreCase));

        comboBox.SelectedItem = selected ?? "Tümü";
    }

    private void StatisticsFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressStatisticsEvents)
            return;

        ApplyStatisticsFilters();
    }

    private void RefreshStatistics_Click(object sender, RoutedEventArgs e) =>
        RefreshStatistics();
}
