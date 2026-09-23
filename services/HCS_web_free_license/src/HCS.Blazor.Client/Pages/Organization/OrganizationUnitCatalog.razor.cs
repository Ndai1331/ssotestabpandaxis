using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Blazorise;
using HCS.Blazor.Client.Components;
using HCS.Permissions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace HCS.Blazor.Client.Pages.Organization;

public partial class OrganizationUnitCatalog : IDisposable
{
    private const int MemberPageSize = 20;

    [Inject] private OrganizationUnitCatalogClient CatalogClient { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

    private readonly List<OrganizationUnitTreeNode> rootNodes = [];
    private readonly List<HCS.OrganizationUnits.OrganizationUnitMemberDto> members = [];
    private readonly List<HCS.OrganizationUnits.OrganizationUnitMemberDto> availableMembers = [];
    private OrganizationUnitTreeNode? selectedNode;
    private CancellationTokenSource? loadCancellation;
    private Modal? unitModal;
    private Modal? moveModal;
    private Modal? moveAllModal;
    private Modal? memberModal;
    private OrganizationUnitFormModel unitForm = new();
    private OrganizationUnitDialogMode unitDialogMode;
    private Guid? actionUnitId;
    private Guid? openMenuId;
    private string memberFilter = string.Empty;
    private string moveTargetValue = string.Empty;
    private string moveAllTargetValue = string.Empty;
    private Guid? selectedAvailableMemberId;
    private string? errorMessage;
    private bool isAuthorized;
    private bool canCreate;
    private bool canUpdate;
    private bool canDelete;
    private bool isLoading;
    private bool isMembersLoading;
    private bool isSaving;
    private bool hasLoaded;
    private int currentMemberPage = 1;
    private long totalMembers;

    private Guid? selectedId => selectedNode?.Id;
    private int TotalMemberPages => Math.Max(1, (int)Math.Ceiling(totalMembers / (double)MemberPageSize));
    private string UnitModalTitle => unitDialogMode == OrganizationUnitDialogMode.Edit
        ? L["OrganizationUnit:Edit"].Value
        : L["OrganizationUnit:AddSubUnit"].Value;

    private IEnumerable<OrganizationUnitTreeNode> allNodes => OrganizationUnitTreeBuilder.Flatten(rootNodes);

    private Guid? MoveTargetId => Guid.TryParse(moveTargetValue, out var id) ? id : null;
    private void OnMoveTargetChanged(Guid? value) => moveTargetValue = value?.ToString("D") ?? string.Empty;
    private void OnMoveAllTargetChanged(Guid? value) => moveAllTargetValue = value?.ToString("D") ?? string.Empty;
    private Guid? MoveAllTargetId => Guid.TryParse(moveAllTargetValue, out var id) ? id : null;
    private string MoveTargetText(Guid? id) => allNodes.FirstOrDefault(node => node.Id == id)?.DisplayName ?? "";

    private Task<CatalogSelect2SearchResponse> SearchMoveTargetsAsync(string term, int page) =>
        SearchMoveTargetsAsync(MoveTargetOptions, term);

    private Task<CatalogSelect2SearchResponse> SearchMoveAllTargetsAsync(string term, int page) =>
        SearchMoveTargetsAsync(MoveAllTargetOptions, term);

    private static Task<CatalogSelect2SearchResponse> SearchMoveTargetsAsync(
        IEnumerable<OrganizationUnitTreeNode> options, string term)
    {
        var query = CatalogSelect2Text.NormalizeSearch(term);
        var items = options.Where(node => query.Length == 0
                || CatalogSelect2Text.NormalizeSearch(node.DisplayName).Contains(query, StringComparison.Ordinal)
                || CatalogSelect2Text.NormalizeSearch(node.Code).Contains(query, StringComparison.Ordinal))
            .Select(node => new CatalogSelect2Item(node.Id.ToString("D"), node.DisplayName))
            .ToList();
        return Task.FromResult(new CatalogSelect2SearchResponse(items, false));
    }

    private IEnumerable<OrganizationUnitTreeNode> MoveTargetOptions => allNodes
        .Where(node => selectedNode is null || (node.Id != selectedNode.Id && !IsDescendant(node)))
        .OrderBy(node => node.Code, StringComparer.Ordinal);

    private IEnumerable<OrganizationUnitTreeNode> MoveAllTargetOptions => allNodes
        .Where(node => selectedNode is null || node.Id != selectedNode.Id)
        .OrderBy(node => node.Code, StringComparer.Ordinal);

    protected override async Task OnInitializedAsync()
    {
        var user = (await AuthenticationStateProvider.GetAuthenticationStateAsync()).User;
        isAuthorized = await IsGrantedAsync(user, HCSOrganizationPermissions.Departments);
        if (!isAuthorized)
        {
            return;
        }

        canCreate = await IsGrantedAsync(user, HcsCrudPermissions.Create(HCSOrganizationPermissions.Departments));
        canUpdate = await IsGrantedAsync(user, HcsCrudPermissions.Update(HCSOrganizationPermissions.Departments));
        canDelete = await IsGrantedAsync(user, HcsCrudPermissions.Delete(HCSOrganizationPermissions.Departments));
        await LoadTreeAsync();
    }

    public void Dispose()
    {
        loadCancellation?.Cancel();
        loadCancellation?.Dispose();
    }

    private Task<bool> IsGrantedAsync(System.Security.Claims.ClaimsPrincipal user, string permission) =>
        IsGrantedCoreAsync(user, permission);

    private async Task<bool> IsGrantedCoreAsync(System.Security.Claims.ClaimsPrincipal user, string permission)
    {
        return (await AuthorizationService.AuthorizeAsync(user, null, permission)).Succeeded;
    }

    private bool IsDescendant(OrganizationUnitTreeNode candidate)
    {
        return selectedNode is not null
            && OrganizationUnitTreeBuilder.Flatten(selectedNode.Children).Any(node => node.Id == candidate.Id);
    }

    private static string MemberRolesText(HCS.OrganizationUnits.OrganizationUnitMemberDto member) =>
        member.RoleNames is { Length: > 0 }
            ? string.Join(", ", member.RoleNames)
            : "—";
}

internal enum OrganizationUnitDialogMode
{
    CreateRoot,
    CreateChild,
    Edit
}
