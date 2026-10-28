using DogruFatura.Core.Services;

namespace DogruFatura.Tests;

public sealed class InstallationProfileTests
{
    [Fact]
    public void DistributionAndPersonalUseDifferentRoots()
    {
        var type = typeof(SchoolDataStore).Assembly.GetType("DogruFatura.Core.Services.RuntimeInstallationProfile");
        Assert.NotNull(type);
        var method = type.GetMethod("GetRoot")!;
        var desktop = Path.Combine(Path.GetTempPath(), "profile-desktop");
        var personal = (string)method.Invoke(null, new object[] { desktop, false })!;
        var distribution = (string)method.Invoke(null, new object[] { desktop, true })!;
        Assert.Equal(Path.Combine(desktop, "DoğruFatura"), personal);
        Assert.NotEqual(personal, distribution);
    }

    [Theory]
    [InlineData(0, false, true)]
    [InlineData(1, false, true)]
    [InlineData(0, true, true)]
    [InlineData(1, true, false)]
    public void SetupNoticeIsInformativeAndOnlyForMissingDefinitions(int count, bool complete, bool expected)
    {
        var type = typeof(SchoolDataStore).Assembly.GetType("DogruFatura.Core.Services.InitialSetupNotice");
        Assert.NotNull(type);
        var notice = type.GetMethod("GetMessage")!.Invoke(null, new object[] { count, complete });
        Assert.Equal(expected, notice is not null);
    }
}
