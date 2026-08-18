using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace HCS.Blazor.Client.Collaboration;

/// <summary>
/// Owns the one browser SignalR connection used by the Chat workspace. Its negotiate
/// request is routed through <see cref="Authentication.BffHttpMessageHandler"/>, so the
/// BFF antiforgery header and credential mode are applied before the WebSocket upgrade.
/// </summary>
public sealed class ChatRealtimeConnection(Uri gatewayBaseAddress) : IAsyncDisposable
{
    private readonly SemaphoreSlim startLock = new(1, 1);
    private HubConnection? connection;

    public event Func<Task>? Changed;
    public event Func<HubConnectionState, Task>? StatusChanged;

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

                connection.On<object>("ReceiveMessage", NotifyChangedAsync);
                connection.On<object>("MessageDeleted", NotifyChangedAsync);
                connection.Reconnecting += _ => NotifyStatusAsync(HubConnectionState.Reconnecting);
                connection.Reconnected += _ => NotifyStatusAsync(HubConnectionState.Connected);
                connection.Closed += _ => NotifyStatusAsync(HubConnectionState.Disconnected);
            }

            if (connection.State == HubConnectionState.Disconnected)
            {
                await connection.StartAsync(cancellationToken);
                await NotifyStatusAsync(HubConnectionState.Connected);
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

    private async Task NotifyChangedAsync(object _)
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
}
