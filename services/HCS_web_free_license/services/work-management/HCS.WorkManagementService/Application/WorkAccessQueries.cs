using HCS.WorkManagementService.Domain;

namespace HCS.WorkManagementService.Application;

public static class WorkAccessQueries
{
    public static IQueryable<Project> OwnedOrAssignedProjects(
        IQueryable<Project> projects, IQueryable<ProjectMember> members, Guid userId) =>
        projects.Where(x => x.OwnerUserId == userId ||
            members.Any(m => m.ProjectId == x.Id && m.UserId == userId && m.IsActive));

    public static IQueryable<ProjectTask> CreatedOrAssignedTasks(
        IQueryable<ProjectTask> tasks, IQueryable<ProjectTaskAssignment> assignments,
        IQueryable<Project> projects, Guid userId) =>
        tasks.Where(t => t.CreatorId == userId
            || assignments.Any(a => a.ProjectTaskId == t.Id && a.UserId == userId)
            || (t.CreatorId == null && projects.Any(p => p.Id == t.ProjectId && p.OwnerUserId == userId)));

    public static bool CanManageProject(Guid ownerUserId, Guid userId, bool isAdministrator) =>
        isAdministrator || ownerUserId == userId;

    public static bool CanDeleteTask(Guid? creatorId, Guid userId, bool isAdministrator, Guid? projectOwnerUserId = null) =>
        isAdministrator || creatorId == userId || (creatorId is null && projectOwnerUserId == userId);

    public static bool CanUpdateTask(Guid? creatorId, bool isAssignee, Guid userId, bool isAdministrator, Guid? projectOwnerUserId = null) =>
        CanDeleteTask(creatorId, userId, isAdministrator, projectOwnerUserId) || isAssignee;

    public static bool CanManageAssignments(Guid? creatorId, Guid userId, bool isAdministrator, Guid? projectOwnerUserId = null) =>
        CanDeleteTask(creatorId, userId, isAdministrator, projectOwnerUserId);
}
