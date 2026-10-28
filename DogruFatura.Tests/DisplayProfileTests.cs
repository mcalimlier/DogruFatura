using DogruFatura.Core.Services;

namespace DogruFatura.Tests;

public sealed class DisplayProfileTests
{
    [Theory]
    [InlineData("1024x768", 1024, 768)]
    [InlineData("1366x768", 1366, 768)]
    [InlineData("1920x1080", 1920, 1080)]
    public void ProfileUsesRequestedPhysicalResolution(string key, double width, double height)
    {
        var size = DisplayProfilePolicy.Fit(key, 1, 3840, 2160);
        Assert.Equal(width, size.Width);
        Assert.Equal(height, size.Height);
    }

    [Fact]
    public void HighDpiProfileStaysInsideUsableDesktop()
    {
        var size = DisplayProfilePolicy.Fit("1920x1080", 1.5, 1000, 680);
        Assert.Equal(1000, size.Width);
        Assert.Equal(680, size.Height);
    }

    [Fact]
    public void UnknownSavedProfileFallsBackTo1366()
    {
        Assert.Equal("1366x768", DisplayProfilePolicy.Resolve("old-profile").Key);
    }
}
