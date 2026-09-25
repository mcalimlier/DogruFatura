using DogruFatura.Core.Models;

namespace DogruFatura.Wpf;

public partial class MainWindow
{
    private void AppendAudit(
        AuditTrailEventType eventType,
        AuditTrailEntityType entityType,
        string entityId,
        string detail = "")
    {
        try
        {
            _auditTrailStore.Append(AuditTrailEntry.Create(
                eventType,
                entityType,
                entityId,
                detail));
        }
        catch (Exception ex)
        {
            // Denetim kaydı ikincil bir izlenebilirlik katmanıdır.
            // Ana kullanıcı işlemi zaten kalıcı başarıya ulaştıysa audit I/O hatası
            // işlemi geri çevirmesin; teknik log üzerinden görünür kalsın.
            ApplicationCrashLogger.Write(ex, $"AuditTrail.{eventType}");
        }
    }
}
