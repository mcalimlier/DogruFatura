namespace DogruFatura.Core.Models;

public enum TransactionJournalOperation
{
    ArchiveActiveList,
    RestoreArchiveToActive
}

public enum TransactionJournalState
{
    Started,
    ArchivePersisted,
    ActiveListCleared,
    ActiveListRestored,
    ArchiveRemoved,
    Completed,
    RolledBack,
    RecoveryRequired
}

public sealed record TransactionJournalEntry(
    Guid EntryId,
    Guid TransactionId,
    DateTimeOffset OccurredAtUtc,
    TransactionJournalOperation Operation,
    TransactionJournalState State,
    string EntityId,
    string Detail)
{
    public bool IsTerminal =>
        State is TransactionJournalState.Completed or TransactionJournalState.RolledBack;

    public static TransactionJournalEntry Create(
        Guid transactionId,
        TransactionJournalOperation operation,
        TransactionJournalState state,
        string entityId,
        string detail = "")
    {
        if (transactionId == Guid.Empty)
            throw new ArgumentException("İşlem journal kimliği boş olamaz.", nameof(transactionId));

        return new TransactionJournalEntry(
            Guid.NewGuid(),
            transactionId,
            DateTimeOffset.UtcNow,
            operation,
            state,
            entityId?.Trim() ?? string.Empty,
            detail?.Trim() ?? string.Empty);
    }
}
