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
}

public static class AuthorityProfileDefaults
{
    public static AuthorityProfile Create() => new(
        "Halil ÖZENÇ",
        "Şube Müdürü",
        "Özgür BAYSAL",
        "İlçe Milli Eğitim Müdürü");
}
