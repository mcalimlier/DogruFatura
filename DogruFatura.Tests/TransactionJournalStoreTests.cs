using DogruFatura.Core.Models;
using DogruFatura.Core.Services;

namespace DogruFatura.Tests;

public sealed class TransactionJournalStoreTests
{
    [Fact]
    public void IncompleteTransactionIsDetectedUntilCompleted()
    {
        using var temp = new TempFolder();
        var path = Path.Combine(temp.Path, "IslemJournal.jsonl");
        var store = new TransactionJournalStore(path);
        var transactionId = Guid.NewGuid();
        var archiveId = Guid.NewGuid().ToString("D");

        store.Append(TransactionJournalEntry.Create(
            transactionId,
            TransactionJournalOperation.ArchiveActiveList,
            TransactionJournalState.Started,
            archiveId,
            "InvoiceCount=2;Type=Elektrik"));
        var startedBytes = File.ReadAllBytes(path);

        store.Append(TransactionJournalEntry.Create(
            transactionId,
            TransactionJournalOperation.ArchiveActiveList,
            TransactionJournalState.ArchivePersisted,
            archiveId,
            "InvoiceCount=2;Type=Elektrik"));

        var afterSecondAppend = File.ReadAllBytes(path);
        Assert.True(afterSecondAppend.Length > startedBytes.Length);
        Assert.True(afterSecondAppend.AsSpan(0, startedBytes.Length).SequenceEqual(startedBytes));

        var pending = Assert.Single(store.GetIncompleteTransactions());
        Assert.Equal(transactionId, pending.TransactionId);
        Assert.Equal(TransactionJournalState.ArchivePersisted, pending.State);
        Assert.False(pending.IsTerminal);

        store.Append(TransactionJournalEntry.Create(
            transactionId,
            TransactionJournalOperation.ArchiveActiveList,
            TransactionJournalState.Completed,
            archiveId,
            "InvoiceCount=2;Type=Elektrik"));

        Assert.Empty(store.GetIncompleteTransactions());
        var entries = store.ReadAll();
        Assert.Equal(3, entries.Count);
        Assert.All(entries, item => Assert.Equal(transactionId, item.TransactionId));
        Assert.All(entries, item => Assert.Equal(TimeSpan.Zero, item.OccurredAtUtc.Offset));
    }

    private sealed class TempFolder : IDisposable
    {
        public TempFolder()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "DogruFatura_TransactionJournalTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path))
                    Directory.Delete(Path, true);
            }
            catch
            {
            }
        }
    }
}
