using DogruFatura.Core.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace DogruFatura.Wpf;

public partial class MainWindow
{
    private bool _applyingProfile;

    private void ArchiveStateTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressArchiveEvents || !IsLoaded || e.Source != ArchiveStateTabs) return;
        RefreshArchiveView();
    }

    private void InitializeDisplayProfiles()
    {
        _applyingProfile = true;
        DisplayProfileBox.ItemsSource = DisplayProfilePolicy.Profiles;
        DisplayProfileBox.SelectedItem = DisplayProfilePolicy.Resolve(_preferences.DisplayProfile);
        _applyingProfile = false;
        ApplyDisplayProfileButton.IsEnabled = false;
        Loaded += (_, _) => ApplyDisplayProfile();
        Loaded += (_, _) => { ArchiveStateTabs.SelectedIndex = 0; RefreshArchiveView(); };
    }

    private void DisplayProfile_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_applyingProfile || DisplayProfileBox.SelectedItem is not DisplayProfile profile) return;
        ApplyDisplayProfileButton.IsEnabled = profile.Key != DisplayProfilePolicy.Resolve(_preferences.DisplayProfile).Key;
    }

    private void ApplyDisplayProfile_Click(object sender, RoutedEventArgs e)
    {
        if (DisplayProfileBox.SelectedItem is not DisplayProfile profile) return;
        _preferences = _preferences with { DisplayProfile = profile.Key };
        ApplyDisplayProfile(); SaveWorkbenchPreferences();
        ApplyDisplayProfileButton.IsEnabled = false;
    }

    private void ApplyDisplayProfile()
    {
        var area = SystemParameters.WorkArea;
        var dpi = VisualTreeHelper.GetDpi(this);
        var size = DisplayProfilePolicy.Fit(_preferences.DisplayProfile, dpi.DpiScaleX, area.Width, area.Height);
        WindowState = WindowState.Normal;
        MinWidth = Math.Min(680, area.Width); MinHeight = Math.Min(480, area.Height);
        Width = size.Width; Height = size.Height;
        Left = area.Left + Math.Max(0, (area.Width - Width) / 2);
        Top = area.Top + Math.Max(0, (area.Height - Height) / 2);
        DisplayProfileStatusText.Text = $"Seçili profil: {DisplayProfilePolicy.Resolve(_preferences.DisplayProfile).Label}. Windows ölçeği: %{dpi.DpiScaleX * 100:0}.";
        ApplyAdaptiveLayout();
    }

    private void RefreshDashboard()
    {
        HomeInvoiceCountText.Text = ActiveInvoices.Count.ToString(System.Globalization.CultureInfo.CurrentCulture);
        HomeInvoiceTypeText.Text = ActiveInvoices.Count == 0 ? "Henüz yok" : InvoiceListTypePolicy.GetListTypeLabel(ActiveInvoices);
        HomePaymentText.Text = NumberParser.Money(ActiveInvoices.Sum(i => i.PaymentAmount));
        HomePageCountText.Text = FinalReportPagePlanner.Plan(ActiveInvoices.Count).Count.ToString(System.Globalization.CultureInfo.CurrentCulture);
        var insights = ActiveListInsights.Build(ActiveInvoices, DateTime.Today);
        HomeOverdueCountText.Text = insights.OverdueCount.ToString(System.Globalization.CultureInfo.CurrentCulture);
        HomeOverdueTotalText.Text = NumberParser.Money(insights.OverdueInvoiceTotal) + " Fatura Tutarı";
        HomeInstitutionCountText.Text = insights.InstitutionCount.ToString(System.Globalization.CultureInfo.CurrentCulture);
        HomePendingText.Text = ArchivedLists.Count(a => !a.IsClosed).ToString(System.Globalization.CultureInfo.CurrentCulture);
    }

    private void ReflowColumns(Grid grid, bool stacked)
    {
        if (!_responsiveGrids.TryGetValue(grid, out var original))
        {
            if (grid.ColumnDefinitions.Count != 3) return;
            original = (grid.ColumnDefinitions.ToArray(), grid.Children.Cast<UIElement>().Select(Grid.GetColumn).ToArray(), grid.Children.Cast<UIElement>().Select(Grid.GetRow).ToArray());
            _responsiveGrids[grid] = original;
        }
        grid.ColumnDefinitions.Clear(); grid.RowDefinitions.Clear();
        if (stacked)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            for (int i = 0; i < grid.Children.Count; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                Grid.SetColumn(grid.Children[i], 0); Grid.SetRow(grid.Children[i], i);
                if (grid.Children[i] is FrameworkElement item) item.Margin = new Thickness(0, 0, 0, 14);
            }
        }
        else
        {
            foreach (var column in original.Item1) grid.ColumnDefinitions.Add(column);
            for (int i = 0; i < grid.Children.Count; i++)
            {
                Grid.SetColumn(grid.Children[i], original.Item2[i]); Grid.SetRow(grid.Children[i], original.Item3[i]);
                if (grid.Children[i] is FrameworkElement item) item.Margin = new Thickness(0);
            }
        }
    }

    private void ApplyAdaptiveLayout()
    {
        if (!IsLoaded || HomeView.Parent is not Grid host || host.Parent is not Border content) return;
        bool compact = ActualWidth < 1200;
        double navWidth = compact ? 64 : 210;
        if (AppScaleViewbox.Children[0] is Border shell && shell.Child is Grid shellGrid)
        {
            shellGrid.ColumnDefinitions[0].Width = new GridLength(navWidth);
            if (shellGrid.Children[0] is Grid header) header.ColumnDefinitions[0].Width = new GridLength(navWidth);
            foreach (var nav in new[] { NavHome, NavInvoice, NavOfficials, NavList, NavSchools, NavArchive, NavStatistics, NavSecurity, NavAbout })
            {
                if (nav.Content is not StackPanel labels || labels.Children[1] is not TextBlock label) continue;
                nav.ToolTip = label.Text; label.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
            }
            if (shellGrid.Children[1] is Grid navigation && navigation.Children[1] is Border signature) signature.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
            if (shellGrid.Children[0] is Grid top && top.Children[0] is Image logo)
            {
                logo.Width = compact ? 38 : 178; logo.Margin = new Thickness(compact ? 10 : 22, 0, 0, 0);
                logo.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(compact ? "pack://application:,,,/DogruFatura;component/Assets/AppIcon_512.png" : "pack://application:,,,/DogruFatura;component/Assets/Logo.png"));
            }
        }
        double width = Math.Max(400, host.ActualWidth - 20);
        HomeFactsGrid.Columns = width >= 1000 ? 3 : 2;
        if (_entryLayoutGrid is not null) ReflowColumns(_entryLayoutGrid, width < 950);
        if (StatisticsCardsGrid is not null) ReflowColumns(StatisticsCardsGrid, width < 1050);
        // Tables scroll internally; forms/detail cards reflow without scaling text.
        foreach (var page in new[] { ListView, ArchiveView, StatisticsView, SchoolsView, OfficialsView })
        {
            if (page.Parent == host)
            {
                host.Children.Remove(page);
                var scroll = new ScrollViewer { Content = page, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
                scroll.SetBinding(VisibilityProperty, new Binding("Visibility") { Source = page });
                host.Children.Add(scroll);
            }
            page.Width = double.NaN;
            page.Height = page == StatisticsView ? double.NaN : Math.Max(560, host.ActualHeight - 10);
        }
        foreach (var page in new[] { ArchiveView, SchoolsView, OfficialsView })
        {
            var grids = page.Children.OfType<Grid>().Where(g => g.ColumnDefinitions.Count == 3 || _responsiveGrids.ContainsKey(g)).ToArray();
            foreach (var grid in grids)
            {
                if (!grid.Children.OfType<Border>().Any()) continue;
                ReflowColumns(grid, width < 1050);
                if (width < 1050) page.Height = Math.Max(page.Height, page == ArchiveView ? 1350 : 1150);
            }
        }
    }
}
