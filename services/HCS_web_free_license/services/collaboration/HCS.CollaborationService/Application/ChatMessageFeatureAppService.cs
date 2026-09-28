using System.Text.RegularExpressions;
using HCS.CollaborationService.Contracts;
using HCS.CollaborationService.Data;
using HCS.CollaborationService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization;
using Volo.Abp.Guids;
using Volo.Abp.Timing;
using Volo.Abp.Users;

namespace HCS.CollaborationService.Application;

/// <summary>Cross-conversation search, shared media, reactions and saved messages for chat clients.</summary>
public partial class ChatMessageFeatureAppService(
    CollaborationDbContext db,
    ICurrentUser currentUser,
    IGuidGenerator guidGenerator,
    IClock clock,
    IChatRealtimeNotifier notifier,
    ChatMessageMapper mapper,
    ILogger<ChatMessageFeatureAppService> logger) : ApplicationService
{
    public const int MinSearchLength = 2;
    public const int MaxSearchTake = 50;

    private Guid UserId => currentUser.Id ?? throw new AbpAuthorizationException("Authenticated user required.");

    public async Task<PagedMessagesDto> SearchAsync(string? text, Guid? conversationId, int skip = 0, int take = 20,
        CancellationToken ct = default)
    {
        var keyword = text?.Trim() ?? string.Empty;
        if (keyword.Length < MinSearchLength) throw new BusinessException("Collaboration:SearchTextTooShort");
        take = Math.Clamp(take, 1, MaxSearchTake);
        var me = UserId;
        var memberships = VisibleMemberships(me);
        if (conversationId.HasValue) memberships = memberships.Where(x => x.ConversationId == conversationId);
        var pattern = $"%{EscapeLike(keyword)}%";
        var query = db.Messages.AsNoTracking()
            .Where(x => !x.IsDeleted && x.RecalledAt == null && EF.Functions.ILike(x.Text, pattern, "\\") &&
                memberships.Any(m => m.ConversationId == x.ConversationId &&
                    (m.HistoryClearedAt == null || x.CreationTime > m.HistoryClearedAt)));
        var count = await query.LongCountAsync(ct);
        var items = await query.Include(x => x.Attachments)
            .OrderByDescending(x => x.CreationTime).ThenByDescending(x => x.Id)
            .Skip(Math.Max(skip, 0)).Take(take).ToListAsync(ct);
        return new PagedMessagesDto(count, await mapper.MapAsync(items, me, ct));
    }

    public async Task<PagedConversationAttachmentsDto> GetAttachmentsAsync(Guid conversationId, string? kind, int skip = 0,
        int take = 30, CancellationToken ct = default)
    {
        var me = UserId;
        var member = await VisibleMemberships(me).AsNoTracking().SingleOrDefaultAsync(x => x.ConversationId == conversationId, ct)
            ?? throw new AbpAuthorizationException("Conversation membership required.");
        skip = Math.Max(skip, 0);
        take = Math.Clamp(take, 1, 100);
        var clearedAt = member.HistoryClearedAt;
        var messages = db.Messages.AsNoTracking()
            .Where(x => x.ConversationId == conversationId && !x.IsDeleted && x.RecalledAt == null &&
                (clearedAt == null || x.CreationTime > clearedAt));

        var normalizedKind = (kind ?? ConversationAttachmentKinds.Media).Trim().ToLowerInvariant();
        if (normalizedKind == ConversationAttachmentKinds.Link)
        {
            var linkMessages = messages.Where(x => EF.Functions.ILike(x.Text, "%http%"));
            var linkCount = await linkMessages.LongCountAsync(ct);
            var rows = await linkMessages.OrderByDescending(x => x.CreationTime).ThenByDescending(x => x.Id)
                .Skip(skip).Take(take)
                .Select(x => new { x.Id, x.SenderUserId, x.CreationTime, x.Text })
                .ToListAsync(ct);
            var links = rows.SelectMany(row => ExtractLinks(row.Text)
                .Select(url => new ConversationAttachmentItemDto(row.Id, row.SenderUserId, row.CreationTime, url))).ToArray();
            return new PagedConversationAttachmentsDto(linkCount, links);
        }

        AttachmentKind[] kinds = normalizedKind switch
        {
            ConversationAttachmentKinds.Media => [AttachmentKind.Image, AttachmentKind.Video],
            ConversationAttachmentKinds.File => [AttachmentKind.File, AttachmentKind.Audio],
            _ => throw new BusinessException("Collaboration:InvalidAttachmentKind").WithData("kind", kind ?? string.Empty)
        };
        var attachments = db.Attachments.AsNoTracking()
            .Where(x => x.ConversationId == conversationId && x.MessageId != null && kinds.Contains(x.Kind) &&
                messages.Any(m => m.Id == x.MessageId));
        var count = await attachments.LongCountAsync(ct);
        var items = await attachments.OrderByDescending(x => x.CreationTime).ThenByDescending(x => x.Id)
            .Skip(skip).Take(take).ToListAsync(ct);
        return new PagedConversationAttachmentsDto(count, items.Select(x => new ConversationAttachmentItemDto(
            x.MessageId!.Value, x.UploadedByUserId, x.CreationTime, $"/api/chat/attachments/{x.Id:D}",
            x.Id, x.FileName, x.ContentType, x.Size, x.Kind)).ToArray());
    }

    public async Task<IReadOnlyList<MessageReactionDto>> SetReactionAsync(Guid messageId, string emoji, CancellationToken ct = default)
    {
        var me = UserId;
        var (message, memberIds) = await RequireVisibleMessageAsync(messageId, me, ct);
        var reaction = await db.MessageReactions.SingleOrDefaultAsync(x => x.MessageId == messageId && x.UserId == me, ct);
        if (reaction is null)
        {
            db.MessageReactions.Add(new ChatMessageReaction(guidGenerator.Create(), messageId, me, emoji, clock.Now.ToUniversalTime()));
            try { await db.SaveChangesAsync(ct); }
            catch (DbUpdateException exception) when (PostgresErrors.IsUniqueViolation(exception))
            {
                db.ChangeTracker.Clear();
                reaction = await db.MessageReactions.SingleAsync(x => x.MessageId == messageId && x.UserId == me, ct);
                reaction.SetEmoji(emoji);
                await db.SaveChangesAsync(ct);
            }
        }
        else
        {
            reaction.SetEmoji(emoji);
            await db.SaveChangesAsync(ct);
        }
        return await PublishReactionsAsync(message, memberIds, me, ct);
    }

    public async Task<IReadOnlyList<MessageReactionDto>> RemoveReactionAsync(Guid messageId, CancellationToken ct = default)
    {
        var me = UserId;
        var (message, memberIds) = await RequireVisibleMessageAsync(messageId, me, ct);
        var removed = await db.MessageReactions.Where(x => x.MessageId == messageId && x.UserId == me).ExecuteDeleteAsync(ct);
        if (removed == 0) return await mapper.GetReactionsAsync(messageId, me, ct);
        return await PublishReactionsAsync(message, memberIds, me, ct);
    }

    public async Task SetSavedAsync(Guid messageId, bool saved, CancellationToken ct = default)
    {
        var me = UserId;
        if (!saved)
        {
            await db.SavedMessages.Where(x => x.UserId == me && x.MessageId == messageId).ExecuteDeleteAsync(ct);
            return;
        }
        await RequireVisibleMessageAsync(messageId, me, ct);
        if (await db.SavedMessages.AnyAsync(x => x.UserId == me && x.MessageId == messageId, ct)) return;
        db.SavedMessages.Add(new ChatSavedMessage(guidGenerator.Create(), me, messageId, clock.Now.ToUniversalTime()));
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException exception) when (PostgresErrors.IsUniqueViolation(exception)) { db.ChangeTracker.Clear(); }
    }

    public async Task<PagedMessagesDto> GetSavedAsync(int skip = 0, int take = 20, CancellationToken ct = default)
    {
        var me = UserId;
        take = Math.Clamp(take, 1, MaxSearchTake);
        var memberships = VisibleMemberships(me);
        var saved = db.SavedMessages.AsNoTracking()
            .Where(s => s.UserId == me && db.Messages.Any(x => x.Id == s.MessageId &&
                memberships.Any(m => m.ConversationId == x.ConversationId &&
                    (m.HistoryClearedAt == null || x.CreationTime > m.HistoryClearedAt))));
        var count = await saved.LongCountAsync(ct);
        var messageIds = await saved.OrderByDescending(x => x.CreationTime).ThenByDescending(x => x.Id)
            .Skip(Math.Max(skip, 0)).Take(take).Select(x => x.MessageId).ToListAsync(ct);
        var messages = await db.Messages.AsNoTracking().Include(x => x.Attachments)
            .Where(x => messageIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var ordered = messageIds.Where(messages.ContainsKey).Select(id => messages[id]).ToArray();
        return new PagedMessagesDto(count, await mapper.MapAsync(ordered, me, ct));
    }

    internal static IReadOnlyList<string> ExtractLinks(string? text) =>
        string.IsNullOrEmpty(text)
            ? []
            : LinkPattern().Matches(text).Select(match => match.Value.TrimEnd('.', ',', ';', ':', '!', '?', ')', ']'))
                .Where(url => url.Length > "https://".Length).Distinct(StringComparer.Ordinal).ToArray();

    internal static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    [GeneratedRegex(@"https?://[^\s<>""']+", RegexOptions.IgnoreCase)]
    private static partial Regex LinkPattern();

    private IQueryable<ConversationMember> VisibleMemberships(Guid userId) =>
        db.ConversationMembers.Where(m => m.UserId == userId &&
            db.Conversations.Any(c => c.Id == m.ConversationId && !c.IsDeleted));

    private async Task<(ChatMessage Message, Guid[] MemberIds)> RequireVisibleMessageAsync(Guid messageId, Guid userId,
        CancellationToken ct)
    {
        var message = await db.Messages.AsNoTracking().SingleOrDefaultAsync(x => x.Id == messageId, ct)
            ?? throw new BusinessException("Collaboration:MessageNotFound");
        var member = await VisibleMemberships(userId).AsNoTracking()
            .SingleOrDefaultAsync(x => x.ConversationId == message.ConversationId, ct)
            ?? throw new AbpAuthorizationException("Conversation membership required.");
        if (message.IsDeleted || message.IsRecalled || !member.CanSee(message.CreationTime))
            throw new BusinessException("Collaboration:MessageNotFound");
        var memberIds = await db.ConversationMembers.AsNoTracking()
            .Where(x => x.ConversationId == message.ConversationId).Select(x => x.UserId).ToArrayAsync(ct);
        return (message, memberIds);
    }

    private async Task<IReadOnlyList<MessageReactionDto>> PublishReactionsAsync(ChatMessage message, Guid[] memberIds, Guid viewerId,
        CancellationToken ct)
    {
        var reactions = await mapper.GetReactionsAsync(message.Id, viewerId, ct);
        try { await notifier.MessageReactionsChangedAsync(message.ConversationId, message.Id, reactions, memberIds, ct); }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Realtime reaction update failed after durable commit for {MessageId}", message.Id);
        }
        return reactions;
    }
}
