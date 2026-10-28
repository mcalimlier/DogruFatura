using DogruFatura.Core.Services;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

namespace DogruFatura.Wpf;

public partial class App : Application
{
    private const string SingleInstanceMutexName = @"Local\DogruFatura.Desktop.SingleInstance.2D22EB36-5AA7-4AD9-9814-DB03D1DFBFD3";
    private static SingleInstanceLease? _singleInstanceLease;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(string appId);

    public App()
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        StartupPerformanceService.Begin();
        StartupSplashHost? splashHost = null;

        try
        {
            var singleInstance = SingleInstanceGuard.TryAcquire(SingleInstanceMutexName + (RuntimeInstallationProfile.IsDistribution ? ".Distribution" : ""));
            if (singleInstance.Status == SingleInstanceAcquireStatus.AlreadyRunning)
            {
                MessageBox.Show(
                    "DoğruFatura zaten açık. Aynı anda ikinci bir DoğruFatura penceresi çalıştırılmaz.",
                    "DoğruFatura",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                Shutdown(0);
                return;
            }

            if (!singleInstance.IsAcquired)
            {
                MessageBox.Show(
                    "DoğruFatura güvenli yazma kilidini oluşturamadığı için başlatılmadı. "
                    + "Veri dosyalarını korumak amacıyla uygulama güvenli biçimde kapatılacaktır.\n\n"
                    + singleInstance.Message,
                    "DoğruFatura - Güvenli Başlatma",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(-2);
                return;
            }

            _singleInstanceLease = singleInstance.Lease;

            InitialUserDataService.Initialize(
                System.IO.Path.Combine(AppContext.BaseDirectory, "InitialData"),
                RuntimeInstallationProfile.RootDirectory);

            try
            {
                var appUserModelIdResult = SetCurrentProcessExplicitAppUserModelID(RuntimeInstallationProfile.IsDistribution ? "DogruFatura.Wpf.Distribution" : "DogruFatura.Wpf.App");
                if (appUserModelIdResult < 0)
                    Marshal.ThrowExceptionForHR(appUserModelIdResult);
            }
            catch
            {
                // Taskbar kimliği uygulanamasa da uygulama güvenle çalışmaya devam edebilir.
            }

            // Splash ayrı STA/Dispatcher thread'inde çalışır. Böylece MainWindow kurulumu
            // ana UI thread'ini meşgul etse bile karşılama animasyonu akıcı kalır.
            splashHost = new StartupSplashHost();
            splashHost.Show();

            var taxService = new TaxCalculationService();
            var finalReportDocumentService = new FinalReportDocumentService();
            var finalReportPdfService = new FinalReportPdfService();
            var documentArchiveStorageService = new DocumentArchiveStorageService();
            var historicalPdfImportService = new HistoricalPdfImportService(taxService);
            var safetyService = new ApplicationSafetyService();
            var schoolDataStore = new SchoolDataStore();
            var invoiceDataStore = new InvoiceDataStore();
            var authorityProfileDataStore = new AuthorityProfileDataStore();
            var archiveDataStore = new ArchivedInvoiceListDataStore();
            var auditTrailStore = new AuditTrailStore();
            var transactionJournalStore = new TransactionJournalStore();
            var archivePdfSynchronizationService = new ArchivePdfSynchronizationService(
                documentArchiveStorageService,
                finalReportPdfService.Save);
            var startupState = MainWindowStartupStateLoader.Load(
                safetyService,
                schoolDataStore,
                invoiceDataStore,
                authorityProfileDataStore,
                archiveDataStore);

            var main = new MainWindow(
                taxService,
                schoolDataStore,
                invoiceDataStore,
                authorityProfileDataStore,
                archiveDataStore,
                safetyService,
                finalReportDocumentService,
                finalReportPdfService,
                documentArchiveStorageService,
                historicalPdfImportService,
                archivePdfSynchronizationService,
                auditTrailStore,
                transactionJournalStore,
                startupState);
            UiPolishService.Apply(main);

            // Performans metriği yalnız gerçek hazırlık süresini ölçer; kullanıcı isteğiyle
            // eklenen 5,5 saniyelik görsel splash süresi performans gerilemesi sayılmaz.
            StartupPerformanceService.Complete();

            // Splash en az yaklaşık 5,5 saniye görünür kalır ve son %100 geçişini tamamlar.
            await splashHost.CompleteAsync();

            MainWindow = main;
            main.Show();
            ShutdownMode = ShutdownMode.OnMainWindowClose;

            splashHost.Dispose();
            splashHost = null;
            var setupMessage = InitialSetupNotice.GetMessage(main.AllSchools.Count, startupState.AuthorityProfileLoadResult.Profile.IsComplete);
            if (setupMessage is not null)
                main.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
                    MessageBox.Show(main, setupMessage, "İlk Kullanım", MessageBoxButton.OK, MessageBoxImage.Information)));
        }
        catch (Exception ex)
        {
            splashHost?.CloseImmediately();
            splashHost?.Dispose();
            ShowFatalError("Program açılışı sırasında hata oluştu.", ex);
            Shutdown(-1);
        }
    }

    internal static void ReleaseSingleInstanceForRestart() => ReleaseSingleInstanceLease();

    private static void ReleaseSingleInstanceLease()
    {
        var lease = Interlocked.Exchange(ref _singleInstanceLease, null);
        lease?.Dispose();
    }

    protected override void OnExit(ExitEventArgs e)
    {

        ReleaseSingleInstanceLease();
        base.OnExit(e);
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ShowFatalError("DoğruFatura beklenmeyen bir hatayla karşılaştı.", e.Exception);
        e.Handled = true;
        Shutdown(-1);
    }

    private void CurrentDomain_UnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            ApplicationCrashLogger.Write(ex, "AppDomain.UnhandledException");
    }

    private static void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        ApplicationCrashLogger.Write(e.Exception, "TaskScheduler.UnobservedTaskException");
        e.SetObserved();
    }

    private static void ShowFatalError(string title, Exception ex)
    {
        var path = ApplicationCrashLogger.Write(ex, "WPF.Dispatcher");
        MessageBox.Show(
            title + "\n\nTeknik ayrıntılar güvenlik amacıyla ekranda gösterilmez. Hata günlüğü:\n" + path,
            "DoğruFatura - Hata",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
