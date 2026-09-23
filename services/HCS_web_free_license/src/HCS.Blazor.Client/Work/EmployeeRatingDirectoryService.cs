using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using HCS.Blazor.Client.Pages.Organization;
using HCS.Blazor.Client.Services;

namespace HCS.Blazor.Client.Work;

public sealed class EmployeeRatingPerson
{
    public required EmployeeDirectoryUserDto User { get; init; }
    public string DepartmentName { get; init; } = string.Empty;
    public EmployeeRatingSummaryDto Summary { get; set; } = EmptySummary(Guid.Empty);
    public bool IsSubmitting { get; set; }
    public bool AvatarFailed { get; set; }
    public int? HoverScore { get; set; }
    public bool HasDepartment => !string.IsNullOrWhiteSpace(DepartmentName);

    public string Initials => string.Join("", User.DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .TakeLast(2).Select(x => x[0])).ToUpperInvariant();

    internal static EmployeeRatingSummaryDto EmptySummary(Guid userId) =>
        new(userId, 0, 0, Enumerable.Range(1, 5).ToDictionary(x => x, _ => 0));
}

public sealed record EmployeeRatingPeoplePage(List<EmployeeRatingPerson> People, long TotalCount);

public sealed class EmployeeRatingDirectoryService(
    EmployeeRatingDirectoryClient directoryClient,
    OrganizationUnitCatalogClient organizationUnits)
{
    public Task<IReadOnlyList<DepartmentCatalogDto>> GetDepartmentsAsync(
        CancellationToken cancellationToken = default) =>
        organizationUnits.GetDepartmentCatalogAsync(cancellationToken);

    public async Task<EmployeeRatingPeoplePage> GetPeoplePageAsync(
        string? search,
        int skip,
        int take,
        Guid? excludeUserId = null,
        Guid? organizationUnitId = null,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 100);
        skip = Math.Max(0, skip);
        if (organizationUnitId is { } unitId)
        {
            var members = await organizationUnits.GetMembersAsync(
                unitId, search, skip, take, cancellationToken);
            var ids = members.Items.Where(member => member.IsActive).Select(member => member.Id);
            if (excludeUserId is { } excludedMember)
                ids = ids.Where(id => id != excludedMember);
            return new EmployeeRatingPeoplePage(
                await GetPeopleByIdsAsync(ids, cancellationToken),
                members.TotalCount);
        }

        var requestTake = excludeUserId.HasValue ? Math.Min(100, take + 1) : take;
        var page = await directoryClient.GetPageAsync(search, skip, requestTake, cancellationToken);
        var users = page.Items.AsEnumerable();
        if (excludeUserId is { } excluded)
            users = users.Where(user => user.UserId != excluded);
        var people = await AttachDepartmentsAsync(users.Take(take).ToList(), cancellationToken);
        var total = page.TotalCount;
        if (excludeUserId.HasValue && total > 0)
            total -= 1;
        return new EmployeeRatingPeoplePage(people, Math.Max(0, total));
    }

    public async Task<EmployeeRatingPerson?> GetPersonAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await directoryClient.GetAsync(userId, cancellationToken);
            var people = await AttachDepartmentsAsync([user], cancellationToken);
            return people.Count == 0 ? null : people[0];
        }
        catch (BffApiException exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<List<EmployeeRatingPerson>> GetPeopleByIdsAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds.Where(id => id != Guid.Empty).Distinct().ToArray();
        if (ids.Length == 0)
            return [];

        var users = new List<EmployeeDirectoryUserDto>();
        foreach (var chunk in ids.Chunk(200))
            users.AddRange((await directoryClient.GetByIdsAsync(chunk, cancellationToken)).Items);

        var people = await AttachDepartmentsAsync(users, cancellationToken);
        var map = people.ToDictionary(person => person.User.UserId);
        return ids.Where(map.ContainsKey).Select(id => map[id]).ToList();
    }

    private async Task<List<EmployeeRatingPerson>> AttachDepartmentsAsync(
        IReadOnlyList<EmployeeDirectoryUserDto> users,
        CancellationToken cancellationToken)
    {
        var departments = new Dictionary<Guid, UserDepartmentLookupDto>();
        foreach (var chunk in users.Select(x => x.UserId).Chunk(200))
        {
            foreach (var department in await organizationUnits.GetUserDepartmentsAsync(chunk, cancellationToken))
                departments[department.UserId] = department;
        }

        return users.Select(user => new EmployeeRatingPerson
        {
            User = user,
            DepartmentName = departments.TryGetValue(user.UserId, out var department)
                ? department.DepartmentName?.Trim() ?? string.Empty
                : string.Empty
        }).ToList();
    }
}
