using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace DogruFatura.Wpf;

internal static class UiPolishService
{
    private static readonly Brush NavyInput = BrushFrom("#082744");
    private static readonly Brush NavyBorder = BrushFrom("#365874");
    private static readonly Brush Gold = BrushFrom("#FFB41F");
    private static readonly Brush White = Brushes.White;
    private static readonly Brush WarningDark = BrushFrom("#8A5A12");

    private static readonly Dictionary<string, string> AccessibleNames = new(StringComparer.Ordinal)
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

    public static void Apply(MainWindow window)
    {
        window.ResizeMode = ResizeMode.CanResizeWithGrip;
        window.MinWidth = 1080;
        window.MinHeight = 700;

        window.Loaded += (_, _) =>
        {
            ApplyVersionAndCopy(window);
            SimplifyHeader(window);
            ImproveHomeLayout(window);
            PolishDatePickers(window);
            NormalizeButtons(window);
            ImproveAccessibility(window);
            AttachKeyboardShortcuts(window);
            UnsavedChangesGuard.Attach(window);
        };
    }

    private static void ApplyVersionAndCopy(MainWindow window)
    {
        foreach (var text in Descendants<TextBlock>(window))
        {
            text.Text = text.Text switch
            {
                "v1.0  •  Aşama 10 R2" => AppVersionInfo.DisplayVersion,
                "DoğruFatura v1.0 • Aşama 10 R2" => $"DoğruFatura {AppVersionInfo.DisplayVersion}",
                "HESAP MOTORU" => "HESAPLAMA",
                "HYS numarasıyla kapatılmış ödeme listelerini gerçek verilerle tek bakışta izleyin." =>
                    "HYS ile kapanmış ödeme listelerinin mali durumunu tek bakışta izleyin.",
                "Aktif listeyi Excel'deki FaturaSonHali düzeninde önizleyin, PDF kaydedin veya yazdırın." =>
                    "Aktif listeyi resmî ödeme düzeninde önizleyin, PDF olarak kaydedin veya yazdırın.",
                "JSON kayıtları ile aktif/arşiv PDF belgeleri birlikte korunur." =>
                    "Kayıtlar ile aktif ve arşiv PDF belgeleri birlikte korunur.",
                _ => text.Text
            };
        }

        if (window.FindName("ArchivePdfSyncButton") is Button syncButton)
        {
            syncButton.Content = "PDF KONTROL";
            syncButton.ToolTip = "Arşiv kayıtları ile PDF belgelerinin uyumunu kontrol eder ve eksik belgeleri güvenli biçimde tamamlar.";
        }
    }

    private static void SimplifyHeader(MainWindow window)
    {
        var versionLabel = Descendants<TextBlock>(window).FirstOrDefault(x => x.Text == "SÜRÜM");
        if (versionLabel is not null)
        {
            var versionBorder = FindAncestor<Border>(versionLabel);
            if (versionBorder is not null)
                versionBorder.Visibility = Visibility.Collapsed;

            var headerStack = versionBorder is null ? null : FindAncestor<StackPanel>(versionBorder);
            var headerGrid = headerStack is null ? null : FindAncestor<Grid>(headerStack);
            if (headerGrid is not null && headerGrid.ColumnDefinitions.Count > 1)
                headerGrid.ColumnDefinitions[1].Width = new GridLength(220);
        }

        var stampLabel = Descendants<TextBlock>(window).FirstOrDefault(x => x.Text == "DAMGA ORANI");
        if (stampLabel is not null)
        {
            var stampBorder = FindAncestor<Border>(stampLabel);
            if (stampBorder is not null)
            {
                stampBorder.Visibility = Visibility.Collapsed;
                var grid = FindAncestor<Grid>(stampBorder);
                if (grid is not null && grid.ColumnDefinitions.Count > 3)
                {
                    grid.ColumnDefinitions[2].Width = new GridLength(0);
                    grid.ColumnDefinitions[3].Width = new GridLength(0);
                }
            }
        }
    }

