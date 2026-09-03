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

public partial class MainWindow : Window
{
    private static readonly string[] InstitutionTypes =
        { "İlkokul", "Ortaokul", "Anasınıfı/Anaokulu", "Kurum" };

    public ObservableCollection<SchoolRecord> AllSchools { get; }
    public ObservableCollection<InvoiceRecord> ActiveInvoices { get; }
    public ObservableCollection<ActiveInvoiceRow> ActiveInvoiceRows { get; } = new();
    public ObservableCollection<ArchivedInvoiceList> ArchivedLists { get; }
    public ObservableCollection<ControlIssue> ControlIssues { get; } = new();
    public ObservableCollection<DashboardBarItem> HomeInvoiceTypeBars { get; } = new();
    public ObservableCollection<DashboardBarItem> HomeMonthlyTrendBars { get; } = new();

    private readonly TaxCalculationService _taxService = new();
    private readonly SchoolDataStore _schoolDataStore = new();
    private readonly InvoiceDataStore _invoiceDataStore = new();
    private readonly AuthorityProfileDataStore _authorityProfileDataStore = new();
    private readonly ArchivedInvoiceListDataStore _archiveDataStore = new();
    private readonly FinalReportDocumentService _finalReportDocumentService = new();
    private readonly FinalReportPdfService _finalReportPdfService = new();
    private readonly DocumentArchiveStorageService _documentArchiveStorageService = new();
    private readonly HistoricalPdfImportService _historicalPdfImportService = new();
    private readonly ArchivePdfSynchronizationService _archivePdfSynchronizationService;
    private readonly ApplicationSafetyService _safetyService = new();
    private readonly SafetyCheckResult _startupSafetyResult;
    private readonly ICollectionView _schoolView;
    private readonly ICollectionView _archiveView;
    private readonly ICollectionView _controlView;
    private bool _suppressFormEvents = true;
    private bool _suppressSchoolEvents = true;
    private bool _suppressAuthorityEvents = true;
    private bool _suppressArchiveEvents = true;
    private bool _suppressControlEvents = true;
    private AuthorityProfile _authorityProfile = AuthorityProfileDefaults.Create();
    private SchoolRecord? _editedSchool;
    private InvoiceRecord? _editedInvoice;
    private bool _hasInterruptionCompensation;
    private FixedDocument? _finalReportDocument;
    private FinalReportData? _finalReportData;
    private ArchivedInvoiceList? _openedArchiveForReport;
    private bool _isWholePageReportView;
    private string _currentView = "Home";
    private string? _lastSafetyBackupPath;

    private const double DesignWidth = 1450.0;
    private const double DesignHeight = 945.0;
    private const double PreferredNormalScale = 0.90;
    private const int WmGetMinMaxInfo = 0x0024;
    private const uint MonitorDefaultToNearest = 0x00000002;
    private HwndSource? _windowSource;

