using System.Diagnostics;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace DogruFatura.Wpf;

public partial class SplashWindow : Window
{
    private const double ProgressTrackWidth = 395.0;
    private readonly Stopwatch _visibleStopwatch = new();
    private DispatcherTimer? _percentTimer;
    private bool _completionStarted;

    public SplashWindow()
    {
        InitializeComponent();
        Loaded += SplashWindow_Loaded;
        Closed += (_, _) => _percentTimer?.Stop();
    }

    private void SplashWindow_Loaded(object sender, RoutedEventArgs e)
    {
        VersionText.Text = AppVersionInfo.DisplayVersion;
        SpinnerRotate.BeginAnimation(
            System.Windows.Media.RotateTransform.AngleProperty,
            new DoubleAnimation(0, 360, TimeSpan.FromSeconds(0.9))
            {
                RepeatBehavior = RepeatBehavior.Forever
            });

        StageText.Text = "Bileşenler hazırlanıyor...";
        PercentText.Text = "%0";
        ProgressFill.Width = 0;

        var timeline = new DoubleAnimationUsingKeyFrames
        {
            Duration = TimeSpan.FromMilliseconds(
                StartupProgressProfile.CruiseDurationMs + StartupProgressProfile.TailDurationMs),
            FillBehavior = FillBehavior.HoldEnd
        };
        timeline.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        timeline.KeyFrames.Add(new LinearDoubleKeyFrame(
            ProgressTrackWidth * (StartupProgressProfile.CruiseTargetPercent / 100.0),
            KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(StartupProgressProfile.CruiseDurationMs))));
        timeline.KeyFrames.Add(new LinearDoubleKeyFrame(
            ProgressTrackWidth * (StartupProgressProfile.TailTargetPercent / 100.0),
            KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(
                StartupProgressProfile.CruiseDurationMs + StartupProgressProfile.TailDurationMs))));

        ProgressFill.BeginAnimation(
            FrameworkElement.WidthProperty,
            timeline,
            HandoffBehavior.SnapshotAndReplace);

        _visibleStopwatch.Start();
        _percentTimer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(StartupProgressProfile.FrameDelayMs),
            DispatcherPriority.Render,
            (_, _) => UpdateProgressText(),
            Dispatcher);
        _percentTimer.Start();
    }

    private void UpdateProgressText()
    {
        if (_completionStarted) return;

        var elapsed = _visibleStopwatch.ElapsedMilliseconds;
        var percent = StartupProgressProfile.CalculateContinuousPercent(elapsed);
        PercentText.Text = $"%{percent}";

        StageText.Text = elapsed switch
        {
            < 1700 => "Bileşenler hazırlanıyor...",
            < 3400 => "Veriler ve hesaplama kuralları hazırlanıyor...",
            < StartupProgressProfile.CruiseDurationMs => "Arayüz ve belgeler hazırlanıyor...",
            _ => "Son kontroller tamamlanıyor..."
        };
    }

    internal async Task CompleteAndCloseAsync()
    {
        if (_completionStarted) return;
        _completionStarted = true;
        _percentTimer?.Stop();

        var currentWidth = Math.Clamp(ProgressFill.ActualWidth, 0, ProgressTrackWidth);
        var currentPercent = Math.Clamp(
            (int)Math.Round(currentWidth / ProgressTrackWidth * 100.0),
            0,
            99);

        ProgressFill.BeginAnimation(FrameworkElement.WidthProperty, null);
        ProgressFill.Width = currentWidth;
        ProgressFill.BeginAnimation(
            FrameworkElement.WidthProperty,
            new DoubleAnimation(currentWidth, ProgressTrackWidth,
                TimeSpan.FromMilliseconds(StartupProgressProfile.CompletionDurationMs))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                FillBehavior = FillBehavior.HoldEnd
            },
            HandoffBehavior.SnapshotAndReplace);

        StageText.Text = "Hazır. Program açılıyor...";
        var started = Environment.TickCount64;
        while (Environment.TickCount64 - started < StartupProgressProfile.CompletionDurationMs)
        {
            var ratio = Math.Clamp(
                (Environment.TickCount64 - started) / (double)StartupProgressProfile.CompletionDurationMs,
                0.0,
                1.0);
            var percent = currentPercent
                          + (int)Math.Floor((100 - currentPercent) * ratio);
            PercentText.Text = $"%{Math.Min(99, percent)}";
            await Task.Delay(StartupProgressProfile.FrameDelayMs);
        }

        PercentText.Text = "%100";
        await Task.Delay(80);
        Close();
    }
}
