using DogruFatura.Wpf;

namespace DogruFatura.Tests;

public sealed class StartupProgressProfileTests
{
    [Fact]
    public void MinimumSplashDurationIsBetweenFiveAndSixSeconds()
    {
        Assert.InRange(StartupProgressProfile.MinimumVisibleDurationMs, 5000, 6000);
    }

    [Fact]
    public void ContinuousProgressIsMonotonicAndStopsBelowCompletion()
    {
        var samples = new[] { 0L, 250, 1000, 2000, 3500, 5000, 6000, 8000, 10000, 15000 }
            .Select(StartupProgressProfile.CalculateContinuousPercent)
            .ToArray();

        for (var index = 1; index < samples.Length; index++)
            Assert.True(samples[index] >= samples[index - 1]);

        Assert.Equal(0, samples[0]);
        Assert.Equal(
            StartupProgressProfile.CruiseTargetPercent,
            StartupProgressProfile.CalculateContinuousPercent(StartupProgressProfile.CruiseDurationMs));
        Assert.True(samples.All(percent => percent <= StartupProgressProfile.TailTargetPercent));
        Assert.True(StartupProgressProfile.TailTargetPercent < 100);
    }

    [Fact]
    public void AnimationProfileUsesSmoothFrameCadence()
    {
        Assert.InRange(StartupProgressProfile.FrameDelayMs, 16, 40);
        Assert.True(StartupProgressProfile.CruiseDurationMs >= 4500);
        Assert.True(StartupProgressProfile.CompletionDurationMs >= 300);
        Assert.True(StartupProgressProfile.CruiseTargetPercent < StartupProgressProfile.TailTargetPercent);
        Assert.True(StartupProgressProfile.TailTargetPercent < 100);
    }
}
