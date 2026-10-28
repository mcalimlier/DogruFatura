using System.Windows;

namespace DogruFatura.Wpf;

public partial class MainWindow
{
    private void ShowView(string view)
    {
        if (view == "Home") RefreshDashboard();
        if (view == "Statistics") RefreshStatistics();
        if (view is "About" or "Security") RefreshSafetyInformation();
        _currentView = view;
        foreach (var (key, page, nav) in new[]
        {
            ("Home", (FrameworkElement)HomeView, NavHome), ("Invoice", InvoiceView, NavInvoice),
            ("Schools", SchoolsView, NavSchools), ("Officials", OfficialsView, NavOfficials),
            ("List", ListView, NavList), ("Archive", ArchiveView, NavArchive),
            ("Statistics", StatisticsView, NavStatistics), ("Security", SecurityView, NavSecurity),
            ("About", AboutView, NavAbout)
        })
        {
            page.Visibility = key == view ? Visibility.Visible : Visibility.Collapsed;
            nav.Tag = key == view ? "Active" : null;
        }
    }
    private void NavHome_Click(object sender, RoutedEventArgs e) => ShowView("Home");
    private void NavInvoice_Click(object sender, RoutedEventArgs e) => ShowView("Invoice");
    private void NavSchools_Click(object sender, RoutedEventArgs e) => ShowView("Schools");
    private void NavOfficials_Click(object sender, RoutedEventArgs e) => ShowView("Officials");
    private void NavList_Click(object sender, RoutedEventArgs e) { ShowView("List"); }
    private void NavArchive_Click(object sender, RoutedEventArgs e) { RefreshArchiveView(); ShowView("Archive"); }
    private void NavStatistics_Click(object sender, RoutedEventArgs e) => ShowView("Statistics");
    private void NavSecurity_Click(object sender, RoutedEventArgs e) => ShowView("Security");
    private void NavAbout_Click(object sender, RoutedEventArgs e) => ShowView("About");
}
