using HCS.CollaborationService.Contracts;
using HCS.CollaborationService.Data;
using HCS.CollaborationService.Domain;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Volo.Abp.Uow;

namespace HCS.CollaborationService.Application;

/// <summary>
/// Persists social notifications in the current unit of work and publishes them
/// only after the transaction has committed successfully.
/// </summary>
public sealed class SocialNotificationService(
    CollaborationDbContext db,
    IGuidGenerator guidGenerator,
    IUnitOfWorkManager unitOfWorkManager,
    IChatRealtimeNotifier realtimeNotifier,
    ILogger<SocialNotificationService> logger) : ITransientDependency
{
    private readonly List<NotificationDto> pending = [];

    public void AddComment(Guid postId, SocialPostVisibility visibility, Guid recipientUserId,
        string actorName, bool isReply, DateTime occurredAtUtc)
    {
        var bodyKey = isReply ? NotificationLocalization.SocialReplyBody : NotificationLocalization.SocialCommentBody;
        var title = isReply ? NotificationLocalization.SocialReplyTitle : NotificationLocalization.SocialCommentTitle;
        Add(postId, visibility, recipientUserId, title,
            NotificationLocalization.Encode(bodyKey, SafeActorName(actorName)), occurredAtUtc);
    }

    public void AddPostReaction(Guid postId, SocialPostVisibility visibility, Guid recipientUserId,
        string actorName, DateTime occurredAtUtc) =>
        Add(postId, visibility, recipientUserId, NotificationLocalization.SocialReactionTitle,
            NotificationLocalization.Encode(NotificationLocalization.SocialReactionBody, SafeActorName(actorName)), occurredAtUtc);

    public void AddCommentReaction(Guid postId, SocialPostVisibility visibility, Guid recipientUserId,
        string actorName, DateTime occurredAtUtc) =>
        Add(postId, visibility, recipientUserId, NotificationLocalization.SocialCommentReactionTitle,
            NotificationLocalization.Encode(NotificationLocalization.SocialCommentReactionBody, SafeActorName(actorName)), occurredAtUtc);

    public void PublishAfterCommit()
    {
        if (pending.Count == 0)
            return;

        var notifications = pending.ToArray();
        pending.Clear();
        if (unitOfWorkManager.Current is { } unitOfWork)
        {
            unitOfWork.OnCompleted(() => PublishAsync(notifications));
            return;
        }

        _ = PublishAsync(notifications);
    }

    private void Add(Guid postId, SocialPostVisibility visibility, Guid recipientUserId,
        string title, string body, DateTime occurredAtUtc)
    {
        if (recipientUserId == Guid.Empty)
            return;

        var notification = new Notification(guidGenerator.Create(), title, body,
            SocialNotificationLinks.Post(postId, visibility), occurredAtUtc);
        var receiver = new NotificationReceiver(guidGenerator.Create(), notification.Id,
            recipientUserId, occurredAtUtc);
        db.Notifications.Add(notification);
        db.NotificationReceivers.Add(receiver);
        pending.Add(new NotificationDto(notification.Id, recipientUserId, notification.Title,
            notification.Body, notification.Link, false, notification.CreationTime));
    }

    private async Task PublishAsync(IReadOnlyList<NotificationDto> notifications)
    {
        foreach (var notification in notifications)
        {
            try
            {
                await realtimeNotifier.NotificationSentAsync(notification);
            }
            catch (Exception exception)
            {
                // The notification is already durable; polling/reconnect will recover it.
                logger.LogDebug(exception, "Realtime social notification {NotificationId} could not be delivered", notification.Id);
            }
        }
    }

    private static string SafeActorName(string actorName) =>
        string.IsNullOrWhiteSpace(actorName) ? "HCS" : actorName.Trim();
}

internal static class SocialNotificationLinks
{
    public static string Post(Guid postId, SocialPostVisibility visibility) =>
        visibility == SocialPostVisibility.Internal
            ? $"/social/profile?visibility=internal&post={postId:D}"
            : $"/social?post={postId:D}";
}

internal static class SocialNotificationKinds
{
    public static readonly string[] TitleKeys =
    [
        NotificationLocalization.SocialCommentTitle,
        NotificationLocalization.SocialReplyTitle,
        NotificationLocalization.SocialReactionTitle,
        NotificationLocalization.SocialCommentReactionTitle
    ];
}
