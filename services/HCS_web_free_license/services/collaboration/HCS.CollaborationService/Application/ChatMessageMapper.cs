using HCS.CollaborationService.Contracts;
using HCS.CollaborationService.Data;
using HCS.CollaborationService.Domain;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;

namespace HCS.CollaborationService.Application;

public sealed class ChatMessageMapper(CollaborationDbContext db) : ITransientDependency
{
    public async Task<IReadOnlyList<ChatMessageDto>> MapAsync(IReadOnlyList<ChatMessage> messages, Guid viewerId,
        CancellationToken ct)
    {
        if (messages.Count == 0) return [];
        var relatedIds = messages
            .SelectMany(message => new Guid?[] { message.ReplyToMessageId, message.ForwardedFromMessageId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();
        var related = relatedIds.Length == 0
            ? new Dictionary<Guid, ChatMessage>()
            : await db.Messages.AsNoTracking()
                .Where(x => relatedIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, ct);
        var ids = messages.Select(x => x.Id).Distinct().ToArray();
        var reactions = (await db.MessageReactions.AsNoTracking()
                .Where(x => ids.Contains(x.MessageId))
                .Select(x => new { x.MessageId, x.UserId, x.Emoji, x.CreationTime })
                .ToListAsync(ct))
            .OrderBy(x => x.CreationTime)
            .ToLookup(x => x.MessageId, x => (x.UserId, x.Emoji));
        var saved = (await db.SavedMessages.AsNoTracking()
                .Where(x => x.UserId == viewerId && ids.Contains(x.MessageId))
                .Select(x => x.MessageId)
                .ToListAsync(ct))
            .ToHashSet();
        return messages.Select(message => CollaborationAppService.MapMessage(message, related) with
        {
            Reactions = message.IsRecalled || message.IsDeleted ? [] : Summarize(reactions[message.Id], viewerId),
            IsSaved = saved.Contains(message.Id)
        }).ToArray();
    }

    public async Task<IReadOnlyList<MessageReactionDto>> GetReactionsAsync(Guid messageId, Guid viewerId, CancellationToken ct)
    {
        var rows = await db.MessageReactions.AsNoTracking()
            .Where(x => x.MessageId == messageId)
            .OrderBy(x => x.CreationTime)
            .Select(x => new { x.UserId, x.Emoji })
            .ToListAsync(ct);
        return Summarize(rows.Select(x => (x.UserId, x.Emoji)), viewerId);
    }

    internal static IReadOnlyList<MessageReactionDto> Summarize(IEnumerable<(Guid UserId, string Emoji)> rows, Guid viewerId) =>
        rows.GroupBy(x => x.Emoji, StringComparer.Ordinal)
            .Select(group => new MessageReactionDto(group.Key, group.Count(), group.Any(x => x.UserId == viewerId),
                group.Select(x => x.UserId).ToArray()))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Emoji, StringComparer.Ordinal)
            .ToArray();
}
