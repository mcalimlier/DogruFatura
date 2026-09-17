using DogruFatura.Wpf;
using System.Threading;

namespace DogruFatura.Tests;

public sealed class SingleInstanceGuardTests
{
    [Fact]
    public void TryAcquire_WhenMutexIsNew_ReturnsAcquiredLease()
    {
        var result = SingleInstanceGuard.TryAcquire(
            "DogruFatura.Tests.Acquired",
            _ => new SingleInstanceMutexCreation(new Mutex(initiallyOwned: true), CreatedNew: true));

        Assert.Equal(SingleInstanceAcquireStatus.Acquired, result.Status);
        Assert.True(result.IsAcquired);
        Assert.NotNull(result.Lease);

        result.Lease!.Dispose();
        result.Lease.Dispose();
    }

    [Fact]
    public void TryAcquire_WhenAnotherInstanceOwnsMutex_ReturnsAlreadyRunning()
    {
        var result = SingleInstanceGuard.TryAcquire(
            "DogruFatura.Tests.AlreadyRunning",
            _ => new SingleInstanceMutexCreation(new Mutex(initiallyOwned: false), CreatedNew: false));

        Assert.Equal(SingleInstanceAcquireStatus.AlreadyRunning, result.Status);
        Assert.False(result.IsAcquired);
        Assert.Null(result.Lease);
    }

    [Fact]
    public void TryAcquire_WhenMutexInfrastructureFails_FailsClosed()
    {
        var result = SingleInstanceGuard.TryAcquire(
            "DogruFatura.Tests.Unavailable",
            _ => throw new UnauthorizedAccessException("test erişim hatası"));

        Assert.Equal(SingleInstanceAcquireStatus.Unavailable, result.Status);
        Assert.False(result.IsAcquired);
        Assert.Null(result.Lease);
        Assert.Contains("güvenlik kilidini", result.Message, StringComparison.CurrentCultureIgnoreCase);
    }
}
