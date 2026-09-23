using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;

namespace HCS.Identity;

/// <summary>
/// ABP's organization-unit member filter uses case-sensitive Contains on user name,
/// email, and given name. PostgreSQL then misses "huỳnh" for "Huỳnh", and a
/// Vietnamese display name (Surname + Name) never matches either column alone.
/// </summary>
[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(IOrganizationUnitRepository), typeof(EfCoreOrganizationUnitRepository), typeof(HcsOrganizationUnitRepository))]
public class HcsOrganizationUnitRepository : EfCoreOrganizationUnitRepository
{
    public HcsOrganizationUnitRepository(IDbContextProvider<IIdentityDbContext> dbContextProvider)
        : base(dbContextProvider)
    {
    }

    public override async Task<List<IdentityUser>> GetUnaddedUsersAsync(
        OrganizationUnit organizationUnit,
        string? sorting = null,
        int maxResultCount = int.MaxValue,
        int skipCount = 0,
        string? filter = null,
        bool includeDetails = false,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyPersonFilter(await CreateUnaddedUsersQueryAsync(organizationUnit), filter)
            .IncludeDetails(includeDetails);
        return await OrderUsers(query, sorting)
            .Skip(skipCount)
            .Take(maxResultCount)
            .ToListAsync(GetCancellationToken(cancellationToken));
    }

    public override async Task<int> GetUnaddedUsersCountAsync(
        OrganizationUnit organizationUnit,
        string? filter = null,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyPersonFilter(await CreateUnaddedUsersQueryAsync(organizationUnit), filter);
        return await query.CountAsync(GetCancellationToken(cancellationToken));
    }

    protected override async Task<IQueryable<IdentityUser>> CreateGetMembersFilteredQueryAsync(
        OrganizationUnit organizationUnit,
        string? filter = null,
        bool includeChildren = false)
    {
        var query = await base.CreateGetMembersFilteredQueryAsync(organizationUnit, filter: null, includeChildren);
        return ApplyPersonFilter(query, filter);
    }

    private async Task<IQueryable<IdentityUser>> CreateUnaddedUsersQueryAsync(OrganizationUnit organizationUnit)
    {
        var dbContext = await GetDbContextAsync();
        var memberIds = dbContext.Set<IdentityUserOrganizationUnit>()
            .Where(link => link.OrganizationUnitId == organizationUnit.Id)
            .Select(link => link.UserId);
        return dbContext.Users.Where(user => !memberIds.Contains(user.Id));
    }

    private static IQueryable<IdentityUser> OrderUsers(IQueryable<IdentityUser> query, string? sorting)
    {
        if (string.Equals(sorting, nameof(IdentityUser.UserName), StringComparison.OrdinalIgnoreCase))
        {
            return query.OrderBy(user => user.UserName);
        }

        if (string.Equals(sorting, nameof(IdentityUser.Email), StringComparison.OrdinalIgnoreCase))
        {
            return query.OrderBy(user => user.Email);
        }

        if (string.Equals(sorting, nameof(IdentityUser.Surname), StringComparison.OrdinalIgnoreCase))
        {
            return query.OrderBy(user => user.Surname).ThenBy(user => user.UserName);
        }

        return query.OrderBy(user => user.Name).ThenBy(user => user.UserName);
    }

    internal static IQueryable<IdentityUser> ApplyPersonFilter(IQueryable<IdentityUser> query, string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return query;
        }

        var term = filter.Trim().ToLower();
        var upper = term.ToUpperInvariant();
        return query.Where(user =>
            user.NormalizedUserName.Contains(upper) ||
            user.NormalizedEmail.Contains(upper) ||
            (user.PhoneNumber != null && user.PhoneNumber.ToLower().Contains(term)) ||
            (user.Name != null && user.Name.ToLower().Contains(term)) ||
            (user.Surname != null && user.Surname.ToLower().Contains(term)) ||
            ((user.Surname ?? string.Empty) + " " + (user.Name ?? string.Empty)).ToLower().Contains(term));
    }
}
