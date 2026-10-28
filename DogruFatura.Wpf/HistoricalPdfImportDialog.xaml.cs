using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using System.Windows.Data;
using DogruFatura.Core.Models;
using UglyToad.PdfPig;
using DogruFatura.Core.Services;
using DogruFatura.Wpf.Workbench;

namespace DogruFatura.Wpf;

public partial class HistoricalPdfImportDialog : Window
{
    private readonly IReadOnlyList<SchoolRecord> _schools;
    private readonly AuthorityProfile _authority;
    private readonly HashSet<string> _knownHys;
    public HistoricalPdfImportDialog(IEnumerable<HistoricalPdfImportPreview> previews, IReadOnlyList<SchoolRecord>? schools = null,
        AuthorityProfile? authority = null, IEnumerable<string>? knownHys = null)
    {
        _schools = schools ?? Array.Empty<SchoolRecord>(); _authority = authority ?? AuthorityProfileDefaults.Create();
        _knownHys = new HashSet<string>(knownHys ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        Items = new ObservableCollection<HistoricalPdfImportPreview>(previews);
        InitializeComponent();
        DataContext = this;
        Width = Math.Min(1200, SystemParameters.WorkArea.Width * .96);
        Height = Math.Min(720, SystemParameters.WorkArea.Height * .92);
        RefreshSummary();
        PreviewGrid.SelectedItem = Items.FirstOrDefault();
        UpdateSelectedDetail();
    }

    private void RefreshSummary()
    {
        var validCount = Items.Count(item => item.IsValid);
        var alreadyArchivedCount = Items.Count(item => item.AlreadyArchived);
        var invalidCount = Items.Count - validCount - alreadyArchivedCount;
        var ocrCount = Items.Count(item => item.OcrAttempted);
        SummaryText.Text = $"{Items.Count} dosya • {validCount} eklemeye hazır • {alreadyArchivedCount} zaten arşivde • {invalidCount} inceleme gerekiyor • {ocrCount} OCR denendi";
        ConfirmButton.IsEnabled = validCount > 0;
        ConfirmButton.Content = $"{validCount} DOSYAYI EKLE";
    }

    public ObservableCollection<HistoricalPdfImportPreview> Items { get; }
    public IReadOnlyList<HistoricalPdfImportPreview> ValidItems =>
        Items.Where(item => item.IsValid && item.Archive is not null).ToArray();

    private void PreviewGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        UpdateSelectedDetail();

    private void UpdateSelectedDetail()
    {
        SelectedDetailText.Text = PreviewGrid.SelectedItem is HistoricalPdfImportPreview selected
            ? $"{selected.FileName} • {selected.ScanDetail}: {selected.Status}"
            : "Ayrıntı için bir dosya seçin.";
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Inspect_Click(object sender, RoutedEventArgs e)
    {
        if (PreviewGrid.SelectedItem is not HistoricalPdfImportPreview selected) return;
        var inspect = new Window { Title = "PDF kaynak ve kayıt karşılaştırması", Owner = this, Width = 1080, Height = 680, MinWidth = 700, MinHeight = 450, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        var root = new DockPanel { Margin = new(16) };
        var heading = new StackPanel(); DockPanel.SetDock(heading, Dock.Top); root.Children.Add(heading);
        heading.Children.Add(new TextBlock { Text = selected.FileName + " · " + selected.ResultText, FontSize = 19, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
        heading.Children.Add(new TextBlock { Text = selected.Status + "\nBelirsiz sonuçlar bu ekrandan zorla onaylanamaz. Kaynağı kontrol edip güçlü taramayla yeniden deneyebilirsiniz.", FontSize = 13, TextWrapping = TextWrapping.Wrap, Margin = new(0, 8, 0, 8) });
        var open = new Button { Content = "Özgün PDF'yi aç", HorizontalAlignment = HorizontalAlignment.Left, Padding = new(12, 7, 12, 7), Margin = new(0, 0, 0, 12) };
        open.Click += (_, _) => { try { if (File.Exists(selected.SourcePath)) Process.Start(new ProcessStartInfo(selected.SourcePath) { UseShellExecute = true }); } catch (Exception ex) { MessageBox.Show(inspect, ex.Message, "PDF açılamadı", MessageBoxButton.OK, MessageBoxImage.Warning); } }; heading.Children.Add(open);
        var columns = new Grid(); columns.ColumnDefinitions.Add(new()); columns.ColumnDefinitions.Add(new()); root.Children.Add(columns);
        var source = new TextBox { IsReadOnly = true, TextWrapping = TextWrapping.Wrap, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, FontSize = 13, Margin = new(0, 0, 10, 0) };
        inspect.Loaded += async (_, _) =>
        {
            source.Text = "Kaynak metni okunuyor…";
            try
            {
                source.Text = await Task.Run(() =>
                {
                    using var pdf = PdfDocument.Open(selected.SourcePath);
                    return "Metin katmanı önizlemesi · İlk 40 sayfa. Tam belgeyi Özgün PDF'yi aç ile inceleyin.\n\n"
                        + string.Join("\n\n", pdf.GetPages().Take(40).Select(p => $"Sayfa {p.Number}\n{p.Text}"));
                });
            }
            catch (Exception ex) { source.Text = "Kaynak metni okunamadı: " + ex.Message; }
        };
        columns.Children.Add(source);
        var records = new DataGrid { AutoGenerateColumns = false, IsReadOnly = true, CanUserAddRows = false, FontSize = 13 };
        foreach (var (title, property) in new[] { ("Kurum", "InstitutionName"), ("Fatura No", "InvoiceNumber"), ("Tutar", "PayableAmount"), ("Damga", "StampDuty"), ("Ödeme", "PaymentAmount") }) records.Columns.Add(new DataGridTextColumn { Header = title, Binding = new Binding(property), Width = DataGridLength.Auto });
        records.ItemsSource = selected.Archive?.Invoices ?? new List<InvoiceRecord>(); Grid.SetColumn(records, 1); columns.Children.Add(records);
        inspect.Content = root; inspect.ShowDialog();
    }

    private void Review_Click(object sender, RoutedEventArgs e)
    {
        if (PreviewGrid.SelectedItem is not HistoricalPdfImportPreview selected || selected.AlreadyArchived) return;
        var rows = new InvoicePasteWindow(_schools, Array.Empty<InvoiceRecord>()) { Owner = this, Title = "PDF satırlarını kaynakla karşılaştırarak girin" };
        if (rows.ShowDialog() != true || rows.Preview?.CanImport != true) return;
        var invoices = rows.Preview.Rows.Select(r => r.Invoice!).ToArray();
        var review = new PdfReviewWindow(selected.FileName, selected.HysNumber, invoices, _authority) { Owner = this };
        if (review.ShowDialog() != true || review.ReviewedArchive is not { } archive) return;
        if (_knownHys.Contains(archive.HysPaymentOrderDocumentNumber)
            || Items.Any(item => !ReferenceEquals(item, selected) && item.IsValid && string.Equals(item.HysNumber, archive.HysPaymentOrderDocumentNumber, StringComparison.OrdinalIgnoreCase)))
        {
            SelectedDetailText.Text = "Bu HYS numarası zaten kayıtlı veya başka seçili dosyada kullanılıyor; düzeltme eklenmedi.";
            return;
        }
        var corrected = selected with { IsValid = true, Status = archive.LegacyEvidenceNote, HysNumber = archive.HysPaymentOrderDocumentNumber,
            InvoiceType = archive.InvoiceType, InvoiceCount = archive.InvoiceCount, PaymentTotal = archive.PaymentTotal, Archive = archive, ScanDetail = "Kullanıcı doğrulaması · toplamlar eşleşti" };
        Items[Items.IndexOf(selected)] = corrected; PreviewGrid.SelectedItem = corrected; RefreshSummary();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed) DragMove();
    }
}
