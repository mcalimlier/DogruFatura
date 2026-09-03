using System.Text.RegularExpressions;

namespace DogruFatura.Core.Services;

public static partial class HysPaymentOrderService
{
    [GeneratedRegex(@"^[1-9][0-9]*$", RegexOptions.CultureInvariant)]
    private static partial Regex NumberPattern();

    public static bool TryNormalize(string? input, out string normalized, out string errorMessage)
    {
        normalized = string.Empty;
        errorMessage = string.Empty;
        var value = (input ?? string.Empty)
            .Trim()
            .ToUpperInvariant()
            .Replace(" ", string.Empty, StringComparison.Ordinal);

        if (value.StartsWith("HYS-", StringComparison.Ordinal))
            value = value[4..];
        else if (value.StartsWith("HYS", StringComparison.Ordinal))
            value = value[3..].TrimStart('-');

        if (string.IsNullOrWhiteSpace(value))
        {
            errorMessage = "HYS Ödeme Emri Belge No boş bırakılamaz.";
            return false;
        }

        if (value.Length > 40 || !NumberPattern().IsMatch(value))
        {
            errorMessage = "HYS Ödeme Emri Belge No sıfırla başlamayan rakamlardan oluşmalıdır. Örnek: HYS-202614586.";
            return false;
        }

        normalized = $"HYS-{value}";
        return true;
    }
}
