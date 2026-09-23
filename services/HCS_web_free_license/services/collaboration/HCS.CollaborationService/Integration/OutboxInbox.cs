using System.Text.Json;
using HCS.CollaborationService.Contracts;
using HCS.CollaborationService.Data;
using HCS.CollaborationService.Domain;
using HCS.IntegrationEvents.Collaboration;
using HCS.IntegrationEvents.Work;
using HCS.WorkManagementService.Contracts.Integration;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Guids;

namespace HCS.CollaborationService.Integration;

public sealed class CollaborationOutboxDispatcher(IServiceScopeFactory scopeFactory, ILogger<CollaborationOutboxDispatcher> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<CollaborationDbContext>();
                var bus = scope.ServiceProvider.GetRequiredService<IDistributedEventBus>();
                var leaseId = Guid.NewGuid();
                var now = DateTime.UtcNow;
                var ids = await db.OutboxMessages
                    .Where(x => x.PublishedAt == null && x.DeadLetteredAt == null && x.Attempts < 10 &&
                        (x.LeaseUntil == null || x.LeaseUntil < now))
                    .OrderBy(x => x.OccurredAt).Select(x => x.Id).Take(50).ToListAsync(stoppingToken);
                if (ids.Count != 0)
                    await db.OutboxMessages.Where(x => ids.Contains(x.Id) && x.PublishedAt == null && x.DeadLetteredAt == null &&
                            x.Attempts < 10 && (x.LeaseUntil == null || x.LeaseUntil < now))
                        .ExecuteUpdateAsync(s => s.SetProperty(x => x.LeaseId, leaseId)
                            .SetProperty(x => x.LeaseUntil, now.AddMinutes(1)), stoppingToken);
                var pending = await db.OutboxMessages.Where(x => x.LeaseId == leaseId).OrderBy(x => x.OccurredAt)
                    .ToListAsync(stoppingToken);
                foreach (var item in pending)
                {
                    try
                    {
                        if (item.EventName == nameof(ChatMessageSentEto))
                            await bus.PublishAsync(JsonSerializer.Deserialize<ChatMessageSentEto>(item.Payload)!, onUnitOfWorkComplete: false);
                        else if (item.EventName == nameof(NotificationRequestedEto))
                            await bus.PublishAsync(JsonSerializer.Deserialize<NotificationRequestedEto>(item.Payload)!, onUnitOfWorkComplete: false);
                        else if (item.EventName == nameof(TaskFromMessageRequestedEto))
                            await bus.PublishAsync(JsonSerializer.Deserialize<TaskFromMessageRequestedEto>(item.Payload)!, onUnitOfWorkComplete: false);
                        else if (item.EventName == HCS.IntegrationEvents.Auditing.AuditRecordCapturedEto.EventName)
                            await bus.PublishAsync(JsonSerializer.Deserialize<HCS.IntegrationEvents.Auditing.AuditRecordCapturedEto>(item.Payload)!, onUnitOfWorkComplete: false);
                        else
                        {
                            item.DeadLetter(DateTime.UtcNow, $"Unknown event type: {item.EventName}");
                            logger.LogError("Dead-lettered unknown collaboration outbox event {EventId} ({EventName})", item.Id, item.EventName);
                            continue;
                        }
                        item.RecordAttempt(true, DateTime.UtcNow);
                    }
                    catch (Exception exception)
                    {
                        item.RecordAttempt(false, DateTime.UtcNow, exception.Message);
                        logger.LogWarning(exception, "Publishing collaboration outbox event {EventId} failed", item.Id);
                    }
                }
                if (pending.Count != 0) await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception exception) { logger.LogError(exception, "Collaboration outbox cycle failed"); }
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}

public sealed class NotificationRequestedHandler(
    CollaborationDbContext db, IGuidGenerator guidGenerator)
    : IDistributedEventHandler<NotificationRequestedEto>, ITransientDependency
{
    public async Task HandleEventAsync(NotificationRequestedEto eventData)
    {
        if (await db.InboxMessages.AnyAsync(x => x.Id == eventData.EventId)) return;
        db.InboxMessages.Add(new InboxMessage(eventData.EventId, nameof(NotificationRequestedEto), DateTime.UtcNow));
        var at = eventData.OccurredAtUtc.UtcDateTime;
        var notification = await db.Notifications.SingleOrDefaultAsync(x => x.Id == eventData.NotificationId);
        if (notification is null)
        {
            notification = new Notification(eventData.NotificationId, NotificationLocalization.GenericTitle,
                NotificationLocalization.Encode(NotificationLocalization.GenericBody, eventData.SubjectId), null, at);
            db.Notifications.Add(notification);
        }
        if (!await db.NotificationReceivers.AnyAsync(x => x.NotificationId == eventData.NotificationId && x.UserId == eventData.RecipientUserId))
        {
            db.NotificationReceivers.Add(new NotificationReceiver(guidGenerator.Create(), notification.Id, eventData.RecipientUserId, at));
            db.PushDeliveries.Add(new PushDelivery(guidGenerator.Create(), eventData.RecipientUserId,
                notification.Title, notification.Body, notification.Link, at));
        }
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException exception) when (PostgresErrors.IsInboxDuplicate(exception)) { db.ChangeTracker.Clear(); }
    }
}

