using DogruFatura.Core.Models;
using System.Globalization;

namespace DogruFatura.Core.Services;

public sealed record SchoolValidationResult(bool IsValid, string Message);

public static class SchoolValidationService
{
    private static readonly HashSet<string> InstitutionTypes = new(StringComparer.Ordinal)
    {
        "İlkokul",
        "Ortaokul",
        "Anasınıfı/Anaokulu",
        "Kurum"
    };

    private static readonly StringComparer TurkishTextComparer =
        StringComparer.Create(CultureInfo.GetCultureInfo("tr-TR"), true);

    public static SchoolRecord Normalize(SchoolRecord school)
    {
        var normalized = school with
        {
            InstitutionType = school.InstitutionType.Trim(),
            Name = school.Name.Trim(),
            Vkn = school.Vkn.Trim(),
            ElectricityServiceNumber = school.ElectricityServiceNumber.Trim(),
            TelephoneServiceNumber = school.TelephoneServiceNumber.Trim(),
            InternetServiceNumber = school.InternetServiceNumber.Trim(),
            WaterServiceNumber = school.WaterServiceNumber.Trim()
        };

        return normalized.SchoolId == Guid.Empty
            ? normalized with { SchoolId = SchoolIdentityService.CreateLegacyId(normalized) }
            : normalized;
    }

    public static SchoolValidationResult Validate(
        SchoolRecord school,
        IEnumerable<SchoolRecord> existingSchools,
        SchoolRecord? editedSchool = null)
    {
        if (!InstitutionTypes.Contains(school.InstitutionType))
            return Invalid("Geçerli bir kurum türü seçin.");

        if (string.IsNullOrWhiteSpace(school.Name))
            return Invalid("Okul / kurum adı boş bırakılamaz.");

        if (school.Name.Length > 200)
            return Invalid("Okul / kurum adı en fazla 200 karakter olabilir.");

        var identifierError = ValidateIdentifier("VKN", school.Vkn, exactLength: 10)
                              ?? ValidateIdentifier("Elektrik hizmet no", school.ElectricityServiceNumber)
                              ?? ValidateIdentifier("Telefon hizmet no", school.TelephoneServiceNumber)
                              ?? ValidateIdentifier("İnternet hizmet no", school.InternetServiceNumber)
                              ?? ValidateIdentifier("Su hizmet no", school.WaterServiceNumber);
        if (identifierError is not null) return Invalid(identifierError);

        var duplicate = existingSchools.Any(existing =>
            !ReferenceEquals(existing, editedSchool)
            && existing.InstitutionType == school.InstitutionType
            && TurkishTextComparer.Equals(existing.Name.Trim(), school.Name));
        if (duplicate)
            return Invalid("Aynı kurum türünde bu ada sahip başka bir kayıt zaten var.");

        return new SchoolValidationResult(true, "Kayıt geçerli.");
    }

    private static string? ValidateIdentifier(string fieldName, string value, int? exactLength = null)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (value.Length > 50) return $"{fieldName} en fazla 50 karakter olabilir.";
        if (!value.All(char.IsDigit)) return $"{fieldName} yalnızca rakamlardan oluşmalıdır.";
        if (exactLength.HasValue && value.Length != exactLength.Value)
            return $"{fieldName} {exactLength.Value} haneli olmalıdır.";
        return null;
    }

    private static SchoolValidationResult Invalid(string message) => new(false, message);
}
