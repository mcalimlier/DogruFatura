using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;

namespace DogruFatura.Wpf;

public partial class MainWindow
{
    private static readonly TimeSpan HistoricalPdfStrongScanTimeout = TimeSpan.FromMinutes(2);
    private CancellationTokenSource? _historicalPdfImportCancellation;

    private void SynchronizeArchivePdfs_Click(object sender, RoutedEventArgs e) =>
        SynchronizeArchivePdfs(true);

    private ArchivePdfSynchronizationResult? SynchronizeArchivePdfs(bool userInitiated)
    {
        ArchivePdfSynchronizationResult result;
        try
        {
            if (userInitiated)
            {
                Mouse.OverrideCursor = Cursors.Wait;
                ArchivePdfSyncButton.IsEnabled = false;
                SetArchiveStatus("Arşiv kayıtları ile PDF klasörleri eşitleniyor...");
            }

            result = _archivePdfSynchronizationService.Synchronize(ArchivedLists.ToList());
        }
        catch (Exception ex)
        {
            SetArchiveStatus("Belge eşitleme başlatılamadı: " + ex.Message, true);
            return null;
        }
        finally
        {
            if (userInitiated)
            {
                Mouse.OverrideCursor = null;
                ArchivePdfSyncButton.IsEnabled = true;
            }
        }

        var replacedArchives = new List<(int Index, ArchivedInvoiceList Original)>();
        foreach (var updatedArchive in result.UpdatedArchives)
        {
            var original = ArchivedLists.FirstOrDefault(archive => archive.Id == updatedArchive.Id);
            if (original is null) continue;
            var index = ArchivedLists.IndexOf(original);
            if (index < 0) continue;
            replacedArchives.Add((index, original));
            ArchivedLists[index] = updatedArchive;
        }

        if (replacedArchives.Count > 0)
        {
            try
            {
                _archiveDataStore.Save(ArchivedLists);
            }
            catch (Exception ex)
            {
                foreach (var replacement in replacedArchives)
                    ArchivedLists[replacement.Index] = replacement.Original;
                RefreshArchiveView();
                SetArchiveStatus(
                    "PDF'ler hazırlandı ancak arşiv bağlantıları kaydedilemedi: " + ex.Message,
                    true);
                return null;
            }
        }

        if (result.HasChanges || result.CreatedCount > 0)
            RefreshArchiveView();

        var summary = $"{result.ScannedCount} arşiv tarandı; "
                      + $"{result.CreatedCount} eksik PDF oluşturuldu, "
                      + $"{result.ExistingCount} PDF hazır bulundu.";
        if (result.Errors.Count > 0)
        {
            SetArchiveStatus(summary + $" {result.Errors.Count} kayıt eşitlenemedi: {result.Errors[0]}", true);
        }
        else if (userInitiated || result.CreatedCount > 0 || result.ReferenceUpdateCount > 0)
        {
            SetArchiveStatus(summary, false, true);
        }

        return result;
    }

