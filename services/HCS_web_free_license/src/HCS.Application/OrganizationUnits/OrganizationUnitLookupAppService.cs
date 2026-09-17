using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Identity;
using Ou = Volo.Abp.Identity.OrganizationUnit;

namespace HCS.OrganizationUnits;

[Authorize]
public class OrganizationUnitLookupAppService : HCSAppService, IOrganizationUnitLookupAppService
{
    private const int MaxTreeItems = 5000;
    private const int MaxUserIds = 200;

    private readonly IOrganizationUnitRepository organizationUnitRepository;
    private readonly IIdentityUserRepository identityUserRepository;
    private readonly IdentityUserManager identityUserManager;

    public OrganizationUnitLookupAppService(
        IOrganizationUnitRepository organizationUnitRepository,
        IIdentityUserRepository identityUserRepository,
        IdentityUserManager identityUserManager)
    {
        this.organizationUnitRepository = organizationUnitRepository;
        this.identityUserRepository = identityUserRepository;
        this.identityUserManager = identityUserManager;
    }

    public virtual async Task<List<OrganizationUnitDto>> GetListAsync()
    {
        var units = await organizationUnitRepository.GetListAsync(
            nameof(Ou.Code), MaxTreeItems, 0, includeDetails: false);
        return units.Select(Map).ToList();
    }

    public virtual async Task<IReadOnlyList<UserOrganizationUnitLookupDto>> GetUsersAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds.Where(id => id != Guid.Empty).Distinct().Take(MaxUserIds).ToArray();
        if (ids.Length == 0)
        {
            return [];
        }

        var results = new List<UserOrganizationUnitLookupDto>(ids.Length);
        foreach (var userId in ids)
        {
            var units = (await identityUserRepository.GetOrganizationUnitsAsync(
                    userId, includeDetails: false, cancellationToken: cancellationToken))
                .OrderBy(unit => unit.Code, StringComparer.Ordinal)
                .ToList();
            if (units.Count == 0)
            {
                results.Add(new UserOrganizationUnitLookupDto { UserId = userId });
                continue;
            }

            var primary = units[0];
            results.Add(new UserOrganizationUnitLookupDto
            {
                UserId = userId,
                OrganizationUnitId = primary.Id,
                OrganizationUnitIds = units.Select(unit => unit.Id).ToArray(),
                DisplayName = string.Join(", ", units.Select(unit => unit.DisplayName))
            });
        }

        return results;
    }

    public virtual async Task<IReadOnlyList<UserOrganizationUnitLookupDto>> GetMembersAsync(
        Guid organizationUnitId,
        CancellationToken cancellationToken = default)
    {
        var unit = await organizationUnitRepository.GetAsync(organizationUnitId, cancellationToken: cancellationToken);
        var users = await identityUserRepository.GetUsersInOrganizationUnitAsync(
            organizationUnitId, cancellationToken);
        return users
            .Where(user => user.IsActive)
            .OrderBy(user => user.UserName, StringComparer.OrdinalIgnoreCase)
            .Select(user => new UserOrganizationUnitLookupDto
            {
                UserId = user.Id,
                OrganizationUnitId = unit.Id,
                DisplayName = unit.DisplayName
            })
            .ToArray();
    }

    [Authorize(IdentityPermissions.Users.Update)]
    public virtual async Task SetUserAsync(Guid userId, SetUserOrganizationUnitsInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var organizationUnitIds = (input.OrganizationUnitIds ?? [])
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();
        foreach (var organizationUnitId in organizationUnitIds)
        {
            await organizationUnitRepository.GetAsync(organizationUnitId);
        }

        await identityUserManager.SetOrganizationUnitsAsync(userId, organizationUnitIds);
    }

    private static OrganizationUnitDto Map(Ou unit) => new()
    {
        Id = unit.Id,
        ParentId = unit.ParentId,
        Code = unit.Code,
        DisplayName = unit.DisplayName,
        ConcurrencyStamp = unit.ConcurrencyStamp ?? string.Empty
    };
}
