using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DogruFatura.Wpf.Workbench;

public sealed class PdfReviewWindow : Window
{
    public ArchivedInvoiceList? ReviewedArchive { get; private set; }
    public PdfReviewWindow(string fileName, string hys, IReadOnlyList<InvoiceRecord> invoices, AuthorityProfile authority)
    {
        Title = "PDF kaynak toplamlarını doğrula"; Width = 650; Height = 610; MinWidth = 550; MinHeight = 540;
        WindowStartupLocation = WindowStartupLocation.CenterOwner; Background = Brushes.White;
        var panel = new StackPanel { Margin = new(20) };
        panel.Children.Add(new TextBlock { Text = fileName, FontSize = 20, FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap });
        panel.Children.Add(new TextBlock { Text = "Özgün PDF üzerindeki resmî toplamları girin. Hesaplanan satırlar bu üç toplamla eşleşmedikçe kayıt oluşturulmaz. HYS kapanışı banka ödeme teyidi değildir.", TextWrapping = TextWrapping.Wrap, FontSize = 13, Margin = new(0, 10, 0, 14) });
        TextBox Field(string label, string text = "")
        {
            panel.Children.Add(new TextBlock { Text = label, FontSize = 13 });
            var box = new TextBox { Text = text, FontSize = 15, Height = 34, Margin = new(0, 3, 0, 9) }; panel.Children.Add(box); return box;
        }
        var number = Field("HYS ödeme emri belge no", hys == "—" ? "" : hys);
        var gross = Field("Kaynak brüt toplamı"); var stamp = Field("Kaynak damga toplamı"); var payment = Field("Kaynak net ödeme toplamı");
        var reason = Field("Hangi alanlar düzeltildi, kaynakta nasıl doğrulandı?"); reason.Height = 55; reason.AcceptsReturn = true; reason.TextWrapping = TextWrapping.Wrap;
        var status = new TextBlock { FontSize = 13, TextWrapping = TextWrapping.Wrap, Foreground = Brushes.Firebrick, Margin = new(0, 5, 0, 10) }; panel.Children.Add(status);
        var button = new Button { Content = "Toplamları doğrula ve önizlemeye dön", Padding = new(12, 8, 12, 8), HorizontalAlignment = HorizontalAlignment.Left };
        button.Click += (_, _) =>
        {
            if (!NumberParser.TryNonNegativeDecimal(gross.Text, out var g) || !NumberParser.TryNonNegativeDecimal(stamp.Text, out var s) || !NumberParser.TryNonNegativeDecimal(payment.Text, out var p))
            { status.Text = "Üç kaynak toplamını geçerli para biçiminde girin."; return; }
            var result = PdfReviewService.Review(number.Text, invoices, g, s, p, reason.Text, authority);
            if (!result.IsValid) { status.Text = result.Message; return; }
            ReviewedArchive = result.Archive; DialogResult = true;
        };
        panel.Children.Add(button); Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    }
}
