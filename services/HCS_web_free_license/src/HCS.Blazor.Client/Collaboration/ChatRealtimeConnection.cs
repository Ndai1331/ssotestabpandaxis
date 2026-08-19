using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HCS.CollaborationService.Contracts;
using Microsoft.AspNetCore.SignalR.Client;

namespace HCS.Blazor.Client.Collaboration;

/// <summary>
/// Owns the one browser SignalR connection used by the Chat workspace. Its negotiate
/// request is routed through <see cref="Authentication.BffHttpMessageHandler"/>, so the BFF
/// antiforgery header and credential mode are applied before the WebSocket upgrade.
/// </summary>
public sealed class ChatRealtimeConnection(Uri gatewayBaseAddress) : IAsyncDisposable
{
    private readonly SemaphoreSlim startLock = new(1, 1);
    private HubConnection? connection;

    public event Func<Task>? Changed;
    public event Func<ChatMessageDto, Task>? MessageReceived;
    public event Func<Guid, Guid, Task>? MessageDeleted;
    public event Func<HubConnectionState, Task>? StatusChanged;

    public Guid? ActiveConversationId { get; set; }

    public bool IsConnected => connection?.State == HubConnectionState.Connected;

    public async Task EnsureStartedAsync(CancellationToken cancellationToken = default)
    {
        if (connection?.State is HubConnectionState.Connected or HubConnectionState.Connecting or HubConnectionState.Reconnecting)
        {
            return;
        }

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
                    .WithAutomaticReconnect()
                    .Build();

                connection.On<ChatMessageDto>("ReceiveMessage", NotifyMessageAsync);
                connection.On<ChatDeletedPayload>("MessageDeleted", NotifyDeletedAsync);
                connection.Reconnecting += _ => NotifyStatusAsync(HubConnectionState.Reconnecting);
                connection.Reconnected += _ => NotifyStatusAsync(HubConnectionState.Connected);
                connection.Closed += _ => NotifyStatusAsync(HubConnectionState.Disconnected);
            }

            if (connection.State == HubConnectionState.Disconnected)
            {
                try
                {
                    await connection.StartAsync(cancellationToken);
                    await NotifyStatusAsync(HubConnectionState.Connected);
                }
                catch
                {
                    await NotifyStatusAsync(HubConnectionState.Disconnected);
                    throw;
                }
            }
        }
        finally
        {
            startLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (connection is not null)
        {
            await connection.DisposeAsync();
        }

        startLock.Dispose();
    }

    private async Task NotifyMessageAsync(ChatMessageDto message)
    {
        var received = MessageReceived;
        if (received is not null)
        {
            foreach (var handler in received.GetInvocationList().Cast<Func<ChatMessageDto, Task>>())
            {
                await handler(message);
            }
        }

        await NotifyChangedAsync();
    }

    private async Task NotifyDeletedAsync(ChatDeletedPayload payload)
    {
        var deleted = MessageDeleted;
        if (deleted is not null)
        {
            foreach (var handler in deleted.GetInvocationList().Cast<Func<Guid, Guid, Task>>())
            {
                await handler(payload.ConversationId, payload.MessageId);
            }
        }

        await NotifyChangedAsync();
    }

    private async Task NotifyChangedAsync()
    {
        var handlers = Changed;
        if (handlers is null)
        {
            return;
        }

        foreach (var handler in handlers.GetInvocationList().Cast<Func<Task>>())
        {
            await handler();
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
            await handler(state);
        }
    }

    private sealed record ChatDeletedPayload(Guid ConversationId, Guid MessageId);
}
