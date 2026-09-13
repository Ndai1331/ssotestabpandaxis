using System;
using System.Threading;

namespace HCS.Blazor.Client.Services;

public sealed class HcsBusyTracker
{
    private int count;

    public event Action? Changed;

    public bool IsBusy => Volatile.Read(ref count) > 0;

    public void Begin()
    {
        if (Interlocked.Increment(ref count) == 1)
        {
            Changed?.Invoke();
        }
    }

    public void End()
    {
        var remaining = Interlocked.Decrement(ref count);
        if (remaining < 0)
        {
            Interlocked.Exchange(ref count, 0);
            remaining = 0;
        }

        if (remaining == 0)
        {
            Changed?.Invoke();
        }
    }
}
