using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace DogruFatura.Wpf;

internal static class UiAccessibilityService
{
    private static readonly Brush Gold = BrushFrom("#FFB41F");
    private static readonly ConditionalWeakTable<Control, FocusSnapshot> FocusSnapshots = new();

    private static readonly IReadOnlyDictionary<string, string> AccessibleNames =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["InstitutionTypeBox"] = "Kurum türü",
            ["InstitutionBox"] = "Okul veya kurum",
            ["InvoiceTypeBox"] = "Fatura türü",
            ["InvoiceNumberBox"] = "Fatura numarası",
            ["InvoiceDatePicker"] = "Fatura tarihi",
            ["DueDatePicker"] = "Son ödeme tarihi",
            ["PayableAmountBox"] = "Ödenecek tutar",
            ["VatAmountBox"] = "KDV tutarı",
            ["InterruptionCompensationBox"] = "Kesinti tazminatı tutarı",
            ["SchoolSearchBox"] = "Kurumlarda ara",
            ["ArchiveSearchBox"] = "İşlem arşivinde ara",
            ["ControlSearchBox"] = "Kontrol bulgularında ara",
            ["ArchiveHysNumberBox"] = "HYS ödeme emri belge numarası"
        };

    private static readonly string[] PreferredTabOrder =
    {
        "InstitutionTypeBox", "InstitutionBox", "InvoiceTypeBox", "InvoiceNumberBox",
        "InvoiceDatePicker", "DueDatePicker", "PayableAmountBox", "VatAmountBox",
        "InterruptionCompensationToggleButton", "InterruptionCompensationBox", "AddInvoiceButton",
        "SchoolSearchBox", "SchoolFilterTypeBox", "SchoolTypeEditorBox", "SchoolNameEditorBox",
        "SchoolVknEditorBox", "SchoolElectricityEditorBox", "SchoolTelephoneEditorBox",
        "SchoolInternetEditorBox", "SchoolWaterEditorBox",
        "ArchiveSearchBox", "ArchiveTypeFilterBox", "ArchiveStatusFilterBox", "ArchiveHysNumberBox",
        "ControlSearchBox", "ControlSeverityFilterBox", "ControlCategoryFilterBox"
    };

    public static void Apply(MainWindow window)
    {
        ApplyAutomationMetadata(window);
        ApplyPreferredTabOrder(window);
        AttachFocusPresentation(window);
        AttachKeyboardShortcuts(window);
        if (SystemParameters.HighContrast) ApplyHighContrast(window);
        KeyboardNavigation.SetTabNavigation(window, KeyboardNavigationMode.Continue);
    }

    private static void ApplyAutomationMetadata(MainWindow window)
    {
        foreach (var control in UiTree.Descendants<Control>(window))
        {
            if (string.IsNullOrWhiteSpace(AutomationProperties.GetName(control)))
            {
                if (!string.IsNullOrWhiteSpace(control.Name)
                    && AccessibleNames.TryGetValue(control.Name, out var friendlyName))
                    AutomationProperties.SetName(control, friendlyName);
                else if (control is Button { Content: string content })
                    AutomationProperties.SetName(control, content.Replace("/", " "));
                else if (control.ToolTip is string tip && !string.IsNullOrWhiteSpace(tip))
                    AutomationProperties.SetName(control, tip);
            }

            if (string.IsNullOrWhiteSpace(AutomationProperties.GetHelpText(control))
                && control.ToolTip is string help
                && !string.IsNullOrWhiteSpace(help))
                AutomationProperties.SetHelpText(control, help);
        }
    }

    private static void ApplyPreferredTabOrder(MainWindow window)
    {
        var assigned = new HashSet<Control>();
        var index = 0;
        foreach (var name in PreferredTabOrder)
        {
            if (window.FindName(name) is not Control control || !control.Focusable) continue;
            control.TabIndex = index++;
            assigned.Add(control);
        }

        foreach (var control in UiTree.Descendants<Control>(window))
        {
            if (!control.Focusable || assigned.Contains(control)) continue;
            control.TabIndex = index++;
        }
    }

    private static void AttachFocusPresentation(MainWindow window)
    {
        foreach (var control in UiTree.Descendants<Control>(window))
        {
            if (control is not (Button or TextBox or ComboBox or DatePicker)) continue;

            control.GotKeyboardFocus += (_, _) =>
            {
                var snapshot = FocusSnapshots.GetValue(
                    control,
                    c => new FocusSnapshot(c.BorderBrush, c.BorderThickness));
                control.BorderBrush = Gold;
                control.BorderThickness = new Thickness(
                    Math.Max(1.5, snapshot.BorderThickness.Left),
                    Math.Max(1.5, snapshot.BorderThickness.Top),
                    Math.Max(1.5, snapshot.BorderThickness.Right),
                    Math.Max(1.5, snapshot.BorderThickness.Bottom));
            };

            control.LostKeyboardFocus += (_, _) =>
            {
                if (!FocusSnapshots.TryGetValue(control, out var snapshot)) return;
                control.BorderBrush = snapshot.BorderBrush;
                control.BorderThickness = snapshot.BorderThickness;
            };
        }
    }

    private static void AttachKeyboardShortcuts(MainWindow window)
    {
        window.PreviewKeyDown += (_, e) =>
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.P)
            {
                ClickNamedButton(window, "PrintReportButton", e);
                return;
            }

            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.F)
            {
                var search = CurrentSearchBox(window);
                if (search is not null)
                {
                    search.Focus();
                    search.SelectAll();
                    e.Handled = true;
                }
                return;
            }

            if ((Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
            {
                var key = e.Key == Key.System ? e.SystemKey : e.Key;
                var navName = key switch
                {
                    Key.A => "NavHome",
                    Key.F => "NavInvoice",
                    Key.Y => "NavOfficials",
                    Key.L => "NavList",
                    Key.R => "NavReports",
                    Key.O => "NavSchools",
                    Key.I => "NavArchive",
                    Key.K => "NavControl",
                    Key.H => "NavAbout",
                    _ => null
                };
                if (navName is not null)
                {
                    ClickNamedButton(window, navName, e);
                    return;
                }
            }

            if (e.Key == Key.Escape)
            {
                if (UiTree.IsVisible(window, "InvoiceView"))
                {
                    var clear = UiTree.Descendants<Button>(window)
                        .FirstOrDefault(x => Equals(x.Content, "TEMİZLE") && UiTree.FindAncestor<Grid>(x) is not null);
                    if (clear is not null) RaiseClick(clear);
                    e.Handled = clear is not null;
                }
                else if (UiTree.IsVisible(window, "SchoolsView"))
                {
                    var cancel = UiTree.Descendants<Button>(window)
                        .FirstOrDefault(x => Equals(x.Content, "VAZGEÇ / TEMİZLE"));
                    if (cancel is not null) RaiseClick(cancel);
                    e.Handled = cancel is not null;
                }
                return;
            }

            if (e.Key != Key.Enter || Keyboard.Modifiers != ModifierKeys.None) return;
            if (Keyboard.FocusedElement is not (TextBox or ComboBox or DatePicker)) return;

            Button? primary = null;
            if (UiTree.IsVisible(window, "InvoiceView"))
                primary = window.FindName("AddInvoiceButton") as Button;
            else if (UiTree.IsVisible(window, "OfficialsView"))
                primary = window.FindName("SaveAuthorityProfileButton") as Button;
            else if (UiTree.IsVisible(window, "SchoolsView"))
                primary = UiTree.Descendants<Button>(window)
                    .FirstOrDefault(x => Equals(x.Content, "KAYDET") && UiTree.IsInsideNamedAncestor(x, "SchoolsView"));

            if (primary?.IsEnabled == true)
            {
                RaiseClick(primary);
                e.Handled = true;
            }
        };
    }

    private static void ApplyHighContrast(MainWindow window)
    {
        foreach (var text in UiTree.Descendants<TextBlock>(window))
            text.Foreground = SystemColors.WindowTextBrush;

        foreach (var control in UiTree.Descendants<Control>(window))
        {
            control.Foreground = SystemColors.WindowTextBrush;
            control.Background = SystemColors.WindowBrush;
            control.BorderBrush = SystemColors.WindowTextBrush;
        }

        foreach (var border in UiTree.Descendants<Border>(window))
        {
            border.Background = SystemColors.WindowBrush;
            border.BorderBrush = SystemColors.WindowTextBrush;
        }
    }

    private static TextBox? CurrentSearchBox(MainWindow window)
    {
        if (UiTree.IsVisible(window, "SchoolsView")) return window.FindName("SchoolSearchBox") as TextBox;
        if (UiTree.IsVisible(window, "ArchiveView")) return window.FindName("ArchiveSearchBox") as TextBox;
        if (UiTree.IsVisible(window, "ControlView")) return window.FindName("ControlSearchBox") as TextBox;
        return null;
    }

    private static void ClickNamedButton(MainWindow window, string name, KeyEventArgs e)
    {
        if (window.FindName(name) is not Button button || !button.IsEnabled) return;
        RaiseClick(button);
        e.Handled = true;
    }

    private static void RaiseClick(Button button) =>
        button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, button));

    private static Brush BrushFrom(string value) =>
        new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));

    private sealed record FocusSnapshot(Brush? BorderBrush, Thickness BorderThickness);
}
