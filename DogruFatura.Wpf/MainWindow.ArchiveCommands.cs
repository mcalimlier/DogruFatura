using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace DogruFatura.Wpf;

public partial class MainWindow
{
    private void ArchiveClosure_Click(object sender, RoutedEventArgs e)
    {
        if (ArchiveGrid.SelectedItem is not ArchivedInvoiceList archive)
        {
            SetArchiveStatus("Kapatmak için arşivden bir liste seçin.", true);
            return;
        }

        ArchivedInvoiceList updated;
        var pdfMove = PdfMoveResult.None(archive.PdfFileName);
        string successMessage;
        if (archive.IsClosed)
        {
            var confirmation = MessageBox.Show(this,
                $"{archive.HysPaymentOrderDocumentNumber} numaralı arşiv kapanışı geri alınacak.\n\n"
                + "Kayıt Ana Ekran istatistiklerinden çıkarılır; fatura ve HYS bilgileri korunur. Devam edilsin mi?",
                "DoğruFatura - Arşiv Kapanışını Geri Al",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);
            if (confirmation != MessageBoxResult.Yes) return;

            try
            {
                pdfMove = _documentArchiveStorageService.MoveArchivedOutputToActive(archive);
            }
            catch (Exception ex)
            {
                SetArchiveStatus("Arşiv PDF'si Aktif Çıktılar klasörüne taşınamadı: " + ex.Message, true);
                return;
            }

            updated = archive with
            {
                IsClosed = false,
                ClosedAt = null,
                PdfFileName = pdfMove.PdfFileName,
                ClosedSnapshotSha256 = string.Empty
            };
            successMessage = pdfMove.Moved
                ? $"{archive.HysPaymentOrderDocumentNumber} kapanışı geri alındı; PDF Aktif Çıktılar klasörüne taşındı."
                : $"{archive.HysPaymentOrderDocumentNumber} kapanışı geri alındı; kayıt Ana Ekran istatistiklerinden çıkarıldı.";
        }
        else
        {
            if (!HysPaymentOrderService.TryNormalize(
                    ArchiveHysNumberBox.Text,
                    out var normalizedHysNumber,
                    out var errorMessage))
            {
                SetArchiveStatus(errorMessage, true);
                ArchiveHysNumberBox.Focus();
                return;
            }

            if (ArchivedLists.Any(candidate => candidate.Id != archive.Id
                                               && string.Equals(
                                                   candidate.HysPaymentOrderDocumentNumber,
                                                   normalizedHysNumber,
                                                   StringComparison.OrdinalIgnoreCase)))
            {
                SetArchiveStatus($"{normalizedHysNumber} başka bir arşiv kaydında kullanılıyor.", true);
                return;
            }

            var closeConfirmation = MessageBox.Show(this,
                $"Liste: {archive.ArchiveNumber}\nTür: {archive.InvoiceType}\nFatura: {archive.InvoiceCount}\nToplam ödeme: {NumberParser.Money(archive.PaymentTotal)}\nHYS belge no: {normalizedHysNumber}\n\nHYS kapanışı banka ödeme teyidi değildir. Bu liste kapatılsın mı?",
                "HYS kapanış kontrolü", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            if (closeConfirmation != MessageBoxResult.Yes) return;

            try
            {
                pdfMove = _documentArchiveStorageService.MoveActiveOutputToArchive(
                    archive,
                    normalizedHysNumber);
            }
            catch (Exception ex)
            {
                SetArchiveStatus("Aktif PDF arşiv klasörüne taşınamadı: " + ex.Message, true);
                return;
            }

            updated = ClosedArchiveSnapshotService.Seal(archive with
            {
                HysPaymentOrderDocumentNumber = normalizedHysNumber,
                IsClosed = true,
                ClosedAt = DateTime.Now,
                PdfFileName = pdfMove.PdfFileName
            });
            successMessage = pdfMove.Moved
                ? $"{normalizedHysNumber} kaydedildi; PDF Arşivlenenler klasörüne taşınıp HYS koduyla adlandırıldı."
                : $"{normalizedHysNumber} kaydedildi; arşiv kapatıldı. Bu listeye ait kayıtlı PDF bulunamadı.";
        }

        var validation = ArchivedInvoiceListValidationService.Validate(updated);
        if (!validation.IsValid)
        {
            DocumentArchiveStorageService.RollbackMove(pdfMove);
            SetArchiveStatus("Arşiv kapanışı kaydedilemedi: " + validation.Message, true);
            return;
        }

        var index = ArchivedLists.IndexOf(archive);
        if (index < 0)
        {
            DocumentArchiveStorageService.RollbackMove(pdfMove);
            return;
        }
        ArchivedLists[index] = updated;
        try
        {
            _archiveDataStore.Save(ArchivedLists);
        }
        catch (Exception ex)
        {
            ArchivedLists[index] = archive;
            DocumentArchiveStorageService.RollbackMove(pdfMove);
            RefreshArchiveView(archive.Id);
            SetArchiveStatus("Arşiv kapanışı kaydedilemedi: " + ex.Message, true);
            return;
        }

        AppendAudit(
            updated.IsClosed ? AuditTrailEventType.ArchiveClosed : AuditTrailEventType.ArchiveReopened,
            AuditTrailEntityType.Archive,
            updated.Id.ToString("D"),
            $"State={updated.State};InvoiceCount={updated.InvoiceCount}");

        var synchronization = SynchronizeArchivePdfs(false);
        var synchronizedArchive = ArchivedLists.FirstOrDefault(candidate => candidate.Id == updated.Id);
        if (!pdfMove.Moved
            && synchronization is not null
            && synchronizedArchive is not null
            && !string.IsNullOrWhiteSpace(synchronizedArchive.PdfFileName))
        {
            successMessage = updated.IsClosed
                ? $"{updated.HysPaymentOrderDocumentNumber} kaydedildi; eksik PDF Arşivlenenler klasöründe oluşturuldu."
                : $"{updated.HysPaymentOrderDocumentNumber} kapanışı geri alındı; eksik PDF Aktif Çıktılar klasöründe oluşturuldu.";
        }

        RefreshArchiveView(updated.Id);
        RefreshDashboard();
        if (synchronizedArchive is null || string.IsNullOrWhiteSpace(synchronizedArchive.PdfFileName))
            SetArchiveStatus(successMessage + " Belge eşitleme daha sonra EŞİTLE düğmesiyle yeniden denenebilir.", true);
        else if (synchronization is { Errors.Count: > 0 })
            SetArchiveStatus(successMessage + $" Diğer {synchronization.Errors.Count} arşiv belgesi eşitlenemedi; EŞİTLE ile yeniden deneyebilirsiniz.", true);
        else
            SetArchiveStatus(successMessage, false, true);
    }

    private void ArchiveActiveList_Click(object sender, RoutedEventArgs e) =>
        TryArchiveActiveList(true);

    private bool TryArchiveActiveList(bool askConfirmation)
    {
        var reportValidation = FinalReportValidationService.Validate(ActiveInvoices);
        if (!reportValidation.IsValid || reportValidation.InvoiceType is null)
        {
            SetArchiveStatus(reportValidation.Message, true);
            return false;
        }

        if (askConfirmation)
        {
            var confirmation = MessageBox.Show(this,
                $"{ActiveInvoices.Count} adet {reportValidation.InvoiceType} faturası işlem arşivine açık kayıt olarak kaydedilecek.\n\n"
                + "Kayıt, HYS Ödeme Emri Belge No girilip kapatılana kadar Ana Ekran istatistiklerine katılmaz. "
                + "Başarılı kayıttan sonra aktif liste temizlenecek. Devam edilsin mi?",
                "DoğruFatura - Aktif Listeyi Arşivle",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);
            if (confirmation != MessageBoxResult.Yes) return false;
        }

        var archivedAt = DateTime.Now;
        var activePdfPath = _documentArchiveStorageService.FindActiveOutputPath(ActiveInvoices.ToList());
        var archive = new ArchivedInvoiceList(
            Guid.NewGuid(),
            CreateArchiveNumber(archivedAt),
            archivedAt,
            reportValidation.InvoiceType,
            ActiveInvoices.ToList(),
            _authorityProfile.Normalize(),
            PdfFileName: activePdfPath is null ? string.Empty : Path.GetFileName(activePdfPath));
        var archiveValidation = ArchivedInvoiceListValidationService.Validate(archive);
        if (!archiveValidation.IsValid)
        {
            SetArchiveStatus("Aktif liste arşivlenemedi: " + archiveValidation.Message, true);
            return false;
        }

        var transactionId = Guid.NewGuid();
        if (!TryAppendArchiveTransaction(
                transactionId,
                TransactionJournalState.Started,
                archive,
                out var journalStartError))
        {
            SetArchiveStatus(
                "Aktif liste arşivlenemedi: işlem journal başlatılamadı. Veri dosyalarında değişiklik yapılmadı. "
                + journalStartError,
                true);
            return false;
        }

        ArchivedLists.Insert(0, archive);
        try
        {
            _archiveDataStore.Save(ArchivedLists);
        }
        catch (Exception ex)
        {
            ArchivedLists.Remove(archive);
            AppendArchiveTransactionBestEffort(
                transactionId,
                TransactionJournalState.RolledBack,
                archive);
            RefreshArchiveView();
            SetArchiveStatus("İşlem arşivine kaydedilemedi: " + ex.Message, true);
            MessageBox.Show(this, "Aktif liste işlem arşivine kaydedilemedi:\n\n" + ex.Message,
                "DoğruFatura - İşlem Arşivi", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        if (!TryAppendArchiveTransaction(
                transactionId,
                TransactionJournalState.ArchivePersisted,
                archive,
                out var journalArchiveError))
        {
            ArchivedLists.Remove(archive);
            var rollbackSucceeded = true;
            try
            {
                _archiveDataStore.Save(ArchivedLists);
            }
            catch
            {
                ArchivedLists.Insert(0, archive);
                rollbackSucceeded = false;
            }

            AppendArchiveTransactionBestEffort(
                transactionId,
                rollbackSucceeded
                    ? TransactionJournalState.RolledBack
                    : TransactionJournalState.RecoveryRequired,
                archive);

            RefreshArchiveView();
            SetArchiveStatus(
                rollbackSucceeded
                    ? "İşlem journal güncellenemedi; arşiv yazımı güvenle geri alındı. " + journalArchiveError
                    : "İşlem journal güncellenemedi ve arşiv rollback tamamlanamadı. "
                      + "Yeni işlem yapmadan önce güvenlik yedeği alın ve veri bütünlüğünü kontrol edin. "
                      + journalArchiveError,
                true);
            return false;
        }

        try
        {
            _invoiceDataStore.Save(Array.Empty<InvoiceRecord>());
        }
        catch (Exception ex)
        {
            ArchivedLists.Remove(archive);
            var rollbackMessage = string.Empty;
            var rollbackSucceeded = true;
            try
            {
                _archiveDataStore.Save(ArchivedLists);
            }
            catch
            {
                ArchivedLists.Insert(0, archive);
                rollbackSucceeded = false;
                rollbackMessage = " Arşiv kopyası güvenlik amacıyla korunmuştur.";
            }

            AppendArchiveTransactionBestEffort(
                transactionId,
                rollbackSucceeded
                    ? TransactionJournalState.RolledBack
                    : TransactionJournalState.RecoveryRequired,
                archive);

            RefreshArchiveView();
            SetArchiveStatus("Aktif liste temizlenemedi; işlem tamamlanmadı." + rollbackMessage, true);
            MessageBox.Show(this,
                "Aktif liste kalıcı dosyada temizlenemedi. Mevcut faturalar korunmuştur.\n\n" + ex.Message + rollbackMessage,
                "DoğruFatura - İşlem Arşivi", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        var journalWarning = false;
        if (!TryAppendArchiveTransaction(
                transactionId,
                TransactionJournalState.ActiveListCleared,
                archive,
                out _))
            journalWarning = true;
        if (!TryAppendArchiveTransaction(
                transactionId,
                TransactionJournalState.Completed,
                archive,
                out _))
            journalWarning = true;

        AppendAudit(
            AuditTrailEventType.ArchiveCreated,
            AuditTrailEntityType.Archive,
            archive.Id.ToString("D"),
            $"State={archive.State};InvoiceCount={archive.InvoiceCount};Type={archive.InvoiceType}");

        ActiveInvoices.Clear();
        _deletedInvoice = null;
        _undoInvoice.IsEnabled = false;
        EndInvoiceEditMode();
        ResetInvoiceEntryAfterArchive();
        UpdateInvoiceListSummary();
        var synchronization = SynchronizeArchivePdfs(false);
        var synchronizedArchive = ArchivedLists.FirstOrDefault(candidate => candidate.Id == archive.Id);
        RefreshArchiveView(archive.Id);

        if (journalWarning)
        {
            SetArchiveStatus(
                $"{archive.ArchiveNumber} arşiv işlemi veri dosyalarında tamamlandı; ancak işlem journal kapanışı tam yazılamadı. "
                + "Sonraki açılışta kontrol uyarısı görülebilir. Yeni işlemden önce güvenlik yedeği alın.",
                true);
        }
        else if (synchronizedArchive is null || string.IsNullOrWhiteSpace(synchronizedArchive.PdfFileName))
        {
            SetArchiveStatus(
                $"{archive.ArchiveNumber} açık arşiv olarak kaydedildi ancak PDF eşitlenemedi. EŞİTLE düğmesiyle yeniden deneyebilirsiniz.",
                true);
        }
        else if (synchronization is { Errors.Count: > 0 })
        {
            SetArchiveStatus(
                $"{archive.ArchiveNumber} açık arşiv ve PDF belgesi kaydedildi. Diğer {synchronization.Errors.Count} arşiv belgesi eşitlenemedi; EŞİTLE ile yeniden deneyebilirsiniz.",
                true);
        }
        else
        {
            SetArchiveStatus(
                $"{archive.ArchiveNumber} açık arşiv ve PDF belgesi kaydedildi. HYS Ödeme Emri Belge No girerek kaydı kapatabilirsiniz.",
                false,
                true);
        }

        return true;
    }

    private string CreateArchiveNumber(DateTime archivedAt)
    {
        var baseNumber = $"DF-{archivedAt:yyyyMMdd-HHmmss}";
        var candidate = baseNumber;
        var suffix = 2;
        while (ArchivedLists.Any(archive => string.Equals(
                   archive.ArchiveNumber,
                   candidate,
                   StringComparison.OrdinalIgnoreCase)))
            candidate = $"{baseNumber}-{suffix++}";
        return candidate;
    }

    private void ResetInvoiceEntryAfterArchive()
    {
        _suppressFormEvents = true;
        InstitutionTypeBox.SelectedIndex = -1;
        InstitutionBox.ItemsSource = Array.Empty<SchoolRecord>();
        InstitutionBox.SelectedIndex = -1;
        InvoiceTypeBox.SelectedIndex = -1;
        InvoiceNumberBox.Clear();
        VknBox.Clear();
        ServiceNumberBox.Clear();
        InvoiceDatePicker.SelectedDate = null;
        DueDatePicker.SelectedDate = null;
        PayableAmountBox.Clear();
        VatAmountBox.Clear();
        InterruptionCompensationBox.Text = "0,00";
        _hasInterruptionCompensation = false;
        _suppressFormEvents = false;

        ActiveInstitutionText.Text = "Henüz seçilmedi";
        SetServiceStatus("Kurum ve fatura türü seçildiğinde hizmet numarası otomatik gelir.", "#FFB41F");
        UpdateInterruptionCompensationPresentation(resetWhenUnavailable: true);
        ResetCalculationDisplay();
        UpdateFormReadiness();
    }

    private void OpenArchivedList_Click(object sender, RoutedEventArgs e)
    {
        if (ArchiveGrid.SelectedItem is not ArchivedInvoiceList archive)
        {
            SetArchiveStatus("Açmak için arşivden bir liste seçin.", true);
            return;
        }

        _openedArchiveForReport = archive;
        ShowView("Reports");
        RefreshFinalReportPreview();
    }

    private void RestoreArchivedListToActive_Click(object sender, RoutedEventArgs e)
    {
        if (ArchiveGrid.SelectedItem is not ArchivedInvoiceList archive)
        {
            SetArchiveStatus("Düzenlemek için arşivden bir liste seçin.", true);
            return;
        }

        if (ActiveInvoices.Count > 0)
        {
            SetArchiveStatus(
                "Arşiv düzenlemeye alınamadı: Aktif Fatura Listesi boş olmalıdır. "
                + "Önce mevcut aktif listeyi tamamlayın veya arşive alın.",
                true);
            return;
        }

        var validation = FinalReportValidationService.Validate(archive.Invoices);
        if (!validation.IsValid)
        {
            SetArchiveStatus("Arşiv düzenlemeye alınamadı: " + validation.Message, true);
            return;
        }

        var confirmation = MessageBox.Show(
            this,
            $"{archive.ArchiveNumber} arşivindeki {archive.InvoiceCount} fatura Aktif Fatura Listesi'ne geri alınacak.\n\n"
            + "Arşiv kaydı ve ona bağlı eski PDF silinecek; faturaları normal listeden düzenleyebileceksiniz. "
            + "HYS kapanış bilgisi arşiv kaydıyla birlikte kaldırılacak. Devam edilsin mi?",
            "DoğruFatura - Arşivi Düzenlemeye Al",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        if (confirmation != MessageBoxResult.Yes)
            return;

        var transactionId = Guid.NewGuid();
        const TransactionJournalOperation operation = TransactionJournalOperation.RestoreArchiveToActive;
        if (!TryAppendArchiveTransaction(
                transactionId,
                TransactionJournalState.Started,
                archive,
                out var journalStartError,
                operation))
        {
            SetArchiveStatus(
                "Arşiv düzenlemeye alınamadı: işlem journal başlatılamadı. Veri değişikliği yapılmadı. "
                + journalStartError,
                true);
            return;
        }

        var restoredInvoices = archive.Invoices.ToList();
        try
        {
            _invoiceDataStore.Save(restoredInvoices);
        }
        catch (Exception ex)
        {
            AppendArchiveTransactionBestEffort(
                transactionId,
                TransactionJournalState.RolledBack,
                archive,
                operation);
            SetArchiveStatus("Faturalar aktif listeye alınamadı: " + ex.Message, true);
            return;
        }

        if (!TryAppendArchiveTransaction(
                transactionId,
                TransactionJournalState.ActiveListRestored,
                archive,
                out var journalActiveError,
                operation))
        {
            var rollbackSucceeded = true;
            try
            {
                _invoiceDataStore.Save(Array.Empty<InvoiceRecord>());
            }
            catch
            {
                rollbackSucceeded = false;
            }

            AppendArchiveTransactionBestEffort(
                transactionId,
                rollbackSucceeded
                    ? TransactionJournalState.RolledBack
                    : TransactionJournalState.RecoveryRequired,
                archive,
                operation);

            SetArchiveStatus(
                rollbackSucceeded
                    ? "İşlem journal güncellenemedi; aktif liste geri alındı. " + journalActiveError
                    : "İşlem journal güncellenemedi ve aktif liste rollback tamamlanamadı. "
                      + "Uygulamayı kapatmadan veri bütünlüğünü kontrol edin. " + journalActiveError,
                true);
            return;
        }

        var archiveIndex = ArchivedLists.IndexOf(archive);
        if (archiveIndex < 0)
        {
            try { _invoiceDataStore.Save(Array.Empty<InvoiceRecord>()); } catch { }
            AppendArchiveTransactionBestEffort(
                transactionId,
                TransactionJournalState.RolledBack,
                archive,
                operation);
            SetArchiveStatus("Seçili arşiv kaydı artık listede bulunmuyor.", true);
            return;
        }

        ArchivedLists.RemoveAt(archiveIndex);
        try
        {
            _archiveDataStore.Save(ArchivedLists);
        }
        catch (Exception ex)
        {
            ArchivedLists.Insert(Math.Clamp(archiveIndex, 0, ArchivedLists.Count), archive);
            var rollbackSucceeded = true;
            try
            {
                _invoiceDataStore.Save(Array.Empty<InvoiceRecord>());
            }
            catch
            {
                rollbackSucceeded = false;
            }

            AppendArchiveTransactionBestEffort(
                transactionId,
                rollbackSucceeded
                    ? TransactionJournalState.RolledBack
                    : TransactionJournalState.RecoveryRequired,
                archive,
                operation);

            RefreshArchiveView(archive.Id);
            SetArchiveStatus(
                "Arşiv kaydı düzenleme için kaldırılamadı: " + ex.Message
                + (rollbackSucceeded
                    ? string.Empty
                    : " Aktif liste rollback tamamlanamadı; veri bütünlüğünü kontrol edin."),
                true);
            return;
        }

        if (!TryAppendArchiveTransaction(
                transactionId,
                TransactionJournalState.ArchiveRemoved,
                archive,
                out var journalArchiveError,
                operation))
        {
            var rollbackArchiveSucceeded = true;
            var rollbackActiveSucceeded = true;
            ArchivedLists.Insert(Math.Clamp(archiveIndex, 0, ArchivedLists.Count), archive);
            try
            {
                _archiveDataStore.Save(ArchivedLists);
            }
            catch
            {
                ArchivedLists.Remove(archive);
                rollbackArchiveSucceeded = false;
            }

            try
            {
                _invoiceDataStore.Save(Array.Empty<InvoiceRecord>());
            }
            catch
            {
                rollbackActiveSucceeded = false;
            }

            var rollbackSucceeded = rollbackArchiveSucceeded && rollbackActiveSucceeded;
            AppendArchiveTransactionBestEffort(
                transactionId,
                rollbackSucceeded
                    ? TransactionJournalState.RolledBack
                    : TransactionJournalState.RecoveryRequired,
                archive,
                operation);

            RefreshArchiveView(archive.Id);
            SetArchiveStatus(
                rollbackSucceeded
                    ? "İşlem journal güncellenemedi; arşiv düzenleme aktarımı geri alındı. " + journalArchiveError
                    : "İşlem journal güncellenemedi ve rollback tamamlanamadı. Veri bütünlüğünü kontrol edin. "
                      + journalArchiveError,
                true);
            return;
        }

        var journalWarning = !TryAppendArchiveTransaction(
            transactionId,
            TransactionJournalState.Completed,
            archive,
            out _,
            operation);

        var pdfDeleteWarning = false;
        try
        {
            _documentArchiveStorageService.DeleteStoredPdf(archive);
        }
        catch (Exception ex)
        {
            pdfDeleteWarning = true;
            ApplicationCrashLogger.Write(ex, "Archive.RestoreToActive.DeletePdf");
        }

        ActiveInvoices.Clear();
        _deletedInvoice = null;
        _undoInvoice.IsEnabled = false;
        foreach (var invoice in restoredInvoices)
            ActiveInvoices.Add(invoice);

        if (_openedArchiveForReport?.Id == archive.Id)
            _openedArchiveForReport = null;

        AppendAudit(
            AuditTrailEventType.ArchiveRestoredToActive,
            AuditTrailEntityType.Archive,
            archive.Id.ToString("D"),
            $"InvoiceCount={archive.InvoiceCount};Type={archive.InvoiceType}");

        EndInvoiceEditMode();
        UpdateInvoiceListSummary();
        RefreshArchiveView();
        RefreshDashboard();
        RefreshControlCenter();

        var message = $"{archive.ArchiveNumber} aktif fatura listesine geri alındı. Faturaları artık normal listeden düzenleyebilirsiniz.";
        if (pdfDeleteWarning)
            message += " Eski PDF silinemedi; dosya klasörde kalmış olabilir.";
        if (journalWarning)
            message += " İşlem journal kapanışı tam yazılamadı; sonraki açılışta kontrol uyarısı görülebilir.";

        SetInvoiceListStatus(message, pdfDeleteWarning || journalWarning, !(pdfDeleteWarning || journalWarning));
        ShowView("List");
    }

    private void DeleteArchivedList_Click(object sender, RoutedEventArgs e)
    {
        if (ArchiveGrid.SelectedItem is not ArchivedInvoiceList archive)
        {
            SetArchiveStatus("Silmek için arşivden bir liste seçin.", true);
            return;
        }

        var confirmation = MessageBox.Show(
            this,
            $"{archive.ArchiveNumber} arşiv kaydı ve ona bağlı PDF kalıcı olarak silinecek.\n\n"
            + "Bu işlem geri alınamaz. Silmek istediğinizden emin misiniz?",
            "DoğruFatura - Arşiv Silme Onayı",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        if (confirmation != MessageBoxResult.Yes)
            return;

        var index = ArchivedLists.IndexOf(archive);
        if (index < 0)
            return;

        ArchivedLists.RemoveAt(index);
        try
        {
            _archiveDataStore.Save(ArchivedLists);
        }
        catch (Exception ex)
        {
            ArchivedLists.Insert(Math.Clamp(index, 0, ArchivedLists.Count), archive);
            RefreshArchiveView(archive.Id);
            SetArchiveStatus("Arşiv kaydı silinemedi: " + ex.Message, true);
            return;
        }

        var pdfDeleted = false;
        var pdfDeleteFailed = false;
        try
        {
            pdfDeleted = _documentArchiveStorageService.DeleteStoredPdf(archive);
        }
        catch (Exception ex)
        {
            pdfDeleteFailed = true;
            ApplicationCrashLogger.Write(ex, "Archive.DeletePdf");
        }

        AppendAudit(
            AuditTrailEventType.ArchiveDeleted,
            AuditTrailEntityType.Archive,
            archive.Id.ToString("D"),
            $"State={archive.State};InvoiceCount={archive.InvoiceCount};Type={archive.InvoiceType};PdfDeleted={pdfDeleted}");

        if (_openedArchiveForReport?.Id == archive.Id)
            _openedArchiveForReport = null;

        RefreshArchiveView();
        RefreshDashboard();

        SetArchiveStatus(
            pdfDeleteFailed
                ? $"{archive.ArchiveNumber} arşiv kaydı silindi ancak bağlı PDF silinemedi."
                : $"{archive.ArchiveNumber} arşiv kaydı"
                  + (pdfDeleted ? " ve bağlı PDF" : string.Empty)
                  + " kalıcı olarak silindi.",
            pdfDeleteFailed,
            !pdfDeleteFailed);
    }

    private void SetArchiveStatus(string message, bool isError = false, bool isSuccess = false)
    {
        ArchiveStatusText.Text = message;
        if (isError)
        {
            ArchiveStatusText.Foreground = Brushes.MistyRose;
            ArchiveStatusBorder.Background = new SolidColorBrush(Color.FromRgb(95, 29, 37));
            ArchiveStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(169, 67, 77));
        }
        else if (isSuccess)
        {
            ArchiveStatusText.Foreground = new SolidColorBrush(Color.FromRgb(221, 246, 231));
            ArchiveStatusBorder.Background = new SolidColorBrush(Color.FromRgb(19, 81, 54));
            ArchiveStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(45, 138, 87));
        }
        else
        {
            ArchiveStatusText.Foreground = new SolidColorBrush(Color.FromRgb(216, 228, 239));
            ArchiveStatusBorder.Background = new SolidColorBrush(Color.FromRgb(18, 59, 91));
            ArchiveStatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(54, 88, 116));
        }
    }

    private void ClearInvoiceForm_Click(object sender, RoutedEventArgs e)
    {
        var cancelledEdit = _editedInvoice is not null;
        EndInvoiceEditMode();
        _suppressFormEvents = true;
        InstitutionTypeBox.SelectedIndex = -1;
        InstitutionBox.ItemsSource = Array.Empty<SchoolRecord>();
        InstitutionBox.SelectedIndex = -1;
        InvoiceTypeBox.SelectedIndex = -1;
        InvoiceNumberBox.Clear();
        VknBox.Clear();
        ServiceNumberBox.Clear();
        InvoiceDatePicker.SelectedDate = null;
        DueDatePicker.SelectedDate = null;
        PayableAmountBox.Clear();
        VatAmountBox.Clear();
        InterruptionCompensationBox.Text = "0,00";
        _hasInterruptionCompensation = false;
        _suppressFormEvents = false;

        ActiveInstitutionText.Text = "Henüz seçilmedi";
        SetServiceStatus("Kurum ve fatura türü seçildiğinde hizmet numarası otomatik gelir.", "#FFB41F");
        UpdateInterruptionCompensationPresentation(resetWhenUnavailable: true);
        ResetCalculationDisplay();
        UpdateFormReadiness();
        if (cancelledEdit)
            SetInvoiceSaveStatus("Fatura düzenleme işlemi iptal edildi; form temizlendi.", false, true);
        ShowView("Invoice");
    }
}
