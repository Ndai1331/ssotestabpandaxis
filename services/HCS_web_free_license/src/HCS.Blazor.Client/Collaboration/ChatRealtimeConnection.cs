using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HCS.CollaborationService.Contracts;
using Microsoft.AspNetCore.SignalR.Client;

namespace HCS.Blazor.Client.Collaboration;

/// <summary>
/// Owns the one browser SignalR connection for chat messaging and presence.
/// Started from the main layout (via <c>NotificationToast</c>) so a user is online on any HCS page,
/// not only while the Chat workspace is open. Negotiate goes through
/// <see cref="Authentication.BffHttpMessageHandler"/> for BFF antiforgery + credentials.
/// </summary>
public sealed class ChatRealtimeConnection(Uri gatewayBaseAddress) : IAsyncDisposable
{
    private readonly SemaphoreSlim startLock = new(1, 1);
    private readonly CancellationTokenSource lifetime = new();
    private HubConnection? connection;
    private int disposed;
    private int retryAttempt;
    private int retryScheduled;

    public event Func<ChatMessageDto, Task>? MessageReceived;
    public event Func<Guid, Guid, Task>? MessageDeleted;
    public event Func<NotificationDto, Task>? NotificationReceived;
    public event Func<PresenceChangedDto, Task>? PresenceChanged;
    public event Func<IReadOnlyList<Guid>, Task>? PresenceSnapshot;
    public event Func<HubConnectionState, Task>? StatusChanged;

    public Guid? ActiveConversationId { get; set; }

    public bool IsConnected => connection?.State == HubConnectionState.Connected;

    public async Task EnsureStartedAsync(CancellationToken cancellationToken = default)
    {
        await startLock.WaitAsync(cancellationToken);
        try
        {
            if (connection is null)
            {
                connection = new HubConnectionBuilder()
                    .WithUrl(new Uri(gatewayBaseAddress, "hubs/chat"), options =>
                    {
                        options.HttpMessageHandlerFactory = innerHandler => new Authentication.BffHttpMessageHandler(gatewayBaseAddress)
                        {
                            InnerHandler = innerHandler
                        };
                    })
                    .WithAutomaticReconnect(new[]
                    {
                        TimeSpan.Zero,
                        TimeSpan.FromSeconds(2),
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(10),
                        TimeSpan.FromSeconds(30)
                    })
                    .Build();

                connection.On<ChatMessageDto>("ReceiveMessage", NotifyMessageAsync);
                connection.On<ChatDeletedPayload>("MessageDeleted", NotifyDeletedAsync);
                // The web UI renders a recalled message exactly like a deleted one.
                connection.On<ChatDeletedPayload>("MessageRecalled", NotifyDeletedAsync);
                connection.On<NotificationDto>("NotificationReceived", NotifyNotificationAsync);
                connection.On<PresenceChangedDto>("PresenceChanged", NotifyPresenceChangedAsync);
                connection.Reconnecting += _ => NotifyStatusAsync(HubConnectionState.Reconnecting);
                connection.Reconnected += async _ =>
                {
                    retryAttempt = 0;
                    await NotifyStatusAsync(HubConnectionState.Connected);
                    await RefreshPresenceSnapshotAsync(CancellationToken.None);
                };
                connection.Closed += async _ =>
                {
                    await NotifyStatusAsync(HubConnectionState.Disconnected);
                    ScheduleRetry();
                };
            }

            if (Volatile.Read(ref disposed) != 0)
                return;

            if (connection.State == HubConnectionState.Disconnected)
            {
                try
                {
                    await connection.StartAsync(cancellationToken);
                    retryAttempt = 0;
                    await NotifyStatusAsync(HubConnectionState.Connected);
                }
                catch
                {
                    await NotifyStatusAsync(HubConnectionState.Disconnected);
                    ScheduleRetry();
                }
            }

            // Always re-fetch when connected so late subscribers (ChatWorkspace after layout
            // already started the hub) receive users who were already online — PresenceChanged
            // only fires on offline→online transitions.
            if (connection.State == HubConnectionState.Connected)
            {
                await RefreshPresenceSnapshotAsync(cancellationToken);
            }
        }
        finally
        {
            startLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0)
            return;

        lifetime.Cancel();
        if (connection is not null)
        {
            await connection.DisposeAsync();
        }

        lifetime.Dispose();
        startLock.Dispose();
    }

