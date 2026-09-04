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
    IClock clock) : ApplicationService
{
    private Guid UserId => currentUser.Id ?? throw new AbpAuthorizationException("Authenticated user required.");

    public async Task<IReadOnlyList<SocialCommentDto>> GetAsync(Guid postId, CancellationToken ct = default)
    {
        await posts.RequireVisibleAsync(postId, ct);
        var comments = await db.SocialPostComments.AsNoTracking()
            .Where(x => x.PostId == postId)
            .OrderBy(x => x.CreationTime).ThenBy(x => x.Id)
            .Take(500).ToListAsync(ct);
        return comments.Select(SocialMapping.Comment).ToArray();
    }

    public async Task<SocialCommentDto> CreateAsync(Guid postId, CreateSocialCommentInput input, CancellationToken ct = default)
    {
        await posts.RequireVisibleAsync(postId, ct);
        var text = Check.NotNullOrWhiteSpace(input.Text, nameof(input.Text), 2000);
        if (input.ParentCommentId.HasValue && !await db.SocialPostComments.AnyAsync(x =>
                x.Id == input.ParentCommentId && x.PostId == postId, ct))
            throw new BusinessException("Collaboration:SocialCommentParentNotFound");

        var comment = new SocialPostComment(guidGenerator.Create(), postId, UserId,
            CurrentDisplayName(), text, input.ParentCommentId, clock.Now.ToUniversalTime());
        db.SocialPostComments.Add(comment);
        await db.SaveChangesAsync(ct);
        return SocialMapping.Comment(comment);
    }

    private string CurrentDisplayName() => UserDisplayNames.FromPerson(
        currentUser.SurName, currentUser.Name, currentUser.UserName, currentUser.FindClaim("name")?.Value);
}
