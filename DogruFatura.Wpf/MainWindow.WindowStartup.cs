using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace DogruFatura.Wpf;

public partial class MainWindow
{
private void ApplyTaskbarIcon()
    {
        try
        {
            Icon = BitmapFrame.Create(new Uri("pack://application:,,,/Assets/AppIcon.ico", UriKind.Absolute));
        }
        catch
        {
            // XAML ve proje simgesi yedek olarak kullanılmaya devam eder.
        }
    }

    private void ConfigureFixedWindowForCurrentScreen()
    {
        var workArea = SystemParameters.WorkArea;
        var designWidth = ResponsiveLayoutService.DesignWidth;
        var designHeight = ResponsiveLayoutService.DesignHeight;
        var maxWidth = Math.Min(designWidth * PreferredNormalScale, workArea.Width * 0.94);
        var maxHeight = Math.Min(designHeight * PreferredNormalScale, workArea.Height * 0.92);
        var scale = Math.Min(maxWidth / designWidth, maxHeight / designHeight);
        Width = Math.Round(designWidth * scale);
        Height = Math.Round(designHeight * scale);
        Left = workArea.Left + (workArea.Width - Width) / 2.0;
        Top = workArea.Top + (workArea.Height - Height) / 2.0;
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        _windowSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        _windowSource?.AddHook(WindowMessageHook);
    }

    private IntPtr WindowMessageHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WmGetMinMaxInfo || lParam == IntPtr.Zero) return IntPtr.Zero;
        var minMax = Marshal.PtrToStructure<MinMaxInfo>(lParam);
        var monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        if (monitor != IntPtr.Zero)
        {
            var monitorInfo = new MonitorInfo { cbSize = Marshal.SizeOf<MonitorInfo>() };
            if (GetMonitorInfo(monitor, ref monitorInfo))
            {
                var work = monitorInfo.rcWork;
                var bounds = monitorInfo.rcMonitor;
                minMax.ptMaxPosition.x = work.left - bounds.left;
                minMax.ptMaxPosition.y = work.top - bounds.top;
                minMax.ptMaxSize.x = work.right - work.left;
                minMax.ptMaxSize.y = work.bottom - work.top;
            }
        }
        Marshal.StructureToPtr(minMax, lParam, false);
        handled = true;
        return IntPtr.Zero;
    }
}
