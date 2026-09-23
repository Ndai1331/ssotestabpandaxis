using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;

namespace HCS.Identity;

/// <summary>
/// ABP's default user filter matches Name/Surname with case-sensitive Contains.
/// PostgreSQL does not match "huỳnh" to "Huỳnh", and a full Vietnamese display name
/// (Surname + Name) never matches either column alone.
/// </summary>
[Dependency(ReplaceServices = true)]
[ExposeServices(typeof(IIdentityUserRepository), typeof(EfCoreIdentityUserRepository), typeof(HcsIdentityUserRepository))]
public class HcsIdentityUserRepository : EfCoreIdentityUserRepository
{
    public HcsIdentityUserRepository(IDbContextProvider<IIdentityDbContext> dbContextProvider)
        : base(dbContextProvider)
    {
    }

    protected override async Task<IQueryable<IdentityUser>> GetFilteredQueryableAsync(
        string? filter = null,
        Guid? roleId = null,
        Guid? organizationUnitId = null,
        Guid? id = null,
        string? userName = null,
        string? phoneNumber = null,
        string? emailAddress = null,
        string? name = null,
        string? surname = null,
        bool? isLockedOut = null,
        bool? notActive = null,
        bool? emailConfirmed = null,
        bool? isExternal = null,
        DateTime? maxCreationTime = null,
        DateTime? minCreationTime = null,
        DateTime? maxModifitionTime = null,
        DateTime? minModifitionTime = null,
        CancellationToken cancellationToken = default)
    {
        var query = await base.GetFilteredQueryableAsync(
            filter: null,
            roleId,
            organizationUnitId,
            id,
            userName,
            phoneNumber,
            emailAddress,
            name,
            surname,
            isLockedOut,
            notActive,
            emailConfirmed,
            isExternal,
            maxCreationTime,
            minCreationTime,
            maxModifitionTime,
            minModifitionTime,
            cancellationToken);

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
