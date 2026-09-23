using System.ComponentModel.DataAnnotations;

namespace HCS.CollaborationService.Contracts;

public enum ConversationType { User = 0, Group = 1, Project = 2, Task = 3 }
public enum ConversationMemberRole { Member = 0, Admin = 1 }
public enum AttachmentKind { File = 0, Image = 1, Video = 2, Audio = 3 }
public enum NotificationStatus { Pending = 0, Delivered = 1, Failed = 2 }

public static partial class CollaborationPermissions
{
    public const string Chat = "Collaboration.Chat";
    public const string Notifications = "Collaboration.Notifications";
    public const string Administration = "Collaboration.Administration";
    public const string Realtime = "Collaboration.Realtime";
}

public sealed record ConversationDto(Guid Id, ConversationType Type, string? Name, string? Description,
    Guid? ProjectId, Guid? TaskId, string? LastMessage, DateTime? LastMessageAt,
    int UnreadCount, bool IsPinned, IReadOnlyList<ConversationMemberDto> Members);

public sealed record ConversationMemberDto(Guid UserId, ConversationMemberRole Role, DateTime JoinedAt);
public sealed record PresenceChangedDto(Guid UserId, bool IsOnline);
public sealed record ChatContactDto(
    Guid Id,
    string UserName,
    string DisplayName,
    bool IsActive,
    string? Surname = null,
    string? Name = null,
    string? PhoneNumber = null,
    string? AvatarUrl = null);
public sealed record PagedChatContactsDto(long TotalCount, IReadOnlyList<ChatContactDto> Items);
public sealed record ConversationPermissionDto(bool CanSend, bool CanManageMembers, bool CanRename, bool CanLeave,
    bool CanModerateMessages = false);

public sealed class CreateConversationInput
{
    public ConversationType Type { get; init; }
    [StringLength(256)] public string? Name { get; init; }
    [StringLength(1024)] public string? Description { get; init; }
    public Guid? TargetUserId { get; init; }
    public Guid? ProjectId { get; init; }
    public Guid? TaskId { get; init; }
    public IReadOnlyCollection<Guid> MemberUserIds { get; init; } = [];
}

public sealed class SendMessageInput
{
    public Guid ConversationId { get; init; }
    [StringLength(4000)] public string Text { get; init; } = string.Empty;
    public Guid? ClientMessageId { get; init; }
    public Guid? ReplyToMessageId { get; init; }
    public IReadOnlyCollection<Guid> AttachmentIds { get; init; } = [];
}

public sealed record MessageAttachmentDto(Guid Id, string FileName, string ContentType, long Size,
    AttachmentKind Kind, Guid? MessageId);

public sealed record ChatMessagePreviewDto(Guid Id, Guid SenderUserId, string Text, bool IsDeleted);

public sealed record ChatMessageDto(Guid Id, Guid ConversationId, Guid SenderUserId, string Text,
    DateTime CreatedAt, Guid? ReplyToMessageId, Guid? ForwardedFromMessageId, bool IsPinned,
    bool IsDeleted, IReadOnlyList<MessageAttachmentDto> Attachments,
    ChatMessagePreviewDto? ReplyTo = null, ChatMessagePreviewDto? ForwardedFrom = null);

public sealed record MessageContextDto(ChatMessageDto Target, IReadOnlyList<ChatMessageDto> Before,
    IReadOnlyList<ChatMessageDto> After, bool HasMoreBefore = false, bool HasMoreAfter = false);

public static class ChatModerationRules
{
    public const string ForwardedPlaceholder = "📤";

    public static bool IsSystemAdmin(bool isAdmin, bool isBdAdmin) => isAdmin || isBdAdmin;

    public static bool CanDeleteMessage(
        Guid currentUserId,
        Guid senderUserId,
        bool isSystemAdmin,
        ConversationMemberRole memberRole) =>
        senderUserId == currentUserId
        || isSystemAdmin
        || memberRole == ConversationMemberRole.Admin;

    public static string ForwardBody(string? comment) =>
        string.IsNullOrWhiteSpace(comment) ? ForwardedPlaceholder : comment.Trim();
}

public sealed record PagedMessagesDto(long TotalCount, IReadOnlyList<ChatMessageDto> Items);

public sealed record UploadAttachmentResult(Guid Id, string FileName, string ContentType, long Size,
    AttachmentKind Kind);

public sealed record AuthorizedDownload(string FileName, string ContentType, Stream Content);

public sealed record NotificationDto(Guid Id, Guid UserId, string Title, string Body, string? Link,
    bool IsRead, DateTime CreatedAt);

