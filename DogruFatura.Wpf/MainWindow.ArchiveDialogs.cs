using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DogruFatura.Wpf;

public partial class MainWindow
{
    private void ArchiveRowAction_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: ArchivedInvoiceList archive }) return;
        ArchiveGrid.SelectedItem = archive;
        CreateArchiveActionWindow(archive).ShowDialog();
        e.Handled = true;
    }

    private void ArchiveRowPdf_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ArchivedInvoiceList archive }) { ArchiveGrid.SelectedItem = archive; OpenArchivedList_Click(sender, e); }
        e.Handled = true;
    }

    private void ArchiveRowDelete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: ArchivedInvoiceList archive }) { ArchiveGrid.SelectedItem = archive; DeleteArchivedList_Click(sender, e); }
        e.Handled = true;
    }

    private string ArchiveDetailsText(ArchivedInvoiceList archive)
    {
        var text = $"Paket: {archive.ArchiveNumber} · Kayıt: {archive.ArchivedAt:dd.MM.yyyy HH:mm}\n"
            + $"Tür: {archive.InvoiceType} · Fatura: {archive.InvoiceCount} · Ödeme: {NumberParser.Money(archive.PaymentTotal)}\n"
            + $"Durum: {archive.WorkflowStatus} · HYS: {archive.HysDisplay} · Kapanış: {archive.ClosedAt:dd.MM.yyyy HH:mm}\n"
            + $"PDF: {archive.PdfFileName}\n"
            + $"Gerçekleştirme Görevlisi: {archive.AuthorityProfile.RealizationOfficerName} · {archive.AuthorityProfile.RealizationOfficerTitle}\n"
            + $"Harcama Yetkilisi: {archive.AuthorityProfile.SpendingAuthorityName} · {archive.AuthorityProfile.SpendingAuthorityTitle}\n"
            + "HYS kapanışı banka ödeme teyidi değildir.\n";
        if (!string.IsNullOrWhiteSpace(archive.LegacyEvidenceNote)) text += archive.LegacyEvidenceNote + "\n";
        try
        {
            var events = _auditTrailStore.ReadAll().Where(item => item.EntityId == archive.Id.ToString("D"))
                .OrderByDescending(item => item.OccurredAtUtc);
            text += "\nİşlem Geçmişi\n" + string.Join("\n", events.Select(item =>
                item.OccurredAtUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.CurrentCulture) + " · " + AuditEventLabel(item.EventType)));
        }
        catch (Exception ex) { text += "\nİşlem geçmişi okunamadı: " + ex.Message; }
        return text;
    }

    private Window CreateArchiveActionWindow(ArchivedInvoiceList archive)
    {
        var window = CreateDetailsWindow(archive.IsClosed ? "HYS Kapanışını Geri Al" : "Paketi HYS İle Kapat", ArchiveDetailsText(archive), archive.Invoices.Cast<object>(), new[]
        {
            ("Kurum", "InstitutionName", ""), ("Fatura No", "InvoiceNumber", ""), ("Hizmet No", "ServiceNumber", ""),
            ("Fatura Tarihi", "InvoiceDate", "dd.MM.yyyy"), ("Son Ödeme", "DueDate", "dd.MM.yyyy"),
            ("Tutar", "PayableAmount", "N2"), ("KDV", "VatAmount", "N2"), ("Damga", "StampDuty", "N2"), ("Ödeme", "PaymentAmount", "N2")
        });
        NameScope.SetNameScope(window, new NameScope());
        var root = (Grid)window.Content;
        var oldHeading = root.Children.OfType<StackPanel>().Single();
        root.Children.Remove(oldHeading);
        var heading = new StackPanel();
        heading.Children.Add(new TextBlock { Text = window.Title, Foreground = (Brush)FindResource("Gold500Brush"), FontSize = 19, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 12) });
        var cards = new System.Windows.Controls.Primitives.UniformGrid { Name = "ArchiveSummaryCards", Columns = 4, Margin = new Thickness(0, 0, 0, 10) };
        cards.Children.Add(ArchiveSummaryCard("\uE8A5", "Paket Bilgisi", archive.ArchiveNumber,
            archive.ArchivedAt.ToString("dd.MM.yyyy HH:mm", CultureInfo.CurrentCulture)
            + "\nHYS: " + (archive.IsClosed ? archive.HysDisplay : "Henüz Girilmedi")));
        cards.Children.Add(ArchiveSummaryCard("\uE8D7", "Toplam Damga Vergisi",
            NumberParser.Money(archive.Invoices.Count > 0 ? archive.Invoices.Sum(i => i.StampDuty) : archive.LegacyStampDutyTotal), "Paketin Damga Vergisi Toplamı"));
        cards.Children.Add(ArchiveSummaryCard("\uE8D4", "Toplam Ödeme", NumberParser.Money(archive.PaymentTotal), "Fatura Tutarı: " + NumberParser.Money(archive.Invoices.Count > 0 ? archive.Invoices.Sum(i => i.PayableAmount) : archive.LegacyGrossTotal)));
        cards.Children.Add(ArchiveSummaryCard("\uE8EC", "Toplam KDV",
            archive.IsLegacyEvidenceOnly ? "Bilgi Yok" : NumberParser.Money(archive.Invoices.Sum(i => i.VatAmount)),
            archive.IsLegacyEvidenceOnly ? "Kaynak Belgede KDV Ayrıntısı Yok" : "Paketin KDV Toplamı"));
        heading.Children.Add(cards);
        var extra = new Expander { Header = "Belge, Yetkili Ve İşlem Ayrıntıları", Foreground = (Brush)FindResource("PremiumMutedTextBrush"), FontSize = 12, Margin = new Thickness(0, 0, 0, 10) };
        extra.Content = new TextBox { Text = ArchiveDetailsText(archive), IsReadOnly = true, Background = Brushes.Transparent, Foreground = (Brush)FindResource("PremiumMutedTextBrush"), BorderThickness = new Thickness(0), FontSize = 12, TextWrapping = TextWrapping.Wrap, MaxHeight = 105, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        heading.Children.Add(extra); root.Children.Add(heading); window.RegisterName(cards.Name, cards);
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var footer = new StackPanel { Margin = new Thickness(0, 14, 0, 0) };
        var label = new TextBlock { Text = archive.IsClosed ? "Kayıtlı HYS Belge No" : "HYS Ödeme Emri Belge No", Foreground = (System.Windows.Media.Brush)FindResource("Gold500Brush"), FontSize = 13, Margin = new Thickness(0, 0, 0, 6) };
        footer.Children.Add(label);
        var actions = new Grid();
        foreach (var width in new[] { new GridLength(1,GridUnitType.Star), new GridLength(12), GridLength.Auto, new GridLength(8), GridLength.Auto }) actions.ColumnDefinitions.Add(new ColumnDefinition { Width = width });
        var hys = new TextBox { Name = "ArchiveHysBox", Style = (Style)FindResource("PremiumFieldTextBox"), Text = archive.HysPaymentOrderDocumentNumber,
            IsReadOnly = archive.IsClosed, MaxLength = 44, ToolTip = "HYS ödeme emri belge numarasını yazın." };
        if (archive.IsClosed) { label.Visibility = Visibility.Collapsed; hys.Visibility = Visibility.Collapsed; }
        var save = new Button { Name = "ArchiveCommitButton", Content = archive.IsClosed ? "Düzenle" : "Kaydet", Style = (Style)FindResource("GoldActionButton"), MinWidth = 100 };
        var cancel = new Button { Name = "ArchiveCancelButton", Content = "Vazgeç", Style = (Style)FindResource("ModernButton"), MinWidth = 100 };
        Grid.SetColumn(save, 2); Grid.SetColumn(cancel, 4); actions.Children.Add(hys); actions.Children.Add(save); actions.Children.Add(cancel);
        footer.Children.Add(actions);
        var error = new TextBlock { Name = "ArchiveErrorText", Foreground = System.Windows.Media.Brushes.LightSalmon, FontSize = 13, TextWrapping = TextWrapping.Wrap, Visibility = Visibility.Collapsed, Margin = new Thickness(0, 8, 0, 0) };
        footer.Children.Add(error); Grid.SetRow(footer, 2); root.Children.Add(footer);
        foreach (var element in new FrameworkElement[] { hys, save, cancel, error }) window.RegisterName(element.Name, element);
        cancel.Click += (_, _) => window.Close();
        window.PreviewKeyDown += (_, e) => { if (e.Key == System.Windows.Input.Key.Escape) { window.Close(); e.Handled = true; } };
        save.Click += (_, _) =>
        {
            if (!window.IsVisible) return;
            var current = ArchivedLists.FirstOrDefault(item => item.Id == archive.Id);
            if (current is null) { error.Text = "Paket artık listede bulunmuyor."; error.Visibility = Visibility.Visible; return; }
            if (current.IsClosed != archive.IsClosed) { error.Text = "Paketin durumu değişti. Bu pencereyi kapatıp işlemi yeniden açın."; error.Visibility = Visibility.Visible; return; }
            if (current.IsClosed && ActiveInvoices.Count > 0)
            {
                MessageBox.Show(window, "Önce mevcut aktif fatura listesini tamamlayıp paket olarak kaydedin. Bu paket üzerinde değişiklik yapılmadı.", "Aktif Liste Dolu", MessageBoxButton.OK, MessageBoxImage.Information);
                window.Close(); return;
            }
            string message;
            var success = current.IsClosed ? TryRestoreArchiveToActive(current, out message) : TryCloseArchive(current, hys.Text, out message);
            if (success) window.Close();
            else { error.Text = message; error.Visibility = Visibility.Visible; }
        };
        window.Loaded += (_, _) => InformationCopyService.Apply(window);
        return window;
    }

    private Border ArchiveSummaryCard(string symbol, string title, string value, string detail)
    {
        var content = new StackPanel();
        var caption = new DockPanel { Margin = new Thickness(0, 0, 0, 10) };
        caption.Children.Add(new TextBlock { Text = symbol, FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 24, Foreground = (Brush)FindResource("Gold500Brush"), Margin = new Thickness(0, 0, 9, 0) });
        caption.Children.Add(new TextBlock { Text = title, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource("Gold500Brush"), VerticalAlignment = VerticalAlignment.Center });
        content.Children.Add(caption);
        content.Children.Add(new TextBox { Text = value, Style = (Style)FindResource("CopyableInfo"), FontSize = 16, FontWeight = FontWeights.Bold, TextWrapping = TextWrapping.Wrap });
        content.Children.Add(new TextBlock { Text = detail, FontSize = 11.5, Foreground = (Brush)FindResource("PremiumMutedTextBrush"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 0) });
        return new Border { Style = (Style)FindResource("PaymentMetricCard"), Padding = new Thickness(12), Margin = new Thickness(0, 0, 8, 0), MinHeight = 112, Child = content };
    }

    private bool TryCloseArchive(ArchivedInvoiceList archive, string hys, out string error)
    {
        error = "";
        var index = ArchivedLists.IndexOf(archive);
        if (index < 0 || archive.IsClosed) { error = "HYS bekleyen geçerli bir paket seçin."; return false; }
        if (archive.IsLegacyEvidenceOnly) { error = "Bu kaynak belgede doğrulanmış fatura satırları bulunmuyor; HYS kaydı değiştirilemez."; return false; }
        if (!HysPaymentOrderService.TryNormalize(hys, out var normalized, out error)) return false;
        if (ArchivedLists.Any(item => item.Id != archive.Id && string.Equals(item.HysPaymentOrderDocumentNumber, normalized, StringComparison.OrdinalIgnoreCase)))
        { error = "Bu HYS belge numarası başka bir pakette kullanılıyor."; return false; }
        var move = PdfMoveResult.None(archive.PdfFileName);
        ArchivedInvoiceList updated;
        try
        {
            move = _documentArchiveStorageService.MoveActiveOutputToArchive(archive, normalized);
            updated = ClosedArchiveSnapshotService.Seal(archive with { HysPaymentOrderDocumentNumber = normalized, IsClosed = true, ClosedAt = DateTime.Now, PdfFileName = move.PdfFileName });
            var validation = ArchivedInvoiceListValidationService.Validate(updated);
            if (!validation.IsValid) { DocumentArchiveStorageService.RollbackMove(move); error = validation.Message; return false; }
            ArchivedLists[index] = updated;
            _archiveDataStore.Save(ArchivedLists);
        }
        catch (Exception ex)
        {
            ArchivedLists[index] = archive; DocumentArchiveStorageService.RollbackMove(move);
            error = "HYS kapanışı kaydedilemedi; paket korundu. " + ex.Message; return false;
        }
        AppendAudit(AuditTrailEventType.ArchiveClosed, AuditTrailEntityType.Archive, archive.Id.ToString("D"), "HYS=" + normalized);
        SynchronizeArchivePdfs(false);
        RefreshArchiveView(updated.Id); RefreshDashboard();
        SetArchiveStatus(normalized + " ile HYS kapanışı kaydedildi.", false, true);
        return true;
    }
}
