using System;
using System.Linq;
using System.Threading.Tasks;
using HCS.Blazor.Client.Components;

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

        selectedAvailableMemberId = null;
        availableMembers.Clear();
        if (memberModal is not null)
        {
            await memberModal.Show();
        }
    }

    private string SelectedAvailableMemberText => availableMembers.FirstOrDefault(x => x.Id == selectedAvailableMemberId) is { } member
        ? $"{member.FullName} ({member.UserName})" : "";

    private async Task<CatalogSelect2SearchResponse> SearchAvailableMembersAsync(string term, int page)
    {
        if (selectedNode is null)
        {
            return new([], false);
        }

        try
        {
            var skip = Math.Max(page - 1, 0) * MemberPageSize;
            var result = await CatalogClient.GetAvailableMembersAsync(
                selectedNode.Id,
                term,
                skip,
                MemberPageSize);
            return CatalogSelect2Cache.Merge(availableMembers, result.Items, x => x.Id,
                x => $"{x.FullName} ({x.UserName})", skip + result.Items.Count < result.TotalCount);
        }
        catch (Exception exception)
        {
            errorMessage = exception is OrganizationUnitApiException
                ? L["OrganizationUnit:LoadError"].Value
                : L["OrganizationUnit:NetworkError"].Value;
            await NotifyErrorAsync(errorMessage);
            return new([], false);
        }
    }
}
