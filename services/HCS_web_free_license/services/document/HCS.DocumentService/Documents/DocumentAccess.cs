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
            DocumentPermissions.View or DocumentPermissions.WorkflowView or
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

    public static bool CanActOnWorkflowTask(ClaimsPrincipal principal, Guid userId, Guid? assigneeUserId)
    {
        if (IsElevated(principal))
            return true;
        return assigneeUserId is { } assignee && assignee == userId;
    }

    public static void EnsureCanActOnWorkflowTask(ClaimsPrincipal principal, Guid userId, Guid? assigneeUserId)
    {
        if (!CanActOnWorkflowTask(principal, userId, assigneeUserId))
            throw new UnauthorizedAccessException("Only the assigned user can act on this workflow step.");
    }

    public static void EnsureCanDecideStep(ClaimsPrincipal principal, bool isSignStep, string requiredPermission) =>
        RequirePermission(principal, isSignStep ? DocumentPermissions.SigningExecute : requiredPermission);

    public static bool IsCreator(DocumentAggregate document, Guid userId) =>
        document.History.Any(x => x.Action == CreatedAction && x.ActorUserId == userId);

    public static bool HasInboxView(DocumentAggregate document, Guid userId) =>
        document.Assignments.Any(x => x.AssigneeUserId == userId && x.IsCurrent &&
            x.Responsibility == "VIEW" && x.StepCode == null);

    public static IQueryable<DocumentAggregate> FilterBySource(
        IQueryable<DocumentAggregate> query, int? sourceType, Guid userId, bool mine, ClaimsPrincipal principal) =>
        sourceType switch
        {
            // Quản lý tài liệu: only văn thư with Documents.View (Truy cập).
            0 when CanBrowseArchive(principal) => query.Where(x => x.SourceType == DocumentSourceType.Archive),
            0 => query.Where(x => false),
            // Văn bản tôi tạo: only documents this user created.
            1 => query.Where(x => x.SourceType == DocumentSourceType.Personal &&
                                  x.History.Any(h => h.Action == CreatedAction && h.ActorUserId == userId)),
            // Văn bản của tôi: documents currently sent to this user.
            2 => query.Where(x => x.Assignments.Any(a => a.AssigneeUserId == userId && a.IsCurrent &&
                                  a.Responsibility == "VIEW" && a.StepCode == null)),
            3 => query.Where(x => x.SourceType == DocumentSourceType.Workflow &&
                                 (x.Assignments.Any(a => a.AssigneeUserId == userId) ||
                                  x.History.Any(h => h.Action == CreatedAction && h.ActorUserId == userId) ||
                                  x.FromUserId == userId)),
            _ => query.Where(x => x.Assignments.Any(a => a.AssigneeUserId == userId) ||
                                 x.History.Any(h => h.Action == CreatedAction && h.ActorUserId == userId))
        };

    public static bool CanBrowseArchive(ClaimsPrincipal principal) =>
        HasGrant(principal, DocumentPermissions.View);

    public static void EnsureCanBrowseArchive(ClaimsPrincipal principal)
    {
        if (!CanBrowseArchive(principal))
            throw new AbpAuthorizationException($"Permission '{DocumentPermissions.View}' is required.");
    }

    public static bool OwnsPersonal(DocumentAggregate document, bool isCreator) =>
        document.SourceType == DocumentSourceType.Personal && isCreator;

    public static void EnsureCanCreate(ClaimsPrincipal principal, DocumentSourceType sourceType)
    {
        if (sourceType == DocumentSourceType.Archive)
            RequireGrant(principal, DocumentPermissions.Create);
    }

    public static void EnsureCanMutate(ClaimsPrincipal principal, DocumentAggregate document, bool isCreator, string permission)
    {
        if (OwnsPersonal(document, isCreator))
            return;
        RequireGrant(principal, permission);
    }

    public static void RequireGrant(ClaimsPrincipal principal, string permission)
    {
        if (!HasGrant(principal, permission))
            throw new AbpAuthorizationException($"Permission '{permission}' is required.");
    }

    public static bool CanView(DocumentAggregate document, Guid userId, ClaimsPrincipal principal)
    {
        if (IsCreator(document, userId) || HasInboxView(document, userId))
            return true;
        if (document.SourceType == DocumentSourceType.Archive)
            return CanBrowseArchive(principal);
        if (document.SourceType == DocumentSourceType.Workflow)
            return IsElevated(principal) || document.Assignments.Any(x => x.AssigneeUserId == userId);
        return false;
    }

    public static bool CanManage(DocumentAggregate document, Guid userId, ClaimsPrincipal principal) =>
        CanManage(document, userId, principal, IsCreator(document, userId));

    public static bool CanManage(DocumentAggregate document, Guid userId, ClaimsPrincipal principal, bool isCreator)
    {
        if (isCreator) return true;
        if (document.SourceType == DocumentSourceType.Personal) return false;
        if (document.SourceType == DocumentSourceType.Archive)
            return HasGrant(principal, DocumentPermissions.Update)
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

    public static void EnsureCanManage(DocumentAggregate document, Guid userId, ClaimsPrincipal principal) =>
        EnsureCanManage(document, userId, principal, IsCreator(document, userId));

    public static void EnsureCanManage(DocumentAggregate document, Guid userId, ClaimsPrincipal principal, bool isCreator)
    {
        if (!CanManage(document, userId, principal, isCreator))
            throw new AbpAuthorizationException("Document modification denied.");
    }

    public static void EnsureCanSend(DocumentAggregate document, Guid userId, ClaimsPrincipal principal)
    {
        if (!CanSend(document, userId, principal)) throw new AbpAuthorizationException("Document send denied.");
    }
}
