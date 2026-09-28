using HCS.CollaborationService.Api;
using HCS.CollaborationService.Application;
using HCS.CollaborationService.Contracts;
using HCS.CollaborationService.Domain;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace HCS.CollaborationService.Tests;

public sealed class ChatMobileApiTests
{
    [Fact]
    public void Chat_entities_set_creation_time_without_abp_audit_setters()
    {
        var at = new DateTime(2026, 9, 28, 3, 40, 47, DateTimeKind.Utc);
        new Conversation(Guid.NewGuid(), ConversationType.Group, "g", null, creationTimeUtc: at).CreationTime.ShouldBe(at);
        new ConversationMember(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ConversationMemberRole.Member, at).CreationTime.ShouldBe(at);
        new ChatMessage(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "hi", creationTimeUtc: at).CreationTime.ShouldBe(at);
        new MessageAttachment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "b", "a.pdf", "application/pdf", 1, AttachmentKind.File, at)
            .CreationTime.ShouldBe(at);
    }

    [Fact]
    public void Chat_entities_default_to_a_valid_utc_creation_time()
    {
        var message = new ChatMessage(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "hi");
        message.CreationTime.Kind.ShouldBe(DateTimeKind.Utc);
        message.CreationTime.ShouldBeGreaterThan(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        new Conversation(Guid.NewGuid(), ConversationType.Group, "g", null).CreationTime.Year.ShouldBeGreaterThan(2000);
        new ConversationMember(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ConversationMemberRole.Member).CreationTime.Year.ShouldBeGreaterThan(2000);
    }

    [Fact]
    public void Only_sender_can_recall_and_recall_hides_content()
    {
        var sender = Guid.NewGuid();
        var message = new ChatMessage(Guid.NewGuid(), Guid.NewGuid(), sender, "secret");
        message.Pin(sender, DateTime.UtcNow);
        Should.Throw<Volo.Abp.Authorization.AbpAuthorizationException>(() => message.Recall(Guid.NewGuid(), DateTime.UtcNow));
        message.Recall(sender, DateTime.UtcNow);
        message.IsRecalled.ShouldBeTrue(); message.Text.ShouldBeEmpty(); message.IsPinned.ShouldBeFalse();
        var dto = CollaborationAppService.MapMessage(message);
        dto.IsRecalled.ShouldBeTrue(); dto.Attachments.ShouldBeEmpty(); dto.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public void Deleted_message_cannot_be_recalled()
    {
        var sender = Guid.NewGuid();
        var message = new ChatMessage(Guid.NewGuid(), Guid.NewGuid(), sender, "x");
        message.SoftDeleteContent();
        Should.Throw<Volo.Abp.BusinessException>(() => message.Recall(sender, DateTime.UtcNow));
    }

    [Fact]
    public void Clear_history_hides_older_messages_and_resets_unread()
    {
        var clearedAt = new DateTime(2026, 9, 28, 8, 0, 0, DateTimeKind.Utc);
        var member = new ConversationMember(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ConversationMemberRole.Member);
        member.IncrementUnread();
        member.ClearHistory(clearedAt);
        member.UnreadCount.ShouldBe(0);
        member.CanSee(clearedAt.AddSeconds(-1)).ShouldBeFalse();
        member.CanSee(clearedAt).ShouldBeFalse();
        member.CanSee(clearedAt.AddMilliseconds(1)).ShouldBeTrue();
        member.HiddenAt.ShouldBeNull();
        member.Hide(clearedAt.AddMinutes(1));
        member.HiddenAt.ShouldNotBeNull();
        member.Unhide(); member.HiddenAt.ShouldBeNull();
    }

    [Fact]
    public void Only_group_conversations_can_be_deleted_or_edited()
    {
        var direct = new Conversation(Guid.NewGuid(), ConversationType.User, null, null, directUserOne: Guid.NewGuid(), directUserTwo: Guid.NewGuid());
        Should.Throw<Volo.Abp.BusinessException>(() => direct.MarkDeleted(Guid.NewGuid(), DateTime.UtcNow));
        Should.Throw<Volo.Abp.BusinessException>(() => direct.UpdateInfo("x", null));
        Should.Throw<Volo.Abp.BusinessException>(() => direct.SetAvatar("b", "image/png"));
        var project = new Conversation(Guid.NewGuid(), ConversationType.Project, "p", null, Guid.NewGuid());
        Should.Throw<Volo.Abp.BusinessException>(() => project.MarkDeleted(Guid.NewGuid(), DateTime.UtcNow));

        var group = new Conversation(Guid.NewGuid(), ConversationType.Group, "old", null);
        group.UpdateInfo("  New name ", " desc ");
        group.Name.ShouldBe("New name"); group.Description.ShouldBe("desc");
        group.MarkDeleted(Guid.NewGuid(), DateTime.UtcNow);
        group.IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public void Conversation_dto_exposes_mute_avatar_and_hides_cleared_preview()
    {
        var me = Guid.NewGuid();
        var group = new Conversation(Guid.NewGuid(), ConversationType.Group, "g", null);
        var member = new ConversationMember(Guid.NewGuid(), group.Id, me, ConversationMemberRole.Admin);
        group.Members.Add(member);
        group.SetLastMessage("hello", new DateTime(2026, 9, 28, 7, 0, 0, DateTimeKind.Utc));
        group.SetAvatar($"conversations/{group.Id:N}/avatar/abc123", "image/png");
        member.SetMuted(true);
        member.ClearHistory(new DateTime(2026, 9, 28, 8, 0, 0, DateTimeKind.Utc));

        var dto = CollaborationAppService.MapConversation(group, me);
        dto.IsMuted.ShouldBeTrue();
        dto.LastMessage.ShouldBeNull();
        dto.AvatarUrl.ShouldBe($"/api/chat/conversations/{group.Id:D}/avatar?v=abc123");
    }

    [Fact]
    public void Conversation_management_routes_match_mobile_contract()
    {
        Route(nameof(ChatController.DeleteConversation), typeof(HttpDeleteAttribute)).ShouldBe("conversations/{id:guid}");
        Route(nameof(ChatController.UpdateConversation), typeof(HttpPutAttribute)).ShouldBe("conversations/{id:guid}");
        Route(nameof(ChatController.ClearHistory), typeof(HttpPostAttribute)).ShouldBe("conversations/{id:guid}/clear-history");
        Route(nameof(ChatController.MuteConversation), typeof(HttpPutAttribute)).ShouldBe("conversations/{id:guid}/mute");
        Route(nameof(ChatController.RecallMessage), typeof(HttpPostAttribute)).ShouldBe("messages/{messageId:guid}/recall");
        Route(nameof(ChatController.UploadAvatar), typeof(HttpPostAttribute)).ShouldBe("conversations/{id:guid}/avatar");
        Route(nameof(ChatController.RemoveAvatar), typeof(HttpDeleteAttribute)).ShouldBe("conversations/{id:guid}/avatar");
        Route(nameof(ChatController.Avatar), typeof(HttpGetAttribute)).ShouldBe("conversations/{id:guid}/avatar");
    }

    [Fact]
    public void Search_reaction_and_saved_routes_match_mobile_contract()
    {
        Route(nameof(ChatController.SearchAllMessages), typeof(HttpGetAttribute)).ShouldBe("messages/search");
        Route(nameof(ChatController.SavedMessages), typeof(HttpGetAttribute)).ShouldBe("messages/saved");
        Route(nameof(ChatController.SaveMessage), typeof(HttpPutAttribute)).ShouldBe("messages/{messageId:guid}/save");
        Route(nameof(ChatController.SetReaction), typeof(HttpPutAttribute)).ShouldBe("messages/{messageId:guid}/reactions");
        Route(nameof(ChatController.RemoveReaction), typeof(HttpDeleteAttribute)).ShouldBe("messages/{messageId:guid}/reactions");
        Route(nameof(ChatController.ConversationAttachments), typeof(HttpGetAttribute)).ShouldBe("conversations/{id:guid}/attachments");
    }

    [Fact]
    public void Reaction_summary_groups_by_emoji_and_marks_viewer()
    {
        var me = Guid.NewGuid(); var other = Guid.NewGuid(); var third = Guid.NewGuid();
        var summary = ChatMessageMapper.Summarize([(other, "👍"), (me, "❤️"), (third, "👍")], me);
        summary.Count.ShouldBe(2);
        summary[0].Emoji.ShouldBe("👍"); summary[0].Count.ShouldBe(2); summary[0].ReactedByMe.ShouldBeFalse();
        summary[1].Emoji.ShouldBe("❤️"); summary[1].ReactedByMe.ShouldBeTrue(); summary[1].UserIds.ShouldBe([me]);
    }

    [Fact]
    public void Reaction_emoji_is_required_and_bounded()
    {
        Should.Throw<ArgumentException>(() => new ChatMessageReaction(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), " "));
        Should.Throw<ArgumentException>(() => new ChatMessageReaction(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new string('x', 33)));
        new ChatMessageReaction(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), " 👍 ").Emoji.ShouldBe("👍");
    }

    [Fact]
    public void Link_extraction_trims_punctuation_and_deduplicates()
    {
        ChatMessageFeatureAppService.ExtractLinks("xem https://a.vn/x?y=1, và (http://b.com/p). https://a.vn/x?y=1")
            .ShouldBe(["https://a.vn/x?y=1", "http://b.com/p"]);
        ChatMessageFeatureAppService.ExtractLinks("không có link").ShouldBeEmpty();
    }

    [Fact]
    public void Like_pattern_escapes_wildcards()
    {
        ChatMessageFeatureAppService.EscapeLike(@"50%_a\b").ShouldBe(@"50\%\_a\\b");
    }

    [Fact]
    public void Push_devices_can_be_unregistered_on_logout()
    {
        typeof(NotificationController).GetMethod(nameof(NotificationController.UnregisterDevice))!
            .GetCustomAttributes(typeof(HttpPostAttribute), true).Cast<HttpPostAttribute>().Single().Template.ShouldBe("devices/unregister");
        typeof(NotificationController).GetMethod(nameof(NotificationController.RegisterDevice))!.ReturnType
            .ShouldBe(typeof(Task<PushDeviceDto>));
        var token = new PushDeviceToken(Guid.NewGuid(), Guid.NewGuid(), "fcm-token", "android");
        token.CreationTime.Year.ShouldBeGreaterThan(2000);
        token.Deactivate(); token.IsActive.ShouldBeFalse();
        token.AssignTo(Guid.NewGuid(), "ios"); token.IsActive.ShouldBeTrue();
    }

    internal static string? Route(string action, Type verb) =>
        typeof(ChatController).GetMethod(action)!.GetCustomAttributes(verb, true).Cast<HttpMethodAttribute>().Single().Template;

    [Fact]
    public void Message_list_accepts_before_message_cursor()
    {
        typeof(ChatController).GetMethod(nameof(ChatController.Search))!.GetParameters()
            .ShouldContain(p => p.Name == "beforeMessageId" && p.ParameterType == typeof(Guid?));
    }
}
