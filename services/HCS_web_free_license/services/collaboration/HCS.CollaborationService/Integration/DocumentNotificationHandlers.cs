using HCS.CollaborationService.Contracts;
using HCS.CollaborationService.Data;
using HCS.CollaborationService.Domain;
using HCS.IntegrationEvents.Documents;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Guids;

namespace HCS.CollaborationService.Integration;

public sealed class DocumentSentToInboxNotificationHandler(CollaborationDbContext db, IGuidGenerator guidGenerator)
    : IDistributedEventHandler<DocumentSentToInboxEto>, ITransientDependency
{
    public Task HandleEventAsync(DocumentSentToInboxEto eventData)
    {
        var recipients = eventData.RecipientUserIds
            .Where(id => id != Guid.Empty && id != eventData.SenderUserId)
            .Distinct();
        var label = string.IsNullOrWhiteSpace(eventData.Title) ? eventData.Number : eventData.Title.Trim();
        return NotificationFanout.UpsertAsync(db, guidGenerator, eventData.EventId, eventData.OccurredAtUtc,
            nameof(DocumentSentToInboxEto), recipients, NotificationLocalization.DocumentSentTitle,
            NotificationLocalization.Encode(NotificationLocalization.DocumentSentBody, label),
            DocumentNotificationLinks.Detail(eventData.DocumentId));
    }
}

public sealed class DocumentInboxClearedNotificationHandler(CollaborationDbContext db)
    : IDistributedEventHandler<DocumentInboxClearedEto>, ITransientDependency
{
    public async Task HandleEventAsync(DocumentInboxClearedEto eventData)
    {
        if (await db.InboxMessages.AnyAsync(x => x.Id == eventData.EventId)) return;
        db.InboxMessages.Add(new InboxMessage(eventData.EventId, nameof(DocumentInboxClearedEto), DateTime.UtcNow));

        var prefix = $"/document-detail/{eventData.DocumentId:D}";
        var previewToken = $"preview={eventData.DocumentId:D}";
        var notifications = await db.Notifications
            .Where(x => x.Title == NotificationLocalization.DocumentSentTitle
                        && x.Link != null
                        && (x.Link.StartsWith(prefix) || x.Link.Contains(previewToken)))
            .ToListAsync();
        var notificationIds = notifications.Select(x => x.Id).ToArray();
        if (notificationIds.Length > 0)
        {
            var receivers = await db.NotificationReceivers
                .Where(x => notificationIds.Contains(x.NotificationId)).ToListAsync();
            db.NotificationReceivers.RemoveRange(receivers);
            db.Notifications.RemoveRange(notifications);
        }

        var deliveries = await db.PushDeliveries
            .Where(x => x.Title == NotificationLocalization.DocumentSentTitle
                        && x.Link != null
                        && (x.Link.StartsWith(prefix) || x.Link.Contains(previewToken)))
            .ToListAsync();
        db.PushDeliveries.RemoveRange(deliveries);

        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException exception) when (PostgresErrors.IsInboxDuplicate(exception)) { db.ChangeTracker.Clear(); }
    }
}

internal static class DocumentNotificationLinks
{
    public static string Detail(Guid documentId) => $"/manage-documents?sourceType=2&preview={documentId:D}";
}