    private async void ImportHistoricalPdf_Click(object sender, RoutedEventArgs e)
    {
        if (_historicalPdfImportCancellation is not null)
            return;

        var dialog = new OpenFileDialog
        {
            Title = "Arşive eklenecek DoğruFatura PDF kayıtlarını seçin",
            Filter = "PDF dosyaları (*.pdf)|*.pdf",
            Multiselect = true,
            CheckFileExists = true,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
        };
        if (dialog.ShowDialog(this) != true || dialog.FileNames.Length == 0)
            return;

        IReadOnlyList<HistoricalPdfImportPreview> previews;
        using var cancellation = new CancellationTokenSource();
        _historicalPdfImportCancellation = cancellation;
        ArchiveImportButton.IsEnabled = false;
        ArchiveImportCancelButton.IsEnabled = false;
        ArchiveImportCancelButton.Visibility = Visibility.Collapsed;
        ArchivePdfSyncButton.IsEnabled = false;

        var progressWindow = new HistoricalPdfImportProgressWindow { Owner = this };
        progressWindow.CancelRequested += (_, _) =>
        {
            if (!cancellation.IsCancellationRequested)
                cancellation.Cancel();
            progressWindow.SetCancelling();
        };
        progressWindow.Show();

        try
        {
            _historicalPdfImportService.ClearBatchCache();
            var knownHysNumbers = ArchivedLists
                .Where(archive => !string.IsNullOrWhiteSpace(archive.HysPaymentOrderDocumentNumber))
                .Select(archive => archive.HysPaymentOrderDocumentNumber)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var selectedHysNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var schools = AllSchools.ToList();
            var authorityProfile = _authorityProfile;
            var buffer = new HistoricalPdfImportPreview?[dialog.FileNames.Length];
            var strongCandidateIndexes = new List<int>();

            for (var index = 0; index < dialog.FileNames.Length; index++)
            {
                cancellation.Token.ThrowIfCancellationRequested();

                var fileName = dialog.FileNames[index];
                progressWindow.SetFileProgress(
                    index + 1,
                    dialog.FileNames.Length,
                    Path.GetFileName(fileName),
                    "1. aşama: hızlı metin taraması");
                SetArchiveStatus(
                    $"Hızlı tarama: {index + 1}/{dialog.FileNames.Length} arşiv PDF işleniyor...");

                var progress = new Progress<string>(message => progressWindow.SetWorking(message));
                var preview = await Task.Run(
                    () => _historicalPdfImportService.Read(
                        fileName,
                        schools,
                        authorityProfile,
                        progress,
                        HistoricalPdfImportScanMode.Fast,
                        cancellation.Token),
                    cancellation.Token);

                cancellation.Token.ThrowIfCancellationRequested();
                buffer[index] = preview;
                if (!preview.IsValid && !preview.OcrSkipped)
                    strongCandidateIndexes.Add(index);

                SetArchiveStatus(
                    $"Hızlı tarama: {index + 1}/{dialog.FileNames.Length} tamamlandı; "
                    + $"{strongCandidateIndexes.Count} dosya güçlü taramaya ayrıldı.");
            }

            for (var strongIndex = 0; strongIndex < strongCandidateIndexes.Count; strongIndex++)
            {
                cancellation.Token.ThrowIfCancellationRequested();

                var fileIndex = strongCandidateIndexes[strongIndex];
                var fileName = dialog.FileNames[fileIndex];
                progressWindow.SetFileProgress(
                    strongIndex + 1,
                    strongCandidateIndexes.Count,
                    Path.GetFileName(fileName),
                    "2. aşama: güçlü özel OCR taraması");
                SetArchiveStatus(
                    $"Güçlü tarama: {strongIndex + 1}/{strongCandidateIndexes.Count} sorunlu PDF işleniyor...");

                using var strongCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellation.Token);
                strongCancellation.CancelAfter(HistoricalPdfStrongScanTimeout);
                var progress = new Progress<string>(message => progressWindow.SetWorking(message));
                try
                {
                    var strongPreview = await Task.Run(
                        () => _historicalPdfImportService.Read(
                            fileName,
                            schools,
                            authorityProfile,
                            progress,
                            HistoricalPdfImportScanMode.Strong,
                            strongCancellation.Token),
                        strongCancellation.Token);
                    buffer[fileIndex] = strongPreview with
                    {
                        ScanDetail = strongPreview.OcrAttempted
                            ? "Güçlü tarama • OCR uygulandı"
                            : strongPreview.OcrSkipped
                                ? "Güçlü tarama • OCR uygulanmadı (metin sayfaları atlandı)"
                                : "Güçlü tarama • OCR başlamadı (hata ayrıntısına bakın)"
                    };
                }
                catch (OperationCanceledException) when (!cancellation.IsCancellationRequested)
                {
                    buffer[fileIndex] = (buffer[fileIndex]
                        ?? throw new InvalidOperationException("Hızlı tarama sonucu bulunamadı."))
                        .WithError("Güçlü tarama 2 dakikalık süre sınırını aştı.") with
                    {
                        ScanDetail = "Güçlü tarama • süre doldu; OCR durumu belirlenemedi"
                    };
                }
            }

            var finalized = new List<HistoricalPdfImportPreview>(dialog.FileNames.Length);
            for (var index = 0; index < buffer.Length; index++)
            {
                var fileName = dialog.FileNames[index];
                var preview = buffer[index]
                    ?? throw new InvalidOperationException("PDF tarama sonucu oluşturulamadı.");

                if (preview.IsValid && knownHysNumbers.Contains(preview.HysNumber))
                {
                    preview = preview.WithError(
                        $"{preview.HysNumber} işlem arşivinde zaten kayıtlı. Okuma sonucu: {preview.Status}")
                        with { AlreadyArchived = true };
                }
                else if (preview.IsValid && !selectedHysNumbers.Add(preview.HysNumber))
                {
                    preview = preview.WithError($"{preview.HysNumber} seçilen dosyalar içinde birden fazla kez bulunuyor.");
                }
                else if (preview.IsValid)
                {
                    var destinationPath = _documentArchiveStorageService.GetArchivedOutputPath(preview.HysNumber);
                    var isSameFile = string.Equals(
                        Path.GetFullPath(fileName),
                        Path.GetFullPath(destinationPath),
                        StringComparison.OrdinalIgnoreCase);
                    if (File.Exists(destinationPath) && !isSameFile)
                        preview = preview.WithError($"{Path.GetFileName(destinationPath)} Arşivlenenler klasöründe zaten bulunuyor.");
                }

                finalized.Add(preview);
            }

            previews = finalized;
        }
        catch (OperationCanceledException)
        {
            SetArchiveStatus(
                "PDF okuma/doğrulama işlemi iptal edildi. Arşiv verilerinde değişiklik yapılmadı.",
                false,
                true);
            return;
        }
        catch (Exception ex)
        {
            SetArchiveStatus("Arşive eklenecek PDF dosyaları okunamadı: " + ex.Message, true);
            return;
        }
        finally
        {
            _historicalPdfImportService.ClearBatchCache();
            progressWindow.CompleteAndClose();
            _historicalPdfImportCancellation = null;
            ArchiveImportButton.IsEnabled = true;
            ArchiveImportCancelButton.IsEnabled = false;
            ArchiveImportCancelButton.Visibility = Visibility.Collapsed;
            ArchivePdfSyncButton.IsEnabled = true;
        }

