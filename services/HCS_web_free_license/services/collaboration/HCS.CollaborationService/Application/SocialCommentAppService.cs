using HCS.CollaborationService.Contracts;
using HCS.CollaborationService.Data;
using HCS.CollaborationService.Domain;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization;
using Volo.Abp.Guids;
using Volo.Abp.Timing;
using Volo.Abp.Users;

namespace HCS.CollaborationService.Application;

public class SocialCommentAppService(
    CollaborationDbContext db,
    SocialPostAppService posts,
    ICurrentUser currentUser,
    IGuidGenerator guidGenerator,
    IClock clock,
    SocialNotificationService socialNotifications) : ApplicationService
{
    private Guid UserId => currentUser.Id ?? throw new AbpAuthorizationException("Authenticated user required.");

    public async Task<IReadOnlyList<SocialCommentDto>> GetAsync(Guid postId, CancellationToken ct = default)
    {
        await posts.RequireVisibleAsync(postId, ct);
        var comments = await db.SocialPostComments.AsNoTracking()
            .Where(x => x.PostId == postId)
            .OrderBy(x => x.CreationTime).ThenBy(x => x.Id)
            .Take(500).ToListAsync(ct);
        var ids = comments.Select(x => x.Id).ToArray();
        var me = UserId;
        var reactionCounts = await db.SocialCommentReactions.AsNoTracking()
            .Where(x => ids.Contains(x.CommentId))
            .GroupBy(x => new { x.CommentId, x.ReactionType })
            .Select(x => new { x.Key.CommentId, x.Key.ReactionType, Count = x.Count() })
            .ToListAsync(ct);
        var currentReactions = await db.SocialCommentReactions.AsNoTracking()
            .Where(x => ids.Contains(x.CommentId) && x.UserId == me)
            .ToDictionaryAsync(x => x.CommentId, x => (SocialReactionType?)x.ReactionType, ct);
        var reactionSummaries = reactionCounts.GroupBy(x => x.CommentId).ToDictionary(group => group.Key,
            group => SocialMapping.Reactions(
                group.OrderByDescending(x => x.Count).ThenBy(x => x.ReactionType)
                    .Select(x => new SocialReactionCountDto(x.ReactionType, x.Count)),
                group.Sum(x => x.Count), currentReactions.GetValueOrDefault(group.Key)));
        return comments.Select(comment => SocialMapping.Comment(comment,
            reactionSummaries.GetValueOrDefault(comment.Id) ?? SocialMapping.EmptyReactions())).ToArray();
    }

    public async Task<SocialCommentDto> CreateAsync(Guid postId, CreateSocialCommentInput input, CancellationToken ct = default)
    {
        var post = await posts.RequireVisibleAsync(postId, ct);
        var text = Check.NotNullOrWhiteSpace(input.Text, nameof(input.Text), 2000);
        Guid? parentAuthorUserId = null;
        if (input.ParentCommentId.HasValue)
        {
            parentAuthorUserId = await db.SocialPostComments.AsNoTracking()
                .Where(x => x.Id == input.ParentCommentId && x.PostId == postId)
                .Select(x => (Guid?)x.AuthorUserId)
                .SingleOrDefaultAsync(ct);
            if (!parentAuthorUserId.HasValue)
                throw new BusinessException("Collaboration:SocialCommentParentNotFound");
        }

        var now = clock.Now.ToUniversalTime();
        var me = UserId;
        var actorName = CurrentDisplayName();
        var comment = new SocialPostComment(guidGenerator.Create(), postId, me,
            actorName, text, input.ParentCommentId, now);
        db.SocialPostComments.Add(comment);
        var notifiedUsers = new HashSet<Guid>();
        if (post.AuthorUserId != me && notifiedUsers.Add(post.AuthorUserId))
            socialNotifications.AddComment(postId, post.Visibility, post.AuthorUserId, actorName,
                isReply: false, now);
        if (parentAuthorUserId is { } parentAuthor && parentAuthor != me && notifiedUsers.Add(parentAuthor))
            socialNotifications.AddComment(postId, post.Visibility, parentAuthor, actorName,
                isReply: true, now);
        await db.SaveChangesAsync(ct);
        socialNotifications.PublishAfterCommit();
        return SocialMapping.Comment(comment, SocialMapping.EmptyReactions());
    }

    public async Task<SocialReactionStateDto> ReactAsync(Guid commentId, SetSocialReactionInput input,
        CancellationToken ct = default)
    {
        var comment = await db.SocialPostComments.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == commentId, ct)
            ?? throw new AbpAuthorizationException("Social comment not found.");
        var post = await posts.RequireVisibleAsync(comment.PostId, ct);
        SocialReactionRules.DemandValid(input.ReactionType);
        var me = UserId;
        await using (var transaction = await SocialReactionTransaction.BeginAsync(db, "comment", commentId, me, ct))
        {
            var reaction = await db.SocialCommentReactions.SingleOrDefaultAsync(x => x.CommentId == commentId && x.UserId == me, ct);
            if (input.Remove)
            {
                if (reaction is not null)
                    db.SocialCommentReactions.Remove(reaction);
            }
            else if (reaction is null)
            {
                db.SocialCommentReactions.Add(new SocialCommentReaction(guidGenerator.Create(), commentId, me, input.ReactionType));
                if (comment.AuthorUserId != me)
                    socialNotifications.AddCommentReaction(comment.PostId, post.Visibility,
                        comment.AuthorUserId, CurrentDisplayName(), clock.Now.ToUniversalTime());
            }
            else if (reaction.ReactionType != input.ReactionType)
                reaction.ChangeTo(input.ReactionType);

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        socialNotifications.PublishAfterCommit();
        var counts = await db.SocialCommentReactions.AsNoTracking().Where(x => x.CommentId == commentId)
            .GroupBy(x => x.ReactionType)
            .Select(x => new SocialReactionCountDto(x.Key, x.Count()))
            .OrderByDescending(x => x.Count).ThenBy(x => x.Type).ToListAsync(ct);
        var current = await db.SocialCommentReactions.AsNoTracking()
            .Where(x => x.CommentId == commentId && x.UserId == me)
            .Select(x => (SocialReactionType?)x.ReactionType).SingleOrDefaultAsync(ct);
        return new(SocialMapping.Reactions(counts, counts.Sum(x => x.Count), current));
    }

    private string CurrentDisplayName() => UserDisplayNames.FromPerson(
        currentUser.SurName, currentUser.Name, currentUser.UserName, currentUser.FindClaim("name")?.Value);
}
