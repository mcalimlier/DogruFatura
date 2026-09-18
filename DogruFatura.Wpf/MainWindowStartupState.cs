using DogruFatura.Core.Models;
using DogruFatura.Core.Services;

namespace DogruFatura.Wpf;

internal sealed record MainWindowStartupState(
    SafetyCheckResult SafetyResult,
    SchoolLoadResult SchoolLoadResult,
    InvoiceLoadResult InvoiceLoadResult,
    AuthorityProfileLoadResult AuthorityProfileLoadResult,
    ArchivedInvoiceListLoadResult ArchiveLoadResult);

internal static class MainWindowStartupStateLoader
{
    public static MainWindowStartupState Load(
        ApplicationSafetyService safetyService,
        SchoolDataStore schoolDataStore,
        InvoiceDataStore invoiceDataStore,
        AuthorityProfileDataStore authorityProfileDataStore,
        ArchivedInvoiceListDataStore archiveDataStore)
    {
        ArgumentNullException.ThrowIfNull(safetyService);
        ArgumentNullException.ThrowIfNull(schoolDataStore);
        ArgumentNullException.ThrowIfNull(invoiceDataStore);
        ArgumentNullException.ThrowIfNull(authorityProfileDataStore);
        ArgumentNullException.ThrowIfNull(archiveDataStore);

        var safetyResult = safetyService.InitializeAndCheck();

        SchoolLoadResult schoolLoadResult;
        try
        {
            schoolLoadResult = schoolDataStore.LoadOrCreate();
        }
        catch (Exception ex)
        {
            schoolLoadResult = new SchoolLoadResult(
                SchoolSeedData.Create(),
                "Kalıcı okul/kurum verisi açılamadı; bu oturumda başlangıç kayıtları kullanılıyor. " + ex.Message,
                schoolDataStore.DataFilePath);
        }

        InvoiceLoadResult invoiceLoadResult;
        try
        {
            invoiceLoadResult = invoiceDataStore.LoadOrCreate();
        }
        catch (Exception ex)
        {
            invoiceLoadResult = new InvoiceLoadResult(
                Array.Empty<InvoiceRecord>(),
                "Kalıcı aktif fatura verisi açılamadı; bu oturumda boş liste kullanılıyor. " + ex.Message,
                invoiceDataStore.DataFilePath);
        }

        AuthorityProfileLoadResult authorityProfileLoadResult;
        try
        {
            authorityProfileLoadResult = authorityProfileDataStore.LoadOrCreate();
        }
        catch (Exception ex)
        {
            authorityProfileLoadResult = new AuthorityProfileLoadResult(
                AuthorityProfileDefaults.Create(),
                "Kalıcı yetkili/imza profili açılamadı; bu oturumda varsayılan bilgiler kullanılıyor. " + ex.Message,
                authorityProfileDataStore.DataFilePath);
        }

        ArchivedInvoiceListLoadResult archiveLoadResult;
        try
        {
            archiveLoadResult = archiveDataStore.LoadOrCreate();
        }
        catch (Exception ex)
        {
            archiveLoadResult = new ArchivedInvoiceListLoadResult(
                Array.Empty<ArchivedInvoiceList>(),
                "Kalıcı işlem arşivi açılamadı; bu oturumda boş arşiv kullanılıyor. " + ex.Message,
                archiveDataStore.DataFilePath);
        }

        return new MainWindowStartupState(
            safetyResult,
            schoolLoadResult,
            invoiceLoadResult,
            authorityProfileLoadResult,
            archiveLoadResult);
    }
}