public static class ChatNotificationRules
{
    public static bool IsChatLink(string? link) =>
        !string.IsNullOrWhiteSpace(link)
        && (link.Equals("/chat", StringComparison.OrdinalIgnoreCase)
            || link.StartsWith("/chat/", StringComparison.OrdinalIgnoreCase)
            || link.StartsWith("/chat1/", StringComparison.OrdinalIgnoreCase));

    public static string ConversationLink(Guid conversationId) => $"/chat/{conversationId:D}";

    public static bool TryGetConversationId(string? link, out Guid conversationId)
    {
        conversationId = default;
        if (string.IsNullOrWhiteSpace(link))
        {
            return false;
        }

        var value = link.Trim();
        foreach (var prefix in new[] { "/chat/", "/chat1/" })
        {
            if (!value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var rest = value[prefix.Length..];
            var end = rest.IndexOfAny(['/', '?', '#']);
            if (end >= 0)
            {
                rest = rest[..end];
            }

            return Guid.TryParse(rest, out conversationId);
        }

        return false;
    }

    public static string ConversationKey(string? link) =>
        TryGetConversationId(link, out var conversationId)
            ? conversationId.ToString("N")
            : (link ?? string.Empty).Trim().ToLowerInvariant();

    public static string[] LinkAliases(string? link)
    {
        if (!TryGetConversationId(link, out var conversationId))
        {
            return string.IsNullOrWhiteSpace(link) ? [] : [link];
        }

        return
        [
            $"/chat/{conversationId:D}",
            $"/chat/{conversationId:N}",
            $"/chat1/{conversationId:D}",
            $"/chat1/{conversationId:N}"
        ];
    }
}

public static class ChatNotificationGrouping
{
    public static IReadOnlyList<NotificationDto> CollapseUnread(IEnumerable<NotificationDto> items)
    {
        var result = new List<NotificationDto>();
        var indexByKey = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var item in items)
        {
            if (!ChatNotificationRules.IsChatLink(item.Link))
            {
                result.Add(item);
                continue;
            }

            var key = ChatNotificationRules.ConversationKey(item.Link);
            if (indexByKey.TryGetValue(key, out var index))
            {
                result[index] = MergeChat(result[index], item);
                continue;
            }

            indexByKey[key] = result.Count;
            result.Add(item);
        }

        return result;
    }

    public static int CountCollapsed(IEnumerable<(bool IsRead, string? Link)> rows)
    {
        var chat = new HashSet<string>(StringComparer.Ordinal);
        var rest = 0;
        foreach (var (_, link) in rows)
        {
            if (ChatNotificationRules.IsChatLink(link))
            {
                chat.Add(ChatNotificationRules.ConversationKey(link));
                continue;
            }

            rest++;
        }

        return chat.Count + rest;
    }

    private static NotificationDto MergeChat(NotificationDto left, NotificationDto right)
    {
        var latest = IsNewer(right, left) ? right : left;
        var unreadCount = UnreadChatCount(left) + UnreadChatCount(right);
        var isRead = unreadCount == 0;
        if (isRead)
        {
            return latest with { IsRead = true };
        }

        var sender = UserDisplayNames.FirstReal(
            NotificationLocalization.ChatSender(left.Body),
            NotificationLocalization.ChatSender(right.Body));
        var body = string.IsNullOrWhiteSpace(sender)
            ? NotificationLocalization.ChatBodyUnknown
            : NotificationLocalization.EncodeChat(unreadCount, sender);
        return latest with { IsRead = false, Body = body };
    }

    private static int UnreadChatCount(NotificationDto item) =>
        item.IsRead ? 0 : NotificationLocalization.ChatCount(item.Body);

    private static bool IsNewer(NotificationDto candidate, NotificationDto current) =>
        candidate.CreatedAt > current.CreatedAt
        || (candidate.CreatedAt == current.CreatedAt && candidate.Id.CompareTo(current.Id) > 0);
}

public static class ChatContactLookup
{
    public const int MaxIds = 200;

    public static Guid[] NormalizeIds(IEnumerable<Guid>? userIds) =>
        (userIds ?? [])
            .Where(id => id != Guid.Empty)
            .Distinct()
            .Take(MaxIds)
            .ToArray();
}

public static class ChatContactSearch
{
    public static bool Matches(ChatContactDto contact, string? filter)
    {
        var term = filter?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(term))
        {
            return true;
        }