public sealed class WorkSubjectAccessChangedHandler(CollaborationDbContext db, IGuidGenerator guidGenerator)
    : IDistributedEventHandler<WorkSubjectAccessChangedEto>, ITransientDependency
{
    public async Task HandleEventAsync(WorkSubjectAccessChangedEto eventData)
    {
        if (eventData.SchemaVersion != 1) return;
        if (eventData.SubjectType is not ("Project" or "Task"))
            throw new BusinessException("Collaboration:InvalidWorkSubjectType");
        if (await db.InboxMessages.AnyAsync(x => x.Id == eventData.EventId)) return;
        await using var transaction = await db.Database.BeginTransactionAsync();
        // All replicas serialize projection updates for the same external subject.
        // Without this lock, two different events can both observe a missing or stale
        // projection and the later database writer can overwrite newer access state.
        if (db.Database.IsNpgsql())
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({SubjectLockKey(eventData)}))");
        db.InboxMessages.Add(new InboxMessage(eventData.EventId, nameof(WorkSubjectAccessChangedEto), DateTime.UtcNow));
        var subject = await db.WorkSubjects.Include(x => x.Members).SingleOrDefaultAsync(x =>
            x.SubjectType == eventData.SubjectType && x.ProjectId == eventData.ProjectId && x.TaskId == eventData.TaskId);
        if (subject is not null && eventData.OccurredAtUtc <= subject.LastOccurredAtUtc)
        {
            try { await db.SaveChangesAsync(); await transaction.CommitAsync(); }
            catch (DbUpdateException exception) when (PostgresErrors.IsInboxDuplicate(exception)) { await transaction.RollbackAsync(); db.ChangeTracker.Clear(); }
            return;
        }
        if (eventData.IsDeleted)
        {
            var conversation = eventData.SubjectType == "Project"
                ? await db.Conversations.SingleOrDefaultAsync(x => x.Type == ConversationType.Project && x.ProjectId == eventData.ProjectId)
                : await db.Conversations.SingleOrDefaultAsync(x => x.Type == ConversationType.Task && x.TaskId == eventData.TaskId);
            if (conversation is not null) db.Conversations.Remove(conversation);
            if (subject is null)
            {
                subject = WorkSubjectProjection.CreateDeleted(guidGenerator.Create(), eventData.SubjectType,
                    eventData.ProjectId, eventData.TaskId, eventData.OccurredAtUtc);
                db.WorkSubjects.Add(subject);
            }
            else
            {
                subject.MarkDeleted(eventData.OccurredAtUtc);
                db.WorkSubjectMembers.RemoveRange(subject.Members);
            }
        }
        else
        {
            if (subject is null)
            {
                subject = new WorkSubjectProjection(guidGenerator.Create(), eventData.SubjectType, eventData.ProjectId, eventData.TaskId, eventData.OccurredAtUtc);
                db.WorkSubjects.Add(subject);
            }
            else subject.Restore(eventData.OccurredAtUtc);
            var wanted = eventData.AuthorizedUserIds.Distinct().ToHashSet();
            db.WorkSubjectMembers.RemoveRange(subject.Members.Where(x => !wanted.Contains(x.UserId)));
            var existing = subject.Members.Select(x => x.UserId).ToHashSet();
            foreach (var userId in wanted.Where(x => !existing.Contains(x)))
                subject.Members.Add(new WorkSubjectMemberProjection(guidGenerator.Create(), subject.Id, userId));

            var conversation = eventData.SubjectType == "Project"
                ? await db.Conversations.Include(x => x.Members).SingleOrDefaultAsync(x =>
                    x.Type == ConversationType.Project && x.ProjectId == eventData.ProjectId)
                : await db.Conversations.Include(x => x.Members).SingleOrDefaultAsync(x =>
                    x.Type == ConversationType.Task && x.TaskId == eventData.TaskId);
            if (conversation is not null)
                db.ConversationMembers.RemoveRange(conversation.Members.Where(x => !wanted.Contains(x.UserId)));
        }
        try { await db.SaveChangesAsync(); await transaction.CommitAsync(); }
        catch (DbUpdateException exception) when (PostgresErrors.IsInboxDuplicate(exception)) { await transaction.RollbackAsync(); db.ChangeTracker.Clear(); }
    }

    private static string SubjectLockKey(WorkSubjectAccessChangedEto eventData) =>
        $"hcs-collaboration:{eventData.SubjectType}:{eventData.ProjectId:N}:{eventData.TaskId?.ToString("N") ?? "none"}";
}

