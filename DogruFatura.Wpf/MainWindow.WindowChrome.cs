using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

namespace DogruFatura.Wpf;

public partial class MainWindow
{
private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximizeRestore();
            e.Handled = true;
            return;
        }
        if (WindowState == WindowState.Normal && e.ButtonState == MouseButtonState.Pressed) DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void MaximizeRestore_Click(object sender, RoutedEventArgs e) => ToggleMaximizeRestore();
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void ToggleMaximizeRestore()
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        UpdateMaximizeRestoreButton();
        Dispatcher.BeginInvoke(new Action(() =>
        {
            AppScaleViewbox.InvalidateMeasure();
            AppScaleViewbox.InvalidateArrange();
        }));
    }

    private void UpdateMaximizeRestoreButton()
    {
        if (MaximizeRestoreIcon is null || MaximizeRestoreButton is null) return;
        var maximized = WindowState == WindowState.Maximized;
        MaximizeRestoreIcon.Text = maximized ? "\uE923" : "\uE922";
        MaximizeRestoreButton.ToolTip = maximized ? "Geri yükle" : "Tam ekran";
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo monitorInfo);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint { public int x; public int y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MinMaxInfo
    {
        public NativePoint ptReserved;
        public NativePoint ptMaxSize;
        public NativePoint ptMaxPosition;
        public NativePoint ptMinTrackSize;
        public NativePoint ptMaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MonitorInfo
    {
        public int cbSize;
        public NativeRect rcMonitor;
        public NativeRect rcWork;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect { public int left; public int top; public int right; public int bottom; }
}