    public MainWindow()
    {
        _startupSafetyResult = _safetyService.InitializeAndCheck();
        _lastSafetyBackupPath = _startupSafetyResult.BackupPath;
        _archivePdfSynchronizationService = new ArchivePdfSynchronizationService(
            _documentArchiveStorageService,
            _finalReportPdfService.Save);

        SchoolLoadResult schoolLoadResult;
        try
        {
            schoolLoadResult = _schoolDataStore.LoadOrCreate();
        }
        catch (Exception ex)
        {
            schoolLoadResult = new SchoolLoadResult(
                SchoolSeedData.Create(),
                "Kalıcı okul/kurum verisi açılamadı; bu oturumda başlangıç kayıtları kullanılıyor. " + ex.Message,
                _schoolDataStore.DataFilePath);
        }
        AllSchools = new ObservableCollection<SchoolRecord>(schoolLoadResult.Schools);

        InvoiceLoadResult invoiceLoadResult;
        try
        {
            invoiceLoadResult = _invoiceDataStore.LoadOrCreate();
        }
        catch (Exception ex)
        {
            invoiceLoadResult = new InvoiceLoadResult(
                Array.Empty<InvoiceRecord>(),
                "Kalıcı aktif fatura verisi açılamadı; bu oturumda boş liste kullanılıyor. " + ex.Message,
                _invoiceDataStore.DataFilePath);
        }
        ActiveInvoices = new ObservableCollection<InvoiceRecord>(invoiceLoadResult.Invoices);

        AuthorityProfileLoadResult authorityProfileLoadResult;
        try
        {
            authorityProfileLoadResult = _authorityProfileDataStore.LoadOrCreate();
        }
        catch (Exception ex)
        {
            authorityProfileLoadResult = new AuthorityProfileLoadResult(
                AuthorityProfileDefaults.Create(),
                "Kalıcı yetkili/imza profili açılamadı; bu oturumda varsayılan bilgiler kullanılıyor. " + ex.Message,
                _authorityProfileDataStore.DataFilePath);
        }
        _authorityProfile = authorityProfileLoadResult.Profile;

        ArchivedInvoiceListLoadResult archiveLoadResult;
        try
        {
            archiveLoadResult = _archiveDataStore.LoadOrCreate();
        }
        catch (Exception ex)
        {
            archiveLoadResult = new ArchivedInvoiceListLoadResult(
                Array.Empty<ArchivedInvoiceList>(),
                "Kalıcı işlem arşivi açılamadı; bu oturumda boş arşiv kullanılıyor. " + ex.Message,
                _archiveDataStore.DataFilePath);
        }
        ArchivedLists = new ObservableCollection<ArchivedInvoiceList>(archiveLoadResult.Archives);

        InitializeComponent();
        try
        {
            _documentArchiveStorageService.EnsureDirectories();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                "Masaüstü DoğruFatura belge klasörleri oluşturulamadı:\n\n" + ex.Message,
                "DoğruFatura - Belge Klasörleri",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        SourceInitialized += MainWindow_SourceInitialized;
        DataContext = this;
        ApplyTaskbarIcon();
        ConfigureFixedWindowForCurrentScreen();
        UpdateMaximizeRestoreButton();

        _suppressFormEvents = true;
        InstitutionTypeBox.ItemsSource = InstitutionTypes;
        InvoiceTypeBox.ItemsSource = new[] { "Elektrik", "Telefon", "İnternet", "Su" };
        InstitutionBox.ItemsSource = Array.Empty<SchoolRecord>();
        _suppressFormEvents = false;

        _suppressSchoolEvents = true;
        SchoolFilterTypeBox.ItemsSource = new[] { "Tümü" }.Concat(InstitutionTypes).ToArray();
        SchoolFilterTypeBox.SelectedIndex = 0;
        SchoolTypeEditorBox.ItemsSource = InstitutionTypes;
        _schoolView = CollectionViewSource.GetDefaultView(AllSchools);
        _schoolView.Filter = FilterSchool;
        _schoolView.SortDescriptions.Add(new SortDescription(nameof(SchoolRecord.InstitutionType), ListSortDirection.Ascending));
        _schoolView.SortDescriptions.Add(new SortDescription(nameof(SchoolRecord.Name), ListSortDirection.Ascending));
        SchoolsGrid.ItemsSource = _schoolView;
        _suppressSchoolEvents = false;
        StartNewSchool();
        RefreshSchoolView();
        LoadAuthorityProfileToForm(_authorityProfile);

        _suppressArchiveEvents = true;
        ArchiveTypeFilterBox.ItemsSource = new[] { "Tümü", "Elektrik", "Telefon", "İnternet", "Su" };
        ArchiveTypeFilterBox.SelectedIndex = 0;
        ArchiveStatusFilterBox.ItemsSource = new[] { "Tümü", "Açık", "Kapalı" };
        ArchiveStatusFilterBox.SelectedIndex = 0;
        _archiveView = CollectionViewSource.GetDefaultView(ArchivedLists);
        _archiveView.Filter = FilterArchivedList;
        ArchiveGrid.ItemsSource = _archiveView;
        ArchiveInvoiceDetailGrid.ItemsSource = Array.Empty<InvoiceRecord>();
        _suppressArchiveEvents = false;
        RefreshArchiveView();

        _suppressControlEvents = true;
        ControlSeverityFilterBox.ItemsSource = new[] { "Tümü", "Hata", "Uyarı" };
        ControlSeverityFilterBox.SelectedIndex = 0;
        ControlCategoryFilterBox.ItemsSource = new[]
        {
            "Tümü",
            "Hizmet Numarası",
            "Tarih",
            "Mükerrer Fatura",
            "Tutar / KDV",
            "Liste Türü",
            "Kurum Bilgisi"
        };
        ControlCategoryFilterBox.SelectedIndex = 0;
        _controlView = CollectionViewSource.GetDefaultView(ControlIssues);
        _controlView.Filter = FilterControlIssue;
        ControlIssuesGrid.ItemsSource = _controlView;
        _suppressControlEvents = false;
        RefreshControlCenter();

        var selfTest = EngineSelfTest.Run();
        if (!selfTest.Success)
        {
            EngineStatusDot.Fill = new SolidColorBrush(Color.FromRgb(229, 57, 53));
            EngineStatusText.Text = "Hata";
            EngineStatusText.Foreground = Brushes.MistyRose;
            MessageBox.Show("C# vergi hesap motoru öz testi başarısız:\n\n" + selfTest.Message,
                "DoğruFatura", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        ResetCalculationDisplay();
        UpdateFormReadiness();
        UpdateInvoiceListSummary();
        ShowView("Home");
        RefreshSafetyInformation();
        Dispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() => SynchronizeArchivePdfs(false)));

        if (!string.IsNullOrWhiteSpace(schoolLoadResult.WarningMessage))
        {
            MessageBox.Show(this, schoolLoadResult.WarningMessage, "DoğruFatura - Kurum Verisi",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        if (!string.IsNullOrWhiteSpace(invoiceLoadResult.WarningMessage))
        {
            MessageBox.Show(this, invoiceLoadResult.WarningMessage, "DoğruFatura - Aktif Fatura Verisi",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        if (!string.IsNullOrWhiteSpace(authorityProfileLoadResult.WarningMessage))
        {
            MessageBox.Show(this, authorityProfileLoadResult.WarningMessage, "DoğruFatura - Yetkili Profili",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        if (!string.IsNullOrWhiteSpace(archiveLoadResult.WarningMessage))
        {
            MessageBox.Show(this, archiveLoadResult.WarningMessage, "DoğruFatura - İşlem Arşivi",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        if (!_startupSafetyResult.IsHealthy)
        {
            MessageBox.Show(this,
                _startupSafetyResult.Message,
                "DoğruFatura - Veri Güvenliği",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void ApplyTaskbarIcon()
    {
        try
        {
            Icon = BitmapFrame.Create(new Uri("pack://application:,,,/Assets/AppIcon.ico", UriKind.Absolute));
        }
        catch
        {
            // XAML ve proje simgesi yedek olarak kullanılmaya devam eder.
        }
    }

    private void ConfigureFixedWindowForCurrentScreen()
    {
        var workArea = SystemParameters.WorkArea;
        var maxWidth = Math.Min(DesignWidth * PreferredNormalScale, workArea.Width * 0.94);
        var maxHeight = Math.Min(DesignHeight * PreferredNormalScale, workArea.Height * 0.92);
        var scale = Math.Min(maxWidth / DesignWidth, maxHeight / DesignHeight);
        Width = Math.Round(DesignWidth * scale);
        Height = Math.Round(DesignHeight * scale);
        Left = workArea.Left + (workArea.Width - Width) / 2.0;
        Top = workArea.Top + (workArea.Height - Height) / 2.0;
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        _windowSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        _windowSource?.AddHook(WindowMessageHook);
    }

    private IntPtr WindowMessageHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WmGetMinMaxInfo || lParam == IntPtr.Zero) return IntPtr.Zero;
        var minMax = Marshal.PtrToStructure<MinMaxInfo>(lParam);
        var monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        if (monitor != IntPtr.Zero)
        {
            var monitorInfo = new MonitorInfo { cbSize = Marshal.SizeOf<MonitorInfo>() };
            if (GetMonitorInfo(monitor, ref monitorInfo))
            {
                var work = monitorInfo.rcWork;
                var bounds = monitorInfo.rcMonitor;
                minMax.ptMaxPosition.x = work.left - bounds.left;
                minMax.ptMaxPosition.y = work.top - bounds.top;
                minMax.ptMaxSize.x = work.right - work.left;
                minMax.ptMaxSize.y = work.bottom - work.top;
            }
        }
        Marshal.StructureToPtr(minMax, lParam, false);
        handled = true;
        return IntPtr.Zero;
    }

    private void InstitutionTypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
            compensation);

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

    private void RefreshDashboard()
    {
        if (HomeMonthLabelText is null) return;

        var today = DateTime.Today;
        var tr = CultureInfo.GetCultureInfo("tr-TR");
        var statistics = DashboardStatisticsService.Build(ArchivedLists, today);
        var closedArchiveCount = ArchivedLists.Count(archive => archive.IsStatisticsEligible);
        var monthName = statistics.PeriodStart.ToString("MMMM yyyy", tr).ToUpper(tr);

        HomeMonthLabelText.Text = monthName;
        HomeMonthPaymentText.Text = NumberParser.Money(statistics.MonthlyPaymentTotal);
        HomeMonthlyPeriodText.Text = $"{monthName} MALİ ÖZETİ";
        HomeMonthlyInvoiceCountText.Text = $"{statistics.MonthlyInvoiceCount} kayıt";
        HomeMonthlyPayableText.Text = NumberParser.Money(statistics.MonthlyPayableTotal);
        HomeMonthlyStampText.Text = NumberParser.Money(statistics.MonthlyStampDutyTotal);
        HomeMonthlyPaymentText.Text = NumberParser.Money(statistics.MonthlyPaymentTotal);
        HomeTotalInvoiceSummaryText.Text = $"{statistics.TotalInvoiceCount} toplam fatura";
        HomeTotalInstitutionSummaryText.Text = $"{statistics.TotalInstitutionCount} farklı kurum";
        HomeArchiveCountText.Text = $"{closedArchiveCount} / {ArchivedLists.Count} KAPALI";

        HomeInvoiceTypeBars.Clear();
        var maxTypePayment = statistics.InvoiceTypeDistribution
            .Select(item => item.PaymentTotal)
            .DefaultIfEmpty(0m)
            .Max();
        foreach (var item in statistics.InvoiceTypeDistribution)
        {
            HomeInvoiceTypeBars.Add(new DashboardBarItem(
                item.Label,
                $"{item.InvoiceCount} fatura",
                NumberParser.Money(item.PaymentTotal),
                CalculateDashboardBarWidth(item.PaymentTotal, maxTypePayment, 430),
                DashboardInvoiceTypeBrush(item.Label)));
        }
        HomeInvoiceTypeEmptyText.Visibility = HomeInvoiceTypeBars.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;

        HomeMonthlyTrendBars.Clear();
        var maxMonthlyPayment = statistics.MonthlyTrend
            .Select(item => item.PaymentTotal)
            .DefaultIfEmpty(0m)
            .Max();
        foreach (var item in statistics.MonthlyTrend)
        {
            var isCurrentMonth = item.Month == statistics.PeriodStart;
            HomeMonthlyTrendBars.Add(new DashboardBarItem(
                item.Month.ToString("MMM yy", tr).ToUpper(tr),
                $"{item.InvoiceCount} fatura",
                NumberParser.Money(item.PaymentTotal),
                CalculateDashboardBarWidth(item.PaymentTotal, maxMonthlyPayment, 245),
                new SolidColorBrush(isCurrentMonth
                    ? Color.FromRgb(255, 180, 31)
                    : Color.FromRgb(36, 104, 216))));
        }

    }

    private static double CalculateDashboardBarWidth(decimal value, decimal maximum, double availableWidth)
    {
        if (value <= 0m || maximum <= 0m) return 0d;
        return Math.Max(8d, (double)(value / maximum) * availableWidth);
    }

    private static Brush DashboardInvoiceTypeBrush(string invoiceType) => invoiceType switch
    {
        "Elektrik" => new SolidColorBrush(Color.FromRgb(255, 180, 31)),
        "Telefon" => new SolidColorBrush(Color.FromRgb(36, 104, 216)),
        "İnternet" => new SolidColorBrush(Color.FromRgb(42, 169, 190)),
        "Su" => new SolidColorBrush(Color.FromRgb(67, 199, 122)),
        _ => new SolidColorBrush(Color.FromRgb(145, 171, 192))
    };

    private void InvalidateFinalReportPreview()
    {
        _finalReportDocument = null;
        _finalReportData = null;
        _isWholePageReportView = false;
        UpdateWholePageReportButton();
        FinalReportViewer.Document = null;
        ReportPageCountText.Text = "0 sayfa";
        SetReportToolsEnabled(false);
        ReportEmptyState.Visibility = Visibility.Visible;
    }

    private void SetReportToolsEnabled(bool isEnabled)
    {
        SavePdfButton.IsEnabled = isEnabled;
        PrintReportButton.IsEnabled = isEnabled;
        WholePageReportButton.IsEnabled = isEnabled;
    }

    private bool RefreshFinalReportPreview()
    {
        var reportInvoices = _openedArchiveForReport?.Invoices ?? ActiveInvoices.ToList();
        var reportAuthority = _openedArchiveForReport?.AuthorityProfile ?? _authorityProfile;
        var reportDate = _openedArchiveForReport?.ArchivedAt.Date ?? DateTime.Today;
        var validation = FinalReportValidationService.Validate(reportInvoices);
        ReportInvoiceCountText.Text = $"{reportInvoices.Count} kayıt";
        ReportInvoiceTypeText.Text = InvoiceListTypePolicy.GetListTypeLabel(reportInvoices);
        ReportPaymentTotalText.Text = NumberParser.Money(reportInvoices.Sum(invoice => invoice.PaymentAmount));

        if (!validation.IsValid || validation.InvoiceType is null)
        {
            InvalidateFinalReportPreview();
            SetReportStatus(validation.Message, true);
            return false;
        }

        try
        {
            _finalReportData = new FinalReportData(
                reportInvoices.ToArray(),
                validation.InvoiceType,
                reportDate,
                new ReportSignature(
                    "Gerçekleştirme Görevlisi",
                    reportAuthority.RealizationOfficerName,
                    reportAuthority.RealizationOfficerTitle),
                new ReportSignature(
                    "Harcama Yetkilisi",
                    reportAuthority.SpendingAuthorityName,
                    reportAuthority.SpendingAuthorityTitle));
            _finalReportDocument = _finalReportDocumentService.Build(_finalReportData);
            FinalReportViewer.Document = _finalReportDocument;

            var pageCount = FinalReportPagePlanner.Plan(reportInvoices.Count).Count;
            ReportPageCountText.Text = $"{pageCount} sayfa";
            SetReportToolsEnabled(true);
            ReportEmptyState.Visibility = Visibility.Collapsed;
            SetReportStatus(
                _openedArchiveForReport is null
                    ? validation.Message
                    : $"{_openedArchiveForReport.ArchiveNumber} arşiv kaydı görüntüleniyor; aktif liste değiştirilmedi.",
                false,
                true);
            _isWholePageReportView = false;
            UpdateWholePageReportButton();

            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                try
                {
                    if (!_isWholePageReportView)
                        FinalReportViewer.FitToWidth();
                }
                catch
                {
                    // Belge görüntüleyici henüz ölçülmediyse varsayılan yakınlaştırma kullanılır.
                }
            }));
            return true;
        }
        catch (Exception ex)
        {
            InvalidateFinalReportPreview();
            SetReportStatus("Nihai çıktı önizlemesi oluşturulamadı: " + ex.Message, true);
            return false;
        }
    }

    private bool EnsureFinalReportPreview() =>
        _finalReportDocument is not null && _finalReportData is not null || RefreshFinalReportPreview();

    private void SaveFinalReportPdf_Click(object sender, RoutedEventArgs e)
    {
        if (!RefreshFinalReportPreview() || _finalReportData is null) return;

        try
        {
            var filePath = SaveManagedReportPdf(_finalReportData);
            SetReportStatus($"PDF başarıyla kaydedildi: {filePath}", false, true);
        }
        catch (Exception ex)
        {
            SetReportStatus("PDF kaydedilemedi: " + ex.Message, true);
            MessageBox.Show(this, "PDF kaydedilemedi:\n\n" + ex.Message,
                "DoğruFatura - PDF", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PrintFinalReport_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureFinalReportPreview() || _finalReportDocument is null) return;

        try
        {
            var dialog = new PrintDialog();
            if (dialog.ShowDialog() != true) return;

            var storedPdfPath = SaveManagedReportPdf(_finalReportData!);

            dialog.PrintDocument(
                _finalReportDocument.DocumentPaginator,
                $"DoğruFatura - {_finalReportData?.InvoiceType} Fatura Ödeme Listesi");
            SetReportStatus($"Nihai liste yazdırıldı; PDF kaydı korundu: {storedPdfPath}", false, true);
        }
        catch (Exception ex)
        {
            SetReportStatus("Yazdırma işlemi tamamlanamadı: " + ex.Message, true);
            MessageBox.Show(this, "Yazdırma işlemi tamamlanamadı:\n\n" + ex.Message,
                "DoğruFatura - Yazdır", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string SaveManagedReportPdf(FinalReportData data)
    {
        string filePath;
        if (_openedArchiveForReport is { IsClosed: true } closedArchive
            && !string.IsNullOrWhiteSpace(closedArchive.HysPaymentOrderDocumentNumber))
        {
            filePath = _documentArchiveStorageService.GetArchivedOutputPath(
                closedArchive.HysPaymentOrderDocumentNumber);
            if (File.Exists(filePath)) return filePath;
        }
        else
        {
            filePath = _documentArchiveStorageService.GetActiveOutputPath(
                data.Invoices,
                data.InvoiceType,
                data.ReportDate);
        }

        _finalReportPdfService.Save(data, filePath);
        return filePath;
    }

    private void WholePageFinalReport_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureFinalReportPreview()) return;

        try
        {
            if (_isWholePageReportView)
            {
                FinalReportViewer.FitToWidth();
                _isWholePageReportView = false;
            }
            else
            {
                FinalReportViewer.FitToHeight();
                _isWholePageReportView = true;
            }

            UpdateWholePageReportButton();
        }
        catch (Exception ex)
        {
            _isWholePageReportView = false;
            UpdateWholePageReportButton();
            SetReportStatus("Belge görünümü değiştirilemedi: " + ex.Message, true);
        }
    }

    private void UpdateWholePageReportButton()
    {
        WholePageReportButton.Content = _isWholePageReportView ? "GERİ DÖN" : "TAM SAYFA";
        WholePageReportButton.ToolTip = _isWholePageReportView
            ? "Geniş belge görünümüne dön"
            : "Sayfanın tamamını göster";
    }

    private void SetReportStatus(string message, bool isError, bool isSuccess = false)
    {
        ReportStatusText.Text = message;
        if (isError)
        {
            ReportStatusText.Foreground = Brushes.MistyRose;
            ReportStatusBorder.Background = new SolidColorBrush(Color.FromRgb(104, 38, 49));
            ReportStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(218, 91, 91));
        }
        else if (isSuccess)
        {
            ReportStatusText.Foreground = new SolidColorBrush(Color.FromRgb(221, 246, 231));
            ReportStatusBorder.Background = new SolidColorBrush(Color.FromRgb(20, 77, 68));
            ReportStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(55, 143, 103));
        }
        else
        {
            ReportStatusText.Foreground = new SolidColorBrush(Color.FromRgb(216, 228, 239));
            ReportStatusBorder.Background = new SolidColorBrush(Color.FromRgb(18, 59, 91));
            ReportStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(54, 88, 116));
        }
    }

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
        var school = AllSchools.FirstOrDefault(candidate =>
            string.Equals(candidate.InstitutionType, invoice.InstitutionType, StringComparison.Ordinal)
            && (string.Equals(candidate.Name, invoice.InstitutionName, StringComparison.CurrentCultureIgnoreCase)
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

        UpdateInvoiceListSummary(row.Invoice.Id);
        SetInvoiceListStatus(
            $"{InvoiceReference(row.Invoice)} {targetIndex + 1}. sıraya taşındı.",
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

    private bool FilterSchool(object item)
    {
        if (item is not SchoolRecord school) return false;

        var selectedType = SchoolFilterTypeBox.SelectedItem as string;
        if (!string.IsNullOrWhiteSpace(selectedType) && selectedType != "Tümü"
            && school.InstitutionType != selectedType)
            return false;

        var search = SchoolSearchBox.Text.Trim();
        if (search.Length == 0) return true;

        return school.InstitutionType.Contains(search, StringComparison.CurrentCultureIgnoreCase)
               || school.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase)
               || school.Vkn.Contains(search, StringComparison.OrdinalIgnoreCase)
               || school.ElectricityServiceNumber.Contains(search, StringComparison.OrdinalIgnoreCase)
               || school.TelephoneServiceNumber.Contains(search, StringComparison.OrdinalIgnoreCase)
               || school.InternetServiceNumber.Contains(search, StringComparison.OrdinalIgnoreCase)
               || school.WaterServiceNumber.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    private void SchoolSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressSchoolEvents) return;
        RefreshSchoolView();
    }

    private void SchoolFilterTypeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressSchoolEvents) return;
        RefreshSchoolView();
    }

    private void RefreshSchoolView()
    {
        _schoolView.Refresh();
        var visibleCount = _schoolView.Cast<object>().Count();
        SchoolCountText.Text = $"{visibleCount} / {AllSchools.Count} kayıt";
        HomeSchoolCountText.Text = $"{AllSchools.Count} KURUM";
    }

    private void SchoolsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressSchoolEvents || SchoolsGrid.SelectedItem is not SchoolRecord school) return;
        LoadSchoolIntoEditor(school);
    }

    private void LoadSchoolIntoEditor(SchoolRecord school)
    {
        _editedSchool = school;
        SchoolEditorModeText.Text = $"Düzenleniyor: {school.Name}";
        SchoolTypeEditorBox.SelectedItem = school.InstitutionType;
        SchoolNameEditorBox.Text = school.Name;
        SchoolVknEditorBox.Text = school.Vkn;
        SchoolElectricityEditorBox.Text = school.ElectricityServiceNumber;
        SchoolTelephoneEditorBox.Text = school.TelephoneServiceNumber;
        SchoolInternetEditorBox.Text = school.InternetServiceNumber;
        SchoolWaterEditorBox.Text = school.WaterServiceNumber;
        DeleteSchoolButton.IsEnabled = true;
        SetSchoolFormStatus("Seçili kaydı düzenleyebilir veya silebilirsiniz.", false);
    }

    private void NewSchool_Click(object sender, RoutedEventArgs e) => StartNewSchool();

    private void CancelSchoolEdit_Click(object sender, RoutedEventArgs e) => StartNewSchool();

    private void StartNewSchool()
    {
        _editedSchool = null;
        _suppressSchoolEvents = true;
        SchoolsGrid.SelectedItem = null;
        _suppressSchoolEvents = false;

        SchoolEditorModeText.Text = "Yeni kayıt";
        var filteredType = SchoolFilterTypeBox.SelectedItem as string;
        SchoolTypeEditorBox.SelectedItem = !string.IsNullOrWhiteSpace(filteredType) && filteredType != "Tümü"
            ? filteredType
            : InstitutionTypes[0];
        SchoolNameEditorBox.Clear();
        SchoolVknEditorBox.Clear();
        SchoolElectricityEditorBox.Clear();
        SchoolTelephoneEditorBox.Clear();
        SchoolInternetEditorBox.Clear();
        SchoolWaterEditorBox.Clear();
        DeleteSchoolButton.IsEnabled = false;
        SetSchoolFormStatus("Yeni kayıt için yıldızlı alanları doldurun.", false);
    }

    private SchoolRecord ReadSchoolEditor() => SchoolValidationService.Normalize(new SchoolRecord(
        SchoolTypeEditorBox.SelectedItem as string ?? string.Empty,
        SchoolNameEditorBox.Text,
        SchoolVknEditorBox.Text,
        SchoolElectricityEditorBox.Text,
        SchoolTelephoneEditorBox.Text,
        SchoolInternetEditorBox.Text,
        SchoolWaterEditorBox.Text));

    private void SaveSchool_Click(object sender, RoutedEventArgs e)
    {
        var candidate = ReadSchoolEditor();
        var validation = SchoolValidationService.Validate(candidate, AllSchools, _editedSchool);
        if (!validation.IsValid)
        {
            SetSchoolFormStatus(validation.Message, true);
            return;
        }

        var invoiceSelection = InstitutionBox.SelectedItem as SchoolRecord;
        var editingInvoiceSelection = _editedSchool is not null && ReferenceEquals(invoiceSelection, _editedSchool);

        if (_editedSchool is null)
        {
            AllSchools.Add(candidate);
            if (!TrySaveSchools(out var errorMessage))
            {
                AllSchools.Remove(candidate);
                SetSchoolFormStatus(errorMessage, true);
                RefreshSchoolView();
                return;
            }
        }
        else
        {
            var editedIndex = AllSchools.IndexOf(_editedSchool);
            if (editedIndex < 0)
            {
                SetSchoolFormStatus("Düzenlenen kayıt artık listede bulunmuyor.", true);
                StartNewSchool();
                return;
            }

            var previous = _editedSchool;
            AllSchools[editedIndex] = candidate;
            if (!TrySaveSchools(out var errorMessage))
            {
                AllSchools[editedIndex] = previous;
                SetSchoolFormStatus(errorMessage, true);
                RefreshSchoolView();
                return;
            }
        }

        RefreshSchoolView();
        if (_schoolView.Contains(candidate))
        {
            _suppressSchoolEvents = true;
            SchoolsGrid.SelectedItem = candidate;
            SchoolsGrid.ScrollIntoView(candidate);
            _suppressSchoolEvents = false;
        }
        else
        {
            _suppressSchoolEvents = true;
            SchoolsGrid.SelectedItem = null;
            _suppressSchoolEvents = false;
        }
        LoadSchoolIntoEditor(candidate);
        SetSchoolFormStatus("Kayıt kalıcı olarak kaydedildi.", false, true);
        RefreshControlCenter();

        if (editingInvoiceSelection)
        {
            var previousSuppression = _suppressFormEvents;
            _suppressFormEvents = true;
            InstitutionTypeBox.SelectedItem = candidate.InstitutionType;
            _suppressFormEvents = previousSuppression;
            RefreshInvoiceInstitutionChoices(candidate);
        }
        else
        {
            RefreshInvoiceInstitutionChoices(invoiceSelection);
        }
    }

    private void DeleteSchool_Click(object sender, RoutedEventArgs e)
    {
        if (_editedSchool is null) return;

        var schoolToDelete = _editedSchool;
        var confirmation = MessageBox.Show(this,
            $"'{schoolToDelete.Name}' kaydı kalıcı olarak silinsin mi?",
            "DoğruFatura - Kurum Silme Onayı",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        if (confirmation != MessageBoxResult.Yes) return;

        var invoiceSelection = InstitutionBox.SelectedItem as SchoolRecord;
        var deletedInvoiceSelection = ReferenceEquals(invoiceSelection, schoolToDelete);
        var deletedIndex = AllSchools.IndexOf(schoolToDelete);
        AllSchools.Remove(schoolToDelete);

        if (!TrySaveSchools(out var errorMessage))
        {
            AllSchools.Insert(Math.Clamp(deletedIndex, 0, AllSchools.Count), schoolToDelete);
            SetSchoolFormStatus(errorMessage, true);
            RefreshSchoolView();
            return;
        }

        RefreshSchoolView();
        StartNewSchool();
        SetSchoolFormStatus("Kurum kaydı silindi.", false, true);
        RefreshInvoiceInstitutionChoices(deletedInvoiceSelection ? null : invoiceSelection);
        RefreshControlCenter();
    }

    private bool TrySaveSchools(out string errorMessage)
    {
        try
        {
            _schoolDataStore.Save(AllSchools);
            errorMessage = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = "Kayıt kalıcı dosyaya yazılamadı. Değişiklik geri alındı: " + ex.Message;
            return false;
        }
    }

    private void SetSchoolFormStatus(string message, bool isError, bool isSuccess = false)
    {
        SchoolFormStatusText.Text = message;
        if (isError)
        {
            SchoolFormStatusText.Foreground = Brushes.MistyRose;
            SchoolFormStatusBorder.Background = new SolidColorBrush(Color.FromRgb(104, 38, 49));
            SchoolFormStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(218, 91, 91));
        }
        else if (isSuccess)
        {
            SchoolFormStatusText.Foreground = new SolidColorBrush(Color.FromRgb(221, 246, 231));
            SchoolFormStatusBorder.Background = new SolidColorBrush(Color.FromRgb(20, 77, 68));
            SchoolFormStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(55, 143, 103));
        }
        else
        {
            SchoolFormStatusText.Foreground = new SolidColorBrush(Color.FromRgb(216, 228, 239));
            SchoolFormStatusBorder.Background = new SolidColorBrush(Color.FromRgb(18, 59, 91));
            SchoolFormStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(54, 88, 116));
        }
    }

