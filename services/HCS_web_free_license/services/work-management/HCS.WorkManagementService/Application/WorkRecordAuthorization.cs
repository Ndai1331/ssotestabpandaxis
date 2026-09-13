using HCS.WorkManagementService.Data;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Authorization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Users;

namespace HCS.WorkManagementService.Application;

public static class SurveyAccessRules
{
    public static bool CanSubmit(bool isAdministrator, Guid ownerUserId, Guid userId, string status,
        DateTime startsAt, DateTime endsAt, DateTime nowUtc) =>
        status == "Active" && startsAt <= nowUtc && endsAt >= nowUtc;
    public static bool CanManage(bool isAdministrator, Guid ownerUserId, Guid userId) =>
        isAdministrator || ownerUserId == userId;
}

public sealed class WorkRecordAuthorization(WorkManagementDbContext db, ICurrentUser currentUser) : ITransientDependency
{
    public Guid UserId => currentUser.Id ?? throw new AbpAuthorizationException("Authenticated user required.");
    public bool IsAdministrator => currentUser.IsInRole("admin") || currentUser.IsInRole("bd-admin");

    public IQueryable<Domain.Project> VisibleProjects() => IsAdministrator
        ? db.Projects
        : WorkAccessQueries.OwnedOrAssignedProjects(db.Projects, db.ProjectMembers, UserId);

    public IQueryable<Domain.ProjectTask> VisibleTasks() => IsAdministrator
        ? db.ProjectTasks
        : WorkAccessQueries.CreatedOrAssignedTasks(db.ProjectTasks, db.ProjectTaskAssignments, db.Projects, UserId);

    public async Task<HashSet<Guid>> CreatableProjectIdsAsync(IReadOnlyCollection<Guid> projectIds, CancellationToken ct)
    {
        var ids = projectIds.Distinct().ToList();
        if (ids.Count == 0) return [];
        if (IsAdministrator) return ids.ToHashSet();
        return (await VisibleProjects().Where(x => ids.Contains(x.Id)).Select(x => x.Id).ToListAsync(ct)).ToHashSet();
    }

    public async Task DemandProjectMemberAsync(Guid projectId, CancellationToken ct)
    {
        if (IsAdministrator) return;
        if (!await VisibleProjects().AnyAsync(x => x.Id == projectId, ct))
            throw new AbpAuthorizationException("Project membership required.");
    }

    public async Task DemandProjectOwnerAsync(Guid projectId, CancellationToken ct)
    {
        if (IsAdministrator) return;
        if (!await db.Projects.AnyAsync(x => x.Id == projectId && x.OwnerUserId == UserId, ct))
            throw new AbpAuthorizationException("Project owner required.");
    }

    public async Task DemandTaskMemberAsync(Guid taskId, CancellationToken ct)
    {
        if (IsAdministrator) return;
        if (!await VisibleTasks().AnyAsync(t => t.Id == taskId, ct))
            throw new AbpAuthorizationException("Task membership required.");
    }

    public async Task DemandTaskOwnerAsync(Guid taskId, CancellationToken ct)
    {
        if (IsAdministrator) return;
        if (!await db.ProjectTasks.AnyAsync(t => t.Id == taskId &&
                (t.CreatorId == UserId ||
                 (t.CreatorId == null && db.Projects.Any(p => p.Id == t.ProjectId && p.OwnerUserId == UserId))), ct))
            throw new AbpAuthorizationException("Task creator required.");
    }

    public async Task DemandEventOwnerAsync(Guid eventId, CancellationToken ct)
    {
        if (IsAdministrator) return;
        if (!await db.ManagedEvents.AnyAsync(x => x.Id == eventId && x.OwnerUserId == UserId, ct))
            throw new AbpAuthorizationException("Event owner required.");
    }

    public async Task DemandSurveyOwnerAsync(Guid sessionId, CancellationToken ct)
    {
        if (IsAdministrator) return;
        if (!await db.SurveySessions.AnyAsync(x => x.Id == sessionId && x.OwnerUserId == UserId, ct))
            throw new AbpAuthorizationException("Survey owner required.");
    }

    public async Task DemandSurveyAudienceAsync(Guid sessionId, DateTime nowUtc, CancellationToken ct)
    {
        var session = await db.SurveySessions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == sessionId, ct)
            ?? throw new Volo.Abp.Domain.Entities.EntityNotFoundException(typeof(Domain.SurveySession), sessionId);
        if (!SurveyAccessRules.CanSubmit(IsAdministrator, session.OwnerUserId, UserId, session.Status,
                session.StartsAt, session.EndsAt, nowUtc))
            throw new AbpAuthorizationException("Survey is outside its active audience window.");
    }
}