    private static void ImproveHomeLayout(MainWindow window)
    {
        foreach (var stageText in Descendants<TextBlock>(window).Where(x => x.Text == "AŞAMA 10 • R2").ToArray())
        {
            if (FindAncestor<Border>(stageText) is { } badge)
                badge.Visibility = Visibility.Collapsed;
        }

        var home = window.FindName("HomeView") as FrameworkElement;
        if (home is null) return;

        // WPF bir Effect uygulanan ebeveynin altındaki metinleri ara yüzeyde bitmap'e
        // dönüştürebilir. Ana "Akıllı Özet" kartındaki DropShadowEffect bu nedenle
        // özellikle Viewbox ölçeklemesinde diğer kartlardan daha yumuşak görünüyordu.
        // Kartın kendisi keskin kalsın; vurgu zaten kenarlık/renk düzeniyle sağlanıyor.
        var smartSummaryLabel = Descendants<TextBlock>(home).FirstOrDefault(x => x.Text == "AKILLI ÖZET");
        if (smartSummaryLabel is not null && FindAncestor<Border>(smartSummaryLabel) is { } smartSummaryCard)
        {
            smartSummaryCard.Effect = null;
            smartSummaryCard.SnapsToDevicePixels = true;
            smartSummaryCard.UseLayoutRounding = true;
        }

        foreach (var border in Descendants<Border>(home))
        {
            if (border.CornerRadius.TopLeft is >= 10 and <= 12)
                border.CornerRadius = new CornerRadius(10);
        }

        // Ana sayfadaki tüm metinlerde aynı ekran odaklı metin rasterizasyonunu kullan.
        // Böylece koyu kartlar ve açık özet kartları arasında keskinlik farkı oluşmaz.
        foreach (var text in Descendants<TextBlock>(home))
        {
            TextOptions.SetTextFormattingMode(text, TextFormattingMode.Display);
            TextOptions.SetTextRenderingMode(text, TextRenderingMode.ClearType);
            text.SnapsToDevicePixels = true;
        }
    }

    private static void PolishDatePickers(MainWindow window)
    {
        foreach (var picker in Descendants<DatePicker>(window))
        {
            picker.Background = NavyInput;
            picker.Foreground = White;
            picker.BorderBrush = NavyBorder;
            picker.BorderThickness = new Thickness(1);
            picker.Height = 42;
            picker.FocusVisualStyle = null;
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
        foreach (var button in Descendants<Button>(window))
        {
            if (button.Content is not string content) continue;
            if (content is not ("—" or "×"))
            {
                button.MinHeight = 42;
                if (button.Height > 0 && button.Height < 42) button.Height = 42;
            }

            if (button.Background is SolidColorBrush background
                && background.Color == (Color)ColorConverter.ConvertFromString("#B7791F"))
            {
                button.Background = WarningDark;
                button.Foreground = White;
            }

            if (button.Background is SolidColorBrush goldBackground
                && goldBackground.Color == (Color)ColorConverter.ConvertFromString("#FFB41F"))
            {
                button.Foreground = BrushFrom("#031C38");
            }

            if (string.IsNullOrWhiteSpace(AutomationProperties.GetName(button)))
                AutomationProperties.SetName(button, content.Replace("/", " "));
        }
    }

    private static void ImproveAccessibility(MainWindow window)
    {
        var tabIndex = 0;
        foreach (var control in Descendants<Control>(window))
        {
            if (!control.Focusable) continue;
            control.TabIndex = tabIndex++;

            if (string.IsNullOrWhiteSpace(AutomationProperties.GetName(control)))
            {
                if (!string.IsNullOrWhiteSpace(control.Name) && AccessibleNames.TryGetValue(control.Name, out var friendly))
                    AutomationProperties.SetName(control, friendly);
                else if (control.ToolTip is string tip && !string.IsNullOrWhiteSpace(tip))
                    AutomationProperties.SetName(control, tip);
            }

            control.GotKeyboardFocus += (_, _) =>
            {
                if (control is Button or TextBox or ComboBox or DatePicker)
                {
                    control.Effect = new DropShadowEffect
                    {
                        Color = (Color)ColorConverter.ConvertFromString("#FFB41F"),
                        BlurRadius = 8,
                        ShadowDepth = 0,
                        Opacity = 0.55
                    };
                }
            };
            control.LostKeyboardFocus += (_, _) =>
            {
                if (control is Button or TextBox or ComboBox or DatePicker)
                    control.Effect = null;
            };
        }

        KeyboardNavigation.SetTabNavigation(window, KeyboardNavigationMode.Continue);
    }

    private static void AttachKeyboardShortcuts(MainWindow window)
    {
        window.PreviewKeyDown += (_, e) =>
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.P)
            {
                if (window.FindName("PrintReportButton") is Button print && print.IsEnabled)
                {
                    RaiseClick(print);
                    e.Handled = true;
                }
                return;
            }

            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.F)
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

