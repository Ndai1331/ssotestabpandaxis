using System.Text.Json;
using HCS.CollaborationService.Contracts;
using HCS.CollaborationService.Data;
using HCS.CollaborationService.Domain;
using HCS.CollaborationService.Integration;
using HCS.IntegrationEvents.Collaboration;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization;
using Volo.Abp.Guids;
using Volo.Abp.Timing;
using Volo.Abp.Users;

namespace HCS.CollaborationService.Application;

public class NotificationAppService(CollaborationDbContext db, ICurrentUser currentUser,
    IGuidGenerator guidGenerator, IClock clock) : ApplicationService
{
    private Guid UserId => currentUser.Id ?? throw new AbpAuthorizationException();

    public async Task<IReadOnlyList<NotificationDto>> GetMineAsync(bool unreadOnly, int skip, int take, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 100);
        var query = from receiver in db.NotificationReceivers.AsNoTracking()
                    join notification in db.Notifications.AsNoTracking() on receiver.NotificationId equals notification.Id
                    where receiver.UserId == UserId && (!unreadOnly || !receiver.IsRead)
                    orderby receiver.CreationTime descending
                    select new NotificationDto(notification.Id, receiver.UserId, notification.Title, notification.Body,
                        notification.Link, receiver.IsRead, receiver.CreationTime);
        return await query.Skip(Math.Max(skip, 0)).Take(take).ToListAsync(ct);
    }

    public async Task CreateAsync(CreateNotificationInput input, CancellationToken ct = default)
    {
        if (!currentUser.IsInRole("admin")) throw new AbpAuthorizationException();
        if (input.UserIds.Distinct().Take(NotificationFanout.MaxRecipients + 1).Count() > NotificationFanout.MaxRecipients)
            throw new BusinessException("Collaboration:TooManyNotificationRecipients");
        var now = clock.Now.ToUniversalTime();
        var notification = new Notification(guidGenerator.Create(), input.Title, input.Body, input.Link);
        db.Notifications.Add(notification);
        foreach (var userId in input.UserIds.Distinct())
        {
            db.NotificationReceivers.Add(new NotificationReceiver(guidGenerator.Create(), notification.Id, userId));
            db.PushDeliveries.Add(new PushDelivery(guidGenerator.Create(), userId, input.Title, input.Body, input.Link, now));
            var evt = new NotificationRequestedEto(guidGenerator.Create(), now, null, notification.Id, userId, "custom", notification.Id.ToString("N"));
            db.OutboxMessages.Add(new OutboxMessage(evt.EventId, nameof(NotificationRequestedEto), JsonSerializer.Serialize(evt), now));
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task MarkReadAsync(Guid notificationId, CancellationToken ct = default)
    {
        var receiver = await db.NotificationReceivers.SingleOrDefaultAsync(x => x.NotificationId == notificationId && x.UserId == UserId, ct)
            ?? throw new BusinessException("Collaboration:NotificationNotFound");
        receiver.MarkRead(clock.Now.ToUniversalTime()); await db.SaveChangesAsync(ct);
    }

    public async Task RegisterDeviceAsync(RegisterPushDeviceInput input, CancellationToken ct = default)
    {
        var existing = await db.PushDeviceTokens.SingleOrDefaultAsync(x => x.Token == input.Token, ct);
        if (existing is null) db.PushDeviceTokens.Add(new PushDeviceToken(guidGenerator.Create(), UserId, input.Token, input.Platform));
        else existing.AssignTo(UserId, input.Platform);
        await db.SaveChangesAsync(ct);
    }
}
