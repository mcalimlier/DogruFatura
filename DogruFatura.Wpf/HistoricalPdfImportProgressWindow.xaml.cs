using System.ComponentModel;
using System.Windows;

namespace DogruFatura.Wpf;

public partial class HistoricalPdfImportProgressWindow : Window
{
    private bool _allowClose;
    private bool _cancelRequested;

    public HistoricalPdfImportProgressWindow()
    {
        InitializeComponent();
    }

    public event EventHandler? CancelRequested;

    public void SetFileProgress(int current, int total, string fileName, string stageMessage)
    {
        FileText.Text = $"{current}/{total} dosya işleniyor • {fileName}";
        MessageText.Text = stageMessage;
    }

    public void SetWorking(string message)
    {
        if (_cancelRequested)
            return;

        MessageText.Text = string.IsNullOrWhiteSpace(message)
            ? "Arşiv PDF taranıyor, lütfen bekleyiniz..."
            : message;
    }

    public void SetCancelling()
    {
        _cancelRequested = true;
        MessageText.Text = "İşlem iptal ediliyor, lütfen bekleyiniz...";
        CancelButton.IsEnabled = false;
    }

    public void CompleteAndClose()
    {
        _allowClose = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) =>
        RequestCancel();

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_allowClose)
        {
            e.Cancel = true;
            RequestCancel();
            return;
        }

        base.OnClosing(e);
    }

    private void RequestCancel()
    {
        if (_cancelRequested)
            return;

        SetCancelling();
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}
