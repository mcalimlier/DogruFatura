using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DogruFatura.Wpf;

public partial class MainWindow
{
    private int FindInvoiceIndex(Guid invoiceId)
    {
        for (var index = 0; index < ActiveInvoices.Count; index++)
            if (ActiveInvoices[index].Id == invoiceId)
                return index;
        return -1;
    }

    private void EndInvoiceEditMode()
    {
        _editedInvoice = null;
        if (AddInvoiceButton is not null)
            AddInvoiceButton.Content = "FATURA LİSTESİNE EKLE";
    }

    private void InvoicesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        UpdateInvoiceActionState();

    private void UpdateInvoiceActionState()
    {
        if (EditInvoiceButton is null) return;

        var row = InvoicesGrid.SelectedItem as ActiveInvoiceRow;
        var index = row is null ? -1 : FindInvoiceIndex(row.Invoice.Id);
        var hasSelection = index >= 0;

        EditInvoiceButton.IsEnabled = hasSelection;
        DeleteInvoiceButton.IsEnabled = hasSelection;
        MoveInvoiceUpButton.IsEnabled = hasSelection && index > 0;
        MoveInvoiceDownButton.IsEnabled = hasSelection && index < ActiveInvoices.Count - 1;

        if (!hasSelection)
        {
            SetInvoiceListStatus(
                ActiveInvoices.Count == 0
                    ? "Aktif liste boş. Fatura Girişi ekranından kayıt ekleyebilirsiniz."
                    : "Düzenlemek, silmek veya sırasını değiştirmek için bir fatura seçin.",
                false);
            return;
        }

        SetInvoiceListStatus(
            $"{row!.SequenceNumber}. sıra seçili: {row.Invoice.InstitutionName} • {InvoiceReference(row.Invoice)}",
            false);
    }

    private void EditSelectedInvoice_Click(object sender, RoutedEventArgs e)
    {
        if (InvoicesGrid.SelectedItem is not ActiveInvoiceRow row) return;

        var invoice = row.Invoice;
        var school = invoice.SchoolId != Guid.Empty
            ? AllSchools.FirstOrDefault(candidate => candidate.SchoolId == invoice.SchoolId)
            : AllSchools.FirstOrDefault(candidate =>
                string.Equals(candidate.InstitutionType, invoice.InstitutionType, StringComparison.Ordinal)
                && (CultureInfo.CurrentCulture.CompareInfo.Compare(
                        candidate.Name,
                        invoice.InstitutionName,
                        CompareOptions.IgnoreCase) == 0
                    || (string.Equals(candidate.Vkn, invoice.Vkn, StringComparison.Ordinal)
                        && string.Equals(candidate.GetServiceNumber(invoice.InvoiceType), invoice.ServiceNumber, StringComparison.Ordinal))));

        if (school is null)
        {
            SetInvoiceListStatus(
                "Bu faturanın bağlı olduğu kurum kaydı bulunamadığı için düzenleme açılamadı. Kurum kaydını yeniden ekleyin.",
                true);
            return;
        }

        _editedInvoice = invoice;
        _suppressFormEvents = true;
        InstitutionTypeBox.SelectedItem = school.InstitutionType;
        InstitutionBox.ItemsSource = AllSchools
            .Where(candidate => candidate.InstitutionType == school.InstitutionType)
            .OrderBy(candidate => candidate.Name)
            .ToArray();
        InstitutionBox.SelectedItem = school;
        InvoiceTypeBox.SelectedItem = invoice.InvoiceType;
        InvoiceNumberBox.Text = invoice.InvoiceNumber;
        InvoiceDatePicker.SelectedDate = invoice.InvoiceDate;
        DueDatePicker.SelectedDate = invoice.DueDate;
        PayableAmountBox.Text = NumberParser.MoneyValue(invoice.PayableAmount);
        VatAmountBox.Text = NumberParser.MoneyValue(invoice.VatAmount);
        InterruptionCompensationBox.Text = NumberParser.MoneyValue(invoice.InterruptionCompensationAmount);
        _hasInterruptionCompensation = invoice.InterruptionCompensationAmount > 0m;
        _suppressFormEvents = false;

        ActiveInstitutionText.Text = school.Name;
        UpdateInterruptionCompensationPresentation(resetWhenUnavailable: true);
        UpdateAutomaticInstitutionFields();
        UpdateCalculation();
        UpdateFormReadiness();
        SetInvoiceSaveStatus(
            $"Düzenleme modu: {InvoiceReference(invoice)}. Değişiklikleri tamamlayıp güncelleyin; vazgeçmek için TEMİZLE'yi kullanın.",
            false);
        ShowView("Invoice");
    }

