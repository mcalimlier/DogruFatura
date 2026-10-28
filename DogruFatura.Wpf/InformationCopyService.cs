using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DogruFatura.Wpf;

internal static class InformationCopyService
{
    public static void Apply(Window window)
    {
        foreach (var text in UiTree.Descendants<TextBlock>(window))
        {
            if (text.FontFamily.Source.Contains("MDL2", StringComparison.Ordinal) || UiTree.FindAncestor<Button>(text) is not null || text.ContextMenu is not null) continue;
            var menu = new ContextMenu { Background = new SolidColorBrush(Color.FromRgb(8,39,68)), Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(200,139,9)), BorderThickness = new Thickness(1) };
            var copy = new MenuItem { Header = "Metni Kopyala" };
            copy.Click += (_, _) =>
            {
                try { if (!string.IsNullOrEmpty(text.Text)) Clipboard.SetText(text.Text); }
                catch (ExternalException ex) { ApplicationCrashLogger.Write(ex, "Information.Copy"); }
            };
            menu.Items.Add(copy); text.ContextMenu = menu;
        }
    }
}
