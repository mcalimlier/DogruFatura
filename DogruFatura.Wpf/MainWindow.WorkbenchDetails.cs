using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace DogruFatura.Wpf;

public partial class MainWindow
{
    private void InitializeArchiveTools()
    {
        ArchiveGrid.RowHeaderWidth = 38;
        ArchiveGrid.LoadingRow += (_, e) => e.Row.Header = (e.Row.GetIndex() + 1).ToString(CultureInfo.CurrentCulture);
        ArchiveGrid.ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader;

    }



    private static string AuditEventLabel(AuditTrailEventType type) => type switch
    {
        AuditTrailEventType.ArchiveCreated => "Liste arşivlendi",
        AuditTrailEventType.ArchiveClosed => "HYS ile kapatıldı",
        AuditTrailEventType.ArchiveReopened => "HYS kapanışı geri alındı",
        AuditTrailEventType.ArchiveRestoredToActive => "Düzenleme için açıldı",
        AuditTrailEventType.ArchiveImported => "PDF'den içe aktarıldı",
        AuditTrailEventType.ReportExported => "PDF çıktısı alındı",
        AuditTrailEventType.ArchiveInstitutionNamesCorrected => "Kurum Adları Düzeltildi",
        _ => type.ToString()
    };

    private void ShowReadOnlyDetails(string title, string description, IEnumerable<InvoiceRecord> invoices)
    {
        var window = CreateDetailsWindow(title, description, invoices.Cast<object>(), new[]
        {
            ("Kurum", "InstitutionName", ""), ("Tür", "InvoiceType", ""), ("Fatura No", "InvoiceNumber", ""),
            ("Tutar", "PayableAmount", "N2"), ("KDV", "VatAmount", "N2"), ("Damga", "StampDuty", "N2"), ("Ödeme", "PaymentAmount", "N2")
        });
        window.ShowDialog();
    }

