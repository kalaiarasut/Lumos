namespace Lumos.Services;

public sealed class ShutdownSignal : IDisposable
{
    private const string EventName = "Local\\LumosBrightnessMemoryAppShutdown";
    private readonly EventWaitHandle _eventHandle;

    public ShutdownSignal()
    {
        _eventHandle = new EventWaitHandle(false, EventResetMode.AutoReset, EventName);
    }

    public bool IsRequested()
    {
        return _eventHandle.WaitOne(0);
    }

    public static bool RequestShutdown()
    {
        try
        {
            using var eventHandle = EventWaitHandle.OpenExisting(EventName);
            eventHandle.Set();
            return true;
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            return false;
        }
    }

    public void Dispose()
    {
        _eventHandle.Dispose();
    }
}