public sealed class ChatMessageSentNotificationHandler(CollaborationDbContext db, IGuidGenerator guidGenerator)
    : IDistributedEventHandler<ChatMessageSentEto>, ITransientDependency
{
    public Task HandleEventAsync(ChatMessageSentEto eventData) =>
        NotificationFanout.UpsertChatAsync(db, guidGenerator, eventData.EventId, eventData.OccurredAtUtc,
            eventData.ConversationId, eventData.RecipientUserIds,
            UserDisplayNames.FirstReal(eventData.SenderDisplayName));
}

public sealed class ProjectTaskChangedNotificationHandler(CollaborationDbContext db, IGuidGenerator guidGenerator)
    : IDistributedEventHandler<ProjectTaskChangedEto>, ITransientDependency
{
    public Task HandleEventAsync(ProjectTaskChangedEto eventData) => eventData.AssigneeUserId.HasValue
        && string.Equals(eventData.ChangeType, "AssignmentChanged", StringComparison.OrdinalIgnoreCase)
        ? NotificationFanout.UpsertAsync(db, guidGenerator, eventData.EventId, eventData.OccurredAtUtc,
            nameof(ProjectTaskChangedEto), [eventData.AssigneeUserId.Value], NotificationLocalization.TaskAssignedTitle,
            NotificationLocalization.Encode(NotificationLocalization.TaskAssignedBody, TaskName(eventData)),
            $"/project-task-detail/{eventData.TaskId}")
        : Task.CompletedTask;

    private static string TaskName(ProjectTaskChangedEto eventData) =>
        string.IsNullOrWhiteSpace(eventData.Title) ? eventData.TaskId.ToString("N")[..8] : eventData.Title.Trim();
}

public sealed class ProjectMemberAssignedNotificationHandler(CollaborationDbContext db, IGuidGenerator guidGenerator)
    : IDistributedEventHandler<ProjectMemberAssignedEto>, ITransientDependency
{
    public Task HandleEventAsync(ProjectMemberAssignedEto eventData) =>
        NotificationFanout.UpsertAsync(db, guidGenerator, eventData.EventId, eventData.OccurredAtUtc,
            nameof(ProjectMemberAssignedEto), [eventData.UserId], NotificationLocalization.ProjectAssignedTitle,
            NotificationLocalization.Encode(NotificationLocalization.ProjectAssignedBody, eventData.ProjectName),
            $"/project-detail/{eventData.ProjectId}");
}

