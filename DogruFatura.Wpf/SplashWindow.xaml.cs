using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace DogruFatura.Wpf;

public partial class SplashWindow : Window
{
    private const double ProgressTrackWidth = 395.0;
    private const double PreloadPercent = 86.0;
    private int _reportedPercent;

    public SplashWindow()
    {
        InitializeComponent();
        Loaded += SplashWindow_Loaded;
    }

    private async void SplashWindow_Loaded(object sender, RoutedEventArgs e)
    {
        VersionText.Text = AppVersionInfo.DisplayVersion;
        SpinnerRotate.BeginAnimation(
            System.Windows.Media.RotateTransform.AngleProperty,
            new DoubleAnimation(0, 360, TimeSpan.FromSeconds(1)) { RepeatBehavior = RepeatBehavior.Forever });

        try
        {
            ReportStage(8, "DoğruFatura başlatılıyor...");
            await Dispatcher.Yield(DispatcherPriority.Render);

            // Ağır MainWindow kurulumu başlamadan önce barın görünür kısmını gerçekten
            // kesintisiz çalıştır. Böylece UI iş parçacığı meşgul olduğunda bar sıçramaz.
            await RunSmoothPreloadAsync();

            ReportStage(86, "Veriler, belgeler ve hesaplama kuralları hazırlanıyor...");
            var main = new MainWindow();

            ReportStage(94, "PDF belgeleri ve arayüz düzeni doğrulanıyor...");
            UiPolishService.Apply(main);
            await Dispatcher.Yield(DispatcherPriority.Render);

            CompleteProgress("Hazır. Program açılıyor...");
            await Task.Delay(360);

            Application.Current.MainWindow = main;
            main.Show();
            StartupPerformanceService.Complete();
            Close();
            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
        }
        catch (Exception ex)
        {
            StageText.Text = "Program başlatılamadı.";
            var path = ApplicationCrashLogger.Write(ex, "Splash.Startup");
            MessageBox.Show(
                "DoğruFatura başlatılamadı. Teknik ayrıntılar güvenlik amacıyla ekranda gösterilmez.\n\nHata günlüğü:\n" + path,
                "DoğruFatura - Açılış Hatası",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Application.Current.Shutdown(-1);
        }
    }

    private async Task RunSmoothPreloadAsync()
    {
        ProgressFill.BeginAnimation(FrameworkElement.WidthProperty, null);
        ProgressFill.Width = 0;

        ProgressFill.BeginAnimation(
            FrameworkElement.WidthProperty,
            new DoubleAnimation
            {
                From = 0,
                To = ProgressTrackWidth * (PreloadPercent / 100.0),
                Duration = TimeSpan.FromMilliseconds(2200),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut },
                FillBehavior = FillBehavior.HoldEnd
            },
            HandoffBehavior.SnapshotAndReplace);

        var start = Environment.TickCount64;
        while (Environment.TickCount64 - start < 2200)
        {
            var elapsed = Math.Clamp((Environment.TickCount64 - start) / 2200.0, 0.0, 1.0);
            var percent = 8 + (int)Math.Round((PreloadPercent - 8) * elapsed);
            if (percent > _reportedPercent)
            {
                _reportedPercent = percent;
                PercentText.Text = $"%{percent}";
            }
            await Task.Delay(32);
        }

        ProgressFill.BeginAnimation(FrameworkElement.WidthProperty, null);
        ProgressFill.Width = ProgressTrackWidth * (PreloadPercent / 100.0);
        _reportedPercent = (int)PreloadPercent;
        PercentText.Text = $"%{_reportedPercent}";
    }

    private void ReportStage(int percent, string stage)
    {
        _reportedPercent = Math.Max(_reportedPercent, Math.Clamp(percent, 0, 96));
        PercentText.Text = $"%{_reportedPercent}";
        StageText.Text = stage;
        Dispatcher.Invoke(() => { }, DispatcherPriority.Render);
    }

    private void CompleteProgress(string stage)
    {
        ProgressFill.BeginAnimation(FrameworkElement.WidthProperty, null);
        var currentWidth = ProgressFill.Width;
        ProgressFill.BeginAnimation(
            FrameworkElement.WidthProperty,
            new DoubleAnimation(currentWidth, ProgressTrackWidth, TimeSpan.FromMilliseconds(340))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                FillBehavior = FillBehavior.HoldEnd
            },
            HandoffBehavior.SnapshotAndReplace);

        _reportedPercent = 100;
        PercentText.Text = "%100";
        StageText.Text = stage;
    }
}
