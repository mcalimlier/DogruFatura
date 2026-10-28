using System.Globalization;

namespace DogruFatura.Wpf;

public sealed record DonutPercentLabel(int Index, decimal Percent, string Text, double AnchorX, double AnchorY,
    double Left, double Top, bool IsCallout);

public static class DonutPercentageLayout
{
    public const double CanvasWidth = 460;
    public const double CanvasHeight = 380;
    public const double LabelWidth = 58;
    public const double LabelHeight = 24;

    public static IReadOnlyList<DonutPercentLabel> Plan(IReadOnlyList<decimal> amounts)
    {
        var total = amounts.Sum();
        if (total <= 0) return Array.Empty<DonutPercentLabel>();
        var labels = new List<DonutPercentLabel>();
        var angle = -90d;
        for (int index = 0; index < amounts.Count; index++)
        {
            if (amounts[index] <= 0) continue;
            var percent = amounts[index] / total * 100m;
            var sweep = (double)percent * 3.6d;
            var radians = (angle + sweep / 2) * Math.PI / 180;
            var callout = percent < 6m;
            var x = callout ? Math.Cos(radians) >= 0 ? CanvasWidth - LabelWidth - 4 : 4
                : CanvasWidth / 2 + Math.Cos(radians) * 121 - LabelWidth / 2;
            var y = CanvasHeight / 2 + Math.Sin(radians) * (callout ? 174 : 121) - LabelHeight / 2;
            var text = FormatPercent(percent);
            labels.Add(new(index, percent, text, CanvasWidth / 2 + Math.Cos(radians) * 157,
                CanvasHeight / 2 + Math.Sin(radians) * 157, Math.Clamp(x, 4, CanvasWidth - LabelWidth - 4),
                Math.Clamp(y, 4, CanvasHeight - LabelHeight - 4), callout));
            angle += sweep;
        }
        var placed = new List<DonutPercentLabel>();
        foreach (var label in labels.OrderByDescending(label => label.Percent))
        {
            var selected = label;
            for (int attempt = 0; attempt < 30; attempt++)
            {
                var offset = attempt == 0 ? 0 : ((attempt + 1) / 2) * 28 * (attempt % 2 == 1 ? 1 : -1);
                var top = Math.Clamp(label.Top + offset, 4, CanvasHeight - LabelHeight - 4);
                if (placed.Any(other => Overlaps(label.Left, top, other.Left, other.Top))) continue;
                selected = label with { Top = top }; break;
            }
            placed.Add(selected);
        }
        return placed.OrderBy(label => label.Index).ToArray();
    }

    public static bool Overlaps(double x, double y, double otherX, double otherY) =>
        x < otherX + LabelWidth + 2 && x + LabelWidth + 2 > otherX
        && y < otherY + LabelHeight + 2 && y + LabelHeight + 2 > otherY;

    public static string FormatPercent(decimal percent) => percent is > 0 and < .01m ? "<%0,01"
        : "%" + percent.ToString(percent % 1 == 0 ? "N0" : percent < 1 ? "N2" : "N1", CultureInfo.GetCultureInfo("tr-TR"));
}
