using System.Threading;

namespace HCS.Identity;

/// <summary>
/// Carries GET /api/identity/users?notActive= across the Identity controller
/// into <see cref="IIdentityUserAppService.GetListAsync"/> (OSS input has no NotActive).
/// </summary>
public static class HcsIdentityUserListQuery
{
    private static readonly AsyncLocal<bool?> NotActiveLocal = new();

    public static bool? NotActive
    {
        get => NotActiveLocal.Value;
        set => NotActiveLocal.Value = value;
    }
}
