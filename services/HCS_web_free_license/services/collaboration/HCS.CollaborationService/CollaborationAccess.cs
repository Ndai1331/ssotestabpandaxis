using System.Security.Claims;
using HCS.CollaborationService.Contracts;

namespace HCS.CollaborationService;

public static class CollaborationAccess
{
    public const string WorkspaceDashboard = "WorkManagement.Dashboard";

    public static bool HasPermission(ClaimsPrincipal? user, string permission) =>
        user is not null && user.HasClaim("permission", permission);

    public static bool CanUseRealtime(ClaimsPrincipal? user) =>
        HasPermission(user, CollaborationPermissions.Notifications)
        || HasPermission(user, CollaborationPermissions.Social)
        || HasPermission(user, CollaborationPermissions.Chat)
        || HasPermission(user, WorkspaceDashboard);
}
