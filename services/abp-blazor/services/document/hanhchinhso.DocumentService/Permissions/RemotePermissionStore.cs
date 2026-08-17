using Volo.Abp.Authorization.Permissions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.PermissionManagement;
using Volo.Abp.Users;

namespace hanhchinhso.DocumentService.Permissions;

/// <summary>
/// Resolves permission grants through AdministrationService's integration API.
/// DocumentService keeps its database isolated and forwards the current
/// access token through the ABP HTTP client proxy.
/// </summary>
[Dependency(ReplaceServices = true)]
public class RemotePermissionStore : IPermissionStore, ITransientDependency
{
    private readonly IPermissionFinder _permissionFinder;
    private readonly ICurrentUser _currentUser;

    public RemotePermissionStore(
        IPermissionFinder permissionFinder,
        ICurrentUser currentUser)
    {
        _permissionFinder = permissionFinder;
        _currentUser = currentUser;
    }

    public async Task<bool> IsGrantedAsync(
        string name,
        string? providerName,
        string? providerKey)
    {
        var result = await FindAsync([name]);
        return result?.Permissions.GetValueOrDefault(name) == true;
    }

    public async Task<MultiplePermissionGrantResult> IsGrantedAsync(
        string[] names,
        string? providerName,
        string? providerKey)
    {
        var grantResult = new MultiplePermissionGrantResult(names);
        var result = await FindAsync(names);

        if (result == null)
        {
            return grantResult;
        }

        foreach (var name in names)
        {
            grantResult.Result[name] =
                result.Permissions.GetValueOrDefault(name)
                    ? PermissionGrantResult.Granted
                    : PermissionGrantResult.Prohibited;
        }

        return grantResult;
    }

    private async Task<IsGrantedResponse?> FindAsync(string[] permissionNames)
    {
        if (!_currentUser.Id.HasValue)
        {
            return null;
        }

        var responses = await _permissionFinder.IsGrantedAsync(
        [
            new IsGrantedRequest
            {
                UserId = _currentUser.Id.Value,
                PermissionNames = permissionNames
            }
        ]);

        return responses.FirstOrDefault(response => response.UserId == _currentUser.Id.Value);
    }
}