        var importDialog = new HistoricalPdfImportDialog(previews) { Owner = this };
        if (importDialog.ShowDialog() != true)
        {
            SaveHistoricalPdfImportReport(
                previews,
                Array.Empty<HistoricalPdfImportPreview>(),
                "Önizlemede vazgeçildi",
                "PDF aktarımından vazgeçildi; seçilen dosyalar arşive eklenmedi.",
                showFailures: false);
            return;
        }

        var validItems = importDialog.ValidItems;
        if (validItems.Count == 0)
        {
            SaveHistoricalPdfImportReport(
                previews,
                Array.Empty<HistoricalPdfImportPreview>(),
                "Geçerli PDF bulunamadı",
                "Aktarılabilecek geçerli PDF bulunamadı.",
                showFailures: true);
            return;
        }

        var addedArchives = new List<ArchivedInvoiceList>();
        var createdFiles = new List<string>();
        SetArchiveStatus($"{validItems.Count} geçerli PDF arşive kaydediliyor...");
        try
        {
            foreach (var item in validItems)
            {
                var archive = item.Archive!;
                var destinationPath = _documentArchiveStorageService.GetArchivedOutputPath(item.HysNumber);
                var sourceIsDestination = string.Equals(
                    Path.GetFullPath(item.SourcePath),
                    Path.GetFullPath(destinationPath),
                    StringComparison.OrdinalIgnoreCase);
                var destinationExisted = File.Exists(destinationPath);
                var pdfFileName = _documentArchiveStorageService.CopyImportedPdf(item.SourcePath, item.HysNumber);
                if (!sourceIsDestination && !destinationExisted)
                    createdFiles.Add(destinationPath);

                var storedArchive = archive with
                {
                    PdfFileName = pdfFileName,
                    PdfSha256 = FileIntegrityService.ComputeSha256(destinationPath)
                };
                ArchivedLists.Add(storedArchive);
                addedArchives.Add(storedArchive);
            }

            _archiveDataStore.Save(ArchivedLists);
        }
        catch (Exception ex)
        {
            foreach (var archive in addedArchives)
                ArchivedLists.Remove(archive);
            foreach (var createdFile in createdFiles)
            {
                try
                {
                    if (File.Exists(createdFile))
                        File.Delete(createdFile);
                }
                catch
                {
                    // Yalnız bu aktarımda oluşturulan dosyalar geri alınmaya çalışılır.
                }
            }

            RefreshArchiveView();
            SaveHistoricalPdfImportReport(
                previews,
                Array.Empty<HistoricalPdfImportPreview>(),
                "Kayıt geri alındı: " + ex.Message,
                "Arşiv ekleme işlemi tamamlanamadı: " + ex.Message,
                showFailures: false);
            MessageBox.Show(
                this,
                "PDF kayıtları arşive eklenirken işlem geri alındı:\n\n" + ex.Message,
                "DoğruFatura - Arşiv Ekle",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return;
        }

        foreach (var importedArchive in addedArchives)
        {
            AppendAudit(
                AuditTrailEventType.ArchiveImported,
                AuditTrailEntityType.Archive,
                importedArchive.Id.ToString("D"),
                $"State={importedArchive.State};InvoiceCount={importedArchive.InvoiceCount};Type={importedArchive.InvoiceType}");
        }

        var firstArchive = addedArchives.First();
        RefreshArchiveView(firstArchive.Id);
        RefreshDashboard();
        SaveHistoricalPdfImportReport(
            previews,
            validItems,
            "Aktarım tamamlandı",
            $"{addedArchives.Count} PDF arşive eklendi.",
            showFailures: true);
    }