internal static class NotificationFanout
{
    public const int MaxRecipients = 500;
    public static async Task UpsertAsync(CollaborationDbContext db, IGuidGenerator guids, Guid eventId,
        DateTimeOffset occurredAtUtc, string eventName, IEnumerable<Guid> recipients, string title, string body, string? link)
    {
        if (await db.InboxMessages.AnyAsync(x => x.Id == eventId)) return;
        var userIds = recipients.Distinct().Take(MaxRecipients + 1).ToArray();
        if (userIds.Length > MaxRecipients) throw new BusinessException("Collaboration:TooManyNotificationRecipients");
        var at = occurredAtUtc.UtcDateTime;
        db.InboxMessages.Add(new InboxMessage(eventId, eventName, at));
        var notification = await db.Notifications.SingleOrDefaultAsync(x => x.Id == eventId);
        if (notification is null)
        {
            notification = new Notification(eventId, title, body, link, at);
            db.Notifications.Add(notification);
        }
        var existing = await db.NotificationReceivers.Where(x => x.NotificationId == eventId && userIds.Contains(x.UserId))
            .Select(x => x.UserId).ToListAsync();
        foreach (var userId in userIds.Except(existing))
        {
            db.NotificationReceivers.Add(new NotificationReceiver(guids.Create(), eventId, userId, at));
            db.PushDeliveries.Add(new PushDelivery(guids.Create(), userId, title, body, link, at));
        }
        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException exception) when (PostgresErrors.IsInboxDuplicate(exception)) { db.ChangeTracker.Clear(); }
    }

    public static async Task UpsertChatAsync(CollaborationDbContext db, IGuidGenerator guids, Guid eventId,
        DateTimeOffset occurredAtUtc, Guid conversationId, IEnumerable<Guid> recipients, string senderName)
    {
        if (await db.InboxMessages.AnyAsync(x => x.Id == eventId)) return;
        var userIds = recipients.Where(id => id != Guid.Empty).Distinct().Take(MaxRecipients + 1).ToArray();
        if (userIds.Length > MaxRecipients) throw new BusinessException("Collaboration:TooManyNotificationRecipients");
        var at = occurredAtUtc.UtcDateTime;
        var link = ChatNotificationRules.ConversationLink(conversationId);
        var aliases = ChatNotificationRules.LinkAliases(link);
        db.InboxMessages.Add(new InboxMessage(eventId, nameof(ChatMessageSentEto), at));
        foreach (var userId in userIds)
        {
            await CoalesceUnreadChatAsync(db, guids, userId, senderName, link, aliases, at);
        }

        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException exception) when (PostgresErrors.IsInboxDuplicate(exception)) { db.ChangeTracker.Clear(); }
    }

    private static async Task CoalesceUnreadChatAsync(CollaborationDbContext db, IGuidGenerator guidGenerator,
        Guid userId, string senderName, string link, string[] aliases, DateTime at)
    {
        var rows = await (
            from receiver in db.NotificationReceivers
            join notification in db.Notifications on receiver.NotificationId equals notification.Id
            where receiver.UserId == userId && notification.Link != null
                && aliases.Contains(notification.Link)
            orderby receiver.CreationTime descending, notification.Id descending
            select new { Receiver = receiver, Notification = notification }
        ).ToListAsync();

        var unread = rows.Where(x => !x.Receiver.IsRead).ToList();
        var keep = unread.Count > 0 ? unread[0] : rows.FirstOrDefault();
        var unreadCount = unread.Sum(x => NotificationLocalization.ChatCount(x.Notification.Body));
        var body = ChatBody(keep is null || keep.Receiver.IsRead ? 1 : unreadCount + 1, senderName,
            unread.Select(x => x.Notification.Body).Concat(rows.Select(x => x.Notification.Body)));
        if (keep is null)
        {
            AddPersonalChat(db, guidGenerator, userId, body, link, at);
            return;
        }

        var extras = rows.Where(x => x.Receiver.Id != keep.Receiver.Id).ToList();
        var sharedKeep = await db.NotificationReceivers.CountAsync(x =>
            x.NotificationId == keep.Notification.Id && x.UserId != userId);
        if (sharedKeep > 0)
        {
            if (!keep.Receiver.IsRead)
            {
                keep.Receiver.MarkRead(at);
            }

            AddPersonalChat(db, guidGenerator, userId, body, link, at);
        }
        else
        {
            keep.Notification.RefreshUnread(body, at);
            if (keep.Receiver.IsRead)
            {
                keep.Receiver.MarkUnread(at);
            }
            else
            {
                keep.Receiver.Touch(at);
            }

            db.PushDeliveries.Add(new PushDelivery(guidGenerator.Create(), userId,
                NotificationLocalization.ChatTitle, body, link, at));
        }

        foreach (var extra in extras)
        {
            var shared = await db.NotificationReceivers.CountAsync(x =>
                x.NotificationId == extra.Notification.Id && x.UserId != userId);
            if (shared > 0)
            {
                extra.Receiver.MarkRead(at);
                continue;
            }

            db.NotificationReceivers.Remove(extra.Receiver);
            db.Notifications.Remove(extra.Notification);
        }
    }

    private static void AddPersonalChat(CollaborationDbContext db, IGuidGenerator guidGenerator,
        Guid userId, string body, string link, DateTime at)
    {
        var id = guidGenerator.Create();
        db.Notifications.Add(new Notification(id, NotificationLocalization.ChatTitle, body, link, at));
        db.NotificationReceivers.Add(new NotificationReceiver(guidGenerator.Create(), id, userId, at));
        db.PushDeliveries.Add(new PushDelivery(guidGenerator.Create(), userId,
            NotificationLocalization.ChatTitle, body, link, at));
    }

    private static string ChatBody(int count, string senderName, IEnumerable<string> existingBodies)
    {
        var name = UserDisplayNames.FirstReal(senderName,
            existingBodies.Select(NotificationLocalization.ChatSender).FirstOrDefault(value =>
                !string.IsNullOrWhiteSpace(value)));
        return string.IsNullOrWhiteSpace(name)
            ? NotificationLocalization.ChatBodyUnknown
            : NotificationLocalization.EncodeChat(count, name);
    }
}
