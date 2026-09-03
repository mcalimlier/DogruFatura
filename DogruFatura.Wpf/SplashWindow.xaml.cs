using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace DogruFatura.Wpf;

public partial class SplashWindow : Window
{
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
            StageText.Text = "Arayüz hazırlanıyor...";
            await Dispatcher.Yield(DispatcherPriority.Background);

            StageText.Text = "Veriler, belgeler ve hesaplama kuralları denetleniyor...";
            await Dispatcher.Yield(DispatcherPriority.Background);

            var main = new MainWindow();
            UiPolishService.Apply(main);

            StageText.Text = "Hazır. Program açılıyor...";
            await Task.Delay(180);

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
}
