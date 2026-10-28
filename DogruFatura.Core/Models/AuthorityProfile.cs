namespace DogruFatura.Core.Models;

public sealed record AuthorityProfile(
    string RealizationOfficerName,
    string RealizationOfficerTitle,
    string SpendingAuthorityName,
    string SpendingAuthorityTitle)
{
    public AuthorityProfile Normalize() => new(
        (RealizationOfficerName ?? string.Empty).Trim(),
        (RealizationOfficerTitle ?? string.Empty).Trim(),
        (SpendingAuthorityName ?? string.Empty).Trim(),
        (SpendingAuthorityTitle ?? string.Empty).Trim());

    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(RealizationOfficerName)
        && !string.IsNullOrWhiteSpace(RealizationOfficerTitle)
        && !string.IsNullOrWhiteSpace(SpendingAuthorityName)
        && !string.IsNullOrWhiteSpace(SpendingAuthorityTitle);

    // Do not add this derived value to existing archive snapshot JSON/hash contracts.
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(RealizationOfficerName)
        && string.IsNullOrWhiteSpace(RealizationOfficerTitle)
        && string.IsNullOrWhiteSpace(SpendingAuthorityName)
        && string.IsNullOrWhiteSpace(SpendingAuthorityTitle);
}

public static class AuthorityProfileDefaults
{
    public static AuthorityProfile Create() => new(
#if DOGRUFATURA_CLEAN_DISTRIBUTION
        "", "", "", "");
#else
        "Halil ÖZENÇ",
        "Şube Müdürü",
        "Özgür BAYSAL",
        "İlçe Milli Eğitim Müdürü");
#endif
}
