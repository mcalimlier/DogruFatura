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
    string ClosedSnapshotSha256 = "",
    List<LegacyInvoiceEvidenceRecord>? LegacyEvidenceItems = null,
    decimal LegacyGrossTotal = 0m,
    decimal LegacyStampDutyTotal = 0m,
    decimal LegacyPaymentTotal = 0m,
    string LegacyEvidenceNote = "")
{
    [JsonIgnore]
    public InvoiceKind Kind => DomainValueText.ParseInvoiceKind(InvoiceType);

    [JsonIgnore]
    public ArchiveState State => IsClosed ? ArchiveState.Closed : ArchiveState.Open;

    [JsonIgnore]
    public bool IsLegacyEvidenceOnly =>
        Invoices.Count == 0 && LegacyEvidenceItems is { Count: > 0 };

    [JsonIgnore]
    public int InvoiceCount => Invoices.Count > 0
        ? Invoices.Count
        : LegacyEvidenceItems?.Count ?? 0;

    [JsonIgnore]
    public decimal PaymentTotal => Invoices.Count > 0
        ? Invoices.Sum(invoice => invoice.PaymentAmount)
        : LegacyPaymentTotal;

    [JsonIgnore]
    public string HysDisplay => string.IsNullOrWhiteSpace(HysPaymentOrderDocumentNumber)
        ? "—"
        : HysPaymentOrderDocumentNumber;

    [JsonIgnore]
    public string ClosureStatus => DomainValueText.ToStorageValue(State);

    [JsonIgnore]
    public bool IsStatisticsEligible =>
        IsClosed
        && !string.IsNullOrWhiteSpace(HysPaymentOrderDocumentNumber)
        && !IsLegacyEvidenceOnly;

    [JsonIgnore]
    public bool HasClosedSnapshot => !string.IsNullOrWhiteSpace(ClosedSnapshotSha256);

    [JsonIgnore]
    public string InstitutionSummary
    {
        get
        {
            var institutions = (Invoices.Count > 0
                    ? Invoices.Select(invoice => invoice.InstitutionName)
                    : LegacyEvidenceItems?.Select(item => item.InstitutionName)
                      ?? Enumerable.Empty<string>())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
            if (institutions.Length == 0) return "—";
            if (institutions.Length <= 2) return string.Join(", ", institutions);
            return $"{institutions[0]}, {institutions[1]} +{institutions.Length - 2}";
        }
    }

    [JsonIgnore]
    public DateTime FirstInvoiceDate => Invoices.Count > 0
        ? Invoices.Min(invoice => invoice.InvoiceDate)
        : LegacyEvidenceItems is { Count: > 0 }
            ? LegacyEvidenceItems.Min(item => item.PeriodStart)
            : ArchivedAt;

    [JsonIgnore]
    public DateTime LastInvoiceDate => Invoices.Count > 0
        ? Invoices.Max(invoice => invoice.InvoiceDate)
        : LegacyEvidenceItems is { Count: > 0 }
            ? LegacyEvidenceItems.Max(item => item.PeriodEnd)
            : ArchivedAt;

    public ArchivedInvoiceList Normalize() => this with
    {
        ArchiveNumber = (ArchiveNumber ?? string.Empty).Trim(),
        InvoiceType = (InvoiceType ?? string.Empty).Trim(),
        HysPaymentOrderDocumentNumber = (HysPaymentOrderDocumentNumber ?? string.Empty).Trim().ToUpperInvariant(),
        PdfFileName = (PdfFileName ?? string.Empty).Trim(),
        PdfSha256 = (PdfSha256 ?? string.Empty).Trim().ToUpperInvariant(),
        ClosedSnapshotSha256 = (ClosedSnapshotSha256 ?? string.Empty).Trim().ToUpperInvariant(),
        LegacyEvidenceNote = (LegacyEvidenceNote ?? string.Empty).Trim(),
        Invoices = Invoices?.ToList() ?? new List<InvoiceRecord>(),
        LegacyEvidenceItems = LegacyEvidenceItems?
            .Select(item => item.Normalize())
            .ToList() ?? new List<LegacyInvoiceEvidenceRecord>(),
        AuthorityProfile = (AuthorityProfile ?? AuthorityProfileDefaults.Create()).Normalize()
    };
}
