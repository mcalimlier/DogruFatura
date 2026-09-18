using DogruFatura.Core.Services;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

namespace DogruFatura.Wpf;

public partial class App : Application
{
    private const string SingleInstanceMutexName = @"Local\DogruFatura.Desktop.SingleInstance.2D22EB36-5AA7-4AD9-9814-DB03D1DFBFD3";
    private static SingleInstanceLease? _singleInstanceLease;
    private bool _fatalShutdown;

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
            var singleInstance = SingleInstanceGuard.TryAcquire(SingleInstanceMutexName);
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

            try
            {
                RecoverySessionService.Begin();
            }
            catch (Exception ex)
            {
                // Oturum korumasının kendisi programın açılmasını engellemesin.
                ApplicationCrashLogger.Write(ex, "RecoverySession.Begin");
            }

            try
            {
                var appUserModelIdResult = SetCurrentProcessExplicitAppUserModelID("DogruFatura.Wpf.App");
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
            var archivePdfSynchronizationService = new ArchivePdfSynchronizationService(
                documentArchiveStorageService,
                finalReportPdfService.Save);

            var main = new MainWindow(
                taxService,
                new SchoolDataStore(),
                new InvoiceDataStore(),
                new AuthorityProfileDataStore(),
                new ArchivedInvoiceListDataStore(),
                safetyService,
                finalReportDocumentService,
                finalReportPdfService,
                documentArchiveStorageService,
                historicalPdfImportService,
                archivePdfSynchronizationService);
            UiPolishService.Apply(main, safetyService);

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
        }
        catch (Exception ex)
        {
            splashHost?.CloseImmediately();
            splashHost?.Dispose();
            _fatalShutdown = true;
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
        if (!_fatalShutdown)
        {
            try { RecoverySessionService.EndCleanly(); } catch { }
        }
        ReleaseSingleInstanceLease();
        base.OnExit(e);
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _fatalShutdown = true;
        ShowFatalError("DoğruFatura beklenmeyen bir hatayla karşılaştı.", e.Exception);
        e.Handled = true;
        Shutdown(-1);
    }

    private void CurrentDomain_UnhandledException(object? sender, UnhandledExceptionEventArgs e)
    {
        _fatalShutdown = true;
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
