using DogruFatura.Core.Services;

namespace DogruFatura.Tests;

public sealed class TurkishTextComparisonTests
{
    [Theory]
    [InlineData("İSTANBUL", "istanbul")]
    [InlineData("ISPARTA", "ısparta")]
    [InlineData("ÇAĞDAŞ", "çağdaş")]
    public void EqualsIgnoreCaseUsesTurkishCasePairs(string left, string right)
    {
        Assert.True(TurkishTextComparison.EqualsIgnoreCase(left, right));
    }

    [Theory]
    [InlineData("ISPARTA", "isparta")]
    [InlineData("İSTANBUL", "ıstanbul")]
    public void EqualsIgnoreCaseDoesNotMergeDifferentTurkishICharacters(string left, string right)
    {
        Assert.False(TurkishTextComparison.EqualsIgnoreCase(left, right));
    }

    [Fact]
    public void EqualsIgnoreCaseHandlesNullDeterministically()
    {
        Assert.True(TurkishTextComparison.EqualsIgnoreCase(null, null));
        Assert.False(TurkishTextComparison.EqualsIgnoreCase("Kurum", null));
        Assert.False(TurkishTextComparison.EqualsIgnoreCase(null, "Kurum"));
    }
}
