using DogruFatura.Core.Models;
using System.Security.Cryptography;
using System.Text;

namespace DogruFatura.Core.Services;

public static class SchoolIdentityService
{
    public static Guid EnsureId(SchoolRecord school)
    {
        ArgumentNullException.ThrowIfNull(school);
        return school.SchoolId == Guid.Empty ? CreateLegacyId(school) : school.SchoolId;
    }

    public static Guid CreateLegacyId(SchoolRecord school)
    {
        ArgumentNullException.ThrowIfNull(school);

        var canonical = string.Join(
            "\u001F",
            school.InstitutionType?.Trim() ?? string.Empty,
            school.Name?.Trim() ?? string.Empty,
            school.Vkn?.Trim() ?? string.Empty,
            school.ElectricityServiceNumber?.Trim() ?? string.Empty,
            school.TelephoneServiceNumber?.Trim() ?? string.Empty,
            school.InternetServiceNumber?.Trim() ?? string.Empty,
            school.WaterServiceNumber?.Trim() ?? string.Empty);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        var guidBytes = hash.AsSpan(0, 16).ToArray();
        var id = new Guid(guidBytes);
        return id == Guid.Empty
            ? new Guid("7b202f95-c423-4c59-b774-1027105f8402")
            : id;
    }
}
