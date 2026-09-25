using System.Windows;

namespace DogruFatura.Wpf;

public partial class MainWindow
{    private void ShowView(string view)
    {
        if (view == "Home") RefreshDashboard();
        if (view == "Statistics") RefreshStatistics();
        if (view == "About") RefreshSafetyInformation();
        _currentView = view;
        HomeView.Visibility = view == "Home" ? Visibility.Visible : Visibility.Collapsed;
        InvoiceView.Visibility = view == "Invoice" ? Visibility.Visible : Visibility.Collapsed;
        SchoolsView.Visibility = view == "Schools" ? Visibility.Visible : Visibility.Collapsed;
        OfficialsView.Visibility = view == "Officials" ? Visibility.Visible : Visibility.Collapsed;
        ListView.Visibility = view == "List" ? Visibility.Visible : Visibility.Collapsed;
        ReportsView.Visibility = view == "Reports" ? Visibility.Visible : Visibility.Collapsed;
        ArchiveView.Visibility = view == "Archive" ? Visibility.Visible : Visibility.Collapsed;
        ControlView.Visibility = view == "Control" ? Visibility.Visible : Visibility.Collapsed;
        StatisticsView.Visibility = view == "Statistics" ? Visibility.Visible : Visibility.Collapsed;
        AboutView.Visibility = view == "About" ? Visibility.Visible : Visibility.Collapsed;

        NavHome.Tag = view == "Home" ? "Active" : null;
        NavInvoice.Tag = view == "Invoice" ? "Active" : null;
        NavSchools.Tag = view == "Schools" ? "Active" : null;
        NavOfficials.Tag = view == "Officials" ? "Active" : null;
        NavList.Tag = view == "List" ? "Active" : null;
        NavReports.Tag = view == "Reports" ? "Active" : null;
        NavArchive.Tag = view == "Archive" ? "Active" : null;
        NavControl.Tag = view == "Control" ? "Active" : null;
        NavStatistics.Tag = view == "Statistics" ? "Active" : null;
        NavAbout.Tag = view == "About" ? "Active" : null;
    }

    private void NavHome_Click(object sender, RoutedEventArgs e) => ShowView("Home");
    private void NavInvoice_Click(object sender, RoutedEventArgs e) => ShowView("Invoice");
    private void NavSchools_Click(object sender, RoutedEventArgs e) => ShowView("Schools");
    private void NavOfficials_Click(object sender, RoutedEventArgs e) => ShowView("Officials");
    private void NavList_Click(object sender, RoutedEventArgs e) => ShowView("List");
    private void NavReports_Click(object sender, RoutedEventArgs e)
    {
        _openedArchiveForReport = null;
        ShowView("Reports");
        RefreshFinalReportPreview();
    }
    private void NavArchive_Click(object sender, RoutedEventArgs e)
    {
        RefreshArchiveView();
        ShowView("Archive");
    }
    private void NavControl_Click(object sender, RoutedEventArgs e)
    {
        RefreshControlCenter();
        ShowView("Control");
    }
    private void NavStatistics_Click(object sender, RoutedEventArgs e) => ShowView("Statistics");
    private void NavAbout_Click(object sender, RoutedEventArgs e) => ShowView("About");
}
