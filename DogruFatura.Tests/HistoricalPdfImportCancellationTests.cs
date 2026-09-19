using DogruFatura.Core.Models;
using DogruFatura.Wpf;

namespace DogruFatura.Tests;

public sealed class HistoricalPdfImportCancellationTests
{
    [Fact]
    public void ReadThrowsWhenCancellationWasRequestedBeforeParsing()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var service = new HistoricalPdfImportService();

        Assert.Throws<OperationCanceledException>(() =>
            service.Read(
                "HYS-2026123456.pdf",
                Array.Empty<SchoolRecord>(),
                AuthorityProfileDefaults.Create(),
                cancellation.Token));
    }
}
