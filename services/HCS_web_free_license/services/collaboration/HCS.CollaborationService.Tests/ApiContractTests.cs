using HCS.CollaborationService.Api;
using HCS.CollaborationService.Application;
using HCS.CollaborationService.Contracts;
using HCS.CollaborationService.Hubs;
using HCS.CollaborationService.Storage;
using Microsoft.AspNetCore.Mvc;
using Shouldly;
using Volo.Abp.DependencyInjection;

namespace HCS.CollaborationService.Tests;

public sealed class ApiContractTests
{
    [Fact]
    public void Chat_and_notification_routes_keep_gateway_contracts()
    {
        typeof(ChatController).GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>().Single().Template.ShouldBe("api/chat");
        typeof(ChatController).GetMethod(nameof(ChatController.FindByProject))!.GetCustomAttributes(typeof(HttpGetAttribute), true).ShouldNotBeEmpty();
        typeof(ChatController).GetMethod(nameof(ChatController.PinMessage))!.GetCustomAttributes(typeof(HttpPutAttribute), true).ShouldNotBeEmpty();
        typeof(ChatController).GetMethod(nameof(ChatController.Search))!.GetParameters().ShouldContain(p => p.Name == "pinnedOnly");
        typeof(NotificationController).GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>().Single().Template.ShouldBe("api/notifications");
    }

    [Fact]
    public void Social_routes_use_the_social_permission_and_media_store_is_transient()
    {
        typeof(SocialController).GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>().Single().Template.ShouldBe("api/social");
        typeof(SocialController).GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true)
            .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().Single().Policy.ShouldBe(CollaborationPermissions.Social);
        typeof(SocialController).GetMethod(nameof(SocialController.Feed))!.GetCustomAttributes(typeof(HttpGetAttribute), true)
            .Cast<HttpGetAttribute>().Single().Template.ShouldBe("feed");
        typeof(SocialController).GetMethod(nameof(SocialController.ProfilePosts))!.GetParameters()
            .ShouldContain(parameter => parameter.Name == "visibility" && parameter.ParameterType == typeof(SocialPostVisibility?));
        typeof(SocialController).GetMethod(nameof(SocialController.Feed))!.GetParameters()
            .ShouldContain(parameter => parameter.Name == "keyword" && parameter.ParameterType == typeof(string));
        typeof(SocialController).GetMethod(nameof(SocialController.ProfilePosts))!.GetParameters()
            .ShouldContain(parameter => parameter.Name == "from" && parameter.ParameterType == typeof(DateOnly?));
        typeof(SocialController).GetMethod(nameof(SocialController.ProfilePosts))!.GetParameters()
            .ShouldContain(parameter => parameter.Name == "to" && parameter.ParameterType == typeof(DateOnly?));
        typeof(SocialController).GetMethod(nameof(SocialController.ProfilePosts))!.GetParameters()
            .ShouldContain(parameter => parameter.Name == "hashtag" && parameter.ParameterType == typeof(string));
        typeof(SocialController).GetMethod(nameof(SocialController.Feed))!.GetParameters()
            .ShouldContain(parameter => parameter.Name == "postId" && parameter.ParameterType == typeof(Guid?));
        typeof(SocialController).GetMethod(nameof(SocialController.UpdatePost))!.GetCustomAttributes(typeof(HttpPutAttribute), true)
            .Cast<HttpPutAttribute>().Single().Template.ShouldBe("posts/{postId:guid}");
        typeof(SocialController).GetMethod(nameof(SocialController.DeletePost))!.GetCustomAttributes(typeof(HttpDeleteAttribute), true)
            .Cast<HttpDeleteAttribute>().Single().Template.ShouldBe("posts/{postId:guid}");
        typeof(SocialController).GetMethod(nameof(SocialController.ReactToPost))!.GetCustomAttributes(typeof(HttpPostAttribute), true)
            .Cast<HttpPostAttribute>().Single().Template.ShouldBe("posts/{postId:guid}/reactions");
        typeof(SocialController).GetMethod(nameof(SocialController.ReactToComment))!.GetCustomAttributes(typeof(HttpPostAttribute), true)
            .Cast<HttpPostAttribute>().Single().Template.ShouldBe("comments/{commentId:guid}/reactions");
        typeof(SocialController).GetMethod(nameof(SocialController.SharePost))!.GetCustomAttributes(typeof(HttpPostAttribute), true)
            .Cast<HttpPostAttribute>().Single().Template.ShouldBe("posts/{postId:guid}/shares");
        typeof(SocialMediaStore).IsAssignableTo(typeof(ITransientDependency)).ShouldBeTrue();
        typeof(SocialPostAppService).IsSealed.ShouldBeFalse();
        typeof(SocialCommentAppService).IsSealed.ShouldBeFalse();
    }

    [Fact]
    public void Attachment_store_is_registered_as_transient()
    {
        typeof(CollaborationAttachmentStore).IsAssignableTo(typeof(ITransientDependency)).ShouldBeTrue();
    }

    [Fact]
    public void Leave_accepts_optional_admin_transfer()
    {
        typeof(ChatController).GetMethod(nameof(ChatController.Leave))!.GetParameters()
            .ShouldContain(p => p.Name == "input");
        typeof(CollaborationAppService).GetMethod(nameof(CollaborationAppService.LeaveAsync))!.GetParameters()
            .ShouldContain(p => p.Name == "transferAdminTo" && p.ParameterType == typeof(Guid?));
    }

    [Fact]
    public void Unread_count_route_is_exposed()
    {
        var method = typeof(ChatController).GetMethod(nameof(ChatController.Unread));
        method.ShouldNotBeNull();
        method!.GetCustomAttributes(typeof(HttpGetAttribute), true).Cast<HttpGetAttribute>()
            .ShouldContain(attribute => attribute.Template == "unread-count");
        typeof(NotificationController).GetMethod(nameof(NotificationController.UnreadCount))!.GetCustomAttributes(typeof(HttpGetAttribute), true)
            .Cast<HttpGetAttribute>().ShouldContain(attribute => attribute.Template == "unread-count");
    }

    [Fact]
    public void Notification_routes_use_the_realtime_channel()
    {
        typeof(NotificationController).GetCustomAttributes(typeof(Microsoft.AspNetCore.Authorization.AuthorizeAttribute), true)
            .Cast<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().Single().Policy.ShouldBe(CollaborationPermissions.Realtime);
    }

    [Fact]
    public void Chat_presence_tracker_is_registered_as_singleton()
    {
        typeof(ChatPresenceTracker).IsAssignableTo(typeof(IChatPresenceTracker)).ShouldBeTrue();
        typeof(ChatPresenceTracker).IsAssignableTo(typeof(ISingletonDependency)).ShouldBeTrue();
    }

    [Fact]
    public void Chat_realtime_notifier_is_registered_as_transient()
    {
        typeof(SignalRChatRealtimeNotifier).IsAssignableTo(typeof(IChatRealtimeNotifier)).ShouldBeTrue();
        typeof(SignalRChatRealtimeNotifier).IsAssignableTo(typeof(ITransientDependency)).ShouldBeTrue();
    }

    [Fact]
    public async Task Attachment_content_is_buffered_into_a_seekable_stream()
    {
        await using var source = new MemoryStream("hello-file"u8.ToArray());
        await using var buffered = await AttachmentContent.BufferAsync(source, 10);
        buffered.CanSeek.ShouldBeTrue();
        buffered.Position.ShouldBe(0);
        buffered.Length.ShouldBe(10);
        (await new StreamReader(buffered).ReadToEndAsync()).ShouldBe("hello-file");
    }
}
