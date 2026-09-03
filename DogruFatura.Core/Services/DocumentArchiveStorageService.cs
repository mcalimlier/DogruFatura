using DogruFatura.Core.Models;
using System.Security.Cryptography;
using System.Text;

namespace DogruFatura.Core.Services;

public sealed record PdfMoveResult(
    bool Moved,
    string? SourcePath,
    string? DestinationPath,
    string PdfFileName)
{
    public static PdfMoveResult None(string? pdfFileName = null) =>
        new(false, null, null, pdfFileName ?? string.Empty);
}

public sealed class DocumentArchiveStorageService
{
    public DocumentArchiveStorageService(string? desktopRoot = null)
    {
        var desktop = desktopRoot ?? Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        RootDirectory = Path.Combine(desktop, "DoğruFatura");
        ActiveOutputDirectory = Path.Combine(RootDirectory, "Aktif Çıktılar");
        ArchivedOutputDirectory = Path.Combine(RootDirectory, "Arşivlenenler");
    }

    public string RootDirectory { get; }
    public string ActiveOutputDirectory { get; }
    public string ArchivedOutputDirectory { get; }

    public void EnsureDirectories()
    {
        Directory.CreateDirectory(ActiveOutputDirectory);
        Directory.CreateDirectory(ArchivedOutputDirectory);
    }

    public string GetActiveOutputPath(
        IReadOnlyList<InvoiceRecord> invoices,
        string invoiceType,
        DateTime reportDate)
    {
        EnsureDirectories();
        var safeType = SafeFilePart(invoiceType).ToUpperInvariant();
        var fingerprint = InvoiceFingerprint(invoices);
        return Path.Combine(
            ActiveOutputDirectory,
            $"AKTIF-{safeType}-{reportDate:yyyyMMdd}-{fingerprint}.pdf");
    }

    public string GetArchivedOutputPath(string normalizedHysNumber)
    {
        EnsureDirectories();
        return Path.Combine(ArchivedOutputDirectory, normalizedHysNumber + ".pdf");
    }

    public string? FindActiveOutputPath(ArchivedInvoiceList archive)
    {
        EnsureDirectories();
        if (!string.IsNullOrWhiteSpace(archive.PdfFileName))
        {
            var preferredPath = Path.Combine(ActiveOutputDirectory, archive.PdfFileName);
            if (File.Exists(preferredPath)) return preferredPath;
        }

        return FindActiveOutputPath(archive.Invoices);
    }

    public string? FindActiveOutputPath(IReadOnlyList<InvoiceRecord> invoices)
    {
        EnsureDirectories();

        var fingerprint = InvoiceFingerprint(invoices);
        return Directory
            .EnumerateFiles(ActiveOutputDirectory, $"*-{fingerprint}.pdf", SearchOption.TopDirectoryOnly)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }

    public PdfMoveResult MoveActiveOutputToArchive(
        ArchivedInvoiceList archive,
        string normalizedHysNumber)
    {
        var destinationPath = GetArchivedOutputPath(normalizedHysNumber);
        if (File.Exists(destinationPath))
            throw new IOException($"Arşivlenenler klasöründe {Path.GetFileName(destinationPath)} zaten bulunuyor.");

        var sourcePath = FindActiveOutputPath(archive);
        if (sourcePath is null) return PdfMoveResult.None();

        File.Move(sourcePath, destinationPath);
        return new PdfMoveResult(true, sourcePath, destinationPath, Path.GetFileName(destinationPath));
    }

    public PdfMoveResult MoveArchivedOutputToActive(ArchivedInvoiceList archive)
    {
        EnsureDirectories();
        var archivedFileName = !string.IsNullOrWhiteSpace(archive.PdfFileName)
            ? archive.PdfFileName
            : !string.IsNullOrWhiteSpace(archive.HysPaymentOrderDocumentNumber)
                ? archive.HysPaymentOrderDocumentNumber + ".pdf"
                : string.Empty;
        if (string.IsNullOrWhiteSpace(archivedFileName)) return PdfMoveResult.None();

        var sourcePath = Path.Combine(ArchivedOutputDirectory, archivedFileName);
        if (!File.Exists(sourcePath)) return PdfMoveResult.None();

        var destinationPath = GetActiveOutputPath(
            archive.Invoices,
            archive.InvoiceType,
            archive.ArchivedAt.Date);
        if (File.Exists(destinationPath))
            throw new IOException($"Aktif Çıktılar klasöründe {Path.GetFileName(destinationPath)} zaten bulunuyor.");

        File.Move(sourcePath, destinationPath);
        return new PdfMoveResult(true, sourcePath, destinationPath, Path.GetFileName(destinationPath));
    }

    public string CopyImportedPdf(string sourcePath, string normalizedHysNumber)
    {
        if (!File.Exists(sourcePath)) throw new FileNotFoundException("İçe aktarılacak PDF bulunamadı.", sourcePath);
        var destinationPath = GetArchivedOutputPath(normalizedHysNumber);
        if (string.Equals(Path.GetFullPath(sourcePath), Path.GetFullPath(destinationPath), StringComparison.OrdinalIgnoreCase))
            return Path.GetFileName(destinationPath);
        if (File.Exists(destinationPath))
            throw new IOException($"Arşivlenenler klasöründe {Path.GetFileName(destinationPath)} zaten bulunuyor.");

        File.Copy(sourcePath, destinationPath, false);
        return Path.GetFileName(destinationPath);
    }

    public static void RollbackMove(PdfMoveResult move)
    {
        if (!move.Moved || move.SourcePath is null || move.DestinationPath is null) return;
        if (!File.Exists(move.DestinationPath) || File.Exists(move.SourcePath)) return;
        File.Move(move.DestinationPath, move.SourcePath);
    }

    public static string InvoiceFingerprint(IReadOnlyList<InvoiceRecord> invoices)
    {
        var source = string.Join("|", invoices.Select(invoice => invoice.Id.ToString("N")));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(source));
        return Convert.ToHexString(hash)[..12];
    }

    private static string SafeFilePart(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = string.Concat((value ?? string.Empty).Select(character =>
            invalid.Contains(character) ? '_' : character));
        return string.IsNullOrWhiteSpace(safe) ? "FATURA" : safe;
    }
}