    private Window CreateDetailsWindow(string title, string description, IEnumerable<object> rows, (string Label, string Property, string Format)[] columns)
    {
        var area = SystemParameters.WorkArea;
        var window = new Window { Title = title, Owner = this, Language = System.Windows.Markup.XmlLanguage.GetLanguage("tr-TR"),
            Width = Math.Min(1050, area.Width), Height = Math.Min(650, area.Height), MinWidth = Math.Min(600, area.Width), MinHeight = Math.Min(400, area.Height),
            WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = (Brush)FindResource("Navy950Brush"), UseLayoutRounding = true };
        var layout = new Grid { Margin = new Thickness(18) };
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); layout.RowDefinitions.Add(new RowDefinition());
        var heading = new StackPanel();
        heading.Children.Add(new TextBlock { Text = title, FontSize = 18, FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource("Gold500Brush"), Margin = new Thickness(0,0,0,10), TextWrapping = TextWrapping.Wrap });
        heading.Children.Add(new TextBox { Text = description, IsReadOnly = true, Background = Brushes.Transparent, Foreground = (Brush)FindResource("PremiumMutedTextBrush"), FontSize = 13,
            BorderThickness = new Thickness(0), TextWrapping = TextWrapping.Wrap, MaxHeight = 140, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Margin = new Thickness(0,0,0,14) });
        layout.Children.Add(heading);
        var sheet = new DataGrid { Name = "DetailsGrid", Style = (Style)FindResource("ModernDataGrid"), AutoGenerateColumns = false, IsReadOnly = true, CanUserAddRows = false,
            ItemsSource = rows.ToArray(), FontSize = 13, RowHeight = 34, CanUserSortColumns = true, HeadersVisibility = DataGridHeadersVisibility.Column };
        foreach (var column in columns)
            sheet.Columns.Add(new DataGridTextColumn { Header = column.Label, Binding = new Binding(column.Property) { StringFormat = column.Format.Length == 0 ? null : column.Format, TargetNullValue = "—" }, Width = new DataGridLength(column.Property == "InstitutionName" ? 2 : 1, DataGridLengthUnitType.Star), MinWidth = 85 });
        var card = new Border { Style = (Style)FindResource("PaymentMetricCard"), Padding = new Thickness(10), Child = sheet };
        Grid.SetRow(card, 1); layout.Children.Add(card); window.Content = layout;
        return window;
    }


    private void InitializeStatisticsTools()
    {
        var actions = StatisticsDetailActions;
        actions.Children.Add(WorkbenchButton("Kurum Bazında Dönem Değişimi", (_, _) => ShowInstitutionChanges()));
        actions.Children.Add(WorkbenchButton("Toplamı Oluşturan Faturalar", (_, _) =>
        {
            var result = ArchiveStatisticsService.Build(ArchivedLists, _statisticsFilter, AllSchools);
            // Use the same public filtering contract as the statistics service.
            var invoices = ArchivedLists.Where(a => a.IsStatisticsEligible).SelectMany(a => a.Invoices).DistinctBy(i => i.Id)
                .Select(i => InstitutionResolutionService.ForReporting(i, AllSchools))
                .Where(i => (!_statisticsFilter.Year.HasValue || i.InvoiceDate.Year == _statisticsFilter.Year)
                    && (string.IsNullOrEmpty(_statisticsFilter.InstitutionName) || string.Equals(i.InstitutionName.Trim(), _statisticsFilter.InstitutionName.Trim(), StringComparison.OrdinalIgnoreCase))
                    && (string.IsNullOrEmpty(_statisticsFilter.InvoiceType) || string.Equals(i.InvoiceType.Trim(), _statisticsFilter.InvoiceType.Trim(), StringComparison.OrdinalIgnoreCase))
                    && (!_statisticsFilter.StartDate.HasValue || i.InvoiceDate.Date >= _statisticsFilter.StartDate.Value.Date)
                    && (!_statisticsFilter.EndDate.HasValue || i.InvoiceDate.Date <= _statisticsFilter.EndDate.Value.Date)).ToArray();
            ShowReadOnlyDetails("İstatistik Kayıt Ayrıntısı", $"Seçili filtrede {result.InvoiceCount} fatura · Ödeme: {NumberParser.Money(result.PaymentTotal)}\nYalnız HYS ile kapatılmış ve satırları doğrulanmış arşivler. Tarih temeli: fatura tarihi. Tutar değişimi tüketim değişimi anlamına gelmez.", invoices);
        }));
        foreach (Button button in actions.Children)
        {
            button.Height = 34; button.MinHeight = 34; button.FontSize = 13;
            button.Padding = new Thickness(10, 5, 10, 5); button.Margin = new Thickness(0,0,8,8);
        }
    }

    private void ShowInstitutionChanges()
    {
        try { CreateInstitutionChangesWindow().ShowDialog(); }
        catch (ArgumentException ex) { MessageBox.Show(this, ex.Message, "Dönem Seçimi", MessageBoxButton.OK, MessageBoxImage.Information); }
    }

    private Window CreateInstitutionChangesWindow()
    {
        var result = InstitutionPeriodComparisonService.Build(ArchivedLists, _statisticsFilter, AllSchools);
        string Period(ArchiveStatisticsFilter filter) => filter.StartDate.HasValue
            ? $"{filter.StartDate:dd.MM.yyyy} – {filter.EndDate:dd.MM.yyyy}" : filter.Year?.ToString(CultureInfo.CurrentCulture) ?? "—";
        var description = $"Önceki Dönem: {Period(result.PreviousFilter)} · Güncel Dönem: {Period(result.CurrentFilter)}.\n"
            + "HYS ile kapanmış kayıtların ödeme tutarları karşılaştırılır. Önceki tutar sıfırsa yüzde hesaplanmaz."
            + (result.Rows.Count == 0 ? "\nSeçili dönem ve filtrelerde kayıt bulunamadı." : "");
        return CreateDetailsWindow("Kurum Bazında Dönem Değişimi", description, result.Rows.Cast<object>(), new[]
        {
            ("Kurum", "InstitutionName", ""), ("Önceki Tutar", "PreviousTotal", "N2"), ("Güncel Tutar", "CurrentTotal", "N2"), ("Fark", "Difference", "N2"), ("Değişim %", "PercentChange", "N1")
        });
    }

    private static string DescribeBackup(string path)
    {
        try
        {
            using var zip = ZipFile.OpenRead(path);
            return $"Dosya: {Path.GetFileName(path)}\nYedek dosyası tarihi: {File.GetLastWriteTime(path):dd.MM.yyyy HH:mm}\n"
                + $"{zip.Entries.Count(e => e.FullName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))} JSON · {zip.Entries.Count(e => e.FullName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))} PDF\n"
                + "Dosyaların bütünlüğü geri yüklemeden önce ayrıca doğrulanır.\n\n";
        }
        catch (Exception ex) { return "Yedek önizlemesi okunamadı: " + ex.Message + "\n\n"; }
    }

}
