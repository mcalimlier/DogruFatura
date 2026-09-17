using DogruFatura.Core.Models;
using DogruFatura.Core.Services;
using System.IO;

namespace DogruFatura.Wpf;

public sealed record ArchivePdfSynchronizationResult(
    int ScannedCount,
    int CreatedCount,
    int ExistingCount,
    IReadOnlyList<ArchivedInvoiceList> UpdatedArchives,
    IReadOnlyList<string> Errors)
{
    public int ReferenceUpdateCount => UpdatedArchives.Count;
    public bool HasChanges => ReferenceUpdateCount > 0;
    public bool IsSuccessful => Errors.Count == 0;
}

public sealed class ArchivePdfSynchronizationService
{
    private readonly DocumentArchiveStorageService _storageService;
    private readonly Action<FinalReportData, string> _savePdf;

    public ArchivePdfSynchronizationService(
        DocumentArchiveStorageService storageService,
        Action<FinalReportData, string> savePdf)
    {
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _savePdf = savePdf ?? throw new ArgumentNullException(nameof(savePdf));
    }

    public ArchivePdfSynchronizationResult Synchronize(
        IReadOnlyList<ArchivedInvoiceList> archives)
    {
        ArgumentNullException.ThrowIfNull(archives);
        _storageService.EnsureDirectories();

        var createdCount = 0;
        var existingCount = 0;
        var updatedArchives = new List<ArchivedInvoiceList>();
        var errors = new List<string>();

        foreach (var archive in archives)
        {
            try
            {
                var targetPath = ResolveTargetPath(archive);
                var existedBeforeSync = File.Exists(targetPath);
                if (existedBeforeSync)
                {
                    existingCount++;
                }
                else
                {
                    var reportData = CreateReportData(archive);
                    _savePdf(reportData, targetPath);
                    createdCount++;
                }

                var currentHash = FileIntegrityService.ComputeSha256(targetPath);
                if (existedBeforeSync
                    && !string.IsNullOrWhiteSpace(archive.PdfSha256)
                    && !string.Equals(archive.PdfSha256, currentHash, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add(
                        $"{archive.ArchiveNumber}: PDF içerik bütünlüğü doğrulanamadı. "
                        + "Dosya kayıtlı SHA-256 özetiyle eşleşmiyor; kayıtlı hash otomatik değiştirilmedi.");
                    continue;
                }

                var expectedFileName = Path.GetFileName(targetPath);
                var needsFileNameUpdate = !string.Equals(
                    archive.PdfFileName,
                    expectedFileName,
                    StringComparison.OrdinalIgnoreCase);
                var needsHashUpdate = !string.Equals(
                    archive.PdfSha256,
                    currentHash,
                    StringComparison.OrdinalIgnoreCase);

                if (needsFileNameUpdate || needsHashUpdate)
                {
                    updatedArchives.Add(archive with
                    {
                        PdfFileName = expectedFileName,
                        PdfSha256 = currentHash
                    });
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{archive.ArchiveNumber}: {ex.Message}");
            }
        }

        return new ArchivePdfSynchronizationResult(
            archives.Count,
            createdCount,
            existingCount,
            updatedArchives,
            errors);
    }

    private string ResolveTargetPath(ArchivedInvoiceList archive)
    {
        var validation = ArchivedInvoiceListValidationService.Validate(archive);
        if (!validation.IsValid)
            throw new InvalidOperationException(validation.Message);

        if (archive.IsClosed)
        {
            if (!HysPaymentOrderService.TryNormalize(
                    archive.HysPaymentOrderDocumentNumber,
                    out var normalizedHysNumber,
                    out var hysError))
                throw new InvalidOperationException(hysError);

            return _storageService.GetArchivedOutputPath(normalizedHysNumber);
        }

        var existingActivePath = _storageService.FindActiveOutputPath(archive);
        return existingActivePath ?? _storageService.GetActiveOutputPath(
            archive.Invoices,
            archive.InvoiceType,
            archive.ArchivedAt.Date);
    }

    private static FinalReportData CreateReportData(ArchivedInvoiceList archive)
    {
        var validation = FinalReportValidationService.Validate(archive.Invoices);
        if (!validation.IsValid || validation.InvoiceType is null)
            throw new InvalidOperationException(validation.Message);

        var authority = archive.AuthorityProfile.Normalize();
        return new FinalReportData(
            archive.Invoices.ToArray(),
            validation.InvoiceType,
            archive.ArchivedAt.Date,
            new ReportSignature(
                "Gerçekleştirme Görevlisi",
                authority.RealizationOfficerName,
                authority.RealizationOfficerTitle),
            new ReportSignature(
                "Harcama Yetkilisi",
                authority.SpendingAuthorityName,
                authority.SpendingAuthorityTitle));
    }
}
