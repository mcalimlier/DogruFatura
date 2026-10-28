using DogruFatura.Core.Models;
using DogruFatura.Core.Services;

using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace DogruFatura.Wpf;

public partial class MainWindow
{

    private readonly Button _undoInvoice = new() { Content = "Silmeyi geri al", IsEnabled = false };


    private readonly Dictionary<Control, TextBlock> _entryErrors = new();

    private InvoiceRecord? _deletedInvoice;
    private int _deletedInvoiceIndex;

    private string PreferencesPath => Path.Combine(Path.GetDirectoryName(_invoiceDataStore.DataFilePath)!, "GorunumTercihleri.json");
    private WorkbenchPreferences _preferences = new();
    private bool _preferencesHealthy = true;
    private readonly Dictionary<Grid, (ColumnDefinition[], int[], int[])> _responsiveGrids = new();

    private Grid? _entryLayoutGrid;

    private sealed record ColumnPreference(string Key, double Width, bool Visible, int DisplayIndex);
    private sealed record WorkbenchPreferences
    {
        public bool Compact { get; init; }

        public string DisplayProfile { get; init; } = "1366x768";
        public List<ColumnPreference> Columns { get; init; } = new();

    }

    private Button WorkbenchButton(string text, RoutedEventHandler handler, bool secondary = true)
    {
        var button = new Button { Content = text, Style = (Style)FindResource("ModernButton"), Margin = new(0, 0, 8, 8), Padding = new(10, 6, 10, 6), FontSize = 13, HorizontalAlignment = HorizontalAlignment.Left };
        if (secondary) button.Background = new SolidColorBrush(Color.FromRgb(49, 83, 113));
        button.Click += handler;
        return button;
    }

    private void InitializeWorkbench()
    {
        try
        {
            if (File.Exists(PreferencesPath))
            {
                var preferences = JsonSerializer.Deserialize<WorkbenchPreferences>(File.ReadAllText(PreferencesPath));
                if (preferences is null || preferences.Columns is null || preferences.Columns.Any(c => c is null))
                    throw new InvalidDataException("Görünüm tercihleri okunamadı.");
                _preferences = preferences;
            }
        }
        catch (Exception ex) { _preferencesHealthy = false; ApplicationCrashLogger.Write(ex, "Workbench.Preferences"); }

        _undoInvoice.Style = (Style)FindResource("ModernButton");
        _undoInvoice.Margin = new(0, 0, 8, 8);
        _undoInvoice.Click += (_, _) => UndoInvoiceDelete();
        ((WrapPanel)EditInvoiceButton.Parent).Children.Add(_undoInvoice);
        InitializeEntryTools();
        _entryLayoutGrid = ((StackPanel)InvoiceView.Content).Children.OfType<Grid>().First(g => g.ColumnDefinitions.Count == 3);

        InitializeArchiveTools();

        InitializeStatisticsTools();
        InitializeDisplayProfiles();
        RefreshWorkbench();
        Closing += (_, _) => SaveWorkbenchPreferences();
        SizeChanged += (_, _) => ApplyAdaptiveLayout();
        Loaded += (_, _) => { ApplyAdaptiveLayout(); ApplyDensity(); };
        PreviewKeyDown += WorkbenchKeyDown;
    }

    private void RefreshWorkbench()
    {
        RefreshDashboard();

    }

    private void ApplyListFilter() => InvoiceListEmptyState.Visibility = ActiveInvoices.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    private void UndoInvoiceDelete()
    {
        if (_deletedInvoice is not { } invoice) return;
        var policy = InvoiceListTypePolicy.Check(ActiveInvoices, invoice.InvoiceType);
        if (!policy.IsAllowed || InvoiceDuplicateService.Check(ActiveInvoices, invoice).IsDuplicate)
        { SetInvoiceListStatus("Geri alma yapılamadı: liste türü değişti veya fatura zaten var. Önce listeyi kontrol edin.", true); return; }
        ActiveInvoices.Insert(Math.Clamp(_deletedInvoiceIndex, 0, ActiveInvoices.Count), invoice);
        if (!TrySaveActiveInvoices(out var error)) { ActiveInvoices.Remove(invoice); SetInvoiceListStatus(error, true); return; }
        AppendAudit(AuditTrailEventType.InvoiceCreated, AuditTrailEntityType.Invoice, invoice.Id.ToString("D"), "Source=UndoDelete");
        _deletedInvoice = null; _undoInvoice.IsEnabled = false;
        UpdateInvoiceListSummary(invoice.Id);
        SetInvoiceListStatus("Silinen fatura geri alındı.", false, true);
    }

