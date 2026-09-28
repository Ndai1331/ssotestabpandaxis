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

public sealed class UserOrganizationNames(IDbContextProvider<IIdentityDbContext> dbContextProvider)
    : IUserOrganizationNames, ITransientDependency
{
    public async Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        var ids = userIds.Where(id => id != Guid.Empty).Distinct().ToArray();
        if (ids.Length == 0)
            return new Dictionary<Guid, string>();

        var db = await dbContextProvider.GetDbContextAsync();
        var links = await db.Set<IdentityUserOrganizationUnit>()
            .AsNoTracking()
            .Where(link => ids.Contains(link.UserId))
            .Select(link => new { link.UserId, link.OrganizationUnitId })
            .ToListAsync(cancellationToken);
        if (links.Count == 0)
            return new Dictionary<Guid, string>();

        var unitIds = links.Select(link => link.OrganizationUnitId).Distinct().ToArray();
        var names = await db.OrganizationUnits
            .AsNoTracking()
            .Where(unit => unitIds.Contains(unit.Id))
            .Select(unit => new { unit.Id, unit.DisplayName })
            .ToDictionaryAsync(unit => unit.Id, unit => unit.DisplayName, cancellationToken);

        return links
            .GroupBy(link => link.UserId)
            .ToDictionary(
                group => group.Key,
                group => string.Join(" · ", group
                    .Select(link => names.GetValueOrDefault(link.OrganizationUnitId))
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)));
    }
}
