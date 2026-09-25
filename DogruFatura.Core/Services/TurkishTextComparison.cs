using System.Globalization;

namespace DogruFatura.Core.Services;

public static class TurkishTextComparison
{
    private static readonly CompareInfo TurkishCompareInfo =
        CultureInfo.GetCultureInfo("tr-TR").CompareInfo;

    public static bool EqualsIgnoreCase(string? left, string? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;

        return TurkishCompareInfo.Compare(left, right, CompareOptions.IgnoreCase) == 0;
    }
}
