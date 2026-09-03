using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace DogruFatura.Wpf;

public partial class SplashWindow : Window
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(30) };
    private int _progress;

    public SplashWindow()
    {
        InitializeComponent();
        Loaded += SplashWindow_Loaded;
    }

    private void SplashWindow_Loaded(object sender, RoutedEventArgs e)
    {
        SpinnerRotate.BeginAnimation(
            System.Windows.Media.RotateTransform.AngleProperty,
            new DoubleAnimation(0, 360, TimeSpan.FromSeconds(1)) { RepeatBehavior = RepeatBehavior.Forever });

        _timer.Tick += (_, _) =>
        {
            _progress++;
            PercentText.Text = $"%{_progress}";
            ProgressFill.Width = 395 * (_progress / 100.0);
            StageText.Text = _progress switch
            {
                < 25 => "Arayüz bileşenleri hazırlanıyor...",
                < 50 => "Veri güvenliği ve klasörler denetleniyor...",
                < 75 => "Kurum, fatura ve arşiv kayıtları yükleniyor...",
                < 95 => "PDF belgeleri ve hesap motoru doğrulanıyor...",
                _ => "Hazır. Program açılıyor..."
            };

            if (_progress < 100) return;
            _timer.Stop();
            var main = new MainWindow();
            Application.Current.MainWindow = main;
            main.Show();
            Close();
            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
        };
        _timer.Start();
    }
}