    private void AuthorityField_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressAuthorityEvents) return;

        UpdateAuthorityProfilePreview();
        var draft = ReadAuthorityProfileForm().Normalize();
        SaveAuthorityProfileButton.IsEnabled = draft.IsComplete;

        if (!draft.IsComplete)
        {
            SetAuthorityStatus("İki yetkilinin ad soyad ve unvan alanları zorunludur.", true);
            return;
        }

        if (draft == _authorityProfile)
        {
            SetAuthorityStatus("Yetkili bilgileri kayıtlı ve nihai çıktıya hazır.", false, true);
            return;
        }

        SetAuthorityStatus("Kaydedilmemiş değişiklikler var. Çıktılara aktarmak için KAYDET'e basın.");
    }

    private void SaveAuthorityProfile_Click(object sender, RoutedEventArgs e)
    {
        var profile = ReadAuthorityProfileForm().Normalize();
        if (!profile.IsComplete)
        {
            SetAuthorityStatus("Kayıt yapılamadı: bütün ad soyad ve unvan alanlarını doldurun.", true);
            return;
        }

        try
        {
            _authorityProfileDataStore.Save(profile);
            _authorityProfile = profile;
            LoadAuthorityProfileToForm(profile);
            InvalidateFinalReportPreview();
            SetAuthorityStatus("Yetkili profili kaydedildi; önizleme, PDF ve yazdırma güncellendi.", false, true);
        }
        catch (Exception ex)
        {
            SetAuthorityStatus("Yetkili profili kaydedilemedi: " + ex.Message, true);
            MessageBox.Show(this, "Yetkili profili kaydedilemedi:\n\n" + ex.Message,
                "DoğruFatura - Yetkili Profili", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RestoreAuthorityDefaults_Click(object sender, RoutedEventArgs e)
    {
        var answer = MessageBox.Show(this,
            "Yetkili bilgileri onaylı başlangıç değerlerine döndürülecek ve kaydedilecek. Devam edilsin mi?",
            "DoğruFatura - Varsayılan Yetkili Profili",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);
        if (answer != MessageBoxResult.Yes) return;

        var defaultProfile = AuthorityProfileDefaults.Create();
        try
        {
            _authorityProfileDataStore.Save(defaultProfile);
            _authorityProfile = defaultProfile;
            LoadAuthorityProfileToForm(defaultProfile);
            InvalidateFinalReportPreview();
            SetAuthorityStatus("Varsayılan yetkili bilgileri geri yüklendi ve kaydedildi.", false, true);
        }
        catch (Exception ex)
        {
            SetAuthorityStatus("Varsayılan bilgiler kaydedilemedi: " + ex.Message, true);
            MessageBox.Show(this, "Varsayılan yetkili profili kaydedilemedi:\n\n" + ex.Message,
                "DoğruFatura - Yetkili Profili", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private AuthorityProfile ReadAuthorityProfileForm() => new(
        RealizationOfficerNameBox.Text,
        RealizationOfficerTitleBox.Text,
        SpendingAuthorityNameBox.Text,
        SpendingAuthorityTitleBox.Text);

    private void LoadAuthorityProfileToForm(AuthorityProfile profile)
    {
        _suppressAuthorityEvents = true;
        RealizationOfficerNameBox.Text = profile.RealizationOfficerName;
        RealizationOfficerTitleBox.Text = profile.RealizationOfficerTitle;
        SpendingAuthorityNameBox.Text = profile.SpendingAuthorityName;
        SpendingAuthorityTitleBox.Text = profile.SpendingAuthorityTitle;
        _suppressAuthorityEvents = false;

        SaveAuthorityProfileButton.IsEnabled = true;
        UpdateAuthorityProfilePreview();
        SetAuthorityStatus("Yetkili bilgileri kayıtlı ve nihai çıktıya hazır.", false, true);
    }

    private void UpdateAuthorityProfilePreview()
    {
        RealizationPreviewNameText.Text = PreviewText(RealizationOfficerNameBox.Text, "Ad soyad");
        RealizationPreviewTitleText.Text = PreviewText(RealizationOfficerTitleBox.Text, "Unvan");
        SpendingPreviewNameText.Text = PreviewText(SpendingAuthorityNameBox.Text, "Ad soyad");
        SpendingPreviewTitleText.Text = PreviewText(SpendingAuthorityTitleBox.Text, "Unvan");
    }

    private static string PreviewText(string value, string placeholder) =>
        string.IsNullOrWhiteSpace(value) ? $"— {placeholder} —" : value.Trim();

    private void SetAuthorityStatus(string message, bool isError = false, bool isSuccess = false)
    {
        AuthorityStatusText.Text = message;
        if (isError)
        {
            AuthorityStatusText.Foreground = Brushes.MistyRose;
            AuthorityStatusBorder.Background = new SolidColorBrush(Color.FromRgb(95, 29, 37));
            AuthorityStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(169, 67, 77));
        }
        else if (isSuccess)
        {
            AuthorityStatusText.Foreground = new SolidColorBrush(Color.FromRgb(221, 246, 231));
            AuthorityStatusBorder.Background = new SolidColorBrush(Color.FromRgb(19, 81, 54));
            AuthorityStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(45, 138, 87));
        }
        else
        {
            AuthorityStatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 231, 179));
            AuthorityStatusBorder.Background = new SolidColorBrush(Color.FromRgb(92, 67, 18));
            AuthorityStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(181, 128, 20));
        }
    }

    private bool FilterControlIssue(object item)
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
                && string.Equals(candidate.Name, issue.SchoolName, StringComparison.CurrentCultureIgnoreCase));
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

    private bool FilterArchivedList(object item)
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
        ArchiveSelectedDateText.Text = archive.ArchivedAt.ToString("dd.MM.yyyy HH:mm");
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

    private void SynchronizeArchivePdfs_Click(object sender, RoutedEventArgs e) =>
        SynchronizeArchivePdfs(true);

    private ArchivePdfSynchronizationResult? SynchronizeArchivePdfs(bool userInitiated)
    {
        ArchivePdfSynchronizationResult result;
        try
        {
            if (userInitiated)
            {
                Mouse.OverrideCursor = Cursors.Wait;
                ArchivePdfSyncButton.IsEnabled = false;
                SetArchiveStatus("Arşiv kayıtları ile PDF klasörleri eşitleniyor...");
            }

            result = _archivePdfSynchronizationService.Synchronize(ArchivedLists.ToList());
        }
        catch (Exception ex)
        {
            SetArchiveStatus("Belge eşitleme başlatılamadı: " + ex.Message, true);
            return null;
        }
        finally
        {
            if (userInitiated)
            {
                Mouse.OverrideCursor = null;
                ArchivePdfSyncButton.IsEnabled = true;
            }
        }

        var replacedArchives = new List<(int Index, ArchivedInvoiceList Original)>();
        foreach (var updatedArchive in result.UpdatedArchives)
        {
            var original = ArchivedLists.FirstOrDefault(archive => archive.Id == updatedArchive.Id);
            if (original is null) continue;
            var index = ArchivedLists.IndexOf(original);
            if (index < 0) continue;
            replacedArchives.Add((index, original));
            ArchivedLists[index] = updatedArchive;
        }

        if (replacedArchives.Count > 0)
        {
            try
            {
                _archiveDataStore.Save(ArchivedLists);
            }
            catch (Exception ex)
            {
                foreach (var replacement in replacedArchives)
                    ArchivedLists[replacement.Index] = replacement.Original;
                RefreshArchiveView();
                SetArchiveStatus(
                    "PDF'ler hazırlandı ancak arşiv bağlantıları kaydedilemedi: " + ex.Message,
                    true);
                return null;
            }
        }

        if (result.HasChanges || result.CreatedCount > 0)
            RefreshArchiveView();

        var summary = $"{result.ScannedCount} arşiv tarandı; "
                      + $"{result.CreatedCount} eksik PDF oluşturuldu, "
                      + $"{result.ExistingCount} PDF hazır bulundu.";
        if (result.Errors.Count > 0)
        {
            SetArchiveStatus(summary + $" {result.Errors.Count} kayıt eşitlenemedi: {result.Errors[0]}", true);
        }
        else if (userInitiated || result.CreatedCount > 0 || result.ReferenceUpdateCount > 0)
        {
            SetArchiveStatus(summary, false, true);
        }

        return result;
    }

    private void ImportHistoricalPdf_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Arşive eklenecek DoğruFatura PDF kayıtlarını seçin",
            Filter = "PDF dosyaları (*.pdf)|*.pdf",
            Multiselect = true,
            CheckFileExists = true,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
        };
        if (dialog.ShowDialog(this) != true || dialog.FileNames.Length == 0) return;

        IReadOnlyList<HistoricalPdfImportPreview> previews;
        try
        {
            Mouse.OverrideCursor = Cursors.Wait;
            SetArchiveStatus($"{dialog.FileNames.Length} PDF okunuyor ve doğrulanıyor...");
            var knownHysNumbers = ArchivedLists
                .Where(archive => !string.IsNullOrWhiteSpace(archive.HysPaymentOrderDocumentNumber))
                .Select(archive => archive.HysPaymentOrderDocumentNumber)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var selectedHysNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var schools = AllSchools.ToList();
            var buffer = new List<HistoricalPdfImportPreview>();
            foreach (var fileName in dialog.FileNames)
            {
                var preview = _historicalPdfImportService.Read(fileName, schools, _authorityProfile);
                if (preview.IsValid && knownHysNumbers.Contains(preview.HysNumber))
                {
                    preview = preview.WithError($"{preview.HysNumber} işlem arşivinde zaten kayıtlı.");
                }
                else if (preview.IsValid && !selectedHysNumbers.Add(preview.HysNumber))
                {
                    preview = preview.WithError($"{preview.HysNumber} seçilen dosyalar içinde birden fazla kez bulunuyor.");
                }
                else if (preview.IsValid)
                {
                    var destinationPath = _documentArchiveStorageService.GetArchivedOutputPath(preview.HysNumber);
                    var isSameFile = string.Equals(
                        Path.GetFullPath(fileName),
                        Path.GetFullPath(destinationPath),
                        StringComparison.OrdinalIgnoreCase);
                    if (File.Exists(destinationPath) && !isSameFile)
                        preview = preview.WithError($"{Path.GetFileName(destinationPath)} Arşivlenenler klasöründe zaten bulunuyor.");
                }

                buffer.Add(preview);
            }
            previews = buffer;
        }
        catch (Exception ex)
        {
            SetArchiveStatus("Arşive eklenecek PDF dosyaları okunamadı: " + ex.Message, true);
            return;
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }

        var importDialog = new HistoricalPdfImportDialog(previews) { Owner = this };
        if (importDialog.ShowDialog() != true) return;

        var validItems = importDialog.ValidItems;
        if (validItems.Count == 0)
        {
            SetArchiveStatus("Aktarılabilecek geçerli PDF bulunamadı.", true);
            return;
        }

        var addedArchives = new List<ArchivedInvoiceList>();
        var createdFiles = new List<string>();
        try
        {
            foreach (var item in validItems)
            {
                var archive = item.Archive!;
                var destinationPath = _documentArchiveStorageService.GetArchivedOutputPath(item.HysNumber);
                var sourceIsDestination = string.Equals(
                    Path.GetFullPath(item.SourcePath),
                    Path.GetFullPath(destinationPath),
                    StringComparison.OrdinalIgnoreCase);
                var destinationExisted = File.Exists(destinationPath);
                var pdfFileName = _documentArchiveStorageService.CopyImportedPdf(item.SourcePath, item.HysNumber);
                if (!sourceIsDestination && !destinationExisted)
                    createdFiles.Add(destinationPath);

                var storedArchive = archive with { PdfFileName = pdfFileName };
                ArchivedLists.Add(storedArchive);
                addedArchives.Add(storedArchive);
            }

            _archiveDataStore.Save(ArchivedLists);
        }
        catch (Exception ex)
        {
            foreach (var archive in addedArchives)
                ArchivedLists.Remove(archive);
            foreach (var createdFile in createdFiles)
            {
                try
                {
                    if (File.Exists(createdFile)) File.Delete(createdFile);
                }
                catch
                {
                    // Yalnız bu aktarımda oluşturulan dosyalar geri alınmaya çalışılır.
                }
            }

            RefreshArchiveView();
            SetArchiveStatus("Arşiv ekleme işlemi tamamlanamadı: " + ex.Message, true);
            MessageBox.Show(this,
                "PDF kayıtları arşive eklenirken işlem geri alındı:\n\n" + ex.Message,
                "DoğruFatura - Arşiv Ekle",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        var firstArchive = addedArchives.First();
        RefreshArchiveView(firstArchive.Id);
        RefreshDashboard();
        SetArchiveStatus(
            $"{addedArchives.Count} PDF, HYS ile kapalı arşiv kaydı olarak eklendi.",
            false,
            true);
    }

    private void ArchiveFilter_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressArchiveEvents) return;
        ApplyArchiveFilters();
    }

    private void ArchiveFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressArchiveEvents) return;
        ApplyArchiveFilters();
    }

    private void ApplyArchiveFilters()
    {
        var from = ArchiveDateFromPicker.SelectedDate?.Date;
        var to = ArchiveDateToPicker.SelectedDate?.Date;
        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            _archiveView.Refresh();
            ArchiveVisibleCountText.Text = $"0 / {ArchivedLists.Count} kayıt";
            ArchiveListEmptyState.Visibility = Visibility.Visible;
            ArchiveGrid.SelectedItem = null;
            UpdateArchiveSelection();
            SetArchiveStatus("Başlangıç tarihi, bitiş tarihinden sonra olamaz.", true);
            return;
        }

        RefreshArchiveView();
        SetArchiveStatus("Arşiv filtreleri uygulandı.", false, true);
    }

    private void ClearArchiveFilters_Click(object sender, RoutedEventArgs e)
    {
        _suppressArchiveEvents = true;
        ArchiveSearchBox.Clear();
        ArchiveTypeFilterBox.SelectedIndex = 0;
        ArchiveStatusFilterBox.SelectedIndex = 0;
        ArchiveDateFromPicker.SelectedDate = null;
        ArchiveDateToPicker.SelectedDate = null;
        _suppressArchiveEvents = false;
        RefreshArchiveView();
        SetArchiveStatus("Arşiv filtreleri temizlendi.", false, true);
    }

    private void ArchiveGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        UpdateArchiveSelection();

    private void ArchiveGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ArchiveGrid.SelectedItem is ArchivedInvoiceList)
            OpenArchivedList_Click(sender, e);
    }

    private void ArchiveClosure_Click(object sender, RoutedEventArgs e)
    {
        if (ArchiveGrid.SelectedItem is not ArchivedInvoiceList archive)
        {
            SetArchiveStatus("Kapatmak için arşivden bir liste seçin.", true);
            return;
        }

        ArchivedInvoiceList updated;
        var pdfMove = PdfMoveResult.None(archive.PdfFileName);
        string successMessage;
        if (archive.IsClosed)
        {
            var confirmation = MessageBox.Show(this,
                $"{archive.HysPaymentOrderDocumentNumber} numaralı arşiv kapanışı geri alınacak.\n\n"
                + "Kayıt Ana Ekran istatistiklerinden çıkarılır; fatura ve HYS bilgileri korunur. Devam edilsin mi?",
                "DoğruFatura - Arşiv Kapanışını Geri Al",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);
            if (confirmation != MessageBoxResult.Yes) return;

            try
            {
                pdfMove = _documentArchiveStorageService.MoveArchivedOutputToActive(archive);
            }
            catch (Exception ex)
            {
                SetArchiveStatus("Arşiv PDF'si Aktif Çıktılar klasörüne taşınamadı: " + ex.Message, true);
                return;
            }

            updated = archive with
            {
                IsClosed = false,
                ClosedAt = null,
                PdfFileName = pdfMove.PdfFileName
            };
            successMessage = pdfMove.Moved
                ? $"{archive.HysPaymentOrderDocumentNumber} kapanışı geri alındı; PDF Aktif Çıktılar klasörüne taşındı."
                : $"{archive.HysPaymentOrderDocumentNumber} kapanışı geri alındı; kayıt Ana Ekran istatistiklerinden çıkarıldı.";
        }
        else
        {
            if (!HysPaymentOrderService.TryNormalize(
                    ArchiveHysNumberBox.Text,
                    out var normalizedHysNumber,
                    out var errorMessage))
            {
                SetArchiveStatus(errorMessage, true);
                ArchiveHysNumberBox.Focus();
                return;
            }

            if (ArchivedLists.Any(candidate => candidate.Id != archive.Id
                                               && string.Equals(
                                                   candidate.HysPaymentOrderDocumentNumber,
                                                   normalizedHysNumber,
                                                   StringComparison.OrdinalIgnoreCase)))
            {
                SetArchiveStatus($"{normalizedHysNumber} başka bir arşiv kaydında kullanılıyor.", true);
                return;
            }

            try
            {
                pdfMove = _documentArchiveStorageService.MoveActiveOutputToArchive(
                    archive,
                    normalizedHysNumber);
            }
            catch (Exception ex)
            {
                SetArchiveStatus("Aktif PDF arşiv klasörüne taşınamadı: " + ex.Message, true);
                return;
            }

            updated = archive with
            {
                HysPaymentOrderDocumentNumber = normalizedHysNumber,
                IsClosed = true,
                ClosedAt = DateTime.Now,
                PdfFileName = pdfMove.PdfFileName
            };
            successMessage = pdfMove.Moved
                ? $"{normalizedHysNumber} kaydedildi; PDF Arşivlenenler klasörüne taşınıp HYS koduyla adlandırıldı."
                : $"{normalizedHysNumber} kaydedildi; arşiv kapatıldı. Bu listeye ait kayıtlı PDF bulunamadı.";
        }

        var validation = ArchivedInvoiceListValidationService.Validate(updated);
        if (!validation.IsValid)
        {
            DocumentArchiveStorageService.RollbackMove(pdfMove);
            SetArchiveStatus("Arşiv kapanışı kaydedilemedi: " + validation.Message, true);
            return;
        }

        var index = ArchivedLists.IndexOf(archive);
        if (index < 0)
        {
            DocumentArchiveStorageService.RollbackMove(pdfMove);
            return;
        }
        ArchivedLists[index] = updated;
        try
        {
            _archiveDataStore.Save(ArchivedLists);
        }
        catch (Exception ex)
        {
            ArchivedLists[index] = archive;
            DocumentArchiveStorageService.RollbackMove(pdfMove);
            RefreshArchiveView(archive.Id);
            SetArchiveStatus("Arşiv kapanışı kaydedilemedi: " + ex.Message, true);
            return;
        }

        var synchronization = SynchronizeArchivePdfs(false);
        var synchronizedArchive = ArchivedLists.FirstOrDefault(candidate => candidate.Id == updated.Id);
        if (!pdfMove.Moved
            && synchronization is not null
            && synchronizedArchive is not null
            && !string.IsNullOrWhiteSpace(synchronizedArchive.PdfFileName))
        {
            successMessage = updated.IsClosed
                ? $"{updated.HysPaymentOrderDocumentNumber} kaydedildi; eksik PDF Arşivlenenler klasöründe oluşturuldu."
                : $"{updated.HysPaymentOrderDocumentNumber} kapanışı geri alındı; eksik PDF Aktif Çıktılar klasöründe oluşturuldu.";
        }

        RefreshArchiveView(updated.Id);
        RefreshDashboard();
        if (synchronizedArchive is null || string.IsNullOrWhiteSpace(synchronizedArchive.PdfFileName))
            SetArchiveStatus(successMessage + " Belge eşitleme daha sonra EŞİTLE düğmesiyle yeniden denenebilir.", true);
        else if (synchronization is { Errors.Count: > 0 })
            SetArchiveStatus(successMessage + $" Diğer {synchronization.Errors.Count} arşiv belgesi eşitlenemedi; EŞİTLE ile yeniden deneyebilirsiniz.", true);
        else
            SetArchiveStatus(successMessage, false, true);
    }

    private void ArchiveActiveList_Click(object sender, RoutedEventArgs e) =>
        TryArchiveActiveList(true);

    private bool TryArchiveActiveList(bool askConfirmation)
    {
        var reportValidation = FinalReportValidationService.Validate(ActiveInvoices);
        if (!reportValidation.IsValid || reportValidation.InvoiceType is null)
        {
            SetArchiveStatus(reportValidation.Message, true);
            return false;
        }

        if (askConfirmation)
        {
            var confirmation = MessageBox.Show(this,
                $"{ActiveInvoices.Count} adet {reportValidation.InvoiceType} faturası işlem arşivine açık kayıt olarak kaydedilecek.\n\n"
                + "Kayıt, HYS Ödeme Emri Belge No girilip kapatılana kadar Ana Ekran istatistiklerine katılmaz. "
                + "Başarılı kayıttan sonra aktif liste temizlenecek. Devam edilsin mi?",
                "DoğruFatura - Aktif Listeyi Arşivle",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);
            if (confirmation != MessageBoxResult.Yes) return false;
        }

        var archivedAt = DateTime.Now;
        var activePdfPath = _documentArchiveStorageService.FindActiveOutputPath(ActiveInvoices.ToList());
        var archive = new ArchivedInvoiceList(
            Guid.NewGuid(),
            CreateArchiveNumber(archivedAt),
            archivedAt,
            reportValidation.InvoiceType,
            ActiveInvoices.ToList(),
            _authorityProfile.Normalize(),
            PdfFileName: activePdfPath is null ? string.Empty : Path.GetFileName(activePdfPath));
        var archiveValidation = ArchivedInvoiceListValidationService.Validate(archive);
        if (!archiveValidation.IsValid)
        {
            SetArchiveStatus("Aktif liste arşivlenemedi: " + archiveValidation.Message, true);
            return false;
        }

        ArchivedLists.Insert(0, archive);
        try
        {
            _archiveDataStore.Save(ArchivedLists);
        }
        catch (Exception ex)
        {
            ArchivedLists.Remove(archive);
            RefreshArchiveView();
            SetArchiveStatus("İşlem arşivine kaydedilemedi: " + ex.Message, true);
            MessageBox.Show(this, "Aktif liste işlem arşivine kaydedilemedi:\n\n" + ex.Message,
                "DoğruFatura - İşlem Arşivi", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        try
        {
            _invoiceDataStore.Save(Array.Empty<InvoiceRecord>());
        }
        catch (Exception ex)
        {
            ArchivedLists.Remove(archive);
            var rollbackMessage = string.Empty;
            try
            {
                _archiveDataStore.Save(ArchivedLists);
            }
            catch
            {
                ArchivedLists.Insert(0, archive);
                rollbackMessage = " Arşiv kopyası güvenlik amacıyla korunmuştur.";
            }

            RefreshArchiveView();
            SetArchiveStatus("Aktif liste temizlenemedi; işlem tamamlanmadı." + rollbackMessage, true);
            MessageBox.Show(this,
                "Aktif liste kalıcı dosyada temizlenemedi. Mevcut faturalar korunmuştur.\n\n" + ex.Message + rollbackMessage,
                "DoğruFatura - İşlem Arşivi", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        ActiveInvoices.Clear();
        EndInvoiceEditMode();
        ResetInvoiceEntryAfterArchive();
        UpdateInvoiceListSummary();
        var synchronization = SynchronizeArchivePdfs(false);
        var synchronizedArchive = ArchivedLists.FirstOrDefault(candidate => candidate.Id == archive.Id);
        RefreshArchiveView(archive.Id);
        if (synchronizedArchive is null || string.IsNullOrWhiteSpace(synchronizedArchive.PdfFileName))
        {
            SetArchiveStatus(
                $"{archive.ArchiveNumber} açık arşiv olarak kaydedildi ancak PDF eşitlenemedi. EŞİTLE düğmesiyle yeniden deneyebilirsiniz.",
                true);
        }
        else if (synchronization is { Errors.Count: > 0 })
        {
            SetArchiveStatus(
                $"{archive.ArchiveNumber} açık arşiv ve PDF belgesi kaydedildi. Diğer {synchronization.Errors.Count} arşiv belgesi eşitlenemedi; EŞİTLE ile yeniden deneyebilirsiniz.",
                true);
        }
        else
        {
            SetArchiveStatus(
                $"{archive.ArchiveNumber} açık arşiv ve PDF belgesi kaydedildi. HYS Ödeme Emri Belge No girerek kaydı kapatabilirsiniz.",
                false,
                true);
        }
        return true;
    }

    private string CreateArchiveNumber(DateTime archivedAt)
    {
        var baseNumber = $"DF-{archivedAt:yyyyMMdd-HHmmss}";
        var candidate = baseNumber;
        var suffix = 2;
        while (ArchivedLists.Any(archive => string.Equals(
                   archive.ArchiveNumber,
                   candidate,
                   StringComparison.OrdinalIgnoreCase)))
            candidate = $"{baseNumber}-{suffix++}";
        return candidate;
    }

    private void ResetInvoiceEntryAfterArchive()
    {
        _suppressFormEvents = true;
        InstitutionTypeBox.SelectedIndex = -1;
        InstitutionBox.ItemsSource = Array.Empty<SchoolRecord>();
        InstitutionBox.SelectedIndex = -1;
        InvoiceTypeBox.SelectedIndex = -1;
        InvoiceNumberBox.Clear();
        VknBox.Clear();
        ServiceNumberBox.Clear();
        InvoiceDatePicker.SelectedDate = null;
        DueDatePicker.SelectedDate = null;
        PayableAmountBox.Clear();
        VatAmountBox.Clear();
        InterruptionCompensationBox.Text = "0,00";
        _hasInterruptionCompensation = false;
        _suppressFormEvents = false;

        ActiveInstitutionText.Text = "Henüz seçilmedi";
        SetServiceStatus("Kurum ve fatura türü seçildiğinde hizmet numarası otomatik gelir.", "#FFB41F");
        UpdateInterruptionCompensationPresentation(resetWhenUnavailable: true);
        ResetCalculationDisplay();
        UpdateFormReadiness();
    }

    private void OpenArchivedList_Click(object sender, RoutedEventArgs e)
    {
        if (ArchiveGrid.SelectedItem is not ArchivedInvoiceList archive)
        {
            SetArchiveStatus("Açmak için arşivden bir liste seçin.", true);
            return;
        }

        _openedArchiveForReport = archive;
        ShowView("Reports");
        RefreshFinalReportPreview();
    }

    private void SetArchiveStatus(string message, bool isError = false, bool isSuccess = false)
    {
        ArchiveStatusText.Text = message;
        if (isError)
        {
            ArchiveStatusText.Foreground = Brushes.MistyRose;
            ArchiveStatusBorder.Background = new SolidColorBrush(Color.FromRgb(95, 29, 37));
            ArchiveStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(169, 67, 77));
        }
        else if (isSuccess)
        {
            ArchiveStatusText.Foreground = new SolidColorBrush(Color.FromRgb(221, 246, 231));
            ArchiveStatusBorder.Background = new SolidColorBrush(Color.FromRgb(19, 81, 54));
            ArchiveStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(45, 138, 87));
        }
        else
        {
            ArchiveStatusText.Foreground = new SolidColorBrush(Color.FromRgb(216, 228, 239));
            ArchiveStatusBorder.Background = new SolidColorBrush(Color.FromRgb(18, 59, 91));
            ArchiveStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(54, 88, 116));
        }
    }

    private void ClearInvoiceForm_Click(object sender, RoutedEventArgs e)
    {
        var cancelledEdit = _editedInvoice is not null;
        EndInvoiceEditMode();
        _suppressFormEvents = true;
        InstitutionTypeBox.SelectedIndex = -1;
        InstitutionBox.ItemsSource = Array.Empty<SchoolRecord>();
        InstitutionBox.SelectedIndex = -1;
        InvoiceTypeBox.SelectedIndex = -1;
        InvoiceNumberBox.Clear();
        VknBox.Clear();
        ServiceNumberBox.Clear();
        InvoiceDatePicker.SelectedDate = null;
        DueDatePicker.SelectedDate = null;
        PayableAmountBox.Clear();
        VatAmountBox.Clear();
        InterruptionCompensationBox.Text = "0,00";
        _hasInterruptionCompensation = false;
        _suppressFormEvents = false;

        ActiveInstitutionText.Text = "Henüz seçilmedi";
        SetServiceStatus("Kurum ve fatura türü seçildiğinde hizmet numarası otomatik gelir.", "#FFB41F");
        UpdateInterruptionCompensationPresentation(resetWhenUnavailable: true);
        ResetCalculationDisplay();
        UpdateFormReadiness();
        if (cancelledEdit)
            SetInvoiceSaveStatus("Fatura düzenleme işlemi iptal edildi; form temizlendi.", false, true);
        ShowView("Invoice");
    }

    private void ShowView(string view)
    {
        if (view == "Home") RefreshDashboard();
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
        AboutView.Visibility = view == "About" ? Visibility.Visible : Visibility.Collapsed;

        NavHome.Tag = view == "Home" ? "Active" : null;
        NavInvoice.Tag = view == "Invoice" ? "Active" : null;
        NavSchools.Tag = view == "Schools" ? "Active" : null;
        NavOfficials.Tag = view == "Officials" ? "Active" : null;
        NavList.Tag = view == "List" ? "Active" : null;
        NavReports.Tag = view == "Reports" ? "Active" : null;
        NavArchive.Tag = view == "Archive" ? "Active" : null;
        NavControl.Tag = view == "Control" ? "Active" : null;
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
    private void NavAbout_Click(object sender, RoutedEventArgs e) => ShowView("About");

    private SafetyCheckResult RefreshSafetyInformation()
    {
        AboutVersionText.Text = "DoğruFatura " + _safetyService.InformationalVersion;
        AboutRuntimeText.Text = _safetyService.RuntimeDescription;
        AboutDataFolderText.Text = _safetyService.DataFolder;
        AboutDocumentFolderText.Text = _safetyService.DocumentRootFolder;
        AboutBackupFolderText.Text = _safetyService.BackupFolder;

        SafetyCheckResult result;
        try
        {
            result = _safetyService.CheckIntegrity();
        }
        catch (Exception ex)
        {
            result = new SafetyCheckResult(
                false,
                "Güvenlik kontrolü çalıştırılamadı: " + ex.Message,
                null,
                1,
                0,
                0);
        }

        DataIntegrityText.Text = result.Message;
        DataIntegrityText.Foreground = new SolidColorBrush(result.IsHealthy
            ? Color.FromRgb(98, 217, 155)
            : Color.FromRgb(255, 180, 31));
        LastSafetyBackupText.Text = string.IsNullOrWhiteSpace(_lastSafetyBackupPath)
            ? "Bu sürümde henüz yeni güvenlik yedeği oluşturulmadı."
            : "Son güvenlik yedeği: " + _lastSafetyBackupPath;
        return result;
    }

    private void RunIntegrityCheck_Click(object sender, RoutedEventArgs e)
    {
        var result = RefreshSafetyInformation();
        if (result.IsHealthy) return;

        MessageBox.Show(this,
            DataIntegrityText.Text,
            "DoğruFatura - Veri Bütünlüğü",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private void CreateSafetyBackup_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Mouse.OverrideCursor = Cursors.Wait;
            _lastSafetyBackupPath = _safetyService.CreateManualSafetyBackup();
            RefreshSafetyInformation();
            MessageBox.Show(this,
                "JSON kayıtları ile aktif/arşiv PDF belgelerinin tam güvenlik yedeği oluşturuldu:\n\n"
                + _lastSafetyBackupPath,
                "DoğruFatura - Güvenlik Yedeği",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                "Güvenlik yedeği oluşturulamadı:\n\n" + ex.Message,
                "DoğruFatura - Güvenlik Yedeği",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private void RestoreSafetyBackup_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "DoğruFatura Güvenlik Yedeğini Seç",
            Filter = "DoğruFatura ZIP yedeği (*.zip)|*.zip|Tüm dosyalar (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false,
            InitialDirectory = _safetyService.BackupFolder
        };
        if (dialog.ShowDialog(this) != true) return;

        var confirmation = MessageBox.Show(this,
            "Seçilen tam yedek geri yüklenecek. Mevcut JSON kayıtları ve Masaüstündeki DoğruFatura PDF klasörleri değiştirilmeden önce otomatik emniyet kopyası oluşturulacaktır.\n\n"
            + "Formlarda henüz kaydedilmemiş değişiklikler kaybolur. Devam edilsin mi?",
            "DoğruFatura - Yedekten Geri Yükle",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        if (confirmation != MessageBoxResult.Yes) return;

        SafetyRestoreResult result;
        try
        {
            Mouse.OverrideCursor = Cursors.Wait;
            result = _safetyService.RestoreFromBackup(dialog.FileName);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }

        if (!result.Success)
        {
            MessageBox.Show(this,
                result.Message,
                "DoğruFatura - Yedekten Geri Yükle",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        _lastSafetyBackupPath = result.SafetyBackupPath;
        var message = result.Message;
        if (!string.IsNullOrWhiteSpace(result.SafetyBackupPath))
            message += "\n\nGeri yükleme öncesi emniyet kopyası:\n" + result.SafetyBackupPath;
        message += "\n\nYeni verilerin güvenli biçimde yüklenmesi için DoğruFatura yeniden başlatılacak.";
        MessageBox.Show(this,
            message,
            "DoğruFatura - Yedekten Geri Yükle",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        RestartApplication();
    }

    private void OpenDataFolder_Click(object sender, RoutedEventArgs e) => OpenFolder(_safetyService.DataFolder);
    private void OpenDocumentFolder_Click(object sender, RoutedEventArgs e) => OpenFolder(_safetyService.DocumentRootFolder);
    private void OpenBackupFolder_Click(object sender, RoutedEventArgs e) => OpenFolder(_safetyService.BackupFolder);
    private void OpenLogFolder_Click(object sender, RoutedEventArgs e) => OpenFolder(_safetyService.LogFolder);

    private static void OpenFolder(string folder)
    {
        try
        {
            Directory.CreateDirectory(folder);
            Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Klasör açılamadı:\n\n" + ex.Message,
                "DoğruFatura",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private static void RestartApplication()
    {
        try
        {
            var executablePath = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(executablePath) && File.Exists(executablePath))
            {
                App.ReleaseSingleInstanceForRestart();
                Process.Start(new ProcessStartInfo
                {
                    FileName = executablePath,
                    UseShellExecute = true
                });
            }
        }
        catch
        {
            // Veriler geri yüklendi; yeniden açma başarısızsa kullanıcı programı normal yoldan açabilir.
        }
        Application.Current.Shutdown();
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximizeRestore();
            e.Handled = true;
            return;
        }
        if (WindowState == WindowState.Normal && e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void MaximizeRestore_Click(object sender, RoutedEventArgs e) => ToggleMaximizeRestore();
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void ToggleMaximizeRestore()
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        UpdateMaximizeRestoreButton();
        Dispatcher.BeginInvoke(new Action(() =>
        {
            AppScaleViewbox.InvalidateMeasure();
            AppScaleViewbox.InvalidateArrange();
        }));
    }

    private void UpdateMaximizeRestoreButton()
    {
        if (MaximizeRestoreIcon is null || MaximizeRestoreButton is null) return;
        var maximized = WindowState == WindowState.Maximized;
        MaximizeRestoreIcon.Text = maximized ? "\uE923" : "\uE922";
        MaximizeRestoreButton.ToolTip = maximized ? "Geri yükle" : "Tam ekran";
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo monitorInfo);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint { public int x; public int y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        public NativePoint ptReserved;
        public NativePoint ptMaxSize;
        public NativePoint ptMaxPosition;
        public NativePoint ptMinTrackSize;
        public NativePoint ptMaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MonitorInfo
    {
        public int cbSize;
        public NativeRect rcMonitor;
        public NativeRect rcWork;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect { public int left; public int top; public int right; public int bottom; }
}
