using DogruFatura.Core.Models;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DogruFatura.Core.Services;

/// <summary>Resolves reporting copies; never rewrites sealed historical records.</summary>
public static class InstitutionResolutionService
{
    private static readonly CompareInfo Turkish = CultureInfo.GetCultureInfo("tr-TR").CompareInfo;
    private static string Key(string? text) => Regex.Replace((text ?? "").Trim(), @"\s+", " ");
    private static bool Same(string? a, string? b) => Turkish.Compare(Key(a), Key(b), CompareOptions.IgnoreCase) == 0;
    private static string Digits(string? value) => new string((value ?? "").Where(char.IsDigit).ToArray()).TrimStart('0');

    public static SchoolRecord? Resolve(string name, string invoiceType, string service, Guid schoolId, IEnumerable<SchoolRecord> catalog)
    {
        var schools = catalog.ToArray();
        if (schoolId != Guid.Empty && schools.SingleOrDefault(s => s.SchoolId == schoolId) is { } byId) return byId;
        var exact = schools.Where(s => Same(s.Name, name)).ToArray();
        if (exact.Length == 1) return exact[0];
        // Only remove a row prefix when the remainder is a known complete school name.
        var withoutRow = Regex.Replace(Key(name), @"^\d{1,4}\s*(?:[|.)-]\s*|\s+)", "");
        var aliases = schools.Where(s => Same(s.Name, withoutRow)).ToArray();
        if (aliases.Length == 1) return aliases[0];
        var nameKey = Key(name);
        if (nameKey.Length >= 12 && nameKey.Split(' ').Length >= 3)
        {
            var expanded = schools.Where(s => Turkish.IsPrefix(Key(s.Name), nameKey + " ", CompareOptions.IgnoreCase)).ToArray();
            if (expanded.Length == 1) return expanded[0];
        }
        var number = Digits(service);
        if (number.Length == 0) return null;
        var candidates = schools.Where(s => Digits(s.GetServiceNumber(invoiceType)) == number).ToArray();
        return candidates.Length == 1 ? candidates[0] : null;
    }

    public static InvoiceRecord ForReporting(InvoiceRecord invoice, IEnumerable<SchoolRecord> catalog)
    {
        var school = Resolve(invoice.InstitutionName, invoice.InvoiceType, invoice.ServiceNumber, invoice.SchoolId, catalog);
        return school is null ? invoice : invoice with { InstitutionName = school.Name, SchoolId = school.SchoolId };
    }

    public static bool MatchesSearch(string name, string query) => Turkish.IndexOf(Key(name), Key(query), CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;
}
