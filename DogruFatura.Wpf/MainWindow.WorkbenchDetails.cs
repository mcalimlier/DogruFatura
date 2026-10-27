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
        var heading = ArchiveView.Children.OfType<FrameworkElement>().FirstOrDefault(child => Grid.GetRow(child) == 0);
        if (heading is Grid header)
        {
            var title = header.Children.OfType<StackPanel>().FirstOrDefault();
            title?.Children.Add(WorkbenchButton("İşlem geçmişi", (_, _) => ShowArchiveHistory()));
        }
        else
        {
            var menu = new ContextMenu(); var item = new MenuItem { Header = "Seçili listenin işlem geçmişi" };
            item.Click += (_, _) => ShowArchiveHistory(); menu.Items.Add(item); ArchiveGrid.ContextMenu = menu;
        }
        ArchiveSelectedClosureStateText.ToolTip = "HYS kapanışı belge takibidir; banka ödeme teyidi değildir.";
    }

    private void ShowArchiveHistory()
    {
        if (ArchiveGrid.SelectedItem is not ArchivedInvoiceList archive) return;
        try
        {
            var events = _auditTrailStore.ReadAll().Where(item => item.EntityId == archive.Id.ToString("D"))
                .OrderByDescending(item => item.OccurredAtUtc).Select(item => new { Zaman = item.OccurredAtUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.CurrentCulture), İşlem = AuditEventLabel(item.EventType) }).ToArray();
            var text = $"{archive.ArchiveNumber}\nOluşturulma: {archive.ArchivedAt:dd.MM.yyyy HH:mm}\n"
                + (archive.ClosedAt.HasValue ? $"HYS kapanışı: {archive.ClosedAt:dd.MM.yyyy HH:mm}\n" : "HYS kapanışı bekliyor.\n")
                + "HYS numarası: " + archive.HysDisplay + "\nBanka ödeme teyidi bu uygulamada tutulmaz.\n\n"
                + (string.IsNullOrWhiteSpace(archive.LegacyEvidenceNote) ? "" : archive.LegacyEvidenceNote + "\n\n")
                + string.Join("\n", events.Select(item => $"{item.Zaman} · {item.İşlem}"));
            ShowReadOnlyDetails("Arşiv işlem geçmişi", text, archive.Invoices);
        }
        catch (Exception ex) { SetArchiveStatus("İşlem geçmişi okunamadı: " + ex.Message, true); }
    }

    private static string AuditEventLabel(AuditTrailEventType type) => type switch
    {
        AuditTrailEventType.ArchiveCreated => "Liste arşivlendi",
        AuditTrailEventType.ArchiveClosed => "HYS ile kapatıldı",
        AuditTrailEventType.ArchiveReopened => "HYS kapanışı geri alındı",
        AuditTrailEventType.ArchiveRestoredToActive => "Düzenleme için açıldı",
        AuditTrailEventType.ArchiveImported => "PDF'den içe aktarıldı",
        AuditTrailEventType.ReportExported => "PDF çıktısı alındı",
        _ => type.ToString()
    };

    private void ShowReadOnlyDetails(string title, string description, IEnumerable<InvoiceRecord> invoices)
    {
        var window = new Window { Title = title, Owner = this, Language = System.Windows.Markup.XmlLanguage.GetLanguage("tr-TR"), Width = 980, Height = 620, MinWidth = 650, MinHeight = 420, WindowStartupLocation = WindowStartupLocation.CenterOwner, Background = Brushes.White };
        var panel = new DockPanel { Margin = new(20) };
        var text = new TextBox { Text = description, IsReadOnly = true, TextWrapping = TextWrapping.Wrap, FontSize = 14, MaxHeight = 220, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, BorderThickness = new(0), Margin = new(0, 0, 0, 12) };
        DockPanel.SetDock(text, Dock.Top); panel.Children.Add(text);
        var grid = new DataGrid { AutoGenerateColumns = false, IsReadOnly = true, CanUserAddRows = false, ItemsSource = invoices.ToArray(), FontSize = 14 };
        foreach (var (label, property) in new[] { ("Kurum", "InstitutionName"), ("Tür", "InvoiceType"), ("Fatura No", "InvoiceNumber"), ("Tutar", "PayableAmount"), ("KDV", "VatAmount"), ("Damga", "StampDuty"), ("Ödeme", "PaymentAmount") })
            grid.Columns.Add(new DataGridTextColumn { Header = label, Binding = new Binding(property) { StringFormat = property.EndsWith("Amount", StringComparison.Ordinal) || property == "StampDuty" ? "N2" : null }, Width = DataGridLength.Auto });
        panel.Children.Add(grid); window.Content = panel; window.ShowDialog();
    }

    private void InitializeStatisticsTools()
    {
        var titlePanel = StatisticsView.Children.OfType<Grid>().FirstOrDefault(child => Grid.GetRow(child) == 0)?.Children.OfType<StackPanel>().FirstOrDefault();
        var actions = new WrapPanel(); titlePanel?.Children.Add(actions);
        actions.Children.Add(WorkbenchButton("Kurum bazında dönem değişimi", (_, _) => ShowInstitutionChanges()));
        actions.Children.Add(WorkbenchButton("Toplamı oluşturan faturalar", (_, _) =>
        {
            var result = ArchiveStatisticsService.Build(ArchivedLists, _statisticsFilter);
            // Use the same public filtering contract as the statistics service.
            var invoices = ArchivedLists.Where(a => a.IsStatisticsEligible).SelectMany(a => a.Invoices).DistinctBy(i => i.Id)
                .Where(i => (!_statisticsFilter.Year.HasValue || i.InvoiceDate.Year == _statisticsFilter.Year)
                    && (string.IsNullOrEmpty(_statisticsFilter.InstitutionName) || string.Equals(i.InstitutionName.Trim(), _statisticsFilter.InstitutionName.Trim(), StringComparison.OrdinalIgnoreCase))
                    && (string.IsNullOrEmpty(_statisticsFilter.InvoiceType) || string.Equals(i.InvoiceType.Trim(), _statisticsFilter.InvoiceType.Trim(), StringComparison.OrdinalIgnoreCase))
                    && (!_statisticsFilter.StartDate.HasValue || i.InvoiceDate.Date >= _statisticsFilter.StartDate.Value.Date)
                    && (!_statisticsFilter.EndDate.HasValue || i.InvoiceDate.Date <= _statisticsFilter.EndDate.Value.Date)).ToArray();
            ShowReadOnlyDetails("İstatistik kayıt ayrıntısı", $"Seçili filtrede {result.InvoiceCount} fatura · Ödeme: {NumberParser.Money(result.PaymentTotal)}\nYalnız HYS ile kapatılmış ve satırları doğrulanmış arşivler. Tarih temeli: fatura tarihi. Tutar değişimi tüketim değişimi anlamına gelmez.", invoices);
        }));
    }

    private void ShowInstitutionChanges()
    {
        var previousFilter = PreviousStatisticsFilter(_statisticsFilter);
        if (previousFilter is null)
        {
            ShowReadOnlyDetails("Kurum bazında dönem değişimi", "Karşılaştırmak için bir yıl veya başlangıç ve bitiş tarihi seçin. Tarih aralığı seçildiğinde önceki aynı uzunluktaki dönem kullanılır.", Array.Empty<InvoiceRecord>());
            return;
        }
        var current = ArchiveStatisticsService.Build(ArchivedLists, _statisticsFilter);
        var previous = ArchiveStatisticsService.Build(ArchivedLists, previousFilter);
        var names = current.Rows.Select(r => r.InstitutionName).Concat(previous.Rows.Select(r => r.InstitutionName)).Distinct(StringComparer.OrdinalIgnoreCase);
        var changes = names.Select(name => new
        {
            Name = name,
            Current = current.Rows.Where(r => string.Equals(r.InstitutionName, name, StringComparison.OrdinalIgnoreCase)).Sum(r => r.PaymentTotal),
            Previous = previous.Rows.Where(r => string.Equals(r.InstitutionName, name, StringComparison.OrdinalIgnoreCase)).Sum(r => r.PaymentTotal)
        }).OrderByDescending(r => r.Current - r.Previous).ToArray();
        var description = "Aynı kurum ve fatura türü filtreleriyle, fatura tarihine göre HYS kapalı kayıtlar karşılaştırılır. Tutar farkı tüketim değişimi değildir.\n\n"
            + string.Join("\n", changes.Select(r => $"{r.Name}: {NumberParser.Money(r.Previous)} → {NumberParser.Money(r.Current)} · Fark {NumberParser.Money(r.Current - r.Previous)}"
                + (r.Previous == 0 ? " · önceki dönemde kayıt yok" : $" · %{((r.Current - r.Previous) / r.Previous * 100).ToString("N1", CultureInfo.GetCultureInfo("tr-TR"))}")));
        ShowReadOnlyDetails("Kurum bazında dönem değişimi", description, Array.Empty<InvoiceRecord>());
    }

    private void InitializeSafetyTools()
    {
        var panel = (StackPanel)AboutView.Content;
        var extra = new StackPanel { Margin = new(0, 14, 0, 10) };
        extra.Children.Add(new TextBlock { Text = "İkinci yedek konumu", FontSize = 18, FontWeight = FontWeights.SemiBold, Foreground = Brushes.DarkSlateGray });
        var location = new TextBlock { Text = string.IsNullOrEmpty(_preferences.SecondaryBackupFolder) ? "İsteğe bağlı: başka disk veya ağ klasörü seçebilirsiniz." : _preferences.SecondaryBackupFolder, FontSize = 13, TextWrapping = TextWrapping.Wrap, Margin = new(0, 6, 0, 8) };
        extra.Children.Add(location);
        var actions = new WrapPanel(); extra.Children.Add(actions);
        actions.Children.Add(WorkbenchButton("Klasör seç", (_, _) =>
        {
            var dialog = new OpenFolderDialog { Title = "İkinci yedek klasörünü seçin" };
            if (dialog.ShowDialog(this) != true) return;
            _preferences = _preferences with { SecondaryBackupFolder = dialog.FolderName }; location.Text = dialog.FolderName; SaveWorkbenchPreferences();
        }));
        actions.Children.Add(WorkbenchButton("İkinci konuma yedekle", (_, _) =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_preferences.SecondaryBackupFolder)) { location.Text = "Önce bir klasör seçin."; return; }
                _lastSafetyBackupPath = _safetyService.CreateManualSafetyBackup();
                Directory.CreateDirectory(_preferences.SecondaryBackupFolder);
                var target = Path.Combine(_preferences.SecondaryBackupFolder, Path.GetFileName(_lastSafetyBackupPath));
                if (!string.Equals(Path.GetFullPath(target), Path.GetFullPath(_lastSafetyBackupPath), StringComparison.OrdinalIgnoreCase)) File.Copy(_lastSafetyBackupPath, target, false);
                location.Text = "İkinci yedek oluşturuldu: " + target; RefreshSafetyInformation();
            }
            catch (Exception ex) { location.Text = "İkinci konuma kopyalanamadı; yerel yedek korunur. " + ex.Message; }
        }));
        panel.Children.Add(extra);
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

    private void ApplyAdaptiveLayout()
    {
        if (!IsLoaded || HomeView.Parent is not Grid host || host.Parent is not Border contentBorder) return;
        var compactNavigation = ActualWidth < 1100;
        var navigationWidth = compactNavigation ? 64 : 210;
        if (AppScaleViewbox.Children[0] is Border shell && shell.Child is Grid shellGrid)
        {
            shellGrid.ColumnDefinitions[0].Width = new GridLength(navigationWidth);
            if (shellGrid.Children[0] is Grid header) header.ColumnDefinitions[0].Width = new GridLength(navigationWidth);
            foreach (var nav in new[] { NavHome, NavInvoice, NavOfficials, NavList, NavReports, NavSchools, NavArchive, NavControl, NavStatistics, NavAbout })
            {
                if (nav.Content is not StackPanel labels || labels.Children[1] is not TextBlock label) continue;
                nav.ToolTip = label.Text; label.Visibility = compactNavigation ? Visibility.Collapsed : Visibility.Visible;
            }
            if (shellGrid.Children[1] is Grid navigation && navigation.Children[1] is Border signature) signature.Visibility = compactNavigation ? Visibility.Collapsed : Visibility.Visible;
            if (shellGrid.Children[0] is Grid top && top.Children[0] is Image logo)
            {
                logo.Width = compactNavigation ? 38 : 178;
                logo.Margin = compactNavigation ? new Thickness(10, 0, 0, 0) : new Thickness(22, 0, 0, 0);
                logo.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(compactNavigation ? "pack://application:,,,/DogruFatura;component/Assets/AppIcon_512.png" : "pack://application:,,,/DogruFatura;component/Assets/Logo.png"));
            }
        }
        var pageWidth = Math.Max(500, ActualWidth - navigationWidth - 58);
        var invoiceGrid = _entryLayoutGrid;
        if (invoiceGrid is not null)
        {
            if (!_responsiveGrids.ContainsKey(invoiceGrid)) _responsiveGrids[invoiceGrid] = (invoiceGrid.ColumnDefinitions.ToArray(), invoiceGrid.Children.Cast<UIElement>().Select(Grid.GetColumn).ToArray(), invoiceGrid.Children.Cast<UIElement>().Select(Grid.GetRow).ToArray());
            var original = _responsiveGrids[invoiceGrid];
            invoiceGrid.ColumnDefinitions.Clear(); invoiceGrid.RowDefinitions.Clear();
            if (pageWidth < 950)
            {
                invoiceGrid.ColumnDefinitions.Add(new ColumnDefinition());
                for (var i = 0; i < invoiceGrid.Children.Count; i++)
                {
                    invoiceGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    Grid.SetColumn(invoiceGrid.Children[i], 0); Grid.SetRow(invoiceGrid.Children[i], i);
                    if (invoiceGrid.Children[i] is FrameworkElement item) item.Margin = new(0, 0, 0, 12);
                }
            }
            else
            {
                foreach (var column in original.Item1) invoiceGrid.ColumnDefinitions.Add(column);
                for (var i = 0; i < invoiceGrid.Children.Count; i++) { Grid.SetColumn(invoiceGrid.Children[i], original.Item2[i]); Grid.SetRow(invoiceGrid.Children[i], original.Item3[i]); }
            }
        }
        // Complex legacy tables keep their column widths and scroll; the entry form reflows.
        foreach (var page in new[] { ListView, ArchiveView, ControlView, StatisticsView, SchoolsView, OfficialsView, ReportsView })
        {
            if (page.Parent == host)
            {
                host.Children.Remove(page);
                var scroll = new ScrollViewer { Content = page, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
                scroll.SetBinding(VisibilityProperty, new Binding("Visibility") { Source = page });
                Grid.SetRow(scroll, 1); host.Children.Add(scroll);
            }
            page.Width = Math.Max(1050, pageWidth - 18);
            page.Height = Math.Max(650, host.ActualHeight - 115);
        }
    }
}
