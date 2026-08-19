using System.Security.Claims;
using HCS.CollaborationService.Application;
using HCS.CollaborationService.Contracts;
using HCS.CollaborationService.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;

namespace HCS.CollaborationService.Hubs;

[Authorize(Policy = CollaborationPermissions.Chat)]
public sealed class ChatHub(CollaborationDbContext db) : Hub
{
    public override async Task OnConnectedAsync()
    {
        if (TryUserId(out var userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId), Context.ConnectionAborted);
        }
        await base.OnConnectedAsync();
    }

    public async Task Subscribe(Guid conversationId)
    {
        if (!TryUserId(out var userId) || !await db.ConversationMembers.AnyAsync(x => x.ConversationId == conversationId && x.UserId == userId, Context.ConnectionAborted))
            throw new HubException("Conversation membership required.");
        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId), Context.ConnectionAborted);
    }

    private bool TryUserId(out Guid userId) => Guid.TryParse(Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Context.User?.FindFirstValue("sub"), out userId);
    internal static string Group(Guid conversationId) => $"conversation:{conversationId:N}";
    internal static string UserGroup(Guid userId) => $"user:{userId:N}";
}

public sealed class SignalRChatRealtimeNotifier(IHubContext<ChatHub> hub) : IChatRealtimeNotifier, ITransientDependency
{
    public Task MessageSentAsync(ChatMessageDto message, IEnumerable<Guid> recipientUserIds, CancellationToken ct = default) =>
        hub.Clients.Groups(recipientUserIds.Distinct().Select(ChatHub.UserGroup)).SendAsync("ReceiveMessage", message, ct);
    public Task MessageDeletedAsync(Guid conversationId, Guid messageId, IEnumerable<Guid> recipientUserIds, CancellationToken ct = default) =>
        hub.Clients.Groups(recipientUserIds.Distinct().Select(ChatHub.UserGroup)).SendAsync("MessageDeleted", new { conversationId, messageId }, ct);
}
