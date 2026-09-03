using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace DogruFatura.Wpf;

internal sealed class UnsavedChangesGuard
{
    private static readonly string[] TrackedNames =
    {
        "InstitutionTypeBox", "InstitutionBox", "InvoiceTypeBox", "InvoiceNumberBox",
        "InvoiceDatePicker", "DueDatePicker", "PayableAmountBox", "VatAmountBox",
        "InterruptionCompensationBox",
        "SchoolTypeEditorBox", "SchoolNameEditorBox", "SchoolVknEditorBox",
        "SchoolElectricityEditorBox", "SchoolTelephoneEditorBox", "SchoolInternetEditorBox", "SchoolWaterEditorBox",
        "RealizationOfficerNameBox", "RealizationOfficerTitleBox",
        "SpendingAuthorityNameBox", "SpendingAuthorityTitleBox"
    };

    private readonly MainWindow _window;
    private string _baseline = string.Empty;
    private bool _suppress;

    private UnsavedChangesGuard(MainWindow window) => _window = window;

    public static void Attach(MainWindow window)
    {
        var guard = new UnsavedChangesGuard(window);
        guard.AttachCore();
    }

    private void AttachCore()
    {
        _window.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(AcceptCurrentState));
        _window.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OnAnyButtonClick), true);
        _window.AddHandler(Selector.SelectionChangedEvent, new SelectionChangedEventHandler(OnSelectionChanged), true);
        _window.Closing += OnClosing;
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.OriginalSource is not DataGrid grid) return;
        if (grid.Name is not ("SchoolsGrid" or "InvoicesGrid")) return;
        _window.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(AcceptCurrentState));
    }

    private void OnAnyButtonClick(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source) return;
        var button = FindAncestor<Button>(source);
        if (button?.Content is not string content) return;

        if (content is "YENİ" or "DÜZENLE" or "VAZGEÇ / TEMİZLE" or "TEMİZLE")
        {
            _window.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(AcceptCurrentState));
            return;
        }

        if (content is "FATURA LİSTESİNE EKLE" or "FATURA KAYDINI GÜNCELLE")
        {
            _window.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                var status = (_window.FindName("InvoiceSaveStatusText") as TextBlock)?.Text ?? string.Empty;
                if (status.Contains("kaydedildi", StringComparison.CurrentCultureIgnoreCase)
                    || status.Contains("güncellendi", StringComparison.CurrentCultureIgnoreCase))
                    AcceptCurrentState();
            }));
            return;
        }

        if (content == "KAYDET")
        {
            _window.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                var schoolStatus = (_window.FindName("SchoolFormStatusText") as TextBlock)?.Text ?? string.Empty;
                var authorityStatus = (_window.FindName("AuthorityStatusText") as TextBlock)?.Text ?? string.Empty;
                if (schoolStatus.Contains("kalıcı olarak kaydedildi", StringComparison.CurrentCultureIgnoreCase)
                    || authorityStatus.Contains("kaydedildi", StringComparison.CurrentCultureIgnoreCase))
                    AcceptCurrentState();
            }));
        }
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_suppress || !IsDirty()) return;

        var result = MessageBox.Show(
            _window,
            "Formlarda kaydedilmemiş değişiklikler var. Program kapatılırsa bu değişiklikler kaybolacak.\n\nYine de çıkmak istiyor musunuz?",
            "DoğruFatura - Kaydedilmemiş Değişiklik",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        if (result != MessageBoxResult.Yes)
            e.Cancel = true;
    }

    private bool IsDirty() => !string.Equals(_baseline, BuildFingerprint(), StringComparison.Ordinal);

    private void AcceptCurrentState()
    {
        _suppress = true;
        try { _baseline = BuildFingerprint(); }
        finally { _suppress = false; }
    }

    private string BuildFingerprint()
    {
        var builder = new StringBuilder();
        foreach (var name in TrackedNames)
        {
            builder.Append(name).Append('=');
            var element = _window.FindName(name);
            builder.Append(element switch
            {
                TextBox box => box.Text,
                ComboBox combo => combo.SelectedValue?.ToString() ?? combo.SelectedItem?.ToString() ?? combo.Text,
                DatePicker picker => picker.SelectedDate?.ToString("O") ?? string.Empty,
                _ => string.Empty
            });
            builder.Append('\u001f');
        }
        return builder.ToString();
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T match) return match;
            current = System.Windows.Media.VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}
