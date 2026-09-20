using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DogruFatura.Wpf;

public partial class MainWindow
{    private void InstitutionTypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressFormEvents) return;
        RefreshInvoiceInstitutionChoices(null);
    }

    private void InstitutionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressFormEvents) return;
        ActiveInstitutionText.Text = InstitutionBox.SelectedItem is SchoolRecord school ? school.Name : "Henüz seçilmedi";
        UpdateAutomaticInstitutionFields();
    }

    private void InvoiceTypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressFormEvents) return;
        UpdateInterruptionCompensationPresentation(resetWhenUnavailable: true);
        UpdateAutomaticInstitutionFields();
        UpdateCalculation();
    }

    private void InterruptionCompensationToggle_Click(object sender, RoutedEventArgs e)
    {
        var invoiceType = InvoiceTypeBox.SelectedItem as string;
        if (!TaxCalculationService.IsInterruptionCompensationAllowed(invoiceType)) return;

        _hasInterruptionCompensation = !_hasInterruptionCompensation;
        if (!_hasInterruptionCompensation)
        {
            var previousSuppression = _suppressFormEvents;
            _suppressFormEvents = true;
            InterruptionCompensationBox.Text = "0,00";
            _suppressFormEvents = previousSuppression;
        }

        UpdateInterruptionCompensationPresentation(resetWhenUnavailable: false);
        UpdateCalculation();
        UpdateFormReadiness();

        if (_hasInterruptionCompensation)
        {
            InterruptionCompensationBox.Focus();
            InterruptionCompensationBox.SelectAll();
        }
    }

    private void UpdateInterruptionCompensationPresentation(bool resetWhenUnavailable)
    {
        if (InterruptionCompensationToggleButton is null || InterruptionCompensationBox is null) return;

        var isElectric = TaxCalculationService.IsInterruptionCompensationAllowed(
            InvoiceTypeBox.SelectedItem as string);
        if (!isElectric && resetWhenUnavailable)
        {
            _hasInterruptionCompensation = false;
            var previousSuppression = _suppressFormEvents;
            _suppressFormEvents = true;
            InterruptionCompensationBox.Text = "0,00";
            _suppressFormEvents = previousSuppression;
        }

        InterruptionCompensationToggleButton.IsEnabled = isElectric;
        InterruptionCompensationToggleButton.Content = !isElectric
            ? "YALNIZ ELEKTRİKTE"
            : _hasInterruptionCompensation
                ? "KESİNTİ TAZMİNATI: VAR"
                : "KESİNTİ TAZMİNATI: YOK";
        InterruptionCompensationToggleButton.Background = new SolidColorBrush(
            _hasInterruptionCompensation && isElectric
                ? Color.FromRgb(183, 121, 31)
                : Color.FromRgb(49, 83, 113));
        InterruptionCompensationBox.IsEnabled = isElectric && _hasInterruptionCompensation;

        if (VatBaseFormulaText is not null)
            VatBaseFormulaText.Text = isElectric && _hasInterruptionCompensation
                ? "Tutar + Tazminat − KDV"
                : "Tutar − KDV";
    }

    private bool TryGetInterruptionCompensation(out decimal amount, out string errorMessage)
    {
        amount = 0m;
        errorMessage = string.Empty;
        if (!_hasInterruptionCompensation) return true;

        if (!TaxCalculationService.IsInterruptionCompensationAllowed(InvoiceTypeBox.SelectedItem as string))
        {
            errorMessage = "Kesinti tazminatı yalnız elektrik faturalarında kullanılabilir.";
            return false;
        }

        if (!NumberParser.TryNonNegativeDecimal(InterruptionCompensationBox.Text, out amount) || amount <= 0m)
        {
            errorMessage = "Kesinti tazminatı etkinse sıfırdan büyük, geçerli bir tutar giriniz.";
            amount = 0m;
            return false;
        }

        return true;
    }

    private void RefreshInvoiceInstitutionChoices(SchoolRecord? preferredSchool)
    {
        var type = InstitutionTypeBox.SelectedItem as string;
        var choices = string.IsNullOrWhiteSpace(type)
            ? Array.Empty<SchoolRecord>()
            : AllSchools.Where(x => x.InstitutionType == type).OrderBy(x => x.Name).ToArray();

        var previousSuppression = _suppressFormEvents;
        _suppressFormEvents = true;
        InstitutionBox.ItemsSource = choices;
        InstitutionBox.SelectedItem = preferredSchool is null
            ? null
            : choices.FirstOrDefault(x => ReferenceEquals(x, preferredSchool) || x == preferredSchool);
        _suppressFormEvents = previousSuppression;

        ActiveInstitutionText.Text = InstitutionBox.SelectedItem is SchoolRecord selected
            ? selected.Name
            : "Henüz seçilmedi";
        UpdateAutomaticInstitutionFields();
    }

    private void UpdateAutomaticInstitutionFields()
    {
        var school = InstitutionBox.SelectedItem as SchoolRecord;
        var invoiceType = InvoiceTypeBox.SelectedItem as string;

        VknBox.Text = school is null ? string.Empty : DisplayIdentifier(school.Vkn);
        var serviceNumber = school?.GetServiceNumber(invoiceType) ?? string.Empty;
        ServiceNumberBox.Text = school is null || string.IsNullOrWhiteSpace(invoiceType)
            ? string.Empty
            : DisplayIdentifier(serviceNumber);

        if (school is null)
        {
            SetServiceStatus("Kurum seçildiğinde VKN bilgisi otomatik gelir.", "#FFB41F");
        }
        else if (string.IsNullOrWhiteSpace(invoiceType))
        {
            SetServiceStatus("Fatura türü seçildiğinde ilgili hizmet numarası otomatik gelir.", "#FFB41F");
        }
        else if (string.IsNullOrWhiteSpace(serviceNumber))
        {
            SetServiceStatus($"{school.Name} için {invoiceType} hizmet numarası kayıtlı değil.", "#FF766F");
        }
        else
        {
            SetServiceStatus($"{invoiceType} hizmet numarası kurum kaydından getirildi ve metin olarak korunuyor.", "#43C77A");
        }

        UpdateFormReadiness();
    }

    private static string DisplayIdentifier(string value) => string.IsNullOrWhiteSpace(value) ? "Kayıtlı değil" : value;

    private static string InvoiceReference(InvoiceRecord invoice) =>
        string.IsNullOrWhiteSpace(invoice.InvoiceNumber)
            ? "Fatura numarası olmayan kayıt"
            : $"{invoice.InvoiceNumber} numaralı fatura";

    private void SetServiceStatus(string message, string color)
    {
        ServiceStatusText.Text = message;
        ServiceStatusIcon.Foreground = (Brush)new BrushConverter().ConvertFromString(color)!;
    }

    private void MoneyInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressFormEvents) return;
        UpdateCalculation();
        UpdateFormReadiness();
    }

    private void FormField_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressFormEvents) return;
        UpdateFormReadiness();
    }

    private void FormDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressFormEvents) return;
        UpdateFormReadiness();
    }

    private void UpdateFormReadiness()
    {
        const int criteriaCount = 6;
        var completed = 0;
        var school = InstitutionBox.SelectedItem as SchoolRecord;
        var invoiceType = InvoiceTypeBox.SelectedItem as string;

        if (school is not null) completed++;
        if (!string.IsNullOrWhiteSpace(invoiceType)) completed++;
        if (school is not null && !string.IsNullOrWhiteSpace(school.GetServiceNumber(invoiceType))) completed++;
        if (InvoiceDatePicker.SelectedDate.HasValue && DueDatePicker.SelectedDate.HasValue &&
            DueDatePicker.SelectedDate.Value.Date >= InvoiceDatePicker.SelectedDate.Value.Date) completed++;

        var payableValid = NumberParser.TryNonNegativeDecimal(PayableAmountBox.Text, out var payable) && payable > 0m;
        var vatValid = NumberParser.TryNonNegativeDecimal(VatAmountBox.Text, out var vat);
        var compensationValid = TryGetInterruptionCompensation(out var compensation, out _);
        if (payableValid) completed++;
        if (payableValid && vatValid && compensationValid
            && _taxService.Calculate(payable, vat, invoiceType, compensation).IsValid) completed++;

        var calculatedPercent = (int)Math.Round(completed * 100.0 / criteriaCount, MidpointRounding.AwayFromZero);
        var listTypeResult = InvoiceListTypePolicy.Check(ActiveInvoices, invoiceType, _editedInvoice?.Id);
        var hasTypeConflict = !string.IsNullOrWhiteSpace(invoiceType) && !listTypeResult.IsAllowed;
        var canSave = calculatedPercent == 100 && listTypeResult.IsAllowed;
        var displayPercent = calculatedPercent == 100 && !listTypeResult.IsAllowed ? 88 : calculatedPercent;

        HeaderReadinessText.Text = $"%{displayPercent}";
        HeaderReadinessArc.Stroke = new SolidColorBrush(canSave
            ? Color.FromRgb(67, 199, 122)
            : Color.FromRgb(255, 180, 31));
        if (AddInvoiceButton is not null && InvoiceSaveStatusText is not null)
        {
            AddInvoiceButton.IsEnabled = canSave;
            AddInvoiceButton.Content = _editedInvoice is null
                ? "FATURA LİSTESİNE EKLE"
                : "FATURA KAYDINI GÜNCELLE";
            if (hasTypeConflict)
            {
                SetInvoiceSaveStatus(listTypeResult.Message, true);
            }
            else
            {
                SetInvoiceSaveStatus(
                    calculatedPercent == 100
                        ? _editedInvoice is null
                            ? "Form hazır. Faturayı aktif listeye ekleyebilirsiniz."
                            : "Form hazır. Seçili fatura kaydını güncelleyebilirsiniz."
                        : "Tüm zorunlu alanlar tamamlandığında kayıt etkinleşir.",
                    false,
                    calculatedPercent == 100);
            }
        }
        UpdateReadinessRing(displayPercent);
    }

    private void UpdateReadinessRing(int percent)
    {
        var boundedPercent = Math.Clamp(percent, 0, 100);
        if (boundedPercent <= 0)
        {
            HeaderReadinessArc.Data = Geometry.Empty;
            return;
        }

        var angle = boundedPercent >= 100 ? 359.99 : 360.0 * boundedPercent / 100.0;
        var radians = (angle - 90.0) * Math.PI / 180.0;
        var x = 24.0 + 20.0 * Math.Cos(radians);
        var y = 24.0 + 20.0 * Math.Sin(radians);
        var largeArc = angle > 180.0 ? 1 : 0;
        HeaderReadinessArc.Data = Geometry.Parse(string.Format(
            CultureInfo.InvariantCulture,
            "M24,4 A20,20 0 {0} 1 {1:0.###},{2:0.###}",
            largeArc,
            x,
            y));
    }

    private void UpdateCalculation()
    {
        UpdateInterruptionCompensationPresentation(resetWhenUnavailable: true);
        UpdateStampDutyPolicyPresentation();
        if (string.IsNullOrWhiteSpace(PayableAmountBox.Text) && string.IsNullOrWhiteSpace(VatAmountBox.Text))
        {
            ResetCalculationDisplay();
            return;
        }

        if (!NumberParser.TryNonNegativeDecimal(PayableAmountBox.Text, out var payable))
        {
            SetCalculationError("Ödenecek tutarı 4.965,10 biçiminde giriniz.");
            return;
        }
        if (!NumberParser.TryNonNegativeDecimal(VatAmountBox.Text, out var vat))
        {
            SetCalculationError("KDV tutarını 827,53 biçiminde giriniz.");
            return;
        }

        if (!TryGetInterruptionCompensation(out var compensation, out var compensationError))
        {
            SetCalculationError(compensationError);
            return;
        }

        var invoiceType = InvoiceTypeBox.SelectedItem as string;
        var result = _taxService.Calculate(payable, vat, invoiceType, compensation);
        if (!result.IsValid)
        {
            SetCalculationError(result.Message);
            return;
        }

        VatBaseText.Text = NumberParser.Money(result.VatBase);
        StampDutyText.Text = NumberParser.Money(result.StampDuty);
        PaymentAmountText.Text = NumberParser.Money(result.PaymentAmount);
        CalculationMessageText.Text = compensation > 0m
            ? "Kesinti tazminatı yalnız KDV matrahına eklendi; ödeme tutarı fatura tutarından damga vergisi düşülerek hesaplandı."
            : TaxCalculationService.IsStampDutyExempt(invoiceType)
                ? "Su faturasına damga vergisi uygulanmadı; ödeme tutarı fatura tutarına eşittir."
                : "Hesaplama doğru. Ara değerler tam hassasiyetle korunur; görünüm 2 ondalıktır.";
        CalculationMessageText.Foreground = new SolidColorBrush(Color.FromRgb(221, 246, 231));
        CalculationStatusBorder.Background = new SolidColorBrush(Color.FromRgb(20, 77, 68));
        CalculationStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(55, 143, 103));
    }

    private void ResetCalculationDisplay()
    {
        VatBaseText.Text = "0,00 TL";
        StampDutyText.Text = "0,00 TL";
        PaymentAmountText.Text = "0,00 TL";
        CalculationMessageText.Text = "Tutar ve KDV girildiğinde hesaplama yapılır.";
        CalculationMessageText.Foreground = new SolidColorBrush(Color.FromRgb(216, 228, 239));
        CalculationStatusBorder.Background = new SolidColorBrush(Color.FromRgb(18, 59, 91));
        CalculationStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(54, 88, 116));
        UpdateStampDutyPolicyPresentation();
    }

    private void UpdateStampDutyPolicyPresentation()
    {
        if (StampDutyFormulaText is null || StampDutyText is null) return;

        var isExempt = TaxCalculationService.IsStampDutyExempt(InvoiceTypeBox.SelectedItem as string);
        StampDutyFormulaText.Text = isExempt
            ? "Su faturasında uygulanmaz"
            : "Matrah × 0,00948";
        StampDutyText.Foreground = new SolidColorBrush(isExempt
            ? Color.FromRgb(67, 199, 122)
            : Color.FromRgb(255, 180, 31));
    }

    private void SetCalculationError(string message)
    {
        VatBaseText.Text = "0,00 TL";
        StampDutyText.Text = "0,00 TL";
        PaymentAmountText.Text = "0,00 TL";
        CalculationMessageText.Text = message;
        CalculationMessageText.Foreground = Brushes.MistyRose;
        CalculationStatusBorder.Background = new SolidColorBrush(Color.FromRgb(104, 38, 49));
        CalculationStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(218, 91, 91));
    }

    private void AddInvoice_Click(object sender, RoutedEventArgs e)
    {
        var school = InstitutionBox.SelectedItem as SchoolRecord;
        var invoiceType = InvoiceTypeBox.SelectedItem as string;
        if (school is null || string.IsNullOrWhiteSpace(invoiceType))
        {
            SetInvoiceSaveStatus("Kurum ve fatura türü seçilmelidir.", true);
            return;
        }

        if (!InvoiceDatePicker.SelectedDate.HasValue || !DueDatePicker.SelectedDate.HasValue)
        {
            SetInvoiceSaveStatus("Fatura ve son ödeme tarihlerini seçin.", true);
            return;
        }

        if (!NumberParser.TryNonNegativeDecimal(PayableAmountBox.Text, out var payable)
            || !NumberParser.TryNonNegativeDecimal(VatAmountBox.Text, out var vat))
        {
            SetInvoiceSaveStatus("Fatura tutarı ve KDV geçerli para biçiminde olmalıdır.", true);
            return;
        }

        if (!TryGetInterruptionCompensation(out var compensation, out var compensationError))
        {
            SetInvoiceSaveStatus(compensationError, true);
            return;
        }

        var calculation = _taxService.Calculate(payable, vat, invoiceType, compensation);
        if (!calculation.IsValid)
        {
            SetInvoiceSaveStatus(calculation.Message, true);
            return;
        }

        var listTypeResult = InvoiceListTypePolicy.Check(ActiveInvoices, invoiceType, _editedInvoice?.Id);
        if (!listTypeResult.IsAllowed)
        {
            SetInvoiceSaveStatus(listTypeResult.Message, true);
            return;
        }

        var editingInvoice = _editedInvoice;
        var invoice = new InvoiceRecord(
            editingInvoice?.Id ?? Guid.NewGuid(),
            editingInvoice?.CreatedAt ?? DateTime.Now,
            school.InstitutionType,
            school.Name,
            school.Vkn,
            invoiceType,
            InvoiceNumberBox.Text.Trim(),
            school.GetServiceNumber(invoiceType),
            InvoiceDatePicker.SelectedDate.Value.Date,
            DueDatePicker.SelectedDate.Value.Date,
            payable,
            vat,
            calculation.VatBase,
            calculation.StampDuty,
            calculation.PaymentAmount,
            compensation,
            school.SchoolId,
            calculation.RuleVersion,
            calculation.RuleEffectiveFrom,
            calculation.AppliedStampDutyRate);

        var validation = InvoiceValidationService.Validate(invoice);
        if (!validation.IsValid)
        {
            SetInvoiceSaveStatus(validation.Message, true);
            return;
        }

        var duplicate = InvoiceDuplicateService.Check(ActiveInvoices, invoice, editingInvoice?.Id);
        if (duplicate.IsDuplicate)
        {
            SetInvoiceSaveStatus("Mükerrer fatura engellendi: " + duplicate.Message, true);
            return;
        }

        var possibleDuplicate = InvoiceDuplicateService.CheckPossibleWhenNumberMissing(
            ActiveInvoices,
            invoice,
            editingInvoice?.Id);
        if (possibleDuplicate.IsPossibleDuplicate)
        {
            var confirmation = MessageBox.Show(
                this,
                "Fatura numarası boş. Aynı kurum/hizmet, fatura türü, tarihler ve tutarlarla aktif listede benzer bir kayıt bulundu.\n\n"
                + possibleDuplicate.Message
                + "\n\nBu faturayı yine de kaydetmek istiyor musunuz?",
                "DoğruFatura - Olası Mükerrer Fatura",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (confirmation != MessageBoxResult.Yes)
            {
                SetInvoiceSaveStatus(
                    "Olası mükerrer fatura uyarısı nedeniyle kayıt kullanıcı tarafından iptal edildi.",
                    true);
                return;
            }
        }

        if (editingInvoice is null)
        {
            ActiveInvoices.Add(invoice);
            if (!TrySaveActiveInvoices(out var errorMessage))
            {
                ActiveInvoices.Remove(invoice);
                SetInvoiceSaveStatus(errorMessage, true);
                return;
            }
        }
        else
        {
            var editedIndex = FindInvoiceIndex(editingInvoice.Id);
            if (editedIndex < 0)
            {
                SetInvoiceSaveStatus("Düzenlenen fatura artık aktif listede bulunmuyor.", true);
                EndInvoiceEditMode();
                return;
            }

            var previous = ActiveInvoices[editedIndex];
            ActiveInvoices[editedIndex] = invoice;
            if (!TrySaveActiveInvoices(out var errorMessage))
            {
                ActiveInvoices[editedIndex] = previous;
                SetInvoiceSaveStatus(errorMessage, true);
                return;
            }
        }

        AppendAudit(
            editingInvoice is null ? AuditTrailEventType.InvoiceCreated : AuditTrailEventType.InvoiceUpdated,
            AuditTrailEntityType.Invoice,
            invoice.Id.ToString("D"),
            $"Type={invoice.InvoiceType};Rule={invoice.TaxRuleVersion}");

        var invoiceReference = InvoiceReference(invoice);
        var successMessage = editingInvoice is null
            ? $"{invoiceReference} aktif listeye kaydedildi."
            : $"{invoiceReference} güncellendi.";
        EndInvoiceEditMode();
        UpdateInvoiceListSummary(invoice.Id);
        ClearInvoiceEntryFieldsAfterSave();
        SetInvoiceSaveStatus(successMessage, false, true);
        SetInvoiceListStatus(successMessage, false, true);

        if (editingInvoice is not null)
            ShowView("List");
    }

    private bool TrySaveActiveInvoices(out string errorMessage)
    {
        try
        {
            _invoiceDataStore.Save(ActiveInvoices);
            errorMessage = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = "Fatura kalıcı dosyaya yazılamadı; kayıt geri alındı: " + ex.Message;
            return false;
        }
    }

    private void ClearInvoiceEntryFieldsAfterSave()
    {
        _suppressFormEvents = true;
        InvoiceNumberBox.Clear();
        InvoiceDatePicker.SelectedDate = null;
        DueDatePicker.SelectedDate = null;
        PayableAmountBox.Clear();
        VatAmountBox.Clear();
        InterruptionCompensationBox.Text = "0,00";
        _hasInterruptionCompensation = false;
        _suppressFormEvents = false;
        UpdateInterruptionCompensationPresentation(resetWhenUnavailable: false);
        ResetCalculationDisplay();
        UpdateFormReadiness();
    }

    private void UpdateInvoiceListSummary(Guid? selectedInvoiceId = null)
    {
        selectedInvoiceId ??= (InvoicesGrid.SelectedItem as ActiveInvoiceRow)?.Invoice.Id;

        ActiveInvoiceRows.Clear();
        for (var index = 0; index < ActiveInvoices.Count; index++)
            ActiveInvoiceRows.Add(new ActiveInvoiceRow(index + 1, ActiveInvoices[index]));

        var count = ActiveInvoices.Count;
        ActiveInvoiceCountText.Text = $"{count} kayıt";
        HomeInvoiceCountText.Text = $"{count} KAYIT";
        var listTypeLabel = InvoiceListTypePolicy.GetListTypeLabel(ActiveInvoices);
        ActiveInvoiceTypeText.Text = $"Tür: {listTypeLabel}";
        HomeInvoiceTypeText.Text = $"Tür: {listTypeLabel}";
        ActivePayableTotalText.Text = NumberParser.Money(ActiveInvoices.Sum(x => x.PayableAmount));
        ActiveStampTotalText.Text = NumberParser.Money(ActiveInvoices.Sum(x => x.StampDuty));
        ActivePaymentTotalText.Text = NumberParser.Money(ActiveInvoices.Sum(x => x.PaymentAmount));
        InvoiceListEmptyState.Visibility = count == 0 ? Visibility.Visible : Visibility.Collapsed;
        ArchiveFromListButton.IsEnabled = count > 0;

        ReportInvoiceCountText.Text = $"{count} kayıt";
        ReportInvoiceTypeText.Text = listTypeLabel;
        ReportPaymentTotalText.Text = NumberParser.Money(ActiveInvoices.Sum(x => x.PaymentAmount));
        _openedArchiveForReport = null;
        InvalidateFinalReportPreview();

        var selectedRow = selectedInvoiceId.HasValue
            ? ActiveInvoiceRows.FirstOrDefault(row => row.Invoice.Id == selectedInvoiceId.Value)
            : null;
        InvoicesGrid.SelectedItem = selectedRow;
        if (selectedRow is not null)
            InvoicesGrid.ScrollIntoView(selectedRow);
        UpdateInvoiceActionState();
        UpdateFormReadiness();
        UpdateArchiveActiveListState();
        RefreshControlCenter();
        RefreshDashboard();
    }
}
