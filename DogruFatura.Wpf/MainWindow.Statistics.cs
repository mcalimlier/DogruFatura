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
        _statisticsFilter = ReadStatisticsFilter();
        var result = ArchiveStatisticsService.Build(ArchivedLists, _statisticsFilter);
        _statisticsResult = result;

        StatisticsArchiveCountText.Text = $"{result.ArchiveCount} arşiv";
        StatisticsInvoiceCountText.Text = $"{result.InvoiceCount} fatura";
        StatisticsInstitutionCountText.Text = $"{result.InstitutionCount} kurum";
        StatisticsPaymentTotalText.Text = NumberParser.Money(result.PaymentTotal);
        StatisticsAverageText.Text = NumberParser.Money(result.AveragePayableAmount);
        StatisticsTotalsText.Text =
            $"Fatura toplamı: {NumberParser.Money(result.PayableTotal)}"
            + $" • KDV: {NumberParser.Money(result.VatTotal)}"
            + $" • Damga: {NumberParser.Money(result.StampDutyTotal)}"
            + $" • Ödeme: {NumberParser.Money(result.PaymentTotal)}";

        RefreshStatisticsPreviousPeriod(result);
        RefreshStatisticsPdfHealth();
        RefreshStatisticsInstitutionProfile(result);
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

    private void RefreshStatisticsPreviousPeriod(ArchiveStatisticsResult current)
    {
        var previousFilter = PreviousStatisticsFilter(_statisticsFilter);
        if (previousFilter is null)
        {
            StatisticsPreviousPeriodText.Text = "Karşılaştırma yok";
            StatisticsPreviousPeriodText.Foreground =
                new SolidColorBrush(Color.FromRgb(49, 83, 113));
            return;
        }

        var previous = ArchiveStatisticsService.Build(ArchivedLists, previousFilter);
        if (previous.PaymentTotal <= 0m)
        {
            StatisticsPreviousPeriodText.Text = "Önceki dönemde veri yok";
            StatisticsPreviousPeriodText.Foreground =
                new SolidColorBrush(Color.FromRgb(49, 83, 113));
            return;
        }

        var difference = current.PaymentTotal - previous.PaymentTotal;
        var percent = difference / previous.PaymentTotal * 100m;
        var direction = difference > 0m ? "↑" : difference < 0m ? "↓" : "→";
        StatisticsPreviousPeriodText.Text =
            $"{direction} %{Math.Abs(percent).ToString("N1", CultureInfo.GetCultureInfo("tr-TR"))}"
            + $" • {NumberParser.Money(previous.PaymentTotal)}";

        StatisticsPreviousPeriodText.Foreground = difference switch
        {
            > 0m => new SolidColorBrush(Color.FromRgb(183, 121, 31)),
            < 0m => new SolidColorBrush(Color.FromRgb(36, 138, 80)),
            _ => new SolidColorBrush(Color.FromRgb(49, 83, 113))
        };
    }

    private static ArchiveStatisticsFilter? PreviousStatisticsFilter(ArchiveStatisticsFilter current)
    {
        if (current.StartDate.HasValue && current.EndDate.HasValue)
        {
            var start = current.StartDate.Value.Date;
            var end = current.EndDate.Value.Date;
            if (end < start)
                return null;

            var dayCount = (end - start).Days + 1;
            var previousEnd = start.AddDays(-1);
            var previousStart = previousEnd.AddDays(-(dayCount - 1));
            return current with
            {
                Year = null,
                StartDate = previousStart,
                EndDate = previousEnd
            };
        }

        if (current.Year.HasValue)
            return current with { Year = current.Year.Value - 1 };

        return null;
    }

    private void RefreshStatisticsPdfHealth()
    {
        var eligible = ArchivedLists
            .Where(archive => archive.IsStatisticsEligible)
            .ToArray();

        var missing = eligible.Count(archive =>
        {
            var fileName = !string.IsNullOrWhiteSpace(archive.PdfFileName)
                ? Path.GetFileName(archive.PdfFileName)
                : !string.IsNullOrWhiteSpace(archive.HysPaymentOrderDocumentNumber)
                    ? archive.HysPaymentOrderDocumentNumber + ".pdf"
                    : string.Empty;

            return string.IsNullOrWhiteSpace(fileName)
                || !File.Exists(Path.Combine(_documentArchiveStorageService.ArchivedOutputDirectory, fileName));
        });

        StatisticsPdfHealthText.Text = eligible.Length == 0
            ? "Arşiv PDF kaydı yok"
            : missing == 0
                ? $"{eligible.Length}/{eligible.Length} PDF mevcut"
                : $"{missing} eksik • {eligible.Length} arşiv";

        StatisticsPdfHealthText.Foreground = missing == 0
            ? new SolidColorBrush(Color.FromRgb(36, 138, 80))
            : new SolidColorBrush(Color.FromRgb(194, 58, 67));
    }

    private void RefreshStatisticsInstitutionProfile(ArchiveStatisticsResult result)
    {
        if (string.IsNullOrWhiteSpace(_statisticsFilter.InstitutionName))
        {
            StatisticsInstitutionProfileText.Text = "Kurum seçilmedi";
            return;
        }

        var typeCount = result.Rows
            .Select(row => row.InvoiceType)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        StatisticsInstitutionProfileText.Text = result.InvoiceCount == 0
            ? $"{_statisticsFilter.InstitutionName}: veri yok"
            : $"{_statisticsFilter.InstitutionName}: {result.InvoiceCount} fatura"
              + $" • {typeCount} tür • {NumberParser.Money(result.PaymentTotal)}";
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
            var percentageText = "%" + (percentage * 100m).ToString(
                "N0",
                CultureInfo.GetCultureInfo("tr-TR"));

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
    }

    private void RefreshStatisticsDonutLabels(
        IReadOnlyList<StatisticsDonutItem> distribution,
        decimal total)
    {
        const double chartSize = 328d;
        const double center = chartSize / 2d;
        const double labelRadius = 121d;
        const double labelWidth = 52d;
        const double labelHeight = 24d;

        var angle = -90d;
        foreach (var item in distribution)
        {
            var percentage = item.PaymentTotal / total;
            var sweep = (double)percentage * 360d;

            if (percentage >= 0.06m)
            {
                var middleAngle = angle + (sweep / 2d);
                var radians = middleAngle * Math.PI / 180d;
                var x = center + (Math.Cos(radians) * labelRadius);
                var y = center + (Math.Sin(radians) * labelRadius);

                var label = new TextBlock
                {
                    Text = "%" + (percentage * 100m).ToString(
                        "N0",
                        CultureInfo.GetCultureInfo("tr-TR")),
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = StatisticsDataLabelBrush(StatisticsSkColor(item.InvoiceType)),
                    TextAlignment = TextAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center,
                    SnapsToDevicePixels = true,
                    UseLayoutRounding = true,
                    IsHitTestVisible = false
                };

                TextOptions.SetTextFormattingMode(label, TextFormattingMode.Display);
                TextOptions.SetTextRenderingMode(label, TextRenderingMode.ClearType);

                var labelHost = new Border
                {
                    Width = labelWidth,
                    Height = labelHeight,
                    Background = Brushes.Transparent,
                    Child = label,
                    SnapsToDevicePixels = true,
                    UseLayoutRounding = true,
                    IsHitTestVisible = false
                };

                TextOptions.SetTextFormattingMode(labelHost, TextFormattingMode.Display);
                TextOptions.SetTextRenderingMode(labelHost, TextRenderingMode.ClearType);

                Canvas.SetLeft(labelHost, Math.Round(x - (labelWidth / 2d)));
                Canvas.SetTop(labelHost, Math.Round(y - (labelHeight / 2d)));
                StatisticsDonutLabelsCanvas.Children.Add(labelHost);
            }

            angle += sweep;
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
            Foreground = new SolidColorBrush(Color.FromRgb(3, 28, 56)),
            FontSize = 16,
            FontWeight = FontWeights.SemiBold
        });
        label.Children.Add(new TextBlock
        {
            Text = $"{item.InvoiceCount} fatura",
            Foreground = new SolidColorBrush(Color.FromRgb(100, 119, 139)),
            FontSize = 13.5
        });

        var value = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Right
        };
        value.Children.Add(new TextBlock
        {
            Text = NumberParser.Money(item.PaymentTotal),
            Foreground = new SolidColorBrush(Color.FromRgb(3, 28, 56)),
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Right
        });
        value.Children.Add(new TextBlock
        {
            Text = "%" + percentage.ToString("N1", CultureInfo.GetCultureInfo("tr-TR")),
            Foreground = new SolidColorBrush(Color.FromRgb(84, 112, 141)),
            FontSize = 13.5,
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
                    var availableYears = ArchiveStatisticsService.Build(ArchivedLists).AvailableYears;
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

    private void ClearStatisticsFilters_Click(object sender, RoutedEventArgs e)
    {
        _suppressStatisticsEvents = true;
        try
        {
            StatisticsYearFilterBox.SelectedIndex = 0;
            StatisticsInstitutionFilterBox.SelectedIndex = 0;
            StatisticsTypeFilterBox.SelectedIndex = 0;
            StatisticsDateFromPicker.SelectedDate = null;
            StatisticsDateToPicker.SelectedDate = null;
            StatisticsTemplateBox.SelectedIndex = 0;
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
