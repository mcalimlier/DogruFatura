using System.Globalization;
using System.Text.RegularExpressions;

namespace DogruFatura.Core.Services;

public static class NumberParser
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    public static bool TryNonNegativeDecimal(string? text, out decimal value)
    {
        value = 0m;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var normalized = text.Trim()
            .Replace("₺", "", StringComparison.OrdinalIgnoreCase)
            .Replace("TL", "", StringComparison.OrdinalIgnoreCase);
        normalized = Regex.Replace(normalized, @"\s+", "");

        var comma = normalized.LastIndexOf(',');
        var dot = normalized.LastIndexOf('.');
        if (comma >= 0 && dot >= 0)
        {
            if (comma > dot)
                normalized = normalized.Replace(".", "", StringComparison.Ordinal).Replace(',', '.');
            else
                normalized = normalized.Replace(",", "", StringComparison.Ordinal);
        }
        else if (comma >= 0)
        {
            normalized = normalized.Replace(".", "", StringComparison.Ordinal).Replace(',', '.');
        }
        else if (dot >= 0)
        {
            var dotCount = normalized.Count(c => c == '.');
            var fractionLength = normalized.Length - dot - 1;
            if (dotCount > 1 || fractionLength == 3 && dot <= 3)
                normalized = normalized.Replace(".", "", StringComparison.Ordinal);
        }

        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out value)
               && value >= 0m;
    }

    public static string Money(decimal value) => value.ToString("N2", Tr) + " TL";

    public static string MoneyValue(decimal value) => value.ToString("N2", Tr);
}