            if (e.Key == Key.Escape)
            {
                if (IsVisible(window, "InvoiceView"))
                {
                    var clear = Descendants<Button>(window)
                        .FirstOrDefault(x => Equals(x.Content, "TEMİZLE") && FindAncestor<Grid>(x) is not null);
                    if (clear is not null) RaiseClick(clear);
                    e.Handled = clear is not null;
                }
                else if (IsVisible(window, "SchoolsView"))
                {
                    var cancel = Descendants<Button>(window).FirstOrDefault(x => Equals(x.Content, "VAZGEÇ / TEMİZLE"));
                    if (cancel is not null) RaiseClick(cancel);
                    e.Handled = cancel is not null;
                }
                return;
            }

            if (e.Key != Key.Enter || Keyboard.Modifiers != ModifierKeys.None) return;
            if (Keyboard.FocusedElement is not (TextBox or ComboBox or DatePicker)) return;

            Button? primary = null;
            if (IsVisible(window, "InvoiceView"))
                primary = window.FindName("AddInvoiceButton") as Button;
            else if (IsVisible(window, "OfficialsView"))
                primary = window.FindName("SaveAuthorityProfileButton") as Button;
            else if (IsVisible(window, "SchoolsView"))
                primary = Descendants<Button>(window).FirstOrDefault(x => Equals(x.Content, "KAYDET") && IsInsideNamedAncestor(x, "SchoolsView"));

            if (primary?.IsEnabled == true)
            {
                RaiseClick(primary);
                e.Handled = true;
            }
        };
    }

    private static TextBox? CurrentSearchBox(MainWindow window)
    {
        if (IsVisible(window, "SchoolsView")) return window.FindName("SchoolSearchBox") as TextBox;
        if (IsVisible(window, "ArchiveView")) return window.FindName("ArchiveSearchBox") as TextBox;
        if (IsVisible(window, "ControlView")) return window.FindName("ControlSearchBox") as TextBox;
        return null;
    }

    private static bool IsVisible(MainWindow window, string name) =>
        window.FindName(name) is FrameworkElement element && element.Visibility == Visibility.Visible;

    private static bool IsInsideNamedAncestor(DependencyObject child, string name)
    {
        var current = child;
        while (current is not null)
        {
            if (current is FrameworkElement element && element.Name == name) return true;
            current = ParentOf(current);
        }
        return false;
    }

    private static void RaiseClick(Button button) =>
        button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, button));

    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var index = 0; index < count; index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T match) yield return match;
            foreach (var nested in Descendants<T>(child)) yield return nested;
        }
    }

    private static T? FindAncestor<T>(DependencyObject child) where T : DependencyObject
    {
        var current = ParentOf(child);
        while (current is not null)
        {
            if (current is T match) return match;
            current = ParentOf(current);
        }
        return null;
    }

    private static DependencyObject? ParentOf(DependencyObject child)
    {
        if (child is Visual or System.Windows.Media.Media3D.Visual3D)
            return VisualTreeHelper.GetParent(child);
        return LogicalTreeHelper.GetParent(child);
    }

    private static Brush BrushFrom(string value) =>
        new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
}
