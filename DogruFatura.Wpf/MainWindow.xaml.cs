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
    private static readonly string[] AllInstitutionTypeFilterPrefix = { "Tümü" };

    public ObservableCollection<SchoolRecord> AllSchools { get; }
    public ObservableCollection<InvoiceRecord> ActiveInvoices { get; }
    public ObservableCollection<ActiveInvoiceRow> ActiveInvoiceRows { get; } = new();
    public ObservableCollection<ArchivedInvoiceList> ArchivedLists { get; }
    public ObservableCollection<ControlIssue> ControlIssues { get; } = new();
    public ObservableCollection<DashboardBarItem> HomeInvoiceTypeBars { get; } = new();
    public ObservableCollection<DashboardBarItem> HomeMonthlyTrendBars { get; } = new();

    private readonly TaxCalculationService _taxService;
    private readonly SchoolDataStore _schoolDataStore;
    private readonly InvoiceDataStore _invoiceDataStore;
    private readonly AuthorityProfileDataStore _authorityProfileDataStore;
    private readonly ArchivedInvoiceListDataStore _archiveDataStore;
    private readonly FinalReportDocumentService _finalReportDocumentService;
    private readonly FinalReportPdfService _finalReportPdfService;
    private readonly DocumentArchiveStorageService _documentArchiveStorageService;
    private readonly HistoricalPdfImportService _historicalPdfImportService;
    private readonly ArchivePdfSynchronizationService _archivePdfSynchronizationService;
    private readonly ApplicationSafetyService _safetyService;
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

    internal MainWindow(
        TaxCalculationService taxService,
        SchoolDataStore schoolDataStore,
        InvoiceDataStore invoiceDataStore,
        AuthorityProfileDataStore authorityProfileDataStore,
        ArchivedInvoiceListDataStore archiveDataStore,
        ApplicationSafetyService safetyService,
        FinalReportDocumentService finalReportDocumentService,
        FinalReportPdfService finalReportPdfService,
        DocumentArchiveStorageService documentArchiveStorageService,
        HistoricalPdfImportService historicalPdfImportService,
        ArchivePdfSynchronizationService archivePdfSynchronizationService)
    {
        ArgumentNullException.ThrowIfNull(taxService);
        ArgumentNullException.ThrowIfNull(schoolDataStore);
        ArgumentNullException.ThrowIfNull(invoiceDataStore);
        ArgumentNullException.ThrowIfNull(authorityProfileDataStore);
        ArgumentNullException.ThrowIfNull(archiveDataStore);
        ArgumentNullException.ThrowIfNull(safetyService);
        ArgumentNullException.ThrowIfNull(finalReportDocumentService);
        ArgumentNullException.ThrowIfNull(finalReportPdfService);
        ArgumentNullException.ThrowIfNull(documentArchiveStorageService);
        ArgumentNullException.ThrowIfNull(historicalPdfImportService);
        ArgumentNullException.ThrowIfNull(archivePdfSynchronizationService);

        _taxService = taxService;
        _schoolDataStore = schoolDataStore;
        _invoiceDataStore = invoiceDataStore;
        _authorityProfileDataStore = authorityProfileDataStore;
        _archiveDataStore = archiveDataStore;
        _safetyService = safetyService;
        _finalReportDocumentService = finalReportDocumentService;
        _finalReportPdfService = finalReportPdfService;
        _documentArchiveStorageService = documentArchiveStorageService;
        _historicalPdfImportService = historicalPdfImportService;
        _archivePdfSynchronizationService = archivePdfSynchronizationService;

        _startupSafetyResult = _safetyService.InitializeAndCheck();
        _lastSafetyBackupPath = _startupSafetyResult.BackupPath;

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
        SchoolFilterTypeBox.ItemsSource = AllInstitutionTypeFilterPrefix.Concat(InstitutionTypes).ToArray();
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
}
