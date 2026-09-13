using System.Security.Claims;
using Volo.Abp.Authorization;

namespace HCS.DocumentService.Documents;

internal static class DocumentAccess
{
    public const string CreatedAction = "Created";
    private static readonly string[] ElevatedRoles = ["admin", "lanhdao"];

    public static Guid RequireUser(ClaimsPrincipal? principal)
    {
        var userIdValue = principal?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal?.FindFirstValue("sub");
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            throw new AbpAuthorizationException("An authenticated user is required.");
        }

        return userId;
    }

    public static void RequirePermission(ClaimsPrincipal principal, string permission)
    {
        if (!HasPermission(principal, permission))
        {
            throw new AbpAuthorizationException($"Permission '{permission}' is required.");
        }
    }

    public static bool HasPermission(ClaimsPrincipal principal, string permission)
    {
        if (HasGrant(principal, permission))
            return true;

        return permission switch
        {
            DocumentPermissions.View or DocumentPermissions.Create or DocumentPermissions.Update or
                DocumentPermissions.ManageFiles or DocumentPermissions.WorkflowView or
                DocumentPermissions.SigningConfigure => principal.Identity?.IsAuthenticated == true,
            DocumentPermissions.Assign or DocumentPermissions.WorkflowStart or
                DocumentPermissions.SigningReport => principal.IsInRole("lanhdao"),
            DocumentPermissions.WorkflowDecide or DocumentPermissions.SigningExecute =>
                principal.IsInRole("lanhdao") || principal.IsInRole("bacsi"),
            "Documents.Review" => principal.IsInRole("lanhdao") || principal.IsInRole("bacsi"),
            "Documents.Approve" => principal.IsInRole("lanhdao"),
            _ => false
        };
    }

    public static bool HasGrant(ClaimsPrincipal principal, string permission) =>
        principal.IsInRole("admin") || principal.Claims.Any(claim =>
            (claim.Type is "permission" or "permissions") && claim.Value == permission);

    public static bool IsElevated(ClaimsPrincipal principal) => ElevatedRoles.Any(principal.IsInRole);

    public static bool IsCreator(DocumentAggregate document, Guid userId) =>
        document.History.Any(x => x.Action == CreatedAction && x.ActorUserId == userId);

    public static bool HasInboxView(DocumentAggregate document, Guid userId) =>
        document.Assignments.Any(x => x.AssigneeUserId == userId && x.IsCurrent &&
            x.Responsibility == "VIEW" && x.StepCode == null);

    public static IQueryable<DocumentAggregate> FilterBySource(
        IQueryable<DocumentAggregate> query, int? sourceType, Guid userId, bool mine, ClaimsPrincipal principal) =>
        sourceType switch
        {
            // Quản lý tài liệu: everyone who can open the menu sees the full archive.
            0 => query.Where(x => x.SourceType == DocumentSourceType.Archive),
            // Văn bản của tôi: only documents this user created.
            1 => query.Where(x => x.SourceType == DocumentSourceType.Personal &&
                                  x.History.Any(h => h.Action == CreatedAction && h.ActorUserId == userId)),
            // Văn bản gửi đến tôi: current inbox VIEW assignments.
            2 => query.Where(x => x.Assignments.Any(a => a.AssigneeUserId == userId && a.IsCurrent &&
                                  a.Responsibility == "VIEW" && a.StepCode == null)),
            3 when !IsElevated(principal) =>
                query.Where(x => x.SourceType == DocumentSourceType.Workflow &&
                                 (x.Assignments.Any(a => a.AssigneeUserId == userId) ||
                                  x.History.Any(h => h.Action == CreatedAction && h.ActorUserId == userId))),
            3 => query.Where(x => x.SourceType == DocumentSourceType.Workflow),
            _ when mine || !IsElevated(principal) =>
                query.Where(x => x.Assignments.Any(a => a.AssigneeUserId == userId) ||
                                 x.History.Any(h => h.Action == CreatedAction && h.ActorUserId == userId)),
            _ => query
        };

    public static bool CanView(DocumentAggregate document, Guid userId, ClaimsPrincipal principal)
    {
        if (document.SourceType == DocumentSourceType.Archive)
            return HasPermission(principal, DocumentPermissions.View);
        if (IsCreator(document, userId) || HasInboxView(document, userId))
            return true;
        if (document.SourceType == DocumentSourceType.Workflow)
            return IsElevated(principal) || document.Assignments.Any(x => x.AssigneeUserId == userId);
        return false;
    }

    public static bool CanManage(DocumentAggregate document, Guid userId, ClaimsPrincipal principal)
    {
        if (IsCreator(document, userId)) return true;
        if (document.SourceType == DocumentSourceType.Personal) return false;
        if (document.SourceType == DocumentSourceType.Archive)
            return IsElevated(principal)
                || HasGrant(principal, DocumentPermissions.Update)
                || HasGrant(principal, DocumentPermissions.Assign);
        return IsElevated(principal);
    }

    public static bool CanSend(DocumentAggregate document, Guid userId, ClaimsPrincipal principal) =>
        CanManage(document, userId, principal) || HasInboxView(document, userId);

    public static bool CanInboxViewAssign(DocumentAggregate document, Guid userId, string responsibility) =>
        HasInboxView(document, userId)
        && string.Equals(responsibility, "VIEW", StringComparison.OrdinalIgnoreCase);

    public static void EnsureCanView(DocumentAggregate document, Guid userId, ClaimsPrincipal principal)
    {
        if (!CanView(document, userId, principal)) throw new AbpAuthorizationException("Document access denied.");
    }

    public static void EnsureCanManage(DocumentAggregate document, Guid userId, ClaimsPrincipal principal)
    {
        if (!CanManage(document, userId, principal)) throw new AbpAuthorizationException("Document modification denied.");
    }

    public static void EnsureCanSend(DocumentAggregate document, Guid userId, ClaimsPrincipal principal)
    {
        if (!CanSend(document, userId, principal)) throw new AbpAuthorizationException("Document send denied.");
    }
}
