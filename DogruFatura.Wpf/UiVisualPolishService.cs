using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace DogruFatura.Wpf;

internal static class UiVisualPolishService
{
    private static readonly Brush NavyInput = BrushFrom("#082744");
    private static readonly Brush NavyBorder = BrushFrom("#365874");
    private static readonly Brush Gold = BrushFrom("#FFB41F");
    private static readonly Brush White = Brushes.White;
    private static readonly Brush WarningDark = BrushFrom("#8A5A12");

    public static void Apply(MainWindow window)
    {
        ArgumentNullException.ThrowIfNull(window);

        ApplyVersionText(window);
        PolishDatePickers(window);
        NormalizeButtons(window);
        NormalizePanels(window);
    }

    private static void ApplyVersionText(MainWindow window)
    {
        window.HeaderVersionText.Text = AppVersionInfo.DisplayVersion;
        window.AboutProductVersionText.Text = $"DoğruFatura {AppVersionInfo.DisplayVersion}";
    }

    private static void PolishDatePickers(MainWindow window)
    {
        foreach (var picker in UiTree.Descendants<DatePicker>(window))
        {
            picker.Background = NavyInput;
            picker.Foreground = White;
            picker.BorderBrush = NavyBorder;
            picker.BorderThickness = new Thickness(1);
            picker.Height = 42;
            picker.Loaded += (_, _) => PolishDatePickerTextBox(picker);
            PolishDatePickerTextBox(picker);
        }
    }

    private static void PolishDatePickerTextBox(DatePicker picker)
    {
        picker.ApplyTemplate();
        if (picker.Template.FindName("PART_TextBox", picker) is not DatePickerTextBox box) return;
        box.Background = NavyInput;
        box.Foreground = White;
        box.BorderBrush = NavyBorder;
        box.CaretBrush = Gold;
        box.SelectionBrush = BrushFrom("#2D6593");
        box.Padding = new Thickness(9, 0, 5, 0);
        box.VerticalContentAlignment = VerticalAlignment.Center;
    }

    private static void NormalizeButtons(MainWindow window)
    {
        foreach (var button in UiTree.Descendants<Button>(window))
        {
            if (button.Content is string content)
            {
                if (content is not ("—" or "×"))
                {
                    button.MinHeight = 42;
                    if (button.Height > 0 && button.Height < 42) button.Height = 42;
                }

                if (string.IsNullOrWhiteSpace(AutomationProperties.GetName(button)))
                    AutomationProperties.SetName(button, content.Replace("/", " "));
            }

            if (button.Background is SolidColorBrush background
                && background.Color == (Color)ColorConverter.ConvertFromString("#B7791F"))
            {
                button.Background = WarningDark;
                button.Foreground = White;
            }

            if (button.Background is SolidColorBrush goldBackground
                && goldBackground.Color == (Color)ColorConverter.ConvertFromString("#FFB41F"))
                button.Foreground = BrushFrom("#031C38");
        }
    }

    private static void NormalizePanels(MainWindow window)
    {
        foreach (var border in UiTree.Descendants<Border>(window))
        {
            if (border.CornerRadius.TopLeft is >= 8 and <= 12
                && border.CornerRadius.TopRight is >= 8 and <= 12)
                border.SnapsToDevicePixels = true;
        }
    }

    private static SolidColorBrush BrushFrom(string value) =>
        new((Color)ColorConverter.ConvertFromString(value));
}