    private void InitializeEntryTools()
    {
        InstitutionBox.Style = (Style)FindResource("PremiumFieldComboBox");
        InstitutionBox.IsEditable = true; InstitutionBox.IsTextSearchEnabled = false;
        TextSearch.SetTextPath(InstitutionBox, "Name");
        InstitutionBox.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(InstitutionSearch_TextChanged));
        InstitutionBox.ToolTip = "Adın herhangi bir bölümünü yazarak tüm kurumlarda arayın. Yazılı arama kurum türü filtresinden bağımsızdır.";
        var panel = (StackPanel)AddInvoiceButton.Parent;
        var saveToList = WorkbenchButton("Kaydet ve Listeye Git", SaveAndOpenList);
        saveToList.SetBinding(IsEnabledProperty, new Binding("IsEnabled") { Source = AddInvoiceButton });
        panel.Children.Add(saveToList);
        foreach (var control in new Control[] { PayableAmountBox, VatAmountBox, InvoiceDatePicker, DueDatePicker })
        {
            if (control.Parent is not StackPanel field) continue;
            var error = new TextBlock { FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(255, 180, 170)), TextWrapping = TextWrapping.Wrap, Visibility = Visibility.Collapsed, Margin = new(0, 4, 0, 0) };
            field.Children.Add(error); _entryErrors[control] = error;
        }

    }



    private static IEnumerable<DependencyObject> LogicalDescendants(DependencyObject parent)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
        {
            yield return child;
            foreach (var descendant in LogicalDescendants(child)) yield return descendant;
        }
    }

    private void RefreshEntryErrors()
    {
        if (_entryErrors.Count == 0) return;
        void Error(Control field, string message)
        {
            _entryErrors[field].Text = message;
            _entryErrors[field].Visibility = message.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        }
        Error(PayableAmountBox, PayableAmountBox.Text.Length > 0 && (!NumberParser.TryNonNegativeDecimal(PayableAmountBox.Text, out var amount) || amount <= 0) ? "Sıfırdan büyük bir tutar girin." : "");
        Error(VatAmountBox, VatAmountBox.Text.Length > 0 && !NumberParser.TryNonNegativeDecimal(VatAmountBox.Text, out _) ? "Geçerli bir KDV tutarı girin." : "");
        Error(InvoiceDatePicker, "");
        Error(DueDatePicker, DueDatePicker.SelectedDate < InvoiceDatePicker.SelectedDate ? "Fatura tarihinden önce olamaz." : "");
    }

    private void SaveAndOpenList(object sender, RoutedEventArgs e)
    {
        var before = ActiveInvoices.ToArray();
        AddInvoice_Click(sender, e);
        if (!before.SequenceEqual(ActiveInvoices)) ShowView("List");
    }

    private void SaveWorkbenchPreferences()
    {
        if (!_preferencesHealthy) return;
        try
        {

            Directory.CreateDirectory(Path.GetDirectoryName(PreferencesPath)!);
            File.WriteAllText(PreferencesPath + ".tmp", JsonSerializer.Serialize(_preferences));
            File.Move(PreferencesPath + ".tmp", PreferencesPath, true);
        }
        catch (Exception ex) { ApplicationCrashLogger.Write(ex, "Workbench.SavePreferences"); }
    }

    private void ApplyDensity()
    {
        InvoicesGrid.RowHeight = _preferences.Compact ? 32 : 36;
        InvoicesGrid.FontSize = 13;
        foreach (var grid in new[] { ArchiveGrid, SchoolsGrid }) { grid.RowHeight = _preferences.Compact ? 32 : 36; grid.FontSize = 13; }
    }

    private void WorkbenchKeyDown(object sender, KeyEventArgs e)
    {
        if (_currentView == "List" && e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control && Keyboard.FocusedElement is not TextBox) { UndoInvoiceDelete(); e.Handled = true; }
    }
}
