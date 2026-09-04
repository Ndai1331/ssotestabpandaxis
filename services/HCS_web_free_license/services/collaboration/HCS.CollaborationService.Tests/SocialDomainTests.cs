using HCS.CollaborationService.Contracts;
using HCS.CollaborationService.Domain;
using Shouldly;
using Volo.Abp;

namespace HCS.CollaborationService.Tests;

public sealed class SocialDomainTests
{
    [Fact]
    public void Post_rules_require_text_or_media_and_limit_visibility()
    {
        Should.Throw<BusinessException>(() => SocialPostRules.DemandContent(null, 0));
        Should.Throw<BusinessException>(() => SocialPostRules.DemandContent("text", 11));
        SocialPostRules.DemandContent("text", 0);
        SocialPostRules.DemandContent(null, 1);
        SocialPostRules.DemandValidVisibility(SocialPostVisibility.Public);
        SocialPostRules.DemandValidVisibility(SocialPostVisibility.Internal);
        Should.Throw<BusinessException>(() => SocialPostRules.DemandValidVisibility((SocialPostVisibility)99));
    }

    [Fact]
    public void Media_can_only_be_attached_once_and_comment_keeps_parent()
    {
        var media = new SocialPostMedia(Guid.NewGuid(), Guid.NewGuid(), "posts/media", "photo.png", "image/png", 12, SocialMediaKind.Image);
        var postId = Guid.NewGuid();
        media.AttachTo(postId);
        Should.Throw<BusinessException>(() => media.AttachTo(Guid.NewGuid()));

        var parentId = Guid.NewGuid();
        var comment = new SocialPostComment(Guid.NewGuid(), postId, Guid.NewGuid(), "Author", "Reply", parentId);
        comment.ParentCommentId.ShouldBe(parentId);
    }
}
