using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HCS.Blazor.Client.Pages.Organization;

namespace HCS.Blazor.Client.Work;

public sealed class EmployeeRatingPerson
{
    public required EmployeeDirectoryUserDto User { get; init; }
    public string DepartmentName { get; init; } = string.Empty;
    public EmployeeRatingSummaryDto Summary { get; set; } = EmptySummary(Guid.Empty);
    public bool IsSubmitting { get; set; }
    public bool AvatarFailed { get; set; }
    public int? HoverScore { get; set; }

    public string Initials => string.Join("", User.DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .TakeLast(2).Select(x => x[0])).ToUpperInvariant();

    private static EmployeeRatingSummaryDto EmptySummary(Guid userId) =>
        new(userId, 0, 0, Enumerable.Range(1, 5).ToDictionary(x => x, _ => 0));
}

public sealed class EmployeeRatingDirectoryService(
    EmployeeRatingDirectoryClient directoryClient,
    OrganizationCatalogClient organizationClient)
{
    public async Task<List<EmployeeRatingPerson>> GetPeopleAsync(CancellationToken cancellationToken = default)
    {
        var users = await directoryClient.GetAllAsync(cancellationToken);
        var departments = new Dictionary<Guid, UserDepartmentLookupDto>();
        foreach (var chunk in users.Select(x => x.UserId).Chunk(200))
        {
            foreach (var department in await organizationClient.GetUserDepartmentsAsync(chunk, cancellationToken))
                departments[department.UserId] = department;
        }

        return users.Select(user => new EmployeeRatingPerson
        {
            User = user,
            DepartmentName = departments.TryGetValue(user.UserId, out var department)
                ? department.DepartmentName ?? "—" : "—"
        }).ToList();
    }
}
