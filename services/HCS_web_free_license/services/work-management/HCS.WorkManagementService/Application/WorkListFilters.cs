using HCS.WorkManagementService.Contracts;
using HCS.WorkManagementService.Data;
using HCS.WorkManagementService.Domain;
using Microsoft.EntityFrameworkCore;

namespace HCS.WorkManagementService.Application;

internal static class WorkListFilters
{
    /// <summary>Projects whose [StartDate, EndDate] range overlaps [from, to].</summary>
    public static IQueryable<Project> ApplyProjectFilters(IQueryable<Project> query, GetProjectListInput input)
    {
        if (input.From is { } from)
        {
            var fromUtc = WorkTimestamps.ToUtc(from);
            query = query.Where(x => x.EndDate >= fromUtc);
        }
        if (input.To is { } to)
        {
            var toUtc = WorkTimestamps.ToUtc(to);
            query = query.Where(x => x.StartDate <= toUtc);
        }
        if (input.OwnerDepartmentId is { } departmentId)
            query = query.Where(x => x.OwnerDepartmentId == departmentId);
        return query;
    }

    public static IQueryable<ProjectTask> ApplyTaskFilters(IQueryable<ProjectTask> query,
        IQueryable<ProjectTaskAssignment> assignments, GetProjectTaskListInput input)
    {
        if (input.From is { } from)
        {
            var fromUtc = WorkTimestamps.ToUtc(from);
            query = query.Where(x => x.DueDate >= fromUtc);
        }
        if (input.To is { } to)
        {
            var toUtc = WorkTimestamps.ToUtc(to);
            query = query.Where(x => x.DueDate <= toUtc);
        }
        if (!string.IsNullOrWhiteSpace(input.Priority))
        {
            var priority = input.Priority.Trim();
            query = query.Where(x => x.Priority == priority);
        }
        if (input.ParentTaskId is { } parentId) query = query.Where(x => x.ParentTaskId == parentId);
        else if (input.RootOnly) query = query.Where(x => x.ParentTaskId == null);
        if (input.AssigneeUserId is { } assignee)
            query = query.Where(x => assignments.Any(a => a.ProjectTaskId == x.Id && a.UserId == assignee));
        return query;
    }

    public static async Task<Dictionary<Guid, IReadOnlyList<Guid>>> AssigneeUserIdsAsync(WorkManagementDbContext db,
        IReadOnlyCollection<Guid> taskIds, CancellationToken ct)
    {
        if (taskIds.Count == 0) return [];
        var rows = await db.ProjectTaskAssignments.AsNoTracking()
            .Where(x => taskIds.Contains(x.ProjectTaskId))
            .Select(x => new { x.ProjectTaskId, x.UserId })
            .ToListAsync(ct);
        return rows.GroupBy(x => x.ProjectTaskId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<Guid>)g.Select(x => x.UserId).Distinct().ToList());
    }

    public static int RoundProgress(double average) =>
        (int)Math.Clamp(Math.Round(average, MidpointRounding.AwayFromZero), 0, 100);
}
