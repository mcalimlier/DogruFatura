using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DogruFatura.Wpf;

public partial class HistoricalPdfImportDialog : Window
{
    public HistoricalPdfImportDialog(IEnumerable<HistoricalPdfImportPreview> previews)
    {
        Items = new ObservableCollection<HistoricalPdfImportPreview>(previews);
        InitializeComponent();
        DataContext = this;

        var validCount = Items.Count(item => item.IsValid);
        var invalidCount = Items.Count - validCount;
        SummaryText.Text = $"{Items.Count} dosya • {validCount} arşive eklemeye hazır • {invalidCount} hatalı";
        ConfirmButton.IsEnabled = validCount > 0;
        ConfirmButton.Content = $"{validCount} DOSYAYI EKLE";
        PreviewGrid.SelectedItem = Items.FirstOrDefault();
        UpdateSelectedDetail();
    }

    public ObservableCollection<HistoricalPdfImportPreview> Items { get; }
    public IReadOnlyList<HistoricalPdfImportPreview> ValidItems =>
        Items.Where(item => item.IsValid && item.Archive is not null).ToArray();

    private void PreviewGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        UpdateSelectedDetail();

    private void UpdateSelectedDetail()
    {
        SelectedDetailText.Text = PreviewGrid.SelectedItem is HistoricalPdfImportPreview selected
            ? $"{selected.FileName}: {selected.Status}"
            : "Ayrıntı için bir dosya seçin.";
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
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
