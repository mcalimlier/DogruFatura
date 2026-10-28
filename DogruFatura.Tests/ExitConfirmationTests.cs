using DogruFatura.Wpf;

namespace DogruFatura.Tests;

public sealed class ExitConfirmationTests
{
    [Theory]
    [InlineData(false, 0, false)]
    [InlineData(false, 2, true)]
    [InlineData(true, 0, true)]
    [InlineData(true, 2, true)]
    public void RequiresConfirmationForActiveListEvenWhenFormsAreSaved(bool unsaved, int count, bool expected)
    {
        var type = typeof(MainWindow).Assembly.GetType("DogruFatura.Wpf.ExitConfirmationPolicy");
        Assert.NotNull(type);
        var prompt = type.GetMethod("Create")!.Invoke(null, new object[] { unsaved, count });
        Assert.Equal(expected, prompt is not null);
        if (count > 0)
        {
            var message = (string)prompt!.GetType().GetProperty("Message")!.GetValue(prompt)!;
            Assert.Contains("2 fatura", message, StringComparison.Ordinal);
            Assert.Contains("yeniden", message, StringComparison.Ordinal);
        }
        if (unsaved)
            Assert.Contains("kaydedilmemiş", (string)prompt!.GetType().GetProperty("Message")!.GetValue(prompt)!, StringComparison.Ordinal);
    }
}
