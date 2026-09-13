using System;
using System.Linq;
using System.Threading.Tasks;
using HCS.OrganizationUnits;

namespace HCS.Blazor.Client.Pages.Organization;

public partial class OrganizationUnitCatalog
{
    private async Task OpenMoveAllAsync(OrganizationUnitTreeNode node)
    {
        actionUnitId = node.Id;
        var firstTarget = MoveAllTargetOptions.FirstOrDefault();
        if (firstTarget is null)
        {
            errorMessage = L["OrganizationUnit:NoMoveTarget"].Value;
            return;
        }

        moveAllTargetValue = firstTarget.Id.ToString("D");
        if (moveAllModal is not null) await moveAllModal.Show();
    }

    private async Task MoveAllMembersAsync()
    {
        if (!Guid.TryParse(moveAllTargetValue, out var targetId)) return;
        isSaving = true;
        try
        {
            await CatalogClient.MoveAllMembersAsync(actionUnitId!.Value, targetId);
            await CloseMoveAllModalAsync();
            await NotifySuccessAsync(L["OrganizationUnit:MembersMoved"]);
            await LoadMembersAsync();
        }
        catch (Exception exception)
        {
            errorMessage = exception is OrganizationUnitApiException
                ? L["OrganizationUnit:MoveError"].Value
                : L["OrganizationUnit:NetworkError"].Value;
        }
        finally
        {
            isSaving = false;
        }
    }

    private async Task AddMemberAsync()
    {
        if (selectedNode is null || !Guid.TryParse(selectedAvailableMemberValue, out var userId)) return;
        isSaving = true;
        try
        {
            await CatalogClient.AddMemberAsync(selectedNode.Id, userId);
            await CloseMemberModalAsync();
            await NotifySuccessAsync(L["OrganizationUnit:MemberAdded"]);
            await LoadMembersAsync();
        }
        catch (Exception exception)
        {
            errorMessage = exception is OrganizationUnitApiException
                ? L["OrganizationUnit:MemberError"].Value
                : L["OrganizationUnit:NetworkError"].Value;
        }
        finally
        {
            isSaving = false;
        }
    }

    private async Task RemoveMemberAsync(OrganizationUnitMemberDto member)
    {
        if (selectedNode is null || !await UiMessageService.Confirm(L["OrganizationUnit:RemoveConfirmation"])) return;
        isSaving = true;
        try
        {
            await CatalogClient.RemoveMemberAsync(selectedNode.Id, member.Id);
            await NotifySuccessAsync(L["OrganizationUnit:MemberRemoved"]);
            await LoadMembersAsync();
        }
        catch (Exception exception)
        {
            errorMessage = exception is OrganizationUnitApiException
                ? L["OrganizationUnit:MemberError"].Value
                : L["OrganizationUnit:NetworkError"].Value;
        }
        finally
        {
            isSaving = false;
        }
    }

    private async Task CloseUnitModalAsync() { if (unitModal is not null) await unitModal.Hide(); }
    private async Task CloseMoveModalAsync() { if (moveModal is not null) await moveModal.Hide(); }
    private async Task CloseMoveAllModalAsync() { if (moveAllModal is not null) await moveAllModal.Hide(); }
    private async Task CloseMemberModalAsync() { if (memberModal is not null) await memberModal.Hide(); }
}
