using PdfSharp.Pdf;

namespace DogruFatura.Wpf;

public static class PdfMetadataContract
{
    public const string Schema = "DogruFaturaPdf/1";
    public const string PaymentListDocumentType = "PaymentList";
    public const string StatisticsReportDocumentType = "StatisticsReport";

    public static void Apply(
        PdfDocument document,
        string title,
        string subject,
        string documentType)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("PDF başlığı boş olamaz.", nameof(title));
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("PDF konusu boş olamaz.", nameof(subject));
        if (string.IsNullOrWhiteSpace(documentType))
            throw new ArgumentException("PDF belge türü boş olamaz.", nameof(documentType));

        document.Info.Title = title.Trim();
        document.Info.Author = "DoğruFatura";
        document.Info.Subject = subject.Trim();
        document.Info.Creator = "DoğruFatura " + AppVersionInfo.InformationalVersion;
        document.Info.Keywords = BuildKeywords(documentType);
    }

    public static string BuildKeywords(string documentType)
    {
        if (string.IsNullOrWhiteSpace(documentType))
            throw new ArgumentException("PDF belge türü boş olamaz.", nameof(documentType));

        return string.Join(
            ';',
            "DoğruFatura",
            "Schema=" + Schema,
            "DocumentType=" + NormalizeToken(documentType),
            "AppVersion=" + NormalizeToken(AppVersionInfo.InformationalVersion));
    }

    private static string NormalizeToken(string value) =>
        value.Trim()
            .Replace(';', '_')
            .Replace('\r', '_')
            .Replace('\n', '_');
}