        return Matches(contact.Surname, contact.Name, contact.PhoneNumber, email: null, term, contact.UserName)
            || ContainsInsensitive(contact.DisplayName, term);
    }

    public static bool Matches(
        string? surname,
        string? givenName,
        string? phoneNumber,
        string? email,
        string searchTerm,
        string? userName = null)
    {
        var fullName = ((surname ?? string.Empty) + " " + (givenName ?? string.Empty)).ToLowerInvariant();
        return fullName.Contains(searchTerm, StringComparison.Ordinal)
            || ContainsInsensitive(phoneNumber, searchTerm)
            || ContainsInsensitive(email, searchTerm)
            || ContainsInsensitive(userName, searchTerm);
    }

    private static bool ContainsInsensitive(string? value, string searchTerm) =>
        !string.IsNullOrEmpty(value)
        && value.ToLowerInvariant().Contains(searchTerm, StringComparison.Ordinal);
}

public static class UserDisplayNames
{
    public static string FromPerson(string? surname, string? givenName, string? userName, string? jwtFullName = null)
    {
        var full = string.Join(' ', new[] { surname, givenName }.Where(value => !string.IsNullOrWhiteSpace(value))).Trim();
        return FirstReal(full, jwtFullName, userName);
    }

    public static string FirstReal(params string?[] candidates)
    {
        foreach (var candidate in candidates)
        {
            var value = candidate?.Trim();
            if (!string.IsNullOrWhiteSpace(value) && !IsPlaceholder(value))
            {
                return value;
            }
        }

        return string.Empty;
    }

    public static bool IsPlaceholder(string? value) =>
        string.IsNullOrWhiteSpace(value)
        || string.Equals(value.Trim(), "User", StringComparison.OrdinalIgnoreCase)
        || string.Equals(value.Trim(), "HCS", StringComparison.OrdinalIgnoreCase);
}

public static class NotificationLocalization
{
    public const string ChatTitle = "Notification:ChatNewMessage";
    public const string ChatBody = "Notification:ChatNewMessageBody";
    public const string ChatBodyMany = "Notification:ChatNewMessageBodyMany";
    public const string ChatBodyUnknown = "Notification:ChatNewMessageBodyUnknown";
    public const string TaskAssignedTitle = "Notification:TaskAssigned";
    public const string TaskAssignedBody = "Notification:TaskAssignedBody";
    public const string ProjectAssignedTitle = "Notification:ProjectAssigned";
    public const string ProjectAssignedBody = "Notification:ProjectAssignedBody";
    public const string GenericTitle = "Notification:Generic";
    public const string GenericBody = "Notification:GenericBody";
    public const string SocialCommentTitle = "Notification:SocialComment";
    public const string SocialCommentBody = "Notification:SocialCommentBody";
    public const string SocialReplyTitle = "Notification:SocialReply";
    public const string SocialReplyBody = "Notification:SocialReplyBody";
    public const string SocialReactionTitle = "Notification:SocialReaction";
    public const string SocialReactionBody = "Notification:SocialReactionBody";
    public const string SocialCommentReactionTitle = "Notification:SocialCommentReaction";
    public const string SocialCommentReactionBody = "Notification:SocialCommentReactionBody";
    public const string DocumentSentTitle = "Notification:DocumentSent";
    public const string DocumentSentBody = "Notification:DocumentSentBody";
    public const string SigningAssignedTitle = "Notification:SigningAssigned";
    public const string SigningAssignedBody = "Notification:SigningAssignedBody";

    private const char Separator = '\u001f';

    public static string Encode(string key, params string[] args) =>
        args.Length == 0 ? key : string.Join(Separator, new[] { key }.Concat(args));

    public static string EncodeChat(int count, string senderName)
    {
        var name = senderName.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return ChatBodyUnknown;
        }

