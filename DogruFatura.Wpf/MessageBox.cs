using System.Windows;

namespace DogruFatura.Wpf;

/// <summary>
/// Kritik hata/onay pencerelerini Windows MessageBox olarak korur; yalnız güvenlik yedeğinin
/// başarılı oluşturulması gibi bloklaması gerekmeyen bilgi mesajlarını uygulama içi bildirime çevirir.
/// </summary>
public static class MessageBox
{
    public static MessageBoxResult Show(string messageBoxText) =>
        System.Windows.MessageBox.Show(messageBoxText);

    public static MessageBoxResult Show(string messageBoxText, string caption) =>
        System.Windows.MessageBox.Show(messageBoxText, caption);

    public static MessageBoxResult Show(
        string messageBoxText,
        string caption,
        MessageBoxButton button,
        MessageBoxImage icon) =>
        System.Windows.MessageBox.Show(messageBoxText, caption, button, icon);

    public static MessageBoxResult Show(
        string messageBoxText,
        string caption,
        MessageBoxButton button,
        MessageBoxImage icon,
        MessageBoxResult defaultResult) =>
        System.Windows.MessageBox.Show(messageBoxText, caption, button, icon, defaultResult);

    public static MessageBoxResult Show(
        Window owner,
        string messageBoxText,
        string caption,
        MessageBoxButton button,
        MessageBoxImage icon)
    {
        if (CanUseInlineInformation(owner, messageBoxText, caption, button, icon))
        {
            UiNotificationService.Show(owner, messageBoxText, TimeSpan.FromSeconds(5.5));
            return MessageBoxResult.OK;
        }
        return System.Windows.MessageBox.Show(owner, messageBoxText, caption, button, icon);
    }

    public static MessageBoxResult Show(
        Window owner,
        string messageBoxText,
        string caption,
        MessageBoxButton button,
        MessageBoxImage icon,
        MessageBoxResult defaultResult)
    {
        if (CanUseInlineInformation(owner, messageBoxText, caption, button, icon))
        {
            UiNotificationService.Show(owner, messageBoxText, TimeSpan.FromSeconds(5.5));
            return MessageBoxResult.OK;
        }
        return System.Windows.MessageBox.Show(owner, messageBoxText, caption, button, icon, defaultResult);
    }

    private static bool CanUseInlineInformation(
        Window owner,
        string message,
        string caption,
        MessageBoxButton button,
        MessageBoxImage icon) =>
        owner is MainWindow
        && owner.IsLoaded
        && button == MessageBoxButton.OK
        && icon == MessageBoxImage.Information
        && caption.Contains("Güvenlik Yedeği", StringComparison.CurrentCultureIgnoreCase)
        && message.Contains("oluşturuldu", StringComparison.CurrentCultureIgnoreCase);
}
