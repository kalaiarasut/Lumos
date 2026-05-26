namespace Lumos.Services;

public sealed class SingleInstanceGuard : IDisposable
{
    private readonly Mutex _mutex;

    public SingleInstanceGuard(string mutexName = "Local\\LumosBrightnessMemoryApp")
    {
        _mutex = new Mutex(initiallyOwned: true, mutexName, out var createdNew);
        IsOwner = createdNew;
    }

    public bool IsOwner { get; }

    public void Dispose()
    {
        if (IsOwner)
        {
            _mutex.ReleaseMutex();
        }

        _mutex.Dispose();
    }
}
