namespace Mediator.Pipes.Utils;

internal static class ReaderWriterLockSlimExtensions
{
    public static IDisposable EnterReadScope(this ReaderWriterLockSlim @lock)
    {
        @lock.EnterReadLock();
        return new LambdaDisposable(@lock.ExitReadLock);
    }

    public static IDisposable EnterWriteScope(this ReaderWriterLockSlim @lock)
    {
        @lock.EnterWriteLock();
        return new LambdaDisposable(@lock.ExitWriteLock);
    }

    private class LambdaDisposable : IDisposable
    {
        private readonly Action _callback;

        public LambdaDisposable(Action callback)
        {
            _callback = callback;
        }

        public void Dispose() => _callback();
    }
}