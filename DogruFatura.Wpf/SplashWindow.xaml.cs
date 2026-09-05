using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace DogruFatura.Wpf;

public partial class SplashWindow : Window
{
    private const double ProgressTrackWidth = 395.0;
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
            StartContinuousProgress();

            ReportStage(12, "Arayüz bileşenleri hazırlanıyor...");
            await Dispatcher.Yield(DispatcherPriority.Background);
            await Task.Delay(80);

            ReportStage(35, "Veri güvenliği ve çalışma alanları denetleniyor...");
            await Dispatcher.Yield(DispatcherPriority.Background);
            await Task.Delay(80);

            ReportStage(72, "Kurum, fatura ve arşiv kayıtları yükleniyor...");
            await Dispatcher.Yield(DispatcherPriority.Background);

            var main = new MainWindow();

            ReportStage(92, "PDF belgeleri ve arayüz düzeni doğrulanıyor...");
            UiPolishService.Apply(main);
            await Dispatcher.Yield(DispatcherPriority.Background);

            CompleteProgress("Hazır. Program açılıyor...");
            await Task.Delay(220);

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

    private void StartContinuousProgress()
    {
        ProgressFill.Width = 0;
        ProgressFill.BeginAnimation(
            FrameworkElement.WidthProperty,
            new DoubleAnimation
            {
                From = 0,
                To = ProgressTrackWidth * 0.92,
                Duration = TimeSpan.FromSeconds(3.2),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
                FillBehavior = FillBehavior.HoldEnd
            },
            HandoffBehavior.SnapshotAndReplace);
    }

    private void ReportStage(int percent, string stage)
    {
        _reportedPercent = Math.Max(_reportedPercent, Math.Clamp(percent, 0, 92));
        PercentText.Text = $"%{_reportedPercent}";
        StageText.Text = stage;
        Dispatcher.Invoke(() => { }, DispatcherPriority.Render);
    }

    private void CompleteProgress(string stage)
    {
        ProgressFill.BeginAnimation(FrameworkElement.WidthProperty, null);
        var currentWidth = ProgressFill.ActualWidth;
        ProgressFill.Width = currentWidth;
        ProgressFill.BeginAnimation(
            FrameworkElement.WidthProperty,
            new DoubleAnimation(currentWidth, ProgressTrackWidth, TimeSpan.FromMilliseconds(260))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
                FillBehavior = FillBehavior.HoldEnd
            },
            HandoffBehavior.SnapshotAndReplace);

        _reportedPercent = 100;
        PercentText.Text = "%100";
        StageText.Text = stage;
    }
}
