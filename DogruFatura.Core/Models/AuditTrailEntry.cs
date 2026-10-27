namespace DogruFatura.Core.Models;

public enum AuditTrailEventType
{
    InvoiceCreated,
    InvoiceUpdated,
    InvoiceDeleted,
    InvoiceReordered,
    SchoolCreated,
    SchoolUpdated,
    SchoolDeleted,
    AuthorityProfileUpdated,
    AuthorityProfileDefaultsRestored,
    ArchiveCreated,
    ArchiveClosed,
    ArchiveReopened,
    ArchiveRestoredToActive,
    ArchiveDeleted,
    ArchiveImported,
    ReportExported
}

public enum AuditTrailEntityType
{
    Invoice,
    School,
    AuthorityProfile,
    Archive
}

public sealed record AuditTrailEntry(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    AuditTrailEventType EventType,
    AuditTrailEntityType EntityType,
    string EntityId,
    string Detail)
{
    public static AuditTrailEntry Create(
        AuditTrailEventType eventType,
        AuditTrailEntityType entityType,
        string entityId,
        string detail = "") =>
        new(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            eventType,
            entityType,
            entityId?.Trim() ?? string.Empty,
            detail?.Trim() ?? string.Empty);
}
