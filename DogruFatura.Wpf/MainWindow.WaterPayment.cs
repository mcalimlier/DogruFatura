using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using System.Windows;
using System.Windows.Controls;

namespace DogruFatura.Wpf;

public partial class MainWindow
{
    private WaterPaymentRecipient CurrentWaterRecipient =>
        !TaxCalculationService.IsWater(InvoiceTypeBox.SelectedItem as string)
            ? WaterPaymentRecipient.Unspecified
            : WaterRecipientBox.SelectedIndex switch
            {
                0 => WaterPaymentRecipient.Municipality,
                1 => WaterPaymentRecipient.Company,
                _ => WaterPaymentRecipient.Unspecified
            };

    private void ApplySchoolWaterRecipient()
    {
        var recipient = (InstitutionBox.SelectedItem as SchoolRecord)?.WaterRecipient
            ?? WaterPaymentRecipient.Unspecified;
        // Existing records use their saved method, even if school defaults changed.
        if (_editedInvoice is not null) recipient = _editedInvoice.WaterRecipient;
        var previous = _suppressFormEvents;
        _suppressFormEvents = true;
        WaterRecipientBox.SelectedIndex = recipient == WaterPaymentRecipient.Company ? 1
            : recipient == WaterPaymentRecipient.Unspecified && _editedInvoice is not null ? 2 : 0;
        _suppressFormEvents = previous;
        UpdateWaterPaymentPresentation();
    }

    private void WaterRecipient_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressFormEvents || VatAmountBox is null) return;
        UpdateWaterPaymentPresentation();
        UpdateCalculation();
        UpdateFormReadiness();
    }

    private void UpdateWaterPaymentPresentation()
    {
        if (WaterRecipientPanel is null || VatAmountBox is null) return;
        WaterRecipientPanel.Visibility = TaxCalculationService.IsWater(InvoiceTypeBox.SelectedItem as string)
            ? Visibility.Visible : Visibility.Collapsed;
        var municipality = CurrentWaterRecipient == WaterPaymentRecipient.Municipality;
        if (TaxPolicyDescription is not null)
            TaxPolicyDescription.Text = municipality
                ? "Belediyeye ödeme: KDV ve damga vergisi sıfırdır."
                : CurrentWaterRecipient == WaterPaymentRecipient.Company
                    ? "Su şirketine ödeme: standart KDV matrahı ve damga hesabı."
                    : TaxCalculationService.IsWater(InvoiceTypeBox.SelectedItem as string)
                        ? "Eski su faturasının kayıtlı hesaplama yöntemi korunur."
                        : "KDV matrahı ve damga vergisi anlık hesaplanır.";
        VatAmountBox.IsEnabled = !municipality;
        if (municipality)
        {
            var previous = _suppressFormEvents;
            _suppressFormEvents = true;
            VatAmountBox.Text = "0,00";
            _suppressFormEvents = previous;
        }
        LegacyWaterRecipientItem.Visibility = _editedInvoice?.WaterRecipient == WaterPaymentRecipient.Unspecified
            ? Visibility.Visible : Visibility.Collapsed;
    }
}
