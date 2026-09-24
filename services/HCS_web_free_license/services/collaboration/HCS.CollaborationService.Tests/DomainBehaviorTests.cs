using HCS.CollaborationService.Contracts;
using HCS.CollaborationService.Domain;
using Shouldly;
using Volo.Abp;

namespace HCS.CollaborationService.Tests;

public sealed class DomainBehaviorTests
{
    [Fact]
    public void Project_and_task_conversations_require_cross_domain_ids()
    {
        Should.Throw<BusinessException>(() => new Conversation(Guid.NewGuid(), ConversationType.Project, "p", null));
        Should.Throw<BusinessException>(() => new Conversation(Guid.NewGuid(), ConversationType.Task, "t", null));
        new Conversation(Guid.NewGuid(), ConversationType.Project, "p", null, Guid.NewGuid()).Type.ShouldBe(ConversationType.Project);
    }

    [Fact]
    public void Member_unread_state_is_monotonic_until_read()
    {
        var member = new ConversationMember(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ConversationMemberRole.Member);
        member.IncrementUnread(); member.IncrementUnread(); member.UnreadCount.ShouldBe(2);
        var readAt = DateTime.UtcNow; member.MarkRead(readAt);
        member.UnreadCount.ShouldBe(0); member.LastReadAt.ShouldBe(readAt);
    }

