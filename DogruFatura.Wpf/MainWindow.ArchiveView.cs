using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace DogruFatura.Wpf;

public partial class MainWindow
{
    private bool FilterArchivedList(object item)
    {
        if (item is not ArchivedInvoiceList archive) return false;

        var selectedType = ArchiveTypeFilterBox.SelectedItem as string;
        if (!string.IsNullOrWhiteSpace(selectedType)
            && selectedType != "Tümü"
            && !string.Equals(archive.InvoiceType, selectedType, StringComparison.Ordinal))
            return false;

        var selectedState = DomainValueText.ParseArchiveState(ArchiveStatusFilterBox.SelectedItem as string);
        if (selectedState.HasValue && archive.State != selectedState.Value) return false;

        var from = ArchiveDateFromPicker.SelectedDate?.Date;
        var to = ArchiveDateToPicker.SelectedDate?.Date;
        if (from.HasValue && archive.ArchivedAt.Date < from.Value) return false;
        if (to.HasValue && archive.ArchivedAt.Date > to.Value) return false;

        var query = ArchiveSearchBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(query)) return true;

        var invoiceText = string.Join(" ", archive.Invoices.Select(invoice =>
            $"{invoice.InstitutionName} {invoice.InvoiceNumber} {invoice.ServiceNumber} {invoice.Vkn} "
            + $"{invoice.InvoiceDate:dd.MM.yyyy} {invoice.DueDate:dd.MM.yyyy}"));
        var evidenceText = string.Join(" ", archive.LegacyEvidenceItems?
            .Select(item =>
                $"{item.InstitutionName} {item.InvoiceNumber} {item.ServiceNumber} "
                + $"{item.PeriodStart:dd.MM.yyyy} {item.PeriodEnd:dd.MM.yyyy} {item.DueDate:dd.MM.yyyy}")
            ?? Enumerable.Empty<string>());
        var searchable = $"{archive.ArchiveNumber} {archive.HysPaymentOrderDocumentNumber} "
                         + $"{archive.ClosureStatus} {archive.ArchivedAt:dd.MM.yyyy HH:mm} "
                         + $"{archive.InvoiceType} {archive.InstitutionSummary} {invoiceText} {evidenceText}";
        return CultureInfo.GetCultureInfo("tr-TR").CompareInfo.IndexOf(
            searchable,
            query,
            CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;
    }

    private void RefreshArchiveView(Guid? selectedArchiveId = null)
    {
        _archiveView.Refresh();
        _archiveView.SortDescriptions.Clear();
        _archiveView.SortDescriptions.Add(new SortDescription(
            nameof(ArchivedInvoiceList.ArchivedAt),
            ListSortDirection.Descending));

        var visibleArchives = _archiveView.Cast<ArchivedInvoiceList>().ToArray();
        var closedArchives = ArchivedLists
            .Where(archive =>
                archive.IsClosed
                && !string.IsNullOrWhiteSpace(archive.HysPaymentOrderDocumentNumber))
            .ToArray();
        ArchiveListCountText.Text = $"{ArchivedLists.Count} liste";
        ArchiveClosedListCountText.Text = $"{closedArchives.Length} liste";
        HomeArchiveCountText.Text = $"{closedArchives.Length} / {ArchivedLists.Count} KAPALI";
        ArchiveInvoiceCountText.Text = $"{closedArchives.Sum(archive => archive.InvoiceCount)} fatura";
        ArchivePaymentTotalText.Text = NumberParser.Money(closedArchives.Sum(archive => archive.PaymentTotal));
        ArchiveVisibleCountText.Text = visibleArchives.Length == ArchivedLists.Count
            ? $"{ArchivedLists.Count} kayıt"
            : $"{visibleArchives.Length} / {ArchivedLists.Count} kayıt";
        ArchiveListEmptyState.Visibility = visibleArchives.Length == 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        ArchivedInvoiceList? selection = null;
        if (selectedArchiveId.HasValue)
            selection = visibleArchives.FirstOrDefault(archive => archive.Id == selectedArchiveId.Value);
        if (selection is null
            && ArchiveGrid.SelectedItem is ArchivedInvoiceList current
            && visibleArchives.Any(archive => archive.Id == current.Id))
            selection = current;
        selection ??= visibleArchives.FirstOrDefault();

        ArchiveGrid.SelectedItem = selection;
        if (selection is not null)
            ArchiveGrid.ScrollIntoView(selection);
        UpdateArchiveSelection();
        UpdateArchiveActiveListState();
    }

