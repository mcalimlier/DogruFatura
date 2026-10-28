using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace DogruFatura.Wpf.Workbench;

public sealed class InvoicePasteWindow : Window
{
    private readonly TextBox _source = new() { AcceptsReturn = true, AcceptsTab = true, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, FontSize = 14, Height = 130 };
    private readonly DataGrid _grid = new() { AutoGenerateColumns = false, IsReadOnly = true, CanUserAddRows = false, FontSize = 14 };
    private readonly Button _import = new() { Content = "Doğrulanan satırları ekle", IsEnabled = false, Padding = new(14, 8, 14, 8) };
    private readonly TextBlock _status = new() { FontSize = 14, TextWrapping = TextWrapping.Wrap, Margin = new(0, 8, 0, 8) };
    public InvoicePastePreview? Preview { get; private set; }

    public InvoicePasteWindow(IEnumerable<SchoolRecord> schools, IEnumerable<InvoiceRecord> existing)
    {
        Title = "DoğruFatura · Hibrit Excel girişi";
        Language = System.Windows.Markup.XmlLanguage.GetLanguage("tr-TR");
        Width = 1050; Height = 690; MinWidth = 720; MinHeight = 500;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brushes.White;
        var schoolSnapshot = schools.ToArray(); var activeSnapshot = existing.ToArray();
        var root = new DockPanel { Margin = new(20) };
        var top = new StackPanel(); DockPanel.SetDock(top, Dock.Top); root.Children.Add(top);
        top.Children.Add(new TextBlock { Text = "Excel'den kopyalayın, doğrulayın, birlikte kaydedin", FontSize = 22, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
        top.Children.Add(new TextBlock { Text = "Sütun sırası: Kurum · Tür · Fatura No · Fatura Tarihi · Son Ödeme · Tutar · KDV · Kesinti Tazminatı (isteğe bağlı).\nHesaplanan matrah, damga ve ödeme alanları korunur. Hata varsa hiçbir satır eklenmez.", FontSize = 13, TextWrapping = TextWrapping.Wrap, Margin = new(0, 8, 0, 10) });
        top.Children.Add(_source);
        var buttons = new WrapPanel { Margin = new(0, 8, 0, 0) }; top.Children.Add(buttons);
        var header = new Button { Content = "Başlıkları kopyala", Padding = new(10, 6, 10, 6), Margin = new(0, 0, 8, 0) };
        header.Click += (_, _) => { try { Clipboard.SetText(InvoicePasteService.Header); } catch (Exception ex) { _status.Text = ex.Message; } }; buttons.Children.Add(header);
        var check = new Button { Content = "Önizle ve doğrula", Padding = new(10, 6, 10, 6) }; buttons.Children.Add(check);
        top.Children.Add(_status);
        foreach (var (title, binding) in new[] { ("Satır", "Line"), ("Kurum", "Institution"), ("Fatura No", "Number"), ("Tutar", "Payable"), ("KDV", "Vat"), ("Damga", "Stamp"), ("Ödeme", "Payment"), ("Kontrol", "Message") })
            _grid.Columns.Add(new DataGridTextColumn { Header = title, Binding = new Binding(binding) { StringFormat = binding is "Payable" or "Vat" or "Stamp" or "Payment" ? "N2" : null }, Width = binding == "Message" ? new DataGridLength(2, DataGridLengthUnitType.Star) : DataGridLength.Auto });
        var footer = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new(0, 10, 0, 0) };
        DockPanel.SetDock(footer, Dock.Bottom); root.Children.Add(footer);
        var cancel = new Button { Content = "Vazgeç", Padding = new(14, 8, 14, 8), Margin = new(0, 0, 8, 0), IsCancel = true }; footer.Children.Add(cancel); footer.Children.Add(_import);
        root.Children.Add(_grid); Content = root;
        _source.TextChanged += (_, _) => { Preview = null; _import.IsEnabled = false; _status.Text = "İçerik değişti; yeniden doğrulayın."; _grid.ItemsSource = null; };
        check.Click += async (_, _) =>
        {
            var text = _source.Text;
            check.IsEnabled = false; _import.IsEnabled = false; _status.Text = "Satırlar doğrulanıyor…";
            try
            {
                var preview = await Task.Run(() => InvoicePasteService.Preview(text, schoolSnapshot, activeSnapshot));
                if (!string.Equals(text, _source.Text, StringComparison.Ordinal)) { _status.Text = "İçerik değişti; yeniden doğrulayın."; return; }
                Preview = preview;
                _grid.ItemsSource = preview.Rows; _import.IsEnabled = preview.CanImport;
                _status.Text = preview.CanImport ? $"{preview.Rows.Count} fatura doğrulandı · Ödeme: {NumberParser.Money(preview.Rows.Sum(r => r.Payment))}" : "Kayıt yapılmadı. Kontrol sütunundaki hataları düzeltip tekrar önizleyin.";
            }
            catch (Exception ex) { Preview = null; _import.IsEnabled = false; _status.Text = "Doğrulama tamamlanamadı; kayıt yapılmadı. " + ex.Message; }
            finally { check.IsEnabled = true; }
        };
        _import.Click += (_, _) => { if (Preview?.CanImport == true) DialogResult = true; };
    }
}
