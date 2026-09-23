using System.Text.Json;
using HCS.CollaborationService.Contracts;
using HCS.CollaborationService.Data;
using HCS.CollaborationService.Domain;
using HCS.CollaborationService.Integration;
using HCS.IntegrationEvents.Collaboration;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization;
using Volo.Abp.Guids;
using Volo.Abp.Timing;
using Volo.Abp.Users;

namespace HCS.CollaborationService.Application;

public class NotificationAppService(CollaborationDbContext db, ICurrentUser currentUser,
    IGuidGenerator guidGenerator, IClock clock, IHttpContextAccessor httpContextAccessor) : ApplicationService
{
    private Guid UserId => currentUser.Id ?? throw new AbpAuthorizationException();
    private bool CanReadGeneralNotifications => httpContextAccessor.HttpContext?.User
        .HasClaim("permission", CollaborationPermissions.Notifications) == true;

    public async Task<IReadOnlyList<NotificationDto>> GetMineAsync(bool unreadOnly, int skip, int take, CancellationToken ct = default,
        DateTime? createdFrom = null, DateTime? toExclusive = null, string? filter = null, bool? isRead = null)
    {
        take = Math.Clamp(take, 1, 100);
        var items = await QueryMine(unreadOnly, createdFrom, toExclusive, filter, isRead)
            .Skip(Math.Max(skip, 0)).Take(take).ToListAsync(ct);
        return ChatNotificationGrouping.CollapseUnread(items);
    }

    public async Task<int> CountMineAsync(bool unreadOnly, CancellationToken ct = default,
        DateTime? createdFrom = null, DateTime? toExclusive = null, string? filter = null, bool? isRead = null)
    {
        var me = UserId;
        if (unreadOnly)
        {
            isRead = false;
        }

        var socialOnly = !CanReadGeneralNotifications;
        var epoch = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var term = string.IsNullOrWhiteSpace(filter) ? null : filter.Trim().ToLower();
        var rows = await (
            from receiver in db.NotificationReceivers.AsNoTracking()
            join notification in db.Notifications.AsNoTracking() on receiver.NotificationId equals notification.Id
            where receiver.UserId == me && (!isRead.HasValue || receiver.IsRead == isRead.Value)
            let createdAt = receiver.CreationTime >= epoch ? receiver.CreationTime
                : notification.CreationTime >= epoch ? notification.CreationTime
                : receiver.CreationTime
            where !socialOnly || SocialNotificationKinds.TitleKeys.Contains(notification.Title)
            where (!createdFrom.HasValue || createdAt >= createdFrom.Value)
                && (!toExclusive.HasValue || createdAt < toExclusive.Value)
            where term == null
                || notification.Title.ToLower().Contains(term)
                || notification.Body.ToLower().Contains(term)
            select new { receiver.IsRead, Link = (string?)notification.Link }
        ).ToListAsync(ct);
        return ChatNotificationGrouping.CountCollapsed(rows.Select(x => (x.IsRead, (string?)x.Link)));
    }

    public Task<int> CountUnreadAsync(CancellationToken ct = default) => CountMineAsync(unreadOnly: true, ct);

    public async Task CreateAsync(CreateNotificationInput input, CancellationToken ct = default)
    {
        if (!currentUser.IsInRole("admin")) throw new AbpAuthorizationException();
        if (input.UserIds.Distinct().Take(NotificationFanout.MaxRecipients + 1).Count() > NotificationFanout.MaxRecipients)
            throw new BusinessException("Collaboration:TooManyNotificationRecipients");
        var now = clock.Now.ToUniversalTime();
        var notification = new Notification(guidGenerator.Create(), input.Title, input.Body, input.Link, now);
        db.Notifications.Add(notification);
        foreach (var userId in input.UserIds.Distinct())
        {
            db.NotificationReceivers.Add(new NotificationReceiver(guidGenerator.Create(), notification.Id, userId, now));
            db.PushDeliveries.Add(new PushDelivery(guidGenerator.Create(), userId, input.Title, input.Body, input.Link, now));
            var evt = new NotificationRequestedEto(guidGenerator.Create(), now, null, notification.Id, userId, "custom", notification.Id.ToString("N"));
            db.OutboxMessages.Add(new OutboxMessage(evt.EventId, nameof(NotificationRequestedEto), JsonSerializer.Serialize(evt), now));
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task MarkReadAsync(Guid notificationId, CancellationToken ct = default)
    {
        var me = UserId;
        var socialOnly = !CanReadGeneralNotifications;
        var receiver = await db.NotificationReceivers.SingleOrDefaultAsync(x => x.NotificationId == notificationId && x.UserId == me
            && (!socialOnly || db.Notifications.Any(notification =>
                notification.Id == x.NotificationId && SocialNotificationKinds.TitleKeys.Contains(notification.Title))), ct)
            ?? throw new BusinessException("Collaboration:NotificationNotFound");
        var now = clock.Now.ToUniversalTime();
        receiver.MarkRead(now);
        var notification = await db.Notifications.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == notificationId, ct);
        if (ChatNotificationRules.IsChatLink(notification?.Link))
        {
            var aliases = ChatNotificationRules.LinkAliases(notification!.Link);
            var siblings = await (
                from other in db.NotificationReceivers
                join item in db.Notifications on other.NotificationId equals item.Id
                where other.UserId == me && !other.IsRead && item.Link != null && aliases.Contains(item.Link)
                select other).ToListAsync(ct);
            foreach (var sibling in siblings)
            {
                sibling.MarkRead(now);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task MarkAllReadAsync(CancellationToken ct = default)
    {
        var me = UserId;
        var socialOnly = !CanReadGeneralNotifications;
        var now = clock.Now.ToUniversalTime();
        await db.NotificationReceivers
            .Where(x => x.UserId == me && !x.IsRead)
            .Where(x => !socialOnly || db.Notifications.Any(notification =>
                notification.Id == x.NotificationId && SocialNotificationKinds.TitleKeys.Contains(notification.Title)))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.IsRead, true)
                .SetProperty(x => x.ReadAt, now), ct);
    }

    public async Task RegisterDeviceAsync(RegisterPushDeviceInput input, CancellationToken ct = default)
    {
        var me = UserId;
        var existing = await db.PushDeviceTokens.SingleOrDefaultAsync(x => x.Token == input.Token, ct);
        if (existing is null) db.PushDeviceTokens.Add(new PushDeviceToken(guidGenerator.Create(), me, input.Token, input.Platform));
        else existing.AssignTo(me, input.Platform);
        await db.SaveChangesAsync(ct);
    }

    private IQueryable<NotificationDto> QueryMine(bool unreadOnly, DateTime? createdFrom, DateTime? toExclusive,
        string? filter, bool? isRead)
    {
        var me = UserId;
        if (unreadOnly)
        {
            isRead = false;
        }

        var socialOnly = !CanReadGeneralNotifications;
        var epoch = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var term = string.IsNullOrWhiteSpace(filter) ? null : filter.Trim().ToLower();
        return from receiver in db.NotificationReceivers.AsNoTracking()
               join notification in db.Notifications.AsNoTracking() on receiver.NotificationId equals notification.Id
               where receiver.UserId == me && (!isRead.HasValue || receiver.IsRead == isRead.Value)
               let createdAt = receiver.CreationTime >= epoch ? receiver.CreationTime
                   : notification.CreationTime >= epoch ? notification.CreationTime
                   : receiver.CreationTime
               where !socialOnly || SocialNotificationKinds.TitleKeys.Contains(notification.Title)
               where (!createdFrom.HasValue || createdAt >= createdFrom.Value)
                   && (!toExclusive.HasValue || createdAt < toExclusive.Value)
               where term == null
                   || notification.Title.ToLower().Contains(term)
                   || notification.Body.ToLower().Contains(term)
               orderby createdAt descending, notification.Id descending
               select new NotificationDto(notification.Id, receiver.UserId, notification.Title, notification.Body,
                   notification.Link, receiver.IsRead, createdAt);
    }
}
