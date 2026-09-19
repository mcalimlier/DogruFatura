using DogruFatura.Core.Models;
using System.Windows;

namespace DogruFatura.Wpf;

public partial class MainWindow
{
    private bool TryAppendArchiveTransaction(
        Guid transactionId,
        TransactionJournalState state,
        ArchivedInvoiceList archive,
        out string errorMessage)
    {
        try
        {
            _transactionJournalStore.Append(TransactionJournalEntry.Create(
                transactionId,
                TransactionJournalOperation.ArchiveActiveList,
                state,
                archive.Id.ToString("D"),
                $"InvoiceCount={archive.InvoiceCount};Type={archive.InvoiceType}"));
            errorMessage = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            ApplicationCrashLogger.Write(ex, $"TransactionJournal.{state}");
            errorMessage = ex.Message;
            return false;
        }
    }

    private void AppendArchiveTransactionBestEffort(
        Guid transactionId,
        TransactionJournalState state,
        ArchivedInvoiceList archive)
    {
        _ = TryAppendArchiveTransaction(transactionId, state, archive, out _);
    }

    private void ShowPendingTransactionJournalWarning()
    {
        try
        {
            var pending = _transactionJournalStore.GetIncompleteTransactions();
            if (pending.Count == 0)
                return;

            var states = string.Join(
                ", ",
                pending.Select(entry => entry.State.ToString()).Distinct(StringComparer.Ordinal));

            MessageBox.Show(
                this,
                $"{pending.Count} tamamlanmamış işlem journal kaydı bulundu ({states}).\n\n"
                + "DoğruFatura otomatik veri değiştirmedi. Aktif Fatura Listesi ile İşlem Arşivi'ni "
                + "karşılaştırın ve yeni işlem yapmadan önce güvenlik yedeği alın.",
                "DoğruFatura - Yarım Kalmış İşlem Uyarısı",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            ApplicationCrashLogger.Write(ex, "TransactionJournal.StartupCheck");
            MessageBox.Show(
                this,
                "İşlem journal dosyası okunamadı. Yeni arşiv işlemi yapmadan önce Hakkında / Güvenlik "
                + "ekranından güvenlik yedeği alın ve veri bütünlüğünü kontrol edin.",
                "DoğruFatura - İşlem Journal",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }
}
