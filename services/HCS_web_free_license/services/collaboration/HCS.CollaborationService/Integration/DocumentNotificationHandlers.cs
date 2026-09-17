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

public sealed class DocumentWorkflowTaskAssignedNotificationHandler(CollaborationDbContext db, IGuidGenerator guidGenerator)
    : IDistributedEventHandler<DocumentWorkflowTaskAssignedEto>, ITransientDependency
{
    public Task HandleEventAsync(DocumentWorkflowTaskAssignedEto eventData)
    {
        var recipients = eventData.RecipientUserIds
            .Where(id => id != Guid.Empty && id != eventData.SenderUserId)
            .Distinct();
        var label = string.IsNullOrWhiteSpace(eventData.Title) ? eventData.Number : eventData.Title.Trim();
        return NotificationFanout.UpsertAsync(db, guidGenerator, eventData.EventId, eventData.OccurredAtUtc,
            nameof(DocumentWorkflowTaskAssignedEto), recipients, NotificationLocalization.SigningAssignedTitle,
            NotificationLocalization.Encode(NotificationLocalization.SigningAssignedBody, label),
            DocumentNotificationLinks.Signing(eventData.DocumentId));
    }
}

public sealed class DocumentInboxClearedNotificationHandler(CollaborationDbContext db)
    : IDistributedEventHandler<DocumentInboxClearedEto>, ITransientDependency
{
    public async Task HandleEventAsync(DocumentInboxClearedEto eventData)
    {
        if (await db.InboxMessages.AnyAsync(x => x.Id == eventData.EventId)) return;
        db.InboxMessages.Add(new InboxMessage(eventData.EventId, nameof(DocumentInboxClearedEto), DateTime.UtcNow));

        var title = NotificationLocalization.DocumentSentTitle;
        var signingTitle = NotificationLocalization.SigningAssignedTitle;
        var detailLink = DocumentNotificationLinks.Detail(eventData.DocumentId);
        var signingLink = DocumentNotificationLinks.Signing(eventData.DocumentId);
        var legacyPrefix = $"/document-detail/{eventData.DocumentId:D}";
        await db.NotificationReceivers
            .Where(receiver => db.Notifications.Any(notification =>
                notification.Id == receiver.NotificationId
                && notification.Link != null
                && ((notification.Title == title
                     && (notification.Link == detailLink || notification.Link.StartsWith(legacyPrefix)))
                    || (notification.Title == signingTitle
                        && (notification.Link == signingLink || notification.Link.StartsWith(signingLink))))))
            .ExecuteDeleteAsync();
        await db.Notifications
            .Where(x => x.Link != null
                        && ((x.Title == title && (x.Link == detailLink || x.Link.StartsWith(legacyPrefix)))
                            || (x.Title == signingTitle && (x.Link == signingLink || x.Link.StartsWith(signingLink)))))
            .ExecuteDeleteAsync();
        await db.PushDeliveries
            .Where(x => x.Link != null
                        && ((x.Title == title && (x.Link == detailLink || x.Link.StartsWith(legacyPrefix)))
                            || (x.Title == signingTitle && (x.Link == signingLink || x.Link.StartsWith(signingLink)))))
            .ExecuteDeleteAsync();

        try { await db.SaveChangesAsync(); }
        catch (DbUpdateException exception) when (PostgresErrors.IsInboxDuplicate(exception)) { db.ChangeTracker.Clear(); }
    }
}

internal static class DocumentNotificationLinks
{
    public static string Detail(Guid documentId) => $"/manage-documents?sourceType=2&preview={documentId:D}";
    public static string Signing(Guid documentId) => $"/document-signing/{documentId:D}";
}
