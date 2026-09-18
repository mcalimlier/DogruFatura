using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace DogruFatura.Wpf;

public partial class MainWindow
{    private SafetyCheckResult RefreshSafetyInformation()
    {
        AboutVersionText.Text = "DoğruFatura " + _safetyService.InformationalVersion;
        AboutRuntimeText.Text = _safetyService.RuntimeDescription;
        AboutDataFolderText.Text = _safetyService.DataFolder;
        AboutDocumentFolderText.Text = _safetyService.DocumentRootFolder;
        AboutBackupFolderText.Text = _safetyService.BackupFolder;

        SafetyCheckResult result;
        try
        {
            result = _safetyService.CheckIntegrity();
        }
        catch (Exception ex)
        {
            result = new SafetyCheckResult(
                false,
                "Güvenlik kontrolü çalıştırılamadı: " + ex.Message,
                null,
                1,
                0,
                0);
        }

        DataIntegrityText.Text = result.Message;
        DataIntegrityText.Foreground = new SolidColorBrush(result.IsHealthy
            ? Color.FromRgb(98, 217, 155)
            : Color.FromRgb(255, 180, 31));
        LastSafetyBackupText.Text = string.IsNullOrWhiteSpace(_lastSafetyBackupPath)
            ? "Bu sürümde henüz yeni güvenlik yedeği oluşturulmadı."
            : "Son güvenlik yedeği: " + _lastSafetyBackupPath;
        return result;
    }

    private void RunIntegrityCheck_Click(object sender, RoutedEventArgs e)
    {
        var result = RefreshSafetyInformation();
        if (result.IsHealthy) return;

        MessageBox.Show(this,
            DataIntegrityText.Text,
            "DoğruFatura - Veri Bütünlüğü",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private void CreateSafetyBackup_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Mouse.OverrideCursor = Cursors.Wait;
            _lastSafetyBackupPath = _safetyService.CreateManualSafetyBackup();
            RefreshSafetyInformation();
            MessageBox.Show(this,
                "JSON kayıtları ile aktif/arşiv PDF belgelerinin tam güvenlik yedeği oluşturuldu:\n\n"
                + _lastSafetyBackupPath,
                "DoğruFatura - Güvenlik Yedeği",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this,
                "Güvenlik yedeği oluşturulamadı:\n\n" + ex.Message,
                "DoğruFatura - Güvenlik Yedeği",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private void RestoreSafetyBackup_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "DoğruFatura Güvenlik Yedeğini Seç",
            Filter = "DoğruFatura ZIP yedeği (*.zip)|*.zip|Tüm dosyalar (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false,
            InitialDirectory = _safetyService.BackupFolder
        };
        if (dialog.ShowDialog(this) != true) return;

        var confirmation = MessageBox.Show(this,
            "Seçilen tam yedek geri yüklenecek. Mevcut JSON kayıtları ve Masaüstündeki DoğruFatura PDF klasörleri değiştirilmeden önce otomatik emniyet kopyası oluşturulacaktır.\n\n"
            + "Formlarda henüz kaydedilmemiş değişiklikler kaybolur. Devam edilsin mi?",
            "DoğruFatura - Yedekten Geri Yükle",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        if (confirmation != MessageBoxResult.Yes) return;

        SafetyRestoreResult result;
        try
        {
            Mouse.OverrideCursor = Cursors.Wait;
            result = _safetyService.RestoreFromBackup(dialog.FileName);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }

        if (!result.Success)
        {
            MessageBox.Show(this,
                result.Message,
                "DoğruFatura - Yedekten Geri Yükle",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        _lastSafetyBackupPath = result.SafetyBackupPath;
        var message = result.Message;
        if (!string.IsNullOrWhiteSpace(result.SafetyBackupPath))
            message += "\n\nGeri yükleme öncesi emniyet kopyası:\n" + result.SafetyBackupPath;
        message += "\n\nYeni verilerin güvenli biçimde yüklenmesi için DoğruFatura yeniden başlatılacak.";
        MessageBox.Show(this,
            message,
            "DoğruFatura - Yedekten Geri Yükle",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        RestartApplication();
    }

    private void CreateDiagnosticPackage_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var path = DiagnosticPackageService.Create(_safetyService);
            UiNotificationService.Show(
                this,
                "Tanılama paketi oluşturuldu:\n" + path,
                TimeSpan.FromSeconds(6));
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                this,
                "Tanılama paketi oluşturulamadı:\n\n" + ex.Message,
                "DoğruFatura - Tanılama",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void OpenDataFolder_Click(object sender, RoutedEventArgs e) => OpenFolder(_safetyService.DataFolder);
    private void OpenDocumentFolder_Click(object sender, RoutedEventArgs e) => OpenFolder(_safetyService.DocumentRootFolder);
    private void OpenBackupFolder_Click(object sender, RoutedEventArgs e) => OpenFolder(_safetyService.BackupFolder);
    private void OpenLogFolder_Click(object sender, RoutedEventArgs e) => OpenFolder(_safetyService.LogFolder);

    private static void OpenFolder(string folder)
    {
        try
        {
            Directory.CreateDirectory(folder);
            Process.Start(new ProcessStartInfo { FileName = folder, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Klasör açılamadı:\n\n" + ex.Message,
                "DoğruFatura",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private static void RestartApplication()
    {
        try
        {
            var executablePath = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(executablePath) && File.Exists(executablePath))
            {
                App.ReleaseSingleInstanceForRestart();
                Process.Start(new ProcessStartInfo
                {
                    FileName = executablePath,
                    UseShellExecute = true
                });
            }
        }
        catch
        {
            // Veriler geri yüklendi; yeniden açma başarısızsa kullanıcı programı normal yoldan açabilir.
        }
        Application.Current.Shutdown();
    }
}