    private void SaveHistoricalPdfImportReport(
        IReadOnlyList<HistoricalPdfImportPreview> previews,
        IReadOnlyList<HistoricalPdfImportPreview> imported,
        string outcome,
        string summary,
        bool showFailures)
    {
        var failures = previews.Where(item => !item.IsValid && !item.AlreadyArchived).ToArray();
        var alreadyArchivedCount = previews.Count(item => item.AlreadyArchived);
        try
        {
            var reportPath = HistoricalPdfImportReport.Write(
                _documentArchiveStorageService.RootDirectory,
                previews,
                imported,
                outcome);
            SetArchiveStatus(
                $"{summary} {alreadyArchivedCount} dosya zaten arşivde; {failures.Length} dosya inceleme gerektiriyor. Rapor: {reportPath}",
                failures.Length > 0,
                true);
            if (showFailures && failures.Length > 0)
            {
                var failureLines = failures
                    .Take(8)
                    .Select(item => $"• {item.FileName} — {item.ScanDetail}: {item.Status}");
                var more = failures.Length > 8
                    ? $"\n... ve {failures.Length - 8} dosya daha."
                    : string.Empty;
                MessageBox.Show(
                    this,
                    $"{summary}\n\nİnceleme gereken dosyalar:\n{string.Join("\n", failureLines)}{more}\n\nTam rapor:\n{reportPath}",
                    "DoğruFatura - Arşiv Aktarım Sonucu",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            SetArchiveStatus(
                $"{summary} {alreadyArchivedCount} dosya zaten arşivde; {failures.Length} dosya inceleme gerektiriyor. Aktarım raporu kaydedilemedi: {ex.Message}",
                true);
            MessageBox.Show(
                this,
                $"Aktarım raporu DoğruFatura klasörüne kaydedilemedi:\n{ex.Message}\n\n"
                + string.Join("\n", failures.Select(item => $"{item.FileName}: {item.Status}")),
                "DoğruFatura - Rapor Hatası",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void CancelHistoricalPdfImport_Click(object sender, RoutedEventArgs e)
    {
        if (_historicalPdfImportCancellation is null)
            return;

        ArchiveImportCancelButton.IsEnabled = false;
        SetArchiveStatus("PDF okuma/doğrulama işlemi iptal ediliyor...");
        _historicalPdfImportCancellation.Cancel();
    }

    private void ArchiveFilter_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressArchiveEvents) return;
        ApplyArchiveFilters();
    }

    private void ArchiveFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressArchiveEvents) return;
        ApplyArchiveFilters();
    }

    private void ApplyArchiveFilters()
    {
        var from = ArchiveDateFromPicker.SelectedDate?.Date;
        var to = ArchiveDateToPicker.SelectedDate?.Date;
        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            _archiveView.Refresh();
            ArchiveVisibleCountText.Text = $"0 / {ArchivedLists.Count} kayıt";
            ArchiveListEmptyState.Visibility = Visibility.Visible;
            ArchiveGrid.SelectedItem = null;
            UpdateArchiveSelection();
            SetArchiveStatus("Başlangıç tarihi, bitiş tarihinden sonra olamaz.", true);
            return;
        }

        RefreshArchiveView();
        SetArchiveStatus("Arşiv filtreleri uygulandı.", false, true);
    }

    private void ClearArchiveFilters_Click(object sender, RoutedEventArgs e)
    {
        _suppressArchiveEvents = true;
        ArchiveSearchBox.Clear();
        ArchiveTypeFilterBox.SelectedIndex = 0;
        ArchiveStatusFilterBox.SelectedIndex = 0;
        ArchiveDateFromPicker.SelectedDate = null;
        ArchiveDateToPicker.SelectedDate = null;
        _suppressArchiveEvents = false;
        RefreshArchiveView();
        SetArchiveStatus("Arşiv filtreleri temizlendi.", false, true);
    }

    private void ArchiveGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        UpdateArchiveSelection();

    private void ArchiveGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ArchiveGrid.SelectedItem is ArchivedInvoiceList)
            OpenArchivedList_Click(sender, e);
    }
}
