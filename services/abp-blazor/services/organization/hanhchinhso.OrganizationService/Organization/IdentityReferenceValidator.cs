using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;

namespace hanhchinhso.OrganizationService.Organization;

public interface IIdentityReferenceValidator
{
    Task EnsureUserExistsAsync(Guid userId);
}

public class IdentityReferenceValidator : IIdentityReferenceValidator, ITransientDependency
{
    private readonly IIdentityUserAppService _identityUsers;

    public IdentityReferenceValidator(IIdentityUserAppService identityUsers)
    {
        _identityUsers = identityUsers;
    }

    public async Task EnsureUserExistsAsync(Guid userId)
    {
        await _identityUsers.GetAsync(userId);
    }
}