        return count <= 1
            ? Encode(ChatBody, name)
            : Encode(ChatBodyMany, Math.Max(2, count).ToString(), name);
    }

    public static int ChatCount(string stored)
    {
        if (string.IsNullOrWhiteSpace(stored))
        {
            return 1;
        }

        var parts = stored.Split(Separator);
        if (parts[0] == ChatBodyMany && parts.Length >= 2 && int.TryParse(parts[1], out var many) && many > 0)
        {
            return many;
        }

        return 1;
    }

    public static string ChatSender(string stored)
    {
        if (string.IsNullOrWhiteSpace(stored))
        {
            return string.Empty;
        }

        var parts = stored.Split(Separator);
        if (parts[0] == ChatBodyMany && parts.Length >= 3)
        {
            return parts[2];
        }

        if (parts[0] == ChatBody && parts.Length >= 2)
        {
            return parts[1];
        }

        return string.Empty;
    }

    public static string Format(
        string stored,
        Func<string, string> localizeKey,
        Func<string, object[], string> localizeKeyed)
    {
        if (string.IsNullOrWhiteSpace(stored)
            || !stored.StartsWith("Notification:", StringComparison.Ordinal))
        {
            return stored;
        }

        var parts = stored.Split(Separator);
        return parts.Length == 1
            ? localizeKey(parts[0])
            : localizeKeyed(parts[0], parts.Skip(1).Cast<object>().ToArray());
    }

    public static string Format(string stored, string culture)
    {
        var vietnamese = culture.StartsWith("vi", StringComparison.OrdinalIgnoreCase);
        return Format(
            stored,
            key => Template(key, vietnamese),
            (key, args) => string.Format(Template(key, vietnamese), args));
    }

    private static string Template(string key, bool vietnamese) => (key, vietnamese) switch
    {
        (ChatTitle, true) => "Bạn có tin nhắn mới",
        (ChatTitle, false) => "You have a new message",
        (ChatBody, true) => "1 tin nhắn mới từ {0}",
        (ChatBody, false) => "1 new message from {0}",
        (ChatBodyMany, true) => "{0} tin nhắn mới từ {1}",
        (ChatBodyMany, false) => "{0} new messages from {1}",
        (ChatBodyUnknown, true) => "Bạn có tin nhắn mới",
        (ChatBodyUnknown, false) => "You have a new message",
        (TaskAssignedTitle, true) => "Có công việc mới",
        (TaskAssignedTitle, false) => "New task assigned",
        (TaskAssignedBody, true) => "Bạn được gán vào công việc {0}",
        (TaskAssignedBody, false) => "You were assigned to task {0}",
        (ProjectAssignedTitle, true) => "Có dự án mới",
        (ProjectAssignedTitle, false) => "New project assigned",
        (ProjectAssignedBody, true) => "Bạn được gán vào dự án {0}",
        (ProjectAssignedBody, false) => "You were added to project {0}",
        (GenericTitle, true) => "Thông báo",
        (GenericTitle, false) => "Notification",
        (GenericBody, true) => "Thông báo cho {0}",
        (GenericBody, false) => "Notification for {0}",
        (SocialCommentTitle, true) => "Bài viết có bình luận mới",
        (SocialCommentTitle, false) => "Your post has a new comment",
        (SocialCommentBody, true) => "{0} đã bình luận về bài viết của bạn",
        (SocialCommentBody, false) => "{0} commented on your post",
        (SocialReplyTitle, true) => "Bình luận có phản hồi mới",
        (SocialReplyTitle, false) => "Your comment has a new reply",
        (SocialReplyBody, true) => "{0} đã phản hồi bình luận của bạn",
        (SocialReplyBody, false) => "{0} replied to your comment",
        (SocialReactionTitle, true) => "Bài viết có tương tác mới",
        (SocialReactionTitle, false) => "Your post has a new reaction",
        (SocialReactionBody, true) => "{0} đã tương tác với bài viết của bạn",
        (SocialReactionBody, false) => "{0} reacted to your post",
        (SocialCommentReactionTitle, true) => "Bình luận có tương tác mới",
        (SocialCommentReactionTitle, false) => "Your comment has a new reaction",
        (SocialCommentReactionBody, true) => "{0} đã tương tác với bình luận của bạn",
        (SocialCommentReactionBody, false) => "{0} reacted to your comment",
        (DocumentSentTitle, true) => "Có văn bản mới",
        (DocumentSentTitle, false) => "New document sent to you",
        (DocumentSentBody, true) => "Bạn được gửi văn bản {0}",
        (DocumentSentBody, false) => "You were sent document {0}",
        (SigningAssignedTitle, true) => "Có trình ký mới",
        (SigningAssignedTitle, false) => "New signing request",
        (SigningAssignedBody, true) => "Bạn được gửi trình ký {0}",
        (SigningAssignedBody, false) => "You were sent a signing request for {0}",
        _ => key
    };
}

public sealed class CreateNotificationInput
{
    public IReadOnlyCollection<Guid> UserIds { get; init; } = [];
    [Required, StringLength(256)] public string Title { get; init; } = string.Empty;
    [Required, StringLength(2000)] public string Body { get; init; } = string.Empty;
    [StringLength(1024)] public string? Link { get; init; }
}

public sealed record RegisterPushDeviceInput([property: Required, StringLength(2048)] string Token,
    [property: StringLength(32)] string Platform);

public sealed record TaskFromMessageRequestedEto(Guid EventId, Guid MessageId, Guid ConversationId,
    Guid RequestedByUserId, string Title, string? Description, DateTime OccurredAt);