    private void InvoicesGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (InvoicesGrid.SelectedItem is ActiveInvoiceRow)
            EditSelectedInvoice_Click(sender, e);
    }

    private void DeleteSelectedInvoice_Click(object sender, RoutedEventArgs e)
    {
        if (InvoicesGrid.SelectedItem is not ActiveInvoiceRow row) return;

        var invoice = row.Invoice;
        var confirmation = MessageBox.Show(this,
            $"{row.SequenceNumber}. sıradaki {invoice.InstitutionName} / {InvoiceReference(invoice)} aktif listeden silinsin mi?",
            "DoğruFatura - Fatura Silme Onayı",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        if (confirmation != MessageBoxResult.Yes) return;

        var deletedIndex = FindInvoiceIndex(invoice.Id);
        if (deletedIndex < 0) return;

        ActiveInvoices.RemoveAt(deletedIndex);
        if (!TrySaveActiveInvoices(out var errorMessage))
        {
            ActiveInvoices.Insert(Math.Clamp(deletedIndex, 0, ActiveInvoices.Count), invoice);
            UpdateInvoiceListSummary(invoice.Id);
            SetInvoiceListStatus(errorMessage, true);
            return;
        }

        AppendAudit(
            AuditTrailEventType.InvoiceDeleted,
            AuditTrailEntityType.Invoice,
            invoice.Id.ToString("D"),
            $"Type={invoice.InvoiceType}");

        if (_editedInvoice?.Id == invoice.Id)
            EndInvoiceEditMode();
        var nextSelection = ActiveInvoices.Count == 0
            ? (Guid?)null
            : ActiveInvoices[Math.Min(deletedIndex, ActiveInvoices.Count - 1)].Id;
        UpdateInvoiceListSummary(nextSelection);
        SetInvoiceListStatus($"{InvoiceReference(invoice)} aktif listeden silindi.", false, true);
    }

    private void MoveSelectedInvoiceUp_Click(object sender, RoutedEventArgs e) => MoveSelectedInvoice(-1);

    private void MoveSelectedInvoiceDown_Click(object sender, RoutedEventArgs e) => MoveSelectedInvoice(1);

    private void MoveSelectedInvoice(int direction)
    {
        if (InvoicesGrid.SelectedItem is not ActiveInvoiceRow row) return;

        var currentIndex = FindInvoiceIndex(row.Invoice.Id);
        var targetIndex = currentIndex + direction;
        if (currentIndex < 0 || targetIndex < 0 || targetIndex >= ActiveInvoices.Count) return;

        ActiveInvoices.Move(currentIndex, targetIndex);
        if (!TrySaveActiveInvoices(out var errorMessage))
        {
            ActiveInvoices.Move(targetIndex, currentIndex);
            UpdateInvoiceListSummary(row.Invoice.Id);
            SetInvoiceListStatus(errorMessage, true);
            return;
        }

        AppendAudit(
            AuditTrailEventType.InvoiceReordered,
            AuditTrailEntityType.Invoice,
            row.Invoice.Id.ToString("D"),
            $"Position={targetIndex + 1}");

        UpdateInvoiceListSummary(row.Invoice.Id);
        SetInvoiceListStatus(
            $"{InvoiceReference(row.Invoice)} {targetIndex + 1}. sıraya taşındı.",
            false,
            true);
    }

    private void ArchiveActiveListFromList_Click(object sender, RoutedEventArgs e)
    {
        if (!TryArchiveActiveList(true))
            return;

        SetInvoiceListStatus(
            "Aktif fatura listesi işlem arşivine kaydedildi ve aktif liste kapatıldı.",
            false,
            true);
    }

    private void SetInvoiceListStatus(string message, bool isError, bool isSuccess = false)
    {
        InvoiceListStatusText.Text = message;
        if (isError)
        {
            InvoiceListStatusText.Foreground = Brushes.MistyRose;
            InvoiceListStatusBorder.Background = new SolidColorBrush(Color.FromRgb(104, 38, 49));
            InvoiceListStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(218, 91, 91));
        }
        else if (isSuccess)
        {
            InvoiceListStatusText.Foreground = new SolidColorBrush(Color.FromRgb(221, 246, 231));
            InvoiceListStatusBorder.Background = new SolidColorBrush(Color.FromRgb(20, 77, 68));
            InvoiceListStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(55, 143, 103));
        }
        else
        {
            InvoiceListStatusText.Foreground = new SolidColorBrush(Color.FromRgb(216, 228, 239));
            InvoiceListStatusBorder.Background = new SolidColorBrush(Color.FromRgb(18, 59, 91));
            InvoiceListStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(54, 88, 116));
        }
    }

    private void SetInvoiceSaveStatus(string message, bool isError, bool isSuccess = false)
    {
        InvoiceSaveStatusText.Text = message;
        if (isError)
        {
            InvoiceSaveStatusText.Foreground = Brushes.MistyRose;
            InvoiceSaveStatusBorder.Background = new SolidColorBrush(Color.FromRgb(104, 38, 49));
            InvoiceSaveStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(218, 91, 91));
        }
        else if (isSuccess)
        {
            InvoiceSaveStatusText.Foreground = new SolidColorBrush(Color.FromRgb(221, 246, 231));
            InvoiceSaveStatusBorder.Background = new SolidColorBrush(Color.FromRgb(20, 77, 68));
            InvoiceSaveStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(55, 143, 103));
        }
        else
        {
            InvoiceSaveStatusText.Foreground = new SolidColorBrush(Color.FromRgb(216, 228, 239));
            InvoiceSaveStatusBorder.Background = new SolidColorBrush(Color.FromRgb(18, 59, 91));
            InvoiceSaveStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(54, 88, 116));
        }
    }
}
