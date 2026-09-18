using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DogruFatura.Wpf;

public partial class MainWindow
{    private bool FilterControlIssue(object item)
    {
        if (item is not ControlIssue issue) return false;

        var severity = ControlSeverityFilterBox.SelectedItem as string;
        if (!string.IsNullOrWhiteSpace(severity)
            && severity != "Tümü"
            && !string.Equals(issue.SeverityLabel, severity, StringComparison.Ordinal))
            return false;

        var category = ControlCategoryFilterBox.SelectedItem as string;
        if (!string.IsNullOrWhiteSpace(category)
            && category != "Tümü"
            && !string.Equals(issue.Category, category, StringComparison.Ordinal))
            return false;

        var query = ControlSearchBox.Text.Trim();
        if (query.Length == 0) return true;

        var searchable = $"{issue.SeverityLabel} {issue.Category} {issue.Title} {issue.Detail} "
                         + $"{issue.InstitutionName} {issue.InvoiceType} {issue.InvoiceNumber} {issue.TargetLabel}";
        return CultureInfo.GetCultureInfo("tr-TR").CompareInfo.IndexOf(
            searchable,
            query,
            CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;
    }

    private void RefreshControlCenter()
    {
        if (ControlIssuesGrid is null) return;

        var scan = ControlCenterService.Scan(AllSchools, ActiveInvoices, DateTime.Today);
        _suppressControlEvents = true;
        ControlIssues.Clear();
        foreach (var issue in scan.Issues)
            ControlIssues.Add(issue);
        _suppressControlEvents = false;

        _controlView.SortDescriptions.Clear();
        _controlView.SortDescriptions.Add(new SortDescription(
            nameof(ControlIssue.SeverityRank),
            ListSortDirection.Ascending));
        _controlView.SortDescriptions.Add(new SortDescription(
            nameof(ControlIssue.Category),
            ListSortDirection.Ascending));
        _controlView.Refresh();

        ControlErrorCountText.Text = scan.ErrorCount.ToString(CultureInfo.CurrentCulture);
        ControlWarningCountText.Text = scan.WarningCount.ToString(CultureInfo.CurrentCulture);
        ControlCheckedCountText.Text = $"{AllSchools.Count} kurum • {ActiveInvoices.Count} fatura";
        ControlOverallStatusText.Text = scan.IsClean ? "SORUN YOK" : $"{scan.Issues.Count} BULGU";
        ControlOverallStatusText.Foreground = scan.IsClean
            ? new SolidColorBrush(Color.FromRgb(36, 138, 80))
            : scan.ErrorCount > 0
                ? new SolidColorBrush(Color.FromRgb(194, 58, 67))
                : new SolidColorBrush(Color.FromRgb(183, 121, 31));
        HomeControlStatusText.Text = scan.IsClean ? "TEMİZ" : $"{scan.Issues.Count} BULGU";
        HomeControlStatusText.Foreground = scan.IsClean
            ? new SolidColorBrush(Color.FromRgb(67, 199, 122))
            : scan.ErrorCount > 0
                ? new SolidColorBrush(Color.FromRgb(255, 118, 128))
                : new SolidColorBrush(Color.FromRgb(255, 180, 31));

        RefreshControlIssueView();
        SetControlStatus(
            scan.IsClean
                ? "Denetim tamamlandı; mevcut kayıtlarda sorun bulunmadı."
                : $"Denetim tamamlandı: {scan.ErrorCount} hata ve {scan.WarningCount} uyarı bulundu.",
            scan.ErrorCount > 0,
            scan.IsClean);
    }

    private void RefreshControlIssueView()
    {
        _controlView.Refresh();
        var visibleIssues = _controlView.Cast<ControlIssue>().ToArray();
        ControlVisibleCountText.Text = visibleIssues.Length == ControlIssues.Count
            ? $"{ControlIssues.Count} bulgu"
            : $"{visibleIssues.Length} / {ControlIssues.Count} bulgu";
        ControlEmptyState.Visibility = visibleIssues.Length == 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        var current = ControlIssuesGrid.SelectedItem as ControlIssue;
        var selection = current is not null && visibleIssues.Contains(current)
            ? current
            : visibleIssues.FirstOrDefault();
        ControlIssuesGrid.SelectedItem = selection;
        if (selection is not null)
            ControlIssuesGrid.ScrollIntoView(selection);
        UpdateControlIssueSelection();
    }

    private void UpdateControlIssueSelection()
    {
        if (ControlIssuesGrid.SelectedItem is not ControlIssue issue)
        {
            ControlSelectedSeverityText.Text = "TEMİZ";
            ControlSelectedTitleText.Text = "Seçili bulgu yok";
            ControlSelectedReferenceText.Text = "Kontrol sonuçlarından bir kayıt seçin.";
            ControlSelectedDetailText.Text = ControlIssues.Count == 0
                ? "Bütün denetimler başarıyla tamamlandı."
                : "Geçerli filtreyle eşleşen bulgu bulunmuyor.";
            ControlSelectedSeverityBadge.Background = new SolidColorBrush(Color.FromRgb(36, 138, 80));
            ControlGoToButton.Content = "KAYDA GİT";
            ControlGoToButton.IsEnabled = false;
            return;
        }

        ControlSelectedSeverityText.Text = issue.SeverityLabel.ToUpper(CultureInfo.GetCultureInfo("tr-TR"));
        ControlSelectedTitleText.Text = issue.Title;
        ControlSelectedReferenceText.Text = $"{issue.TargetLabel} • {issue.Reference}";
        ControlSelectedDetailText.Text = issue.Detail;
        ControlSelectedSeverityBadge.Background = issue.Severity == ControlIssueSeverity.Error
            ? new SolidColorBrush(Color.FromRgb(194, 58, 67))
            : new SolidColorBrush(Color.FromRgb(183, 121, 31));
        ControlGoToButton.Content = issue.ActionLabel;
        ControlGoToButton.IsEnabled = true;
    }

    private void ControlFilter_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressControlEvents) return;
        RefreshControlIssueView();
        SetControlStatus("Kontrol sonuçları filtrelendi.");
    }

    private void ControlFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressControlEvents) return;
        RefreshControlIssueView();
        SetControlStatus("Kontrol sonuçları filtrelendi.");
    }

    private void ClearControlFilters_Click(object sender, RoutedEventArgs e)
    {
        _suppressControlEvents = true;
        ControlSearchBox.Clear();
        ControlSeverityFilterBox.SelectedIndex = 0;
        ControlCategoryFilterBox.SelectedIndex = 0;
        _suppressControlEvents = false;
        RefreshControlIssueView();
        SetControlStatus("Kontrol filtreleri temizlendi.", false, true);
    }

    private void RefreshControlCenter_Click(object sender, RoutedEventArgs e) =>
        RefreshControlCenter();

    private void ControlIssuesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressControlEvents) return;
        UpdateControlIssueSelection();
    }

    private void ControlIssuesGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ControlIssuesGrid.SelectedItem is ControlIssue)
            ControlGoTo_Click(sender, e);
    }

    private void ControlGoTo_Click(object sender, RoutedEventArgs e)
    {
        if (ControlIssuesGrid.SelectedItem is not ControlIssue issue) return;

        if (issue.Target == ControlIssueTarget.Invoice && issue.InvoiceId.HasValue)
        {
            UpdateInvoiceListSummary(issue.InvoiceId.Value);
            var row = ActiveInvoiceRows.FirstOrDefault(candidate => candidate.Invoice.Id == issue.InvoiceId.Value);
            if (row is null)
            {
                RefreshControlCenter();
                SetControlStatus("İlgili fatura artık aktif listede bulunmuyor; denetim yenilendi.", true);
                return;
            }

            InvoicesGrid.SelectedItem = row;
            InvoicesGrid.ScrollIntoView(row);
            ShowView("List");
            SetInvoiceListStatus(
                $"Kontrol Merkezi: {issue.Title}. Seçili faturayı DÜZENLE ile açabilirsiniz.",
                issue.Severity == ControlIssueSeverity.Error);
            return;
        }

        if (issue.Target == ControlIssueTarget.School)
        {
            var school = AllSchools.FirstOrDefault(candidate =>
                string.Equals(candidate.InstitutionType, issue.SchoolInstitutionType, StringComparison.Ordinal)
                && CultureInfo.CurrentCulture.CompareInfo.Compare(
                    candidate.Name,
                    issue.SchoolName,
                    CompareOptions.IgnoreCase) == 0);
            if (school is null)
            {
                RefreshControlCenter();
                SetControlStatus("İlgili okul/kurum kaydı artık bulunmuyor; denetim yenilendi.", true);
                return;
            }

            _suppressSchoolEvents = true;
            SchoolSearchBox.Clear();
            SchoolFilterTypeBox.SelectedIndex = 0;
            _suppressSchoolEvents = false;
            RefreshSchoolView();
            SchoolsGrid.SelectedItem = school;
            SchoolsGrid.ScrollIntoView(school);
            LoadSchoolIntoEditor(school);
            ShowView("Schools");
            SetSchoolFormStatus($"Kontrol Merkezi: {issue.Title}. Eksik veya hatalı alanı düzenleyip KAYDET'e basın.", issue.Severity == ControlIssueSeverity.Error);
            return;
        }

        UpdateInvoiceListSummary();
        ShowView("List");
        SetInvoiceListStatus("Kontrol Merkezi: aktif liste türlerini düzenleme veya silme yoluyla tek türe indirin.", true);
    }

    private void SetControlStatus(string message, bool isError = false, bool isSuccess = false)
    {
        ControlStatusText.Text = message;
        if (isError)
        {
            ControlStatusText.Foreground = Brushes.MistyRose;
            ControlStatusBorder.Background = new SolidColorBrush(Color.FromRgb(95, 29, 37));
            ControlStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(169, 67, 77));
        }
        else if (isSuccess)
        {
            ControlStatusText.Foreground = new SolidColorBrush(Color.FromRgb(221, 246, 231));
            ControlStatusBorder.Background = new SolidColorBrush(Color.FromRgb(19, 81, 54));
            ControlStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(45, 138, 87));
        }
        else
        {
            ControlStatusText.Foreground = new SolidColorBrush(Color.FromRgb(216, 228, 239));
            ControlStatusBorder.Background = new SolidColorBrush(Color.FromRgb(18, 59, 91));
            ControlStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(54, 88, 116));
        }
    }
}
