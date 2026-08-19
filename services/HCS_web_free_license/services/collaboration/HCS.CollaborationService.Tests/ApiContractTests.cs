using HCS.CollaborationService.Api;
using HCS.CollaborationService.Application;
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
        typeof(NotificationController).GetCustomAttributes(typeof(RouteAttribute), true).Cast<RouteAttribute>().Single().Template.ShouldBe("api/notifications");
    }

    [Fact]
    public void Attachment_store_is_registered_as_transient()
    {
        typeof(CollaborationAttachmentStore).IsAssignableTo(typeof(ITransientDependency)).ShouldBeTrue();
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
