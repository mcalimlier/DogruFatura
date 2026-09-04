using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace DogruFatura.Wpf;

public partial class App : Application
{
    private const string SingleInstanceMutexName = @"Local\DogruFatura.Desktop.SingleInstance.2D22EB36-5AA7-4AD9-9814-DB03D1DFBFD3";
    private static Mutex? _singleInstanceMutex;
    private static bool _ownsMutex;
    private bool _fatalShutdown;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SetCurrentProcessExplicitAppUserModelID(string appId);

    public App()
    {
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
    }

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        StartupPerformanceService.Begin();
        try
        {
            if (!TryAcquireSingleInstance())
            {
                MessageBox.Show(
                    "DoğruFatura zaten açık. Aynı anda ikinci bir DoğruFatura penceresi çalıştırılmaz.",
                    "DoğruFatura",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                Shutdown(0);
                return;
            }

            try
            {
                RecoverySessionService.Begin();
            }
            catch (Exception ex)
            {
                // Oturum korumasının kendisi programın açılmasını engellemesin.
                ApplicationCrashLogger.Write(ex, "RecoverySession.Begin");
            }

            try { SetCurrentProcessExplicitAppUserModelID("DogruFatura.Wpf.App"); } catch { }
            new SplashWindow().Show();
        }
        catch (Exception ex)
        {
            _fatalShutdown = true;
            ShowFatalError("Program açılışı sırasında hata oluştu.", ex);
            Shutdown(-1);
        }
    }

    private static bool TryAcquireSingleInstance()
    {
        try
        {
            _singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out var createdNew);
            _ownsMutex = createdNew;
            if (!createdNew)
            {
                _singleInstanceMutex.Dispose();
                _singleInstanceMutex = null;
            }
            return createdNew;
        }
        catch
        {
            // Nadir bir Windows oturum/izin sorunu programı tamamen kullanılamaz hale getirmesin.
            _singleInstanceMutex = null;
            _ownsMutex = false;
            return true;
        }
    }

    internal static void ReleaseSingleInstanceForRestart() => ReleaseSingleInstanceMutex();

    private static void ReleaseSingleInstanceMutex()
    {
        if (_singleInstanceMutex is null) return;
        try
        {
            if (_ownsMutex) _singleInstanceMutex.ReleaseMutex();
        }
        catch { }
        finally
        {
            _ownsMutex = false;
            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (!_fatalShutdown)
        {
            try { RecoverySessionService.EndCleanly(); } catch { }
        }
        ReleaseSingleInstanceMutex();
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
