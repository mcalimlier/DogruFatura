using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DogruFatura.Wpf;

/// <summary>
/// Özel başlık çubuğundaki küçült, büyüt/geri yükle ve kapat kontrollerinin
/// aynı optik ölçüde görünmesini sağlar.
/// </summary>
internal static class WindowControlPolishService
{
    private static readonly FontFamily SymbolFont = new("Segoe MDL2 Assets");
    private const double SymbolSize = 16.5;

    public static void Apply(MainWindow window)
    {
        foreach (var button in UiTree.Descendants<Button>(window))
        {
            if (button.Name == "MaximizeRestoreButton")
            {
                StandardizeButton(button);
                if (button.Content is TextBlock maximizeIcon)
                    StandardizeIcon(maximizeIcon);
                continue;
            }

            var tooltip = button.ToolTip as string;
            if (string.Equals(tooltip, "Küçült", StringComparison.Ordinal))
            {
                StandardizeButton(button);
                button.Content = CreateIcon("\uE921");
            }
            else if (string.Equals(tooltip, "Kapat", StringComparison.Ordinal))
            {
                StandardizeButton(button);
                button.Content = CreateIcon("\uE8BB");
            }
        }
    }

    private static void StandardizeButton(Button button)
    {
        button.Width = 44;
        button.Height = 42;
        button.Padding = new Thickness(0);
        button.HorizontalContentAlignment = HorizontalAlignment.Center;
        button.VerticalContentAlignment = VerticalAlignment.Center;
        button.FontSize = SymbolSize;
        button.FontFamily = SymbolFont;
    }

    private static TextBlock CreateIcon(string glyph)
    {
        var icon = new TextBlock { Text = glyph };
        StandardizeIcon(icon);
        return icon;
    }

    private static void StandardizeIcon(TextBlock icon)
    {
        icon.FontFamily = SymbolFont;
        icon.FontSize = SymbolSize;
        icon.FontWeight = FontWeights.Normal;
        icon.HorizontalAlignment = HorizontalAlignment.Center;
        icon.VerticalAlignment = VerticalAlignment.Center;
        icon.TextAlignment = TextAlignment.Center;
    }
}
