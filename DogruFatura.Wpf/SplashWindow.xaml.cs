using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace DogruFatura.Wpf;

public partial class SplashWindow : Window
{
    private const double ProgressTrackWidth = 395.0;

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
            SetProgress(12, "Arayüz bileşenleri hazırlanıyor...");
            await Dispatcher.Yield(DispatcherPriority.Background);
            await Task.Delay(80);

            SetProgress(35, "Veri güvenliği ve çalışma alanları denetleniyor...");
            await Dispatcher.Yield(DispatcherPriority.Background);
            await Task.Delay(80);

            SetProgress(72, "Kurum, fatura ve arşiv kayıtları yükleniyor...");
            await Dispatcher.Yield(DispatcherPriority.Background);

            var main = new MainWindow();

            SetProgress(92, "PDF belgeleri ve arayüz düzeni doğrulanıyor...");
            UiPolishService.Apply(main);
            await Dispatcher.Yield(DispatcherPriority.Background);

            SetProgress(100, "Hazır. Program açılıyor...");
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
            ApplicationCrashLogger.Write(ex, "Splash.Startup");
            MessageBox.Show(
                "DoğruFatura başlatılamadı:\n\n" + ex.Message,
                "DoğruFatura - Açılış Hatası",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Application.Current.Shutdown(-1);
        }
    }

    private void SetProgress(int percent, string stage)
    {
        var safePercent = Math.Clamp(percent, 0, 100);
        PercentText.Text = $"%{safePercent}";
        StageText.Text = stage;

        var targetWidth = ProgressTrackWidth * safePercent / 100.0;
        ProgressFill.BeginAnimation(
            FrameworkElement.WidthProperty,
            new DoubleAnimation
            {
                To = targetWidth,
                Duration = TimeSpan.FromMilliseconds(180),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            });
    }
}