    [Fact]
    public void Message_allows_attachment_only_empty_text()
    {
        var message = new ChatMessage(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), string.Empty);
        message.Text.ShouldBeEmpty();
    }

    [Fact]
    public void Message_preserves_reply_forward_pin_and_soft_delete_audit_semantics()
    {
        var reply = Guid.NewGuid(); var forwarded = Guid.NewGuid(); var user = Guid.NewGuid();
        var message = new ChatMessage(Guid.NewGuid(), Guid.NewGuid(), user, "hello", Guid.NewGuid(), reply, forwarded);
        message.ReplyToMessageId.ShouldBe(reply); message.ForwardedFromMessageId.ShouldBe(forwarded);
        message.Pin(user, DateTime.UtcNow); message.IsPinned.ShouldBeTrue();
        message.Unpin(); message.IsPinned.ShouldBeFalse();
        message.SoftDeleteContent(); message.IsDeleted.ShouldBeTrue(); message.Text.ShouldBeEmpty();
    }

    [Fact]
    public void Attachment_can_only_be_bound_to_one_message()
    {
        var attachment = new MessageAttachment(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "blob", "a.pdf", "application/pdf", 12, AttachmentKind.File);
        attachment.AttachTo(Guid.NewGuid());
        Should.Throw<BusinessException>(() => attachment.AttachTo(Guid.NewGuid()));
    }

    [Fact]
    public void Inbox_id_is_the_event_id_and_outbox_records_attempts()
    {
        var eventId = Guid.NewGuid();
        new InboxMessage(eventId, "event", DateTime.UtcNow).Id.ShouldBe(eventId);
        var outbox = new OutboxMessage(eventId, "event", "{}", DateTime.UtcNow);
        outbox.RecordAttempt(false, DateTime.UtcNow); outbox.Attempts.ShouldBe(1); outbox.PublishedAt.ShouldBeNull();
        outbox.RecordAttempt(true, DateTime.UtcNow); outbox.PublishedAt.ShouldNotBeNull();
    }

    [Fact]
    public void Chat_notification_links_are_identified_for_toast_dedupe()
    {
        ChatNotificationRules.IsChatLink("/chat/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee").ShouldBeTrue();
        ChatNotificationRules.IsChatLink("/chat").ShouldBeTrue();
        ChatNotificationRules.IsChatLink("/document-signing").ShouldBeFalse();
        ChatNotificationRules.IsChatLink(null).ShouldBeFalse();
        ChatNotificationRules.TryGetConversationId("/chat/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee", out var conversationId)
            .ShouldBeTrue();
        conversationId.ShouldBe(Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"));
        ChatNotificationRules.ConversationKey("/chat1/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee")
            .ShouldBe(conversationId.ToString("N"));
    }

    [Fact]
    public void Unread_chat_notifications_from_the_same_conversation_collapse_to_one_row()
    {
        var conversationId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var userId = Guid.NewGuid();
        var first = new NotificationDto(Guid.NewGuid(), userId, NotificationLocalization.ChatTitle,
            NotificationLocalization.Encode(NotificationLocalization.ChatBody, "Tran Viet Hung"),
            ChatNotificationRules.ConversationLink(conversationId), false, new DateTime(2026, 9, 22, 8, 9, 0, DateTimeKind.Utc));
        var second = new NotificationDto(Guid.NewGuid(), userId, NotificationLocalization.ChatTitle,
            NotificationLocalization.Encode(NotificationLocalization.ChatBody, "Tran Viet Hung"),
            $"/chat1/{conversationId:D}", false, new DateTime(2026, 9, 14, 5, 22, 0, DateTimeKind.Utc));
        var third = new NotificationDto(Guid.NewGuid(), userId, NotificationLocalization.ChatTitle,
            NotificationLocalization.Encode(NotificationLocalization.ChatBody, "Tran Viet Hung"),
            ChatNotificationRules.ConversationLink(conversationId), false, new DateTime(2026, 9, 14, 5, 21, 0, DateTimeKind.Utc));
        var document = new NotificationDto(Guid.NewGuid(), userId, NotificationLocalization.DocumentSentTitle,
            NotificationLocalization.Encode(NotificationLocalization.DocumentSentBody, "Test"),
            "/manage-documents", false, new DateTime(2026, 9, 13, 2, 25, 0, DateTimeKind.Utc));

        var collapsed = ChatNotificationGrouping.CollapseUnread([first, second, third, document]);
        collapsed.Count.ShouldBe(2);
        var chat = collapsed.Single(item => ChatNotificationRules.IsChatLink(item.Link));
        chat.Id.ShouldBe(first.Id);
        NotificationLocalization.ChatCount(chat.Body).ShouldBe(3);
        NotificationLocalization.Format(chat.Body, "vi").ShouldBe("3 tin nhắn mới từ Tran Viet Hung");
        NotificationLocalization.Format(chat.Body, "en").ShouldBe("3 new messages from Tran Viet Hung");
        collapsed.ShouldContain(document);
        ChatNotificationGrouping.CountCollapsed(
            [
                (false, first.Link),
                (false, second.Link),
                (false, third.Link),
                (false, document.Link)
            ]).ShouldBe(2);
        ChatNotificationGrouping.CountCollapsed(
            [
                (true, first.Link),
                (true, second.Link),
                (true, third.Link),
                (true, document.Link)
            ]).ShouldBe(2);
    }

    [Fact]
    public void Read_chat_notifications_from_the_same_conversation_collapse_to_one_row()
    {
        var conversationId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var userId = Guid.NewGuid();
        var first = new NotificationDto(Guid.NewGuid(), userId, NotificationLocalization.ChatTitle,
            NotificationLocalization.Encode(NotificationLocalization.ChatBody, "Tran Viet Hung"),
            ChatNotificationRules.ConversationLink(conversationId), true, new DateTime(2026, 9, 22, 2, 17, 9, DateTimeKind.Utc));
        var second = new NotificationDto(Guid.NewGuid(), userId, NotificationLocalization.ChatTitle,
            NotificationLocalization.Encode(NotificationLocalization.ChatBody, "Tran Viet Hung"),
            ChatNotificationRules.ConversationLink(conversationId), true, new DateTime(2026, 9, 22, 2, 12, 23, DateTimeKind.Utc));
        var third = new NotificationDto(Guid.NewGuid(), userId, NotificationLocalization.ChatTitle,
            NotificationLocalization.Encode(NotificationLocalization.ChatBody, "Tran Viet Hung"),
            ChatNotificationRules.ConversationLink(conversationId), true, new DateTime(2026, 9, 22, 2, 12, 20, DateTimeKind.Utc));

        var collapsed = ChatNotificationGrouping.CollapseUnread([first, second, third]);
        collapsed.Count.ShouldBe(1);
        collapsed[0].Id.ShouldBe(first.Id);
        collapsed[0].IsRead.ShouldBeTrue();
        NotificationLocalization.Format(collapsed[0].Body, "vi").ShouldBe("1 tin nhắn mới từ Tran Viet Hung");
    }

    [Fact]
    public void Mixed_read_and_unread_chat_from_the_same_conversation_stay_one_unread_row()
    {
        var conversationId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var userId = Guid.NewGuid();
        var unread = new NotificationDto(Guid.NewGuid(), userId, NotificationLocalization.ChatTitle,
            NotificationLocalization.Encode(NotificationLocalization.ChatBody, "Tran Viet Hung"),
            ChatNotificationRules.ConversationLink(conversationId), false, new DateTime(2026, 9, 22, 3, 0, 0, DateTimeKind.Utc));
        var read = new NotificationDto(Guid.NewGuid(), userId, NotificationLocalization.ChatTitle,
            NotificationLocalization.EncodeChat(2, "Tran Viet Hung"),
            ChatNotificationRules.ConversationLink(conversationId), true, new DateTime(2026, 9, 22, 2, 0, 0, DateTimeKind.Utc));

        var collapsed = ChatNotificationGrouping.CollapseUnread([unread, read]);
        collapsed.Count.ShouldBe(1);
        collapsed[0].Id.ShouldBe(unread.Id);
        collapsed[0].IsRead.ShouldBeFalse();
        NotificationLocalization.ChatCount(collapsed[0].Body).ShouldBe(1);
    }

    [Fact]
    public void System_or_conversation_admin_can_delete_other_people_messages()
    {
        var me = Guid.NewGuid();
        var other = Guid.NewGuid();
        ChatModerationRules.CanDeleteMessage(me, me, false, ConversationMemberRole.Member).ShouldBeTrue();
        ChatModerationRules.CanDeleteMessage(me, other, false, ConversationMemberRole.Member).ShouldBeFalse();
        ChatModerationRules.CanDeleteMessage(me, other, true, ConversationMemberRole.Member).ShouldBeTrue();
        ChatModerationRules.CanDeleteMessage(me, other, false, ConversationMemberRole.Admin).ShouldBeTrue();
        ChatModerationRules.IsSystemAdmin(true, false).ShouldBeTrue();
        ChatModerationRules.IsSystemAdmin(false, true).ShouldBeTrue();
        ChatModerationRules.IsSystemAdmin(false, false).ShouldBeFalse();
        ChatModerationRules.ForwardBody("  note  ").ShouldBe("note");
        ChatModerationRules.ForwardBody(" ").ShouldBe(ChatModerationRules.ForwardedPlaceholder);
    }

    [Fact]
    public void Chat_task_card_round_trips_payload_and_preview()
    {
        var taskId = Guid.NewGuid();
        var assigneeId = Guid.NewGuid();
        var payload = new ChatTaskCardMessage.Payload(
            taskId, "Soạn tờ trình", assigneeId, "Nguyễn Văn A", new DateOnly(2026, 9, 30), "Gấp");
        var text = ChatTaskCardMessage.Format(payload);
        text.ShouldStartWith(ChatTaskCardMessage.Prefix);
        ChatTaskCardMessage.TryParse(text, out var parsed).ShouldBeTrue();
        parsed.ShouldNotBeNull();
        parsed!.TaskId.ShouldBe(taskId);
        parsed.Title.ShouldBe("Soạn tờ trình");
        parsed.AssigneeUserId.ShouldBe(assigneeId);
        parsed.AssigneeName.ShouldBe("Nguyễn Văn A");
        parsed.DueDate.ShouldBe(new DateOnly(2026, 9, 30));
        parsed.Note.ShouldBe("Gấp");
        ChatTaskCardMessage.Preview(text).ShouldBe("Soạn tờ trình");
        ChatTaskCardMessage.TryParse("hello", out _).ShouldBeFalse();
        ChatTaskCardMessage.TryParse("hcs.task:{bad", out _).ShouldBeFalse();
    }

    [Fact]
    public void Chat_attachment_policy_clamps_megabytes_to_the_admin_range()
    {
        ChatAttachmentPolicy.ClampMegabytes(0).ShouldBe(ChatAttachmentPolicy.MinMegabytes);
        ChatAttachmentPolicy.ClampMegabytes(25).ShouldBe(25);
        ChatAttachmentPolicy.ClampMegabytes(512).ShouldBe(512);
        ChatAttachmentPolicy.ClampMegabytes(10_000).ShouldBe(ChatAttachmentPolicy.MaxMegabytes);
        ChatAttachmentPolicy.ToBytes(1).ShouldBe(1024 * 1024);
    }

    [Fact]
    public void Push_token_can_be_safely_reassigned_without_mutating_the_provider_token()
    {
        var firstUser = Guid.NewGuid(); var nextUser = Guid.NewGuid();
        var device = new PushDeviceToken(Guid.NewGuid(), firstUser, "provider-token", "ios");
        device.Deactivate(); device.AssignTo(nextUser, "android");
        device.UserId.ShouldBe(nextUser); device.Token.ShouldBe("provider-token"); device.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Contact_search_matches_lowercase_full_name_phone_or_email()
    {
        const string term = "hạnh";
        ChatContactSearch.Matches("Hoàng Thị", "Hạnh", "0326054106", "hanh@bv.com", term)
            .ShouldBeTrue();
        ChatContactSearch.Matches("Trần Thị Ngọc", "Thạnh", null, null, term)
            .ShouldBeTrue();
        ChatContactSearch.Matches("Hoàng Thị", "Hạnh", null, null, "0326054106")
            .ShouldBeFalse();
        ChatContactSearch.Matches("Nguyễn", "Văn A", "0326054106", null, "032605")
            .ShouldBeTrue();
        ChatContactSearch.Matches("Nguyễn", "Văn A", null, "hanh@bv.com", "hanh@")
            .ShouldBeTrue();
        ChatContactSearch.Matches("Nguyễn", "Văn A", null, null, "hanh", "hanh.ht")
            .ShouldBeTrue();
        ChatContactSearch.Matches("Nguyễn", "Văn A", "0901111111", "a@bv.com", term)
            .ShouldBeFalse();
        var contact = new ChatContactDto(Guid.NewGuid(), "hanh.ht", "Hoàng Thị Hạnh", true, "Hoàng Thị", "Hạnh", "0326054106");
        ChatContactSearch.Matches(contact, "hạnh").ShouldBeTrue();
        ChatContactSearch.Matches(contact, "032605").ShouldBeTrue();
        ChatContactSearch.Matches(contact, "hanh.ht").ShouldBeTrue();
        ChatContactSearch.Matches(contact, "không-có").ShouldBeFalse();
    }

    [Fact]
    public void Contact_lookup_keeps_distinct_ids_and_ignores_empty()
    {
        var first = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var second = Guid.Parse("22222222-2222-2222-2222-222222222222");
        ChatContactLookup.NormalizeIds([Guid.Empty, first, first, second])
            .ShouldBe([first, second]);
        ChatContactLookup.NormalizeIds(null).ShouldBeEmpty();
        ChatContactLookup.NormalizeIds(Enumerable.Range(1, ChatContactLookup.MaxIds + 5)
                .Select(index => Guid.Parse($"00000000-0000-0000-0000-{index:D12}")))
            .Length.ShouldBe(ChatContactLookup.MaxIds);
    }

    [Fact]
    public void Display_name_prefers_vietnamese_full_name_over_generic_user_fallback()
    {
        UserDisplayNames.FromPerson("Nguyễn", "Văn A", "doctor").ShouldBe("Nguyễn Văn A");
        UserDisplayNames.FromPerson(null, null, "doctor", "User").ShouldBe("doctor");
        UserDisplayNames.FirstReal("User", "HCS", "  ").ShouldBeEmpty();
        UserDisplayNames.FirstReal("User", "Nguyễn Văn A").ShouldBe("Nguyễn Văn A");
    }

    [Fact]
    public void Notification_copy_localizes_by_culture_and_keeps_legacy_plain_text()
    {
        var body = NotificationLocalization.Encode(NotificationLocalization.ChatBody, "Nguyễn Văn A");
        NotificationLocalization.Format(body, "vi").ShouldBe("1 tin nhắn mới từ Nguyễn Văn A");
        NotificationLocalization.Format(body, "en").ShouldBe("1 new message from Nguyễn Văn A");
        var many = NotificationLocalization.EncodeChat(3, "Nguyễn Văn A");
        NotificationLocalization.ChatCount(many).ShouldBe(3);
        NotificationLocalization.ChatSender(many).ShouldBe("Nguyễn Văn A");
        NotificationLocalization.Format(many, "vi").ShouldBe("3 tin nhắn mới từ Nguyễn Văn A");
        NotificationLocalization.Format(many, "en").ShouldBe("3 new messages from Nguyễn Văn A");
        NotificationLocalization.Format(NotificationLocalization.ChatTitle, "en").ShouldBe("You have a new message");
        NotificationLocalization.Format("Bạn có tin nhắn mới", "en").ShouldBe("Bạn có tin nhắn mới");
    }

    [Fact]
    public void Chat_notification_can_refresh_unread_count_and_timestamp()
    {
        var at = new DateTime(2026, 9, 22, 8, 9, 0, DateTimeKind.Utc);
        var notification = new Notification(Guid.NewGuid(), NotificationLocalization.ChatTitle,
            NotificationLocalization.EncodeChat(1, "Tran Viet Hung"), "/chat", at);
        var later = at.AddMinutes(10);
        notification.RefreshUnread(NotificationLocalization.EncodeChat(3, "Tran Viet Hung"), later);
        NotificationLocalization.ChatCount(notification.Body).ShouldBe(3);
        notification.CreationTime.ShouldBe(later);

        var receiver = new NotificationReceiver(Guid.NewGuid(), notification.Id, Guid.NewGuid(), at);
        receiver.Touch(later);
        receiver.CreationTime.ShouldBe(later);
        receiver.MarkRead(later);
        receiver.IsRead.ShouldBeTrue();
        receiver.MarkUnread(later.AddMinutes(1));
        receiver.IsRead.ShouldBeFalse();
        receiver.ReadAt.ShouldBeNull();
        receiver.CreationTime.ShouldBe(later.AddMinutes(1));
    }

    [Fact]
    public void Social_notifications_are_localized_with_the_actor_name()
    {
        NotificationLocalization.Format(NotificationLocalization.SocialCommentTitle, "vi")
            .ShouldBe("Bài viết có bình luận mới");
        NotificationLocalization.Format(
                NotificationLocalization.Encode(NotificationLocalization.SocialCommentBody, "Nguyễn Văn A"), "vi")
            .ShouldBe("Nguyễn Văn A đã bình luận về bài viết của bạn");
        var body = NotificationLocalization.Encode(NotificationLocalization.SocialReactionBody, "Nguyễn Văn A");
        NotificationLocalization.Format(body, "vi").ShouldBe("Nguyễn Văn A đã tương tác với bài viết của bạn");
        NotificationLocalization.Format(body, "en").ShouldBe("Nguyễn Văn A reacted to your post");
        NotificationLocalization.Format(NotificationLocalization.DocumentSentTitle, "vi").ShouldBe("Có văn bản mới");
        NotificationLocalization.Format(
                NotificationLocalization.Encode(NotificationLocalization.DocumentSentBody, "Công văn 01"), "en")
            .ShouldBe("You were sent document Công văn 01");
        NotificationLocalization.Format(NotificationLocalization.SigningAssignedTitle, "vi").ShouldBe("Có trình ký mới");
        NotificationLocalization.Format(
                NotificationLocalization.Encode(NotificationLocalization.SigningAssignedBody, "Công văn 01"), "en")
            .ShouldBe("You were sent a signing request for Công văn 01");
    }
}
