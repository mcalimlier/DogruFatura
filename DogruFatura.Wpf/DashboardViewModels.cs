using System.Windows.Media;

namespace DogruFatura.Wpf;

public sealed record DashboardBarItem(
    string Label,
    string CountLabel,
    string AmountLabel,
    double BarWidth,
    Brush BarBrush);