    private void ScheduleRetry()
    {
        if (Volatile.Read(ref disposed) != 0)
            return;
        if (Interlocked.CompareExchange(ref retryScheduled, 1, 0) != 0)
            return;

        _ = RetryLaterAsync(lifetime.Token);
    }

    private async Task RetryLaterAsync(CancellationToken cancellationToken)
    {
        try
        {
            var seconds = Math.Min(30, 2 << Math.Min(retryAttempt, 4));
            retryAttempt = Math.Min(retryAttempt + 1, 5);
            await Task.Delay(TimeSpan.FromSeconds(seconds), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Interlocked.Exchange(ref retryScheduled, 0);
            return;
        }

        Interlocked.Exchange(ref retryScheduled, 0);
        if (cancellationToken.IsCancellationRequested || Volatile.Read(ref disposed) != 0)
            return;

        try
        {
            await EnsureStartedAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task RefreshPresenceSnapshotAsync(CancellationToken cancellationToken)
    {
        if (connection is null || connection.State != HubConnectionState.Connected)
        {
            return;
        }

        try
        {
            var ids = await connection.InvokeAsync<Guid[]>("GetOnlineUserIds", cancellationToken);
            await NotifyPresenceSnapshotAsync(ids ?? []);
        }
        catch
        {
            // Presence is best-effort; chat messaging still works without a snapshot.
        }
    }

    private async Task NotifyMessageAsync(ChatMessageDto message)
    {
        var received = MessageReceived;
        if (received is not null)
        {
            foreach (var handler in received.GetInvocationList().Cast<Func<ChatMessageDto, Task>>())
            {
                await InvokeHandlerAsync(() => handler(message));
            }
        }
    }

    private async Task NotifyDeletedAsync(ChatDeletedPayload payload)
    {
        var deleted = MessageDeleted;
        if (deleted is not null)
        {
            foreach (var handler in deleted.GetInvocationList().Cast<Func<Guid, Guid, Task>>())
            {
                await InvokeHandlerAsync(() => handler(payload.ConversationId, payload.MessageId));
            }
        }
    }

    private async Task NotifyNotificationAsync(NotificationDto notification)
    {
        var handlers = NotificationReceived;
        if (handlers is not null)
        {
            foreach (var handler in handlers.GetInvocationList().Cast<Func<NotificationDto, Task>>())
            {
                await InvokeHandlerAsync(() => handler(notification));
            }
        }
    }

    private async Task NotifyPresenceChangedAsync(PresenceChangedDto change)
    {
        var handlers = PresenceChanged;
        if (handlers is null)
        {
            return;
        }

        foreach (var handler in handlers.GetInvocationList().Cast<Func<PresenceChangedDto, Task>>())
        {
            await InvokeHandlerAsync(() => handler(change));
        }
    }

    private async Task NotifyPresenceSnapshotAsync(IReadOnlyList<Guid> userIds)
    {
        var handlers = PresenceSnapshot;
        if (handlers is null)
        {
            return;
        }

        foreach (var handler in handlers.GetInvocationList().Cast<Func<IReadOnlyList<Guid>, Task>>())
        {
            await InvokeHandlerAsync(() => handler(userIds));
        }
    }

    private async Task NotifyStatusAsync(HubConnectionState state)
    {
        var handlers = StatusChanged;
        if (handlers is null)
        {
            return;
        }

        foreach (var handler in handlers.GetInvocationList().Cast<Func<HubConnectionState, Task>>())
        {
            await InvokeHandlerAsync(() => handler(state));
        }
    }

    private static async Task InvokeHandlerAsync(Func<Task> handler)
    {
        try
        {
            await handler();
        }
        catch
        {
            // A disposed page must not turn a dropped socket into the app-wide error bar.
        }
    }

    private sealed record ChatDeletedPayload(Guid ConversationId, Guid MessageId);
}
