using System.Threading;

namespace DogruFatura.Wpf;

public enum SingleInstanceAcquireStatus
{
    Acquired,
    AlreadyRunning,
    Unavailable
}

public sealed record SingleInstanceAcquireResult(
    SingleInstanceAcquireStatus Status,
    SingleInstanceLease? Lease,
    string Message)
{
    public bool IsAcquired => Status == SingleInstanceAcquireStatus.Acquired && Lease is not null;
}

public sealed record SingleInstanceMutexCreation(Mutex Mutex, bool CreatedNew);

public sealed class SingleInstanceLease : IDisposable
{
    private Mutex? _mutex;
    private bool _ownsMutex;

    internal SingleInstanceLease(Mutex mutex, bool ownsMutex)
    {
        _mutex = mutex;
        _ownsMutex = ownsMutex;
    }

    public void Dispose()
    {
        var mutex = Interlocked.Exchange(ref _mutex, null);
        if (mutex is null) return;

        try
        {
            if (_ownsMutex)
                mutex.ReleaseMutex();
        }
        catch (ApplicationException)
        {
            // Sahiplik beklenmedik biçimde kaybolduysa kapanış yine güvenli biçimde devam eder.
        }
        finally
        {
            _ownsMutex = false;
            mutex.Dispose();
        }
    }
}

public static class SingleInstanceGuard
{
    public static SingleInstanceAcquireResult TryAcquire(
        string mutexName,
        Func<string, SingleInstanceMutexCreation>? factory = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(mutexName);
        factory ??= CreateMutex;

        Mutex? mutex = null;
        try
        {
            var creation = factory(mutexName);
            mutex = creation.Mutex ?? throw new InvalidOperationException("Tek örnek kilidi oluşturulamadı.");

            if (!creation.CreatedNew)
            {
                mutex.Dispose();
                return new SingleInstanceAcquireResult(
                    SingleInstanceAcquireStatus.AlreadyRunning,
                    null,
                    "DoğruFatura zaten açık.");
            }

            return new SingleInstanceAcquireResult(
                SingleInstanceAcquireStatus.Acquired,
                new SingleInstanceLease(mutex, ownsMutex: true),
                "Tek örnek kilidi edinildi.");
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException
                                   or WaitHandleCannotBeOpenedException
                                   or IOException
                                   or InvalidOperationException)
        {
            mutex?.Dispose();
            return new SingleInstanceAcquireResult(
                SingleInstanceAcquireStatus.Unavailable,
                null,
                "DoğruFatura tek örnek güvenlik kilidini edinemedi: " + ex.Message);
        }
    }

    private static SingleInstanceMutexCreation CreateMutex(string mutexName)
    {
        var mutex = new Mutex(initiallyOwned: true, mutexName, out var createdNew);
        return new SingleInstanceMutexCreation(mutex, createdNew);
    }
}
