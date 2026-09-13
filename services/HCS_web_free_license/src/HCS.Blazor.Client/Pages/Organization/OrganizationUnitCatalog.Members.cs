using System;
using System.Threading.Tasks;

namespace HCS.Blazor.Client.Pages.Organization;

public partial class OrganizationUnitCatalog
{
    private Task SearchMembersAsync()
    {
        currentMemberPage = 1;
        return LoadMembersAsync();
    }

    private Task PreviousMembersPageAsync()
    {
        if (currentMemberPage > 1)
        {
            currentMemberPage--;
        }

        return LoadMembersAsync();
    }

    private Task NextMembersPageAsync()
    {
        if (currentMemberPage < TotalMemberPages)
        {
            currentMemberPage++;
        }

        return LoadMembersAsync();
    }

    private async Task OpenAddMemberAsync()
    {
        if (selectedNode is null)
        {
            return;
        }

        availableMemberFilter = string.Empty;
        selectedAvailableMemberValue = string.Empty;
        availableMemberPage = 1;
        await LoadAvailableMembersAsync();
        if (memberModal is not null)
        {
            await memberModal.Show();
        }
    }

    private Task SearchAvailableMembersAsync()
    {
        availableMemberPage = 1;
        return LoadAvailableMembersAsync();
    }

    private async Task LoadAvailableMembersAsync()
    {
        if (selectedNode is null)
        {
            return;
        }

        try
        {
            var result = await CatalogClient.GetAvailableMembersAsync(
                selectedNode.Id,
                availableMemberFilter,
                (availableMemberPage - 1) * 100,
                100);
            availableMembers.Clear();
            availableMembers.AddRange(result.Items);
        }
        catch (Exception exception)
        {
            errorMessage = exception is OrganizationUnitApiException
                ? L["OrganizationUnit:LoadError"].Value
                : L["OrganizationUnit:NetworkError"].Value;
            availableMembers.Clear();
        }
    }
}
