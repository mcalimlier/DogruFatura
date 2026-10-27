using System.Windows;
using System.Windows.Controls;
using DogruFatura.Core.Services;
using System.Globalization;

namespace DogruFatura.Wpf.Workbench;

public partial class WorkbenchPanel : UserControl
{
    public event Action<string>? ActionRequested;
    public WorkbenchPanel() => InitializeComponent();
    private void Navigate_Click(object sender, RoutedEventArgs e) => ActionRequested?.Invoke((string)((Button)sender).Tag);
    public void Refresh(WorkbenchSummary summary, string type)
    {
        ActiveText.Text = summary.ActiveCount.ToString(CultureInfo.CurrentCulture);
        DueText.Text = $"{summary.DueSoonCount} yakın · {summary.OverdueCount} geçmiş";
        PendingText.Text = summary.PendingHysCount.ToString(CultureInfo.CurrentCulture);
        IssuesText.Text = summary.IssueCount.ToString(CultureInfo.CurrentCulture);
        ContinueText.Text = $"{type} · {summary.ActiveCount} fatura · {NumberParser.Money(summary.ActivePaymentTotal)}";
        NextText.Text = summary.NextAction;
    }
}
