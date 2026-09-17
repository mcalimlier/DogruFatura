using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;

namespace DogruFatura.Wpf;

public partial class MainWindow
{    private bool FilterArchivedList(object item)
    {
        if (item is not ArchivedInvoiceList archive) return false;

        var selectedType = ArchiveTypeFilterBox.SelectedItem as string;
        if (!string.IsNullOrWhiteSpace(selectedType)
            && selectedType != "Tümü"
            && !string.Equals(archive.InvoiceType, selectedType, StringComparison.Ordinal))
            return false;

        var selectedStatus = ArchiveStatusFilterBox.SelectedItem as string;
        if (selectedStatus == "Açık" && archive.IsClosed) return false;
        if (selectedStatus == "Kapalı" && !archive.IsClosed) return false;

        var from = ArchiveDateFromPicker.SelectedDate?.Date;
        var to = ArchiveDateToPicker.SelectedDate?.Date;
        if (from.HasValue && archive.ArchivedAt.Date < from.Value) return false;
        if (to.HasValue && archive.ArchivedAt.Date > to.Value) return false;

        var query = ArchiveSearchBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(query)) return true;

        var invoiceText = string.Join(" ", archive.Invoices.Select(invoice =>
            $"{invoice.InstitutionName} {invoice.InvoiceNumber} {invoice.ServiceNumber} {invoice.Vkn} "
            + $"{invoice.InvoiceDate:dd.MM.yyyy} {invoice.DueDate:dd.MM.yyyy}"));
        var searchable = $"{archive.ArchiveNumber} {archive.HysPaymentOrderDocumentNumber} "
                         + $"{archive.ClosureStatus} {archive.ArchivedAt:dd.MM.yyyy HH:mm} "
                         + $"{archive.InvoiceType} {archive.InstitutionSummary} {invoiceText}";
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
        var closedArchives = ArchivedLists.Where(archive => archive.IsStatisticsEligible).ToArray();
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
            return;
        }

        ArchiveSelectedNumberText.Text = archive.ArchiveNumber;
        ArchiveSelectedPaymentText.Text = NumberParser.Money(archive.PaymentTotal);
        ArchiveSelectedInfoText.Text = $"{archive.InvoiceType} • {archive.InvoiceCount} fatura"
            + (string.IsNullOrWhiteSpace(archive.PdfFileName) ? string.Empty : " • PDF kayıtlı");
        ArchiveSelectedDateText.Text = archive.ArchivedAt.ToString("dd.MM.yyyy HH:mm", CultureInfo.CurrentCulture);
        ArchiveSelectedClosureStateText.Text = archive.IsClosed ? "KAPALI" : "AÇIK";
        ArchiveSelectedClosureStateText.Foreground = new SolidColorBrush(archive.IsClosed
            ? Color.FromRgb(67, 199, 122)
            : Color.FromRgb(255, 180, 31));
        ArchiveHysNumberBox.Text = archive.HysPaymentOrderDocumentNumber;
        ArchiveHysNumberBox.IsReadOnly = archive.IsClosed;
        ArchiveClosureButton.Content = archive.IsClosed ? "KAPANIŞI GERİ AL" : "HYS İLE KAPAT";
        ArchiveClosureButton.Background = new SolidColorBrush(archive.IsClosed
            ? Color.FromRgb(145, 63, 72)
            : Color.FromRgb(36, 138, 80));
        ArchiveClosureButton.IsEnabled = true;
        ArchiveInvoiceDetailGrid.ItemsSource = archive.Invoices;
        ArchiveDetailEmptyState.Visibility = Visibility.Collapsed;
        OpenArchivedListButton.IsEnabled = true;
        SetArchiveStatus("Seçili listeyi açtığınızda arşiv çıktısı gösterilir; aktif listeniz değişmez.");
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
