using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DogruFatura.Wpf;

internal static class UiPerformanceService
{
    private static readonly Dictionary<string, string> EmptyMessages =
        new(StringComparer.Ordinal)
        {
            ["ActiveInvoiceGrid"] = "Aktif listede henüz fatura yok.",
            ["SchoolsGrid"] = "Bu filtreye uyan okul / kurum bulunamadı.",
            ["ArchiveGrid"] = "Bu filtreye uyan arşiv kaydı bulunamadı.",
            ["ArchiveInvoiceDetailGrid"] = "Görüntülenecek fatura detayı yok.",
            ["ControlIssuesGrid"] = "Kontrol bulgusu yok. Kayıtlar temiz görünüyor."
        };

    public static void Apply(MainWindow window)
    {
        foreach (var grid in UiTree.Descendants<DataGrid>(window))
        {
            grid.EnableRowVirtualization = true;
            grid.EnableColumnVirtualization = true;
            VirtualizingPanel.SetIsVirtualizing(grid, true);
            VirtualizingPanel.SetVirtualizationMode(grid, VirtualizationMode.Recycling);
            ScrollViewer.SetCanContentScroll(grid, true);

            if (EmptyMessages.TryGetValue(grid.Name, out var message))
                AttachEmptyState(grid, message);
        }
    }

    private static void AttachEmptyState(DataGrid grid, string message)
    {
        var normalBackground = grid.Background;
        void Refresh()
        {
            if (grid.Items.Count > 0)
            {
                grid.Background = normalBackground;
                return;
            }

            var text = new TextBlock
            {
                Text = message,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5F768B")),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Width = 430,
                Padding = new Thickness(14)
            };
            TextOptions.SetTextFormattingMode(text, TextFormattingMode.Display);
            TextOptions.SetTextRenderingMode(text, TextRenderingMode.ClearType);

            grid.Background = new VisualBrush(text)
            {
                Stretch = Stretch.None,
                AlignmentX = AlignmentX.Center,
                AlignmentY = AlignmentY.Center
            };
        }

        grid.Loaded += (_, _) => Refresh();
        if (grid.Items is INotifyCollectionChanged observable)
            observable.CollectionChanged += (_, _) => Refresh();
    }
}
