using System;
using System.Linq;
using System.Threading.Tasks;
using HCS.OrganizationUnits;

namespace HCS.Blazor.Client.Pages.Organization;

public partial class OrganizationUnitCatalog
{
    private Task HandleActionAsync(OrganizationUnitActionRequest request) => request.Action switch
    {
        OrganizationUnitAction.AddSubUnit => OpenCreateChildAsync(request.Node),
        OrganizationUnitAction.Edit => OpenEditAsync(request.Node),
        OrganizationUnitAction.Move => OpenMoveAsync(request.Node),
        OrganizationUnitAction.Delete => DeleteAsync(request.Node),
        OrganizationUnitAction.MoveAllMembers => OpenMoveAllAsync(request.Node),
        _ => Task.CompletedTask
    };

    private async Task OpenCreateRootAsync()
    {
        actionUnitId = null;
        unitDialogMode = OrganizationUnitDialogMode.CreateRoot;
        unitForm = new();
        if (unitModal is not null) await unitModal.Show();
    }

    private async Task OpenCreateChildAsync(OrganizationUnitTreeNode node)
    {
        actionUnitId = node.Id;
        unitDialogMode = OrganizationUnitDialogMode.CreateChild;
        unitForm = new() { ParentId = node.Id };
        if (unitModal is not null) await unitModal.Show();
    }

    private async Task OpenEditAsync(OrganizationUnitTreeNode node)
    {
        actionUnitId = node.Id;
        unitDialogMode = OrganizationUnitDialogMode.Edit;
        unitForm = new() { DisplayName = node.DisplayName };
        if (unitModal is not null) await unitModal.Show();
    }

    private async Task SaveUnitAsync()
    {
        if (string.IsNullOrWhiteSpace(unitForm.DisplayName))
        {
            errorMessage = L["OrganizationUnit:NameRequired"].Value;
            return;
        }

        isSaving = true;
        try
        {
            var result = unitDialogMode == OrganizationUnitDialogMode.Edit
                ? await CatalogClient.UpdateAsync(actionUnitId!.Value, unitForm.DisplayName)
                : await CatalogClient.CreateAsync(
                    unitForm.DisplayName,
                    unitDialogMode == OrganizationUnitDialogMode.CreateChild ? actionUnitId : null);
            await CloseUnitModalAsync();
            await NotifySuccessAsync(L["OrganizationUnit:Saved"]);
            await LoadTreeAsync(result.Id);
        }
        catch (Exception exception)
        {
            errorMessage = exception is OrganizationUnitApiException
                ? L["OrganizationUnit:SaveError"].Value
                : L["OrganizationUnit:NetworkError"].Value;
        }
        finally
        {
            isSaving = false;
        }
    }

    private async Task OpenMoveAsync(OrganizationUnitTreeNode node)
    {
        actionUnitId = node.Id;
        moveTargetValue = node.ParentId?.ToString("D") ?? string.Empty;
        if (moveModal is not null) await moveModal.Show();
    }

    private async Task MoveUnitAsync()
    {
        if (!Guid.TryParse(moveTargetValue, out var parsedTarget) && !string.IsNullOrWhiteSpace(moveTargetValue))
        {
            return;
        }

        isSaving = true;
        try
        {
            var result = await CatalogClient.MoveAsync(
                actionUnitId!.Value,
                string.IsNullOrWhiteSpace(moveTargetValue) ? null : parsedTarget);
            await CloseMoveModalAsync();
            await NotifySuccessAsync(L["OrganizationUnit:Moved"]);
            await LoadTreeAsync(result.Id);
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

    private async Task DeleteAsync(OrganizationUnitTreeNode node)
    {
        if (!await UiMessageService.Confirm(L["OrganizationUnit:DeleteConfirmation"]))
        {
            return;
        }

        isSaving = true;
        try
        {
            await CatalogClient.DeleteAsync(node.Id);
            await NotifySuccessAsync(L["OrganizationUnit:Deleted"]);
            await LoadTreeAsync();
        }
        catch (Exception exception)
        {
            errorMessage = exception is OrganizationUnitApiException
                ? L["OrganizationUnit:DeleteError"].Value
                : L["OrganizationUnit:NetworkError"].Value;
        }
        finally
        {
            isSaving = false;
        }
    }

}
