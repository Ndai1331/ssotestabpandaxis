using System;

namespace HCS.Blazor.Client.Collaboration;

public sealed class NotificationBadgeState
{
    public int UnreadNotifications { get; private set; }

    public int UnreadChat { get; private set; }

    public event EventHandler? Changed;

    public void SetUnreadNotifications(int count)
    {
        if (UnreadNotifications == count)
        {
            return;
        }

        UnreadNotifications = count;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SetUnreadChat(int count)
    {
        if (UnreadChat == count)
        {
            return;
        }

        UnreadChat = count;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
