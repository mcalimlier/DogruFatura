using DogruFatura.Wpf;

namespace DogruFatura.Tests;

public sealed class DonutPercentageLayoutTests
{
    private static readonly decimal[] SmallSlices = { 97m, 1m, 1m, 1m };
    private static readonly decimal[] ClusteredSlices = { 99.97m, .01m, .01m, .01m };
    private static readonly decimal[] ZeroSlices = { 0m, 0m };
    [Fact]
    public void EveryPositiveSliceHasALabelIncludingBelowSixPercent()
    {
        var labels = DonutPercentageLayout.Plan(SmallSlices);
        Assert.Equal(4, labels.Count);
        Assert.Equal(100m, labels.Sum(label => label.Percent));
        Assert.Equal(3, labels.Count(label => label.Percent < 6));
        Assert.Equal(3, labels.Count(label => label.IsCallout));
    }

    [Fact]
    public void ClusteredTinySlicesStayInsideCanvasWithoutOverlapping()
    {
        var labels = DonutPercentageLayout.Plan(ClusteredSlices);
        foreach (var label in labels)
        {
            Assert.InRange(label.Left, 0, DonutPercentageLayout.CanvasWidth - DonutPercentageLayout.LabelWidth);
            Assert.InRange(label.Top, 0, DonutPercentageLayout.CanvasHeight - DonutPercentageLayout.LabelHeight);
            foreach (var other in labels.Where(item => item.Index != label.Index))
                Assert.False(DonutPercentageLayout.Overlaps(label.Left, label.Top, other.Left, other.Top));
        }
        Assert.All(labels, label => Assert.False(string.IsNullOrWhiteSpace(label.Text)));
    }

    [Fact]
    public void EmptyOrZeroAmountsHaveNoLabels() => Assert.Empty(DonutPercentageLayout.Plan(ZeroSlices));

    [Fact]
    public void TinyPercentageKeepsPrecisionInBothChartAndLegend()
    {
        Assert.Equal("%0,04", DonutPercentageLayout.FormatPercent(.041m));
        Assert.Equal("<%0,01", DonutPercentageLayout.FormatPercent(.001m));
    }
}
