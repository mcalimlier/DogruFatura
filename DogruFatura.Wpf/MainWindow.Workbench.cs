using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using DogruFatura.Wpf.Workbench;
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
    private WorkbenchPanel? _workbench;
    private ICollectionView? _invoiceFilterView;
    private readonly TextBox _listSearch = new() { Width = 180, ToolTip = "Kurum, fatura veya hizmet numarası ara" };
    private readonly DatePicker _dueFrom = new() { Width = 125, ToolTip = "Son ödeme başlangıç tarihi" };
    private readonly DatePicker _dueTo = new() { Width = 125, ToolTip = "Son ödeme bitiş tarihi" };
    private readonly TextBox _minimumAmount = new() { Width = 90, ToolTip = "En az fatura tutarı" };
    private readonly TextBox _maximumAmount = new() { Width = 90, ToolTip = "En çok fatura tutarı" };
    private readonly CheckBox _dueOnly = new() { Content = "Vadesi yakın / geçmiş", VerticalAlignment = VerticalAlignment.Center };
    private readonly TextBlock _filteredTotals = new() { FontSize = 13, Foreground = Brushes.DarkSlateGray, TextWrapping = TextWrapping.Wrap };
    private readonly Button _undoInvoice = new() { Content = "Silmeyi geri al", IsEnabled = false };
    private readonly CheckBox _keepDates = new() { Content = "Yeni faturada tarihleri koru", Foreground = Brushes.White, FontSize = 13, Margin = new(0, 10, 0, 8) };
    private readonly TextBox _institutionSearch = new() { Height = 34, FontSize = 14, ToolTip = "Kurum adının bir bölümünü yazın" };
    private readonly Dictionary<Control, TextBlock> _entryErrors = new();
    private readonly TextBlock _workflowText = new() { FontSize = 13, Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap };
    private InvoiceRecord? _deletedInvoice;
    private int _deletedInvoiceIndex;
    private InvoiceDraftStore? _draftStore;
    private bool _draftsHealthy = true;
    private string PreferencesPath => Path.Combine(Path.GetDirectoryName(_invoiceDataStore.DataFilePath)!, "GorunumTercihleri.json");
    private WorkbenchPreferences _preferences = new();
    private bool _preferencesHealthy = true;
    private readonly Dictionary<Grid, (ColumnDefinition[], int[], int[])> _responsiveGrids = new();
    private ComboBox? _recentSchools;
    private Grid? _entryLayoutGrid;

    private sealed record ColumnPreference(string Key, double Width, bool Visible, int DisplayIndex);
    private sealed record WorkbenchPreferences
    {
        public bool Compact { get; init; }
        public bool KeepDates { get; init; }
        public string SecondaryBackupFolder { get; init; } = "";
        public List<ColumnPreference> Columns { get; init; } = new();
        public List<Guid> RecentSchools { get; init; } = new();
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
                if (preferences is null || preferences.Columns is null || preferences.Columns.Any(c => c is null) || preferences.RecentSchools is null || preferences.SecondaryBackupFolder is null)
                    throw new InvalidDataException("Görünüm tercihleri okunamadı.");
                _preferences = preferences;
            }
        }
        catch (Exception ex) { _preferencesHealthy = false; ApplicationCrashLogger.Write(ex, "Workbench.Preferences"); }
        _keepDates.IsChecked = _preferences.KeepDates;
        _draftStore = new InvoiceDraftStore(Path.Combine(Path.GetDirectoryName(_invoiceDataStore.DataFilePath)!, "FaturaTaslaklari.json"));
        _workbench = new WorkbenchPanel();
        if (!_preferencesHealthy) _workbench.NoticeText.Text = "Görünüm tercihleri okunamadı; varsayılan görünüm açıldı, eski dosya korunuyor.";
        var home = (StackPanel)HomeView.Content;
        var history = new StackPanel();
        while (home.Children.Count > 0) { var child = home.Children[0]; home.Children.RemoveAt(0); history.Children.Add(child); }
        home.Children.Add(_workbench);
        home.Children.Add(new Expander { Header = "Mali özet ve geçmiş hareketler", Content = history, FontSize = 16, Foreground = Brushes.DarkSlateGray });
        _workbench.ActionRequested += WorkbenchAction;
        RefreshDrafts();
        InitializeListTools();
        InitializeEntryTools();
        _entryLayoutGrid = ((StackPanel)InvoiceView.Content).Children.OfType<Grid>().First(g => g.ColumnDefinitions.Count == 3);
        InitializeWorkflow();
        InitializeArchiveTools();
        InitializeSafetyTools();
        InitializeStatisticsTools();
        RefreshWorkbench();
        Closing += (_, _) => SaveWorkbenchPreferences();
        SizeChanged += (_, _) => ApplyAdaptiveLayout();
        Loaded += (_, _) => { ApplyAdaptiveLayout(); ApplyDensity(); };
        PreviewKeyDown += WorkbenchKeyDown;
    }

    private void InitializeWorkflow()
    {
        var host = (Grid)HomeView.Parent;
        host.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        host.RowDefinitions.Add(new RowDefinition());
        foreach (UIElement child in host.Children) Grid.SetRow(child, 1);
        var panel = new StackPanel();
        var actions = new WrapPanel();
        actions.Children.Add(WorkbenchButton("1 · Faturalar", (_, _) => ShowView("Invoice")));
        actions.Children.Add(WorkbenchButton("2 · Kontrol", NavControl_Click));
        actions.Children.Add(WorkbenchButton("3 · Çıktı", NavReports_Click));
        actions.Children.Add(WorkbenchButton("4 · HYS kapanışı", NavArchive_Click));
        foreach (Button button in actions.Children) { button.MinHeight = 30; button.Height = 32; button.FontSize = 12; button.Margin = new(0, 0, 6, 5); }
        panel.Children.Add(actions); panel.Children.Add(_workflowText);
        var border = new Border { Background = new SolidColorBrush(Color.FromRgb(8, 39, 68)), CornerRadius = new(8), Padding = new(8), Margin = new(0, 0, 0, 10), Child = panel };
        host.Children.Add(border);
    }

    private void RefreshWorkbench()
    {
        if (_workbench is null) return;
        var summary = WorkbenchService.Summarize(ActiveInvoices, ArchivedLists, DateTime.Today, ControlIssues.Count);
        _workbench.Refresh(summary, InvoiceListTypePolicy.GetListTypeLabel(ActiveInvoices));
        _workflowText.Text = ActiveInvoices.Count == 0 ? "1 · Faturaları ekle   →   2 · Kontrol et   →   3 · Çıktıyı hazırla   →   4 · HYS ile kapat"
            : ControlIssues.Any(i => i.Severity == ControlIssueSeverity.Error) ? "Sıradaki adım: Kontrol bulgularını düzeltin."
            : $"{ActiveInvoices.Count} fatura hazır · Çıktıyı kontrol edin, sonra arşivleyip HYS ile kapatın.";
        RefreshFilteredTotals();
        if (_recentSchools is not null) _recentSchools.ItemsSource = _preferences.RecentSchools.Select(id => AllSchools.FirstOrDefault(s => s.SchoolId == id)).Where(s => s is not null).ToArray();
    }

    private void WorkbenchAction(string action)
    {
        switch (action)
        {
            case "Due": _dueOnly.IsChecked = true; ShowView("List"); break;
            case "Pending": ArchiveStatusFilterBox.SelectedIndex = 1; RefreshArchiveView(); ShowView("Archive"); break;
            case "Continue": if (ActiveInvoices.Count == 0) ShowView("Invoice"); else ShowView(ControlIssues.Count > 0 ? "Control" : "List"); break;
            case "SaveDraft": SaveCurrentDraft(); break;
            case "NewDraft": SwitchDraft(null); break;
            case "OpenDraft": if (_workbench?.DraftBox.SelectedItem is InvoiceDraft draft) SwitchDraft(draft); break;
            default: ShowView(action); break;
        }
    }

    private void RefreshDrafts()
    {
        if (_workbench is null || _draftStore is null) return;
        try { _workbench.DraftBox.ItemsSource = _draftStore.Load().OrderByDescending(d => d.SavedAt).ToArray(); }
        catch (Exception ex) { _draftsHealthy = false; _workbench.NoticeText.Text = "Taslak dosyası okunamadı; üzerine yazılmadı. Güvenlik yedeği alın. " + ex.Message; }
    }

    private bool SaveCurrentDraft()
    {
        if (!_draftsHealthy || _draftStore is null || _workbench is null) return false;
        if (ActiveInvoices.Count == 0) { _workbench.NoticeText.Text = "Taslak saklamak için aktif listeye fatura ekleyin."; return true; }
        try
        {
            var drafts = _draftStore.Load().ToList();
            var name = _workbench.DraftNameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name)) name = $"{DateTime.Now:dd.MM.yyyy HH:mm} · {InvoiceListTypePolicy.GetListTypeLabel(ActiveInvoices)}";
            drafts.Add(new(Guid.NewGuid(), name, DateTimeOffset.Now, ActiveInvoices.ToList()));
            _draftStore.Save(drafts);
            RefreshDrafts();
            _workbench.NoticeText.Text = "Aktif liste taslak olarak saklandı. Aynı adlı kayıtların önceki kopyaları korunur.";
            return true;
        }
        catch (Exception ex) { _workbench.NoticeText.Text = "Taslak kaydedilemedi; aktif liste korunuyor. " + ex.Message; return false; }
    }

    private void SwitchDraft(InvoiceDraft? target)
    {
        if (!_draftsHealthy || !ConfirmEntryDiscard()) return;
        // Preserve current invoices durably before replacing the separate active file.
        if (!SaveCurrentDraft()) return;
        var previous = ActiveInvoices.ToArray();
        ActiveInvoices.Clear();
        foreach (var invoice in target?.Invoices ?? new List<InvoiceRecord>()) ActiveInvoices.Add(invoice);
        if (!TrySaveActiveInvoices(out var error))
        {
            ActiveInvoices.Clear(); foreach (var invoice in previous) ActiveInvoices.Add(invoice);
            if (_workbench is not null) _workbench.NoticeText.Text = error;
        }
        else
        {
            _deletedInvoice = null; _undoInvoice.IsEnabled = false;
            EndInvoiceEditMode(); ClearInvoiceEntryFieldsAfterSave();
            if (_workbench is not null) _workbench.NoticeText.Text = target is null ? "Boş liste açıldı; önceki listeniz taslaklarda." : $"{target.Name} açıldı; güvenlik kopyası taslaklarda korunuyor.";
        }
        UpdateInvoiceListSummary();
        ResetListFilters();
    }

    private void InitializeListTools()
    {
        _invoiceFilterView = CollectionViewSource.GetDefaultView(ActiveInvoiceRows);
        _invoiceFilterView.Filter = item => item is ActiveInvoiceRow row && WorkbenchService.Matches(row.Invoice, ReadListFilter());
        var tools = new StackPanel { Margin = new(0, 0, 0, 8) };
        var filters = new WrapPanel(); tools.Children.Add(filters);
        foreach (var (label, control) in new (string, FrameworkElement)[] { ("Ara", _listSearch), ("Vade başlangıç", _dueFrom), ("Vade bitiş", _dueTo), ("En az TL", _minimumAmount), ("En çok TL", _maximumAmount) })
        {
            var field = new StackPanel { Margin = new(0, 0, 8, 8) };
            field.Children.Add(new TextBlock { Text = label, FontSize = 12, Foreground = Brushes.DarkSlateGray });
            control.Height = 32; field.Children.Add(control); filters.Children.Add(field);
        }
        filters.Children.Add(_dueOnly);
        var actions = new WrapPanel(); tools.Children.Add(actions);
        actions.Children.Add(WorkbenchButton("Filtreleri temizle", (_, _) => ResetListFilters()));
        actions.Children.Add(WorkbenchButton("Excel'den yapıştır", (_, _) => ImportPastedInvoices(), false));
        actions.Children.Add(WorkbenchButton("Sütunlar", (_, _) => ShowColumnMenu()));
        _undoInvoice.Style = (Style)FindResource("ModernButton"); _undoInvoice.Margin = new(0, 0, 8, 8); _undoInvoice.FontSize = 13;
        _undoInvoice.Click += (_, _) => UndoInvoiceDelete(); actions.Children.Add(_undoInvoice);
        actions.Children.Add(WorkbenchButton("Rahat / Sıkı görünüm", (_, _) => { _preferences = _preferences with { Compact = !_preferences.Compact }; ApplyDensity(); SaveWorkbenchPreferences(); }));
        tools.Children.Add(_filteredTotals);
        ListView.RowDefinitions.Insert(3, new RowDefinition { Height = GridLength.Auto });
        foreach (UIElement child in ListView.Children) if (Grid.GetRow(child) >= 3) Grid.SetRow(child, Grid.GetRow(child) + 1);
        Grid.SetRow(tools, 3); ListView.Children.Add(tools);
        _listSearch.TextChanged += (_, _) => ApplyListFilter(); _minimumAmount.TextChanged += (_, _) => ApplyListFilter(); _maximumAmount.TextChanged += (_, _) => ApplyListFilter();
        _dueFrom.SelectedDateChanged += (_, _) => ApplyListFilter(); _dueTo.SelectedDateChanged += (_, _) => ApplyListFilter();
        StyleLightDatePicker(_dueFrom); StyleLightDatePicker(_dueTo);
        _dueOnly.Checked += (_, _) => ApplyListFilter(); _dueOnly.Unchecked += (_, _) => ApplyListFilter();
        InvoicesGrid.SelectionMode = DataGridSelectionMode.Extended;
        InvoicesGrid.CanUserSortColumns = true; InvoicesGrid.CanUserReorderColumns = true;
        var headerStyle = new Style(typeof(DataGridColumnHeader), (Style)FindResource("ModernDataGridHeader"));
        headerStyle.Setters.Add(new Setter(FontSizeProperty, 13d));
        headerStyle.Setters.Add(new Setter(PaddingProperty, new Thickness(8, 6, 8, 6)));
        InvoicesGrid.ColumnHeaderStyle = headerStyle;
        InvoicesGrid.SelectionChanged += (_, _) => RefreshFilteredTotals();
        InvoicesGrid.Columns.Add(new DataGridTextColumn { Header = "Vade durumu", Binding = new Binding("DueStatus"), Width = 135 });
        foreach (var column in InvoicesGrid.Columns)
        {
            if (column is DataGridBoundColumn bound && bound.Binding is Binding binding) column.SortMemberPath = binding.Path.Path;
            column.MinWidth = column.Header?.ToString() == "Kurum" ? 180 : column.Header?.ToString() == "Fatura No" ? 135 : Math.Max(70, (column.Header?.ToString()?.Length ?? 0) * 7 + 20);
            var pref = _preferences.Columns.FirstOrDefault(c => c.Key == column.SortMemberPath);
            if (pref is null) continue;
            column.Visibility = pref.Visible ? Visibility.Visible : Visibility.Collapsed;
            if (double.IsFinite(pref.Width) && pref.Width >= 60 && pref.Width <= 1000) column.Width = pref.Width;
            column.DisplayIndex = Math.Clamp(pref.DisplayIndex, 0, InvoicesGrid.Columns.Count - 1);
        }
        // Always retain an identifying column, even when restoring an old preference file.
        InvoicesGrid.Columns[1].Visibility = Visibility.Visible;
        ApplyListFilter();
    }

    private InvoiceWorkbenchFilter ReadListFilter()
    {
        decimal? Min(string value) => NumberParser.TryNonNegativeDecimal(value, out var amount) ? amount : null;
        return new(_listSearch.Text, _dueFrom.SelectedDate, _dueTo.SelectedDate, Min(_minimumAmount.Text), Min(_maximumAmount.Text), _dueOnly.IsChecked == true);
    }

    private void ApplyListFilter()
    {
        _invoiceFilterView?.Refresh(); RefreshFilteredTotals();
        InvoiceListEmptyState.Visibility = _invoiceFilterView?.IsEmpty == true ? Visibility.Visible : Visibility.Collapsed;
        if (InvoiceListEmptyState.Child is StackPanel empty && empty.Children[1] is TextBlock label)
            label.Text = ActiveInvoices.Count == 0 ? "Aktif listede henüz fatura yok" : "Filtreyle eşleşen fatura yok";
    }

    private void ResetListFilters()
    {
        _listSearch.Clear(); _dueFrom.SelectedDate = null; _dueTo.SelectedDate = null; _minimumAmount.Clear(); _maximumAmount.Clear(); _dueOnly.IsChecked = false; ApplyListFilter();
    }

    private void RefreshFilteredTotals()
    {
        if (_invoiceFilterView is null) return;
        var visible = _invoiceFilterView.Cast<ActiveInvoiceRow>().Select(r => r.Invoice).ToArray();
        var selected = InvoicesGrid.SelectedItems.Cast<ActiveInvoiceRow>().Select(r => r.Invoice).ToArray();
        var invalid = (!string.IsNullOrWhiteSpace(_minimumAmount.Text) && !NumberParser.TryNonNegativeDecimal(_minimumAmount.Text, out _))
            || (!string.IsNullOrWhiteSpace(_maximumAmount.Text) && !NumberParser.TryNonNegativeDecimal(_maximumAmount.Text, out _))
            || (_dueFrom.SelectedDate > _dueTo.SelectedDate)
            || (ReadListFilter().MinimumAmount > ReadListFilter().MaximumAmount);
        _filteredTotals.Text = invalid ? "Filtrede geçersiz tutar veya ters tarih/tutar aralığı var; alanları düzeltin."
            : $"Görünen: {visible.Length}/{ActiveInvoices.Count} · Ödeme: {NumberParser.Money(visible.Sum(i => i.PaymentAmount))}   |   Seçili: {selected.Length} · {NumberParser.Money(selected.Sum(i => i.PaymentAmount))}   |   Üst kartlar tüm listeyi gösterir.";
    }

    private void ShowColumnMenu()
    {
        var menu = new ContextMenu();
        foreach (var column in InvoicesGrid.Columns)
        {
            var item = new MenuItem { Header = column.Header, IsCheckable = true, IsChecked = column.Visibility == Visibility.Visible, IsEnabled = column != InvoicesGrid.Columns[1], StaysOpenOnClick = true };
            item.Click += (_, _) => { column.Visibility = item.IsChecked ? Visibility.Visible : Visibility.Collapsed; SaveWorkbenchPreferences(); };
            menu.Items.Add(item);
        }
        menu.PlacementTarget = InvoicesGrid; menu.IsOpen = true;
    }

    private void ImportPastedInvoices()
    {
        var dialog = new InvoicePasteWindow(AllSchools, ActiveInvoices) { Owner = this };
        if (dialog.ShowDialog() != true || dialog.Preview?.CanImport != true) return;
        var incoming = dialog.Preview.Rows.Select(r => r.Invoice!).ToArray();
        foreach (var invoice in incoming) ActiveInvoices.Add(invoice);
        if (!TrySaveActiveInvoices(out var error))
        {
            foreach (var invoice in incoming) ActiveInvoices.Remove(invoice);
            SetInvoiceListStatus(error, true);
        }
        else
        {
            foreach (var invoice in incoming) AppendAudit(AuditTrailEventType.InvoiceCreated, AuditTrailEntityType.Invoice, invoice.Id.ToString("D"), "Source=ExcelClipboard");
            SetInvoiceListStatus($"{incoming.Length} fatura doğrulanarak kaydedildi.", false, true);
        }
        UpdateInvoiceListSummary();
    }

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
        // The legacy ComboBox template lacks PART_EditableTextBox. Use the native template for a real editable input.
        InstitutionBox.Style = (Style)FindResource(typeof(ComboBox));
        InstitutionBox.Height = 42; InstitutionBox.FontSize = 15;
        InstitutionBox.Background = Brushes.White; InstitutionBox.Foreground = Brushes.DarkSlateGray;
        InstitutionBox.IsEditable = true; InstitutionBox.IsTextSearchEnabled = true; TextSearch.SetTextPath(InstitutionBox, "Name");
        var panel = (StackPanel)AddInvoiceButton.Parent;
        panel.Children.Add(_keepDates);
        var saveToList = WorkbenchButton("Kaydet ve listeye git", SaveAndOpenList);
        saveToList.SetBinding(IsEnabledProperty, new Binding("IsEnabled") { Source = AddInvoiceButton });
        panel.Children.Add(saveToList);
        var details = new Expander { Header = "Hesap ayrıntısı", Foreground = Brushes.White, FontSize = 14, Margin = new(0, 8, 0, 0) };
        var detailText = new TextBlock { FontSize = 13, Foreground = Brushes.White, TextWrapping = TextWrapping.Wrap };
        details.Content = detailText;
        details.Expanded += (_, _) => detailText.Text = $"Kural: {_taxService.ActiveRule.Version}\nYürürlük: {_taxService.ActiveRule.EffectiveFrom:dd.MM.yyyy}\nDamga oranı: {_taxService.ActiveRule.StampDutyRate.ToString(CultureInfo.GetCultureInfo("tr-TR"))}\n{VatBaseFormulaText.Text}: {VatBaseText.Text}\n{StampDutyFormulaText.Text}: {StampDutyText.Text}\nÖdeme: {PaymentAmountText.Text}\nAra değerler tam hassasiyetle korunur; görünüm iki ondalıktır.";
        panel.Children.Add(details);
        var recent = new ComboBox { Height = 32, FontSize = 13, Margin = new(0, 0, 0, 8), ToolTip = "Son kullanılan kurumlar", DisplayMemberPath = "Name" };
        _recentSchools = recent;
        recent.ItemsSource = _preferences.RecentSchools.Select(id => AllSchools.FirstOrDefault(s => s.SchoolId == id)).Where(s => s is not null).ToArray();
        recent.SelectionChanged += (_, _) => { if (recent.SelectedItem is SchoolRecord school) { InstitutionTypeBox.SelectedItem = school.InstitutionType; RefreshInvoiceInstitutionChoices(school); } };
        var shortcuts = new WrapPanel { Margin = new(0, 0, 0, 10) };
        var searchField = new StackPanel { Width = 250, Margin = new(0, 0, 12, 0) };
        searchField.Children.Add(new TextBlock { Text = "Kurum ara", FontSize = 12, Foreground = Brushes.DarkSlateGray }); searchField.Children.Add(_institutionSearch);
        var recentField = new StackPanel { Width = 300 };
        recentField.Children.Add(new TextBlock { Text = "Son kullanılan kurumlar", FontSize = 12, Foreground = Brushes.DarkSlateGray }); recentField.Children.Add(recent);
        shortcuts.Children.Add(searchField); shortcuts.Children.Add(recentField);
        _institutionSearch.TextChanged += (_, _) => RefreshInvoiceInstitutionChoices(InstitutionBox.SelectedItem as SchoolRecord);
        ((StackPanel)InvoiceView.Content).Children.Insert(2, shortcuts);
        foreach (var control in new Control[] { PayableAmountBox, VatAmountBox, InvoiceDatePicker, DueDatePicker })
        {
            if (control.Parent is not StackPanel field) continue;
            var error = new TextBlock { FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(255, 180, 170)), TextWrapping = TextWrapping.Wrap, Visibility = Visibility.Collapsed, Margin = new(0, 4, 0, 0) };
            field.Children.Add(error); _entryErrors[control] = error;
        }
        ApplyLightEntrySurface();
    }

    private static void StyleLightDatePicker(DatePicker picker)
    {
        var textStyle = new Style(typeof(System.Windows.Controls.Primitives.DatePickerTextBox));
        textStyle.Setters.Add(new Setter(ForegroundProperty, Brushes.DarkSlateGray));
        textStyle.Setters.Add(new Setter(BackgroundProperty, Brushes.White));
        textStyle.Setters.Add(new Setter(FontSizeProperty, 14d));
        picker.Resources[typeof(System.Windows.Controls.Primitives.DatePickerTextBox)] = textStyle;
        picker.Background = Brushes.White; picker.Foreground = Brushes.DarkSlateGray;
    }

    private void ApplyLightEntrySurface()
    {
        var form = ((StackPanel)InvoiceView.Content).Children.OfType<Grid>().First(g => g.ColumnDefinitions.Count == 3).Children.OfType<Border>().First();
        form.Background = Brushes.White; form.BorderBrush = new SolidColorBrush(Color.FromRgb(210, 222, 234));
        foreach (var text in LogicalDescendants(form).OfType<TextBlock>()) text.Foreground = new SolidColorBrush(Color.FromRgb(37, 56, 79));
        foreach (var box in LogicalDescendants(form).OfType<TextBox>())
        {
            box.Style = (Style)FindResource(typeof(TextBox)); box.Height = 38; box.FontSize = 15; box.Padding = new(10, 0, 10, 0);
            box.Foreground = Brushes.DarkSlateGray; box.Background = box.IsReadOnly ? new SolidColorBrush(Color.FromRgb(235, 240, 246)) : Brushes.White;
            box.BorderBrush = new SolidColorBrush(Color.FromRgb(184, 201, 219));
        }
        foreach (var combo in LogicalDescendants(form).OfType<ComboBox>())
        {
            combo.Style = (Style)FindResource(typeof(ComboBox)); combo.Height = 38; combo.FontSize = 15;
            combo.Background = Brushes.White; combo.Foreground = Brushes.DarkSlateGray;
        }
        StyleLightDatePicker(InvoiceDatePicker); StyleLightDatePicker(DueDatePicker);
        foreach (var error in _entryErrors.Values) error.Foreground = Brushes.Firebrick;
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

    private bool ConfirmEntryDiscard()
    {
        if (string.IsNullOrWhiteSpace(InvoiceNumberBox.Text) && string.IsNullOrWhiteSpace(PayableAmountBox.Text) && string.IsNullOrWhiteSpace(VatAmountBox.Text)) return true;
        return MessageBox.Show(this, "Fatura formundaki kaydedilmemiş alanlar temizlenecek. Devam edilsin mi?", "Taslak değiştir", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes;
    }

    private void SaveWorkbenchPreferences()
    {
        if (!_preferencesHealthy) return;
        try
        {
            _preferences = _preferences with { KeepDates = _keepDates.IsChecked == true, Columns = InvoicesGrid.Columns.Select(c => new ColumnPreference(c.SortMemberPath, c.ActualWidth, c.Visibility == Visibility.Visible, c.DisplayIndex)).ToList() };
            Directory.CreateDirectory(Path.GetDirectoryName(PreferencesPath)!);
            File.WriteAllText(PreferencesPath + ".tmp", JsonSerializer.Serialize(_preferences));
            File.Move(PreferencesPath + ".tmp", PreferencesPath, true);
        }
        catch (Exception ex) { ApplicationCrashLogger.Write(ex, "Workbench.SavePreferences"); }
    }

    private void ApplyDensity()
    {
        InvoicesGrid.RowHeight = _preferences.Compact ? 32 : 44;
        InvoicesGrid.FontSize = _preferences.Compact ? 13 : 15;
        foreach (var grid in new[] { ArchiveGrid, SchoolsGrid, ControlIssuesGrid }) { grid.RowHeight = _preferences.Compact ? 32 : 42; grid.FontSize = _preferences.Compact ? 13 : 15; }
    }

    private void WorkbenchKeyDown(object sender, KeyEventArgs e)
    {
        if (_currentView == "List" && e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control) { _listSearch.Focus(); _listSearch.SelectAll(); e.Handled = true; }
        if (_currentView == "List" && e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control && Keyboard.FocusedElement is not TextBox) { UndoInvoiceDelete(); e.Handled = true; }
    }
}
