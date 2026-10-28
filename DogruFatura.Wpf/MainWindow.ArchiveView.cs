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

        if (archive.IsClosed != (ArchiveStateTabs.SelectedIndex == 1)) return false;

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
        if (selectedArchiveId.HasValue && ArchivedLists.FirstOrDefault(a => a.Id == selectedArchiveId.Value) is { } target)
        {
            _suppressArchiveEvents = true;
            ArchiveStateTabs.SelectedIndex = target.IsClosed ? 1 : 0;
            _suppressArchiveEvents = false;
        }
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


    }



}
