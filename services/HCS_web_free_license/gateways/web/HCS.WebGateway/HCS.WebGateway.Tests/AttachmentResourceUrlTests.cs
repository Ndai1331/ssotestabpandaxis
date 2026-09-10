using HCS.Blazor.Client.Collaboration;
using HCS.Blazor.Client.Work;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace HCS.WebGateway.Tests;

public sealed class AttachmentResourceUrlTests
{
    private static IConfiguration Configuration() => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Bff:PublicOrigin"] = "https://api-hcs.htltech.vn"
        })
        .Build();

    [Fact]
    public void Builds_social_media_url_from_the_media_id()
    {
        var client = new SocialClient(null!, Configuration());
        var id = Guid.Parse("11111111-2222-3333-4444-555555555555");

        Assert.Equal(
            "https://api-hcs.htltech.vn/api/social/media/11111111-2222-3333-4444-555555555555",
            client.BuildMediaUrl(id));
    }

    [Fact]
    public void Builds_social_comment_attachment_url_from_the_attachment_id()
    {
        var client = new SocialClient(null!, Configuration());
        var id = Guid.Parse("11111111-2222-3333-4444-555555555555");

        Assert.Equal(
            "https://api-hcs.htltech.vn/api/social/comment-media/11111111-2222-3333-4444-555555555555",
            client.BuildCommentAttachmentUrl(id));
    }

    [Fact]
    public void Builds_event_attachment_url_from_the_file_id()
    {
        var client = new WorkManagementClient(null!, Configuration());
        var id = Guid.Parse("11111111-2222-3333-4444-555555555555");

        Assert.Equal(
            "https://api-hcs.htltech.vn/api/events/attachments/11111111-2222-3333-4444-555555555555",
            client.BuildEventAttachmentUrl(id));
    }
}
