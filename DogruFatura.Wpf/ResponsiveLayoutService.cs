using System.Windows;
using System.Windows.Controls;

namespace DogruFatura.Wpf;

public readonly record struct WindowLayoutProfile(
    double WorkWidthDip,
    double WorkHeightDip,
    double WindowWidthDip,
    double WindowHeightDip,
    double ContentScale,
    bool RequiresDownscale);

public static class ResponsiveLayoutService
{
    public const double DesignWidth = 1450.0;
    public const double DesignHeight = 945.0;
    private const double WorkAreaWidthUsage = 0.96;
    private const double WorkAreaHeightUsage = 0.94;
    private const double PreferredNormalScale = 0.90;
    private const double StartupWorkAreaWidthUsage = 0.94;
    private const double StartupWorkAreaHeightUsage = 0.92;

    public static WindowLayoutProfile Calculate(double workWidthDip, double workHeightDip)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(workWidthDip);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(workHeightDip);

        var availableWidth = workWidthDip * WorkAreaWidthUsage;
        var availableHeight = workHeightDip * WorkAreaHeightUsage;
        var scale = Math.Min(1.0, Math.Min(availableWidth / DesignWidth, availableHeight / DesignHeight));
        scale = Math.Max(0.50, scale);

        return new WindowLayoutProfile(
            workWidthDip,
            workHeightDip,
            Math.Round(DesignWidth * scale, 1),
            Math.Round(DesignHeight * scale, 1),
            scale,
            scale < 0.999);
    }

    public static WindowLayoutProfile CalculateForPixels(double pixelWidth, double pixelHeight, double dpiPercent)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dpiPercent);
        var dpiScale = dpiPercent / 100.0;
        return Calculate(pixelWidth / dpiScale, pixelHeight / dpiScale);
    }

    public static void Apply(MainWindow window)
    {
        ArgumentNullException.ThrowIfNull(window);

        window.ResizeMode = ResizeMode.CanResizeWithGrip;
        window.MinWidth = 760;
        window.MinHeight = 520;

        if (window.FindName("AppScaleViewbox") is Viewbox viewbox)
        {
            // Büyük ekranlarda tasarımı bitmap gibi büyütmeyerek metin keskinliğini koru;
            // küçük ekranlarda ise tüm formu orantılı biçimde aşağı ölçekle.
            viewbox.Stretch = System.Windows.Media.Stretch.Uniform;
            viewbox.StretchDirection = StretchDirection.DownOnly;
            viewbox.SnapsToDevicePixels = true;
            viewbox.UseLayoutRounding = true;
        }

        FitStartupWindowToCurrentWorkArea(window);
        window.SizeChanged += (_, _) =>
        {
            if (window.FindName("AppScaleViewbox") is FrameworkElement content)
            {
                content.SnapsToDevicePixels = true;
                content.UseLayoutRounding = true;
            }
        };
    }

    private static void FitStartupWindowToCurrentWorkArea(Window window)
    {
        var workArea = SystemParameters.WorkArea;
        var maxWidth = Math.Min(DesignWidth * PreferredNormalScale, workArea.Width * StartupWorkAreaWidthUsage);
        var maxHeight = Math.Min(DesignHeight * PreferredNormalScale, workArea.Height * StartupWorkAreaHeightUsage);
        var scale = Math.Min(maxWidth / DesignWidth, maxHeight / DesignHeight);

        window.Width = Math.Round(DesignWidth * scale);
        window.Height = Math.Round(DesignHeight * scale);
        window.Left = workArea.Left + Math.Max(0, (workArea.Width - window.Width) / 2.0);
        window.Top = workArea.Top + Math.Max(0, (workArea.Height - window.Height) / 2.0);
    }
}
