using System.Security.Claims;
using HCS.CollaborationService.Application;
using HCS.CollaborationService.Contracts;
using HCS.CollaborationService.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;

namespace HCS.CollaborationService.Hubs;

[Authorize(Policy = CollaborationPermissions.Realtime)]
public sealed class ChatHub(CollaborationDbContext db, IChatPresenceTracker presence) : Hub
{
    internal const string PresenceGroupName = "presence";

    public override async Task OnConnectedAsync()
    {
        if (TryUserId(out var userId))
        {
            if (HasChatPermission())
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, ChatUserGroup(userId), Context.ConnectionAborted);
                await Groups.AddToGroupAsync(Context.ConnectionId, PresenceGroupName, Context.ConnectionAborted);
                if (presence.TryMarkOnline(Context.ConnectionId, userId))
                {
                    await Clients.OthersInGroup(PresenceGroupName).SendAsync(
                        "PresenceChanged",
                        new PresenceChangedDto(userId, true),
                        Context.ConnectionAborted);
                }
            }

            if (HasNotificationPermission())
                await Groups.AddToGroupAsync(Context.ConnectionId, NotificationGroup(userId), Context.ConnectionAborted);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (presence.TryMarkOffline(Context.ConnectionId, out var userId))
        {
            await Clients.OthersInGroup(PresenceGroupName).SendAsync(
                "PresenceChanged",
                new PresenceChangedDto(userId, false),
                Context.ConnectionAborted);
        }

        await base.OnDisconnectedAsync(exception);
    }

    [Authorize(Policy = CollaborationPermissions.Chat)]
    public async Task Subscribe(Guid conversationId)
    {
        if (!TryUserId(out var userId) || !await db.ConversationMembers.AnyAsync(x => x.ConversationId == conversationId && x.UserId == userId, Context.ConnectionAborted))
            throw new HubException("Conversation membership required.");
        await Groups.AddToGroupAsync(Context.ConnectionId, ChatUserGroup(userId), Context.ConnectionAborted);
    }

    [Authorize(Policy = CollaborationPermissions.Chat)]
    public Task<Guid[]> GetOnlineUserIds() =>
        Task.FromResult(presence.GetOnlineUserIds().ToArray());

    private bool TryUserId(out Guid userId) => Guid.TryParse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Context.User?.FindFirstValue("sub"), out userId);
    private bool HasChatPermission() => Context.User?.HasClaim("permission", CollaborationPermissions.Chat) == true;
    private bool HasNotificationPermission() => Context.User?.HasClaim("permission", CollaborationPermissions.Notifications) == true
        || Context.User?.HasClaim("permission", CollaborationPermissions.Social) == true;
    internal static string Group(Guid conversationId) => $"conversation:{conversationId:N}";
    internal static string ChatUserGroup(Guid userId) => $"chat-user:{userId:N}";
    internal static string NotificationGroup(Guid userId) => $"notification-user:{userId:N}";
}

public sealed class SignalRChatRealtimeNotifier(IHubContext<ChatHub> hub) : IChatRealtimeNotifier, ITransientDependency
{
    public Task MessageSentAsync(ChatMessageDto message, IEnumerable<Guid> recipientUserIds, CancellationToken ct = default) =>
        hub.Clients.Groups(recipientUserIds.Distinct().Select(ChatHub.ChatUserGroup)).SendAsync("ReceiveMessage", message, ct);
    public Task MessageDeletedAsync(Guid conversationId, Guid messageId, IEnumerable<Guid> recipientUserIds, CancellationToken ct = default) =>
        hub.Clients.Groups(recipientUserIds.Distinct().Select(ChatHub.ChatUserGroup)).SendAsync("MessageDeleted", new { conversationId, messageId }, ct);

    public Task NotificationSentAsync(NotificationDto notification, CancellationToken ct = default) =>
        hub.Clients.Group(ChatHub.NotificationGroup(notification.UserId)).SendAsync("NotificationReceived", notification, ct);
}
