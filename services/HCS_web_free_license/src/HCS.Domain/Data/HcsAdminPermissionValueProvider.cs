using System;
using System.Threading.Tasks;
using HCS.Permissions;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.DependencyInjection;

namespace HCS.Data;

/// <summary>
/// Matches the Blazor admin shell: the built-in <c>admin</c> role may use every
/// permission, including ones added after the role's persisted grants were last seeded.
/// </summary>
public sealed class HcsAdminPermissionValueProvider(IPermissionStore permissionStore)
    : PermissionValueProvider(permissionStore), ITransientDependency
{
    public const string ProviderName = "HCSAdminRole";

    public override string Name => ProviderName;

    public override Task<PermissionGrantResult> CheckAsync(PermissionValueCheckContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Task.FromResult(HcsAdministrator.Is(context.Principal)
            ? PermissionGrantResult.Granted
            : PermissionGrantResult.Undefined);
    }

    public override Task<MultiplePermissionGrantResult> CheckAsync(PermissionValuesCheckContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var result = new MultiplePermissionGrantResult();
        var granted = HcsAdministrator.Is(context.Principal);
        foreach (var permission in context.Permissions)
        {
            result.Result[permission.Name] = granted
                ? PermissionGrantResult.Granted
                : PermissionGrantResult.Undefined;
        }

        return Task.FromResult(result);
    }
}
