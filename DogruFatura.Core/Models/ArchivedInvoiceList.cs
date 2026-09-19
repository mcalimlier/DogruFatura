using System.Text.Json.Serialization;

namespace DogruFatura.Core.Models;

public sealed record ArchivedInvoiceList(
    Guid Id,
    string ArchiveNumber,
    DateTime ArchivedAt,
    string InvoiceType,
    List<InvoiceRecord> Invoices,
    AuthorityProfile AuthorityProfile,
    string HysPaymentOrderDocumentNumber = "",
    bool IsClosed = false,
    DateTime? ClosedAt = null,
    string PdfFileName = "",
    string PdfSha256 = "",
    string ClosedSnapshotSha256 = "")
{
    [JsonIgnore]
    public InvoiceKind Kind => DomainValueText.ParseInvoiceKind(InvoiceType);

    [JsonIgnore]
    public ArchiveState State => IsClosed ? ArchiveState.Closed : ArchiveState.Open;

    [JsonIgnore]
    public int InvoiceCount => Invoices.Count;

    [JsonIgnore]
    public decimal PaymentTotal => Invoices.Sum(invoice => invoice.PaymentAmount);

    [JsonIgnore]
    public string HysDisplay => string.IsNullOrWhiteSpace(HysPaymentOrderDocumentNumber)
        ? "—"
        : HysPaymentOrderDocumentNumber;

    [JsonIgnore]
    public string ClosureStatus => DomainValueText.ToStorageValue(State);

    [JsonIgnore]
    public bool IsStatisticsEligible => IsClosed && !string.IsNullOrWhiteSpace(HysPaymentOrderDocumentNumber);

    [JsonIgnore]
    public bool HasClosedSnapshot => !string.IsNullOrWhiteSpace(ClosedSnapshotSha256);

    [JsonIgnore]
    public string InstitutionSummary
    {
        get
        {
            var institutions = Invoices
                .Select(invoice => invoice.InstitutionName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
            if (institutions.Length == 0) return "—";
            if (institutions.Length <= 2) return string.Join(", ", institutions);
            return $"{institutions[0]}, {institutions[1]} +{institutions.Length - 2}";
        }
    }

    [JsonIgnore]
    public DateTime FirstInvoiceDate => Invoices.Min(invoice => invoice.InvoiceDate);

    [JsonIgnore]
    public DateTime LastInvoiceDate => Invoices.Max(invoice => invoice.InvoiceDate);

    public ArchivedInvoiceList Normalize() => this with
    {
        ArchiveNumber = (ArchiveNumber ?? string.Empty).Trim(),
        InvoiceType = (InvoiceType ?? string.Empty).Trim(),
        HysPaymentOrderDocumentNumber = (HysPaymentOrderDocumentNumber ?? string.Empty).Trim().ToUpperInvariant(),
        PdfFileName = (PdfFileName ?? string.Empty).Trim(),
        PdfSha256 = (PdfSha256 ?? string.Empty).Trim().ToUpperInvariant(),
        ClosedSnapshotSha256 = (ClosedSnapshotSha256 ?? string.Empty).Trim().ToUpperInvariant(),
        Invoices = Invoices?.ToList() ?? new List<InvoiceRecord>(),
        AuthorityProfile = (AuthorityProfile ?? AuthorityProfileDefaults.Create()).Normalize()
    };
}