    private void UpdateArchiveSelection()
    {
        if (ArchiveGrid.SelectedItem is not ArchivedInvoiceList archive)
        {
            ArchiveSelectedNumberText.Text = "Henüz seçilmedi";
            ArchiveSelectedPaymentText.Text = "0,00 TL";
            ArchiveSelectedInfoText.Text = "Bir arşiv kaydı seçin.";
            ArchiveSelectedDateText.Text = string.Empty;
            ArchiveSelectedClosureStateText.Text = "AÇIK";
            ArchiveSelectedClosureStateText.Foreground = new SolidColorBrush(Color.FromRgb(145, 171, 192));
            ArchiveHysNumberBox.Clear();
            ArchiveHysNumberBox.IsReadOnly = true;
            ArchiveClosureButton.Content = "HYS İLE KAPAT";
            ArchiveClosureButton.Background = new SolidColorBrush(Color.FromRgb(49, 83, 113));
            ArchiveClosureButton.IsEnabled = false;
            ArchiveInvoiceDetailGrid.ItemsSource = Array.Empty<InvoiceRecord>();
            ArchiveDetailEmptyState.Visibility = Visibility.Visible;
            OpenArchivedListButton.IsEnabled = false;
            EditArchivedListButton.IsEnabled = false;
            DeleteArchivedListButton.IsEnabled = false;
            return;
        }

        ArchiveSelectedNumberText.Text = archive.ArchiveNumber;
        ArchiveSelectedPaymentText.Text = NumberParser.Money(archive.PaymentTotal);
        ArchiveSelectedInfoText.Text = archive.IsLegacyEvidenceOnly
            ? $"{archive.InvoiceType} • {archive.InvoiceCount} kaynak satırı • vergisel satır detayı yok"
              + (string.IsNullOrWhiteSpace(archive.PdfFileName) ? string.Empty : " • özgün PDF kayıtlı")
              + (archive.HasClosedSnapshot ? " • Mühürlü" : string.Empty)
            : $"{archive.InvoiceType} • {archive.InvoiceCount} fatura"
              + (string.IsNullOrWhiteSpace(archive.PdfFileName) ? string.Empty : " • PDF kayıtlı")
              + (archive.HasClosedSnapshot ? " • Mühürlü" : string.Empty);
        ArchiveSelectedDateText.Text = archive.ArchivedAt.ToString("dd.MM.yyyy HH:mm", CultureInfo.CurrentCulture);
        ArchiveSelectedClosureStateText.Text = archive.IsClosed ? "HYS ile kapalı" : "HYS bekliyor";
        ArchiveSelectedClosureStateText.Foreground = new SolidColorBrush(archive.IsClosed
            ? Color.FromRgb(67, 199, 122)
            : Color.FromRgb(255, 180, 31));
        ArchiveHysNumberBox.Text = archive.HysPaymentOrderDocumentNumber;
        ArchiveHysNumberBox.IsReadOnly = archive.IsClosed;
        ArchiveClosureButton.Content = archive.IsClosed ? "KAPANIŞI GERİ AL" : "HYS İLE KAPAT";
        ArchiveClosureButton.Background = new SolidColorBrush(archive.IsClosed
            ? Color.FromRgb(145, 63, 72)
            : Color.FromRgb(36, 138, 80));
        ArchiveClosureButton.IsEnabled = !archive.IsLegacyEvidenceOnly;
        ArchiveInvoiceDetailGrid.ItemsSource = archive.IsLegacyEvidenceOnly
            ? Array.Empty<InvoiceRecord>()
            : archive.Invoices;
        ArchiveDetailEmptyState.Visibility = archive.IsLegacyEvidenceOnly
            ? Visibility.Visible
            : Visibility.Collapsed;
        OpenArchivedListButton.IsEnabled = !archive.IsLegacyEvidenceOnly;
        EditArchivedListButton.IsEnabled = !archive.IsLegacyEvidenceOnly;
        DeleteArchivedListButton.IsEnabled = true;
        SetArchiveStatus(archive.IsLegacyEvidenceOnly
            ? archive.LegacyEvidenceNote
              + " Özgün PDF ve resmî HYS toplamları korunmuştur; bu kayıt fatura düzenleme/istatistik hesabına sokulmaz."
            : archive.HasClosedSnapshot
                ? "Seçili HYS arşivinin bütünlük mührü geçerli. Kapanışı geri alırsanız mühür kaldırılır; yeniden kapatınca yeni mühür üretilir."
                : "Seçili listeyi açtığınızda arşiv çıktısı gösterilir; aktif listeniz değişmez.");
    }

    private void UpdateArchiveActiveListState()
    {
        var validation = FinalReportValidationService.Validate(ActiveInvoices);
        ArchiveActiveListButton.IsEnabled = validation.IsValid;
        ArchiveActiveListButton.ToolTip = validation.IsValid
            ? $"{ActiveInvoices.Count} faturadan oluşan aktif listeyi açık arşiv kaydı olarak sakla"
            : validation.Message;
    }
}
