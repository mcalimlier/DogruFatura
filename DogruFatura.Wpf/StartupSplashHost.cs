using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;

namespace DogruFatura.Wpf;

/// <summary>
/// Splash penceresini ana WPF thread'inden bağımsız bir STA/Dispatcher thread'inde çalıştırır.
/// Böylece MainWindow hazırlanırken splash animasyonu takılmaz.
/// </summary>
internal sealed class StartupSplashHost : IDisposable
{
    private readonly ManualResetEventSlim _shown = new(false);
    private readonly Stopwatch _visibleStopwatch = new();
    private Thread? _thread;
    private SplashWindow? _window;
    private Exception? _startupException;
    private bool _disposed;

    public void Show()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_thread is not null) return;

        _thread = new Thread(SplashThreadMain)
        {
            IsBackground = true,
            Name = "DoğruFatura Splash"
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();

        if (!_shown.Wait(TimeSpan.FromSeconds(5)))
            throw new TimeoutException("Karşılama ekranı zamanında başlatılamadı.");
        if (_startupException is not null)
            throw new InvalidOperationException("Karşılama ekranı başlatılamadı.", _startupException);
    }

    public async Task CompleteAsync()
    {
        var earliestCompletionStart = Math.Max(
            0,
            StartupProgressProfile.MinimumVisibleDurationMs
            - StartupProgressProfile.CompletionDurationMs
            - 80);
        var remaining = earliestCompletionStart - _visibleStopwatch.ElapsedMilliseconds;
        if (remaining > 0)
            await Task.Delay((int)remaining);

        var window = _window;
        if (window is null) return;

        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        window.Dispatcher.BeginInvoke(new Action(async () =>
        {
            try
            {
                await window.CompleteAndCloseAsync();
                completion.TrySetResult(true);
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
        }));

        await completion.Task;
    }

    public void CloseImmediately()
    {
        var window = _window;
        if (window is null) return;
        try
        {
            window.Dispatcher.BeginInvoke(new Action(window.Close));
        }
        catch
        {
            // Açılış zaten hata yolundaysa splash kapatma hatası ikincildir.
        }
    }

    private void SplashThreadMain()
    {
        try
        {
            var window = new SplashWindow();
            _window = window;
            window.Loaded += (_, _) =>
            {
                _visibleStopwatch.Start();
                _shown.Set();
            };
            window.Closed += (_, _) =>
                Dispatcher.CurrentDispatcher.BeginInvokeShutdown(DispatcherPriority.Background);
            window.Show();
            Dispatcher.Run();
        }
        catch (Exception ex)
        {
            _startupException = ex;
            _shown.Set();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        CloseImmediately();
        _shown.Dispose();
    }
}
