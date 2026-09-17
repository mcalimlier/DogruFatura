using DogruFatura.Core.Services;

namespace DogruFatura.Tests;

public sealed class TurkishTextComparisonTests
{
    [Theory]
    [InlineData("İSTANBUL", "istanbul")]
    [InlineData("ISPARTA", "ısparta")]
    [InlineData("ÇAĞDAŞ", "çağdaş")]
    public void EqualsIgnoreCase_UsesTurkishCasePairs(string left, string right)
    {
        Assert.True(TurkishTextComparison.EqualsIgnoreCase(left, right));
    }

    [Theory]
    [InlineData("ISPARTA", "isparta")]
    [InlineData("İSTANBUL", "ıstanbul")]
    public void EqualsIgnoreCase_DoesNotMergeDifferentTurkishICharacters(string left, string right)
    {
        Assert.False(TurkishTextComparison.EqualsIgnoreCase(left, right));
    }

    [Fact]
    public void EqualsIgnoreCase_HandlesNullDeterministically()
    {
        Assert.True(TurkishTextComparison.EqualsIgnoreCase(null, null));
        Assert.False(TurkishTextComparison.EqualsIgnoreCase("Kurum", null));
        Assert.False(TurkishTextComparison.EqualsIgnoreCase(null, "Kurum"));
    }
}
