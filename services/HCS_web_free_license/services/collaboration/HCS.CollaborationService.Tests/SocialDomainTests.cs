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

    [Fact]
    public void Post_rules_extract_links_and_index_hashtags_without_partial_matches()
    {
        SocialPostRules.ExtractFirstUrl("Read https://example.com/news?id=1. ")
            .ShouldBe("https://example.com/news?id=1");
        SocialPostRules.ExtractFirstUrl("www.example.com").ShouldBeNull();

        SocialPostRules.ExtractHashtags("#HCS #nội_bộ #HCS")
            .ShouldBe(new[] { "hcs", "nội_bộ" });
        SocialPostRules.BuildHashtagIndex("#hcs #internal").ShouldBe("|hcs||internal|");
        SocialPostRules.NormalizeHashtag(" #HCS ").ShouldBe("hcs");
        SocialPostRules.NormalizeHashtag("#bad tag").ShouldBeNull();
    }

    [Fact]
    public void Reaction_rules_allow_toggle_and_replacement_values_only()
    {
        var reaction = new SocialPostReaction(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), SocialReactionType.Like);
        reaction.ReactionType.ShouldBe(SocialReactionType.Like);
        reaction.ChangeTo(SocialReactionType.Love);
        reaction.ReactionType.ShouldBe(SocialReactionType.Love);
        Should.Throw<BusinessException>(() => reaction.ChangeTo((SocialReactionType)99));
        Should.Throw<BusinessException>(() => new SocialCommentReaction(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), (SocialReactionType)99));
    }
}
