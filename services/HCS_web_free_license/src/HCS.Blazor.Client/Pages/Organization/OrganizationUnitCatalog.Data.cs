using System;
using System.Linq;
using System.Threading.Tasks;

namespace HCS.Blazor.Client.Pages.Organization;

public partial class OrganizationUnitCatalog
{
    private async Task LoadTreeAsync(Guid? preferredSelectedId = null)
    {
        loadCancellation?.Cancel();
        loadCancellation?.Dispose();
        loadCancellation = new System.Threading.CancellationTokenSource();
        isLoading = true;
        errorMessage = null;

        var expandedState = OrganizationUnitTreeBuilder.CaptureExpandedState(rootNodes);
        var previousSelectedId = preferredSelectedId ?? selectedNode?.Id;
        try
        {
            var source = await CatalogClient.GetTreeAsync(loadCancellation.Token);
            var nextTree = OrganizationUnitTreeBuilder.Build(source, expandedState);
            rootNodes.Clear();
            rootNodes.AddRange(nextTree);
            selectedNode = previousSelectedId.HasValue
                ? allNodes.FirstOrDefault(node => node.Id == previousSelectedId.Value)
                : null;
            hasLoaded = true;

            if (selectedNode is null)
            {
                members.Clear();
                totalMembers = 0;
            }
            else
            {
                await LoadMembersAsync();
            }
        }
        catch (OperationCanceledException) when (loadCancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            errorMessage = exception is OrganizationUnitApiException
                ? L["OrganizationUnit:LoadError"].Value
                : L["OrganizationUnit:NetworkError"].Value;
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task SelectNodeAsync(OrganizationUnitTreeNode node)
    {
        selectedNode = node;
        currentMemberPage = 1;
        memberFilter = string.Empty;
        await LoadMembersAsync();
    }

    private async Task LoadMembersAsync()
    {
        if (selectedNode is null)
        {
            members.Clear();
            totalMembers = 0;
            return;
        }

        isMembersLoading = true;
        errorMessage = null;
        try
        {
            var result = await CatalogClient.GetMembersAsync(
                selectedNode.Id,
                memberFilter,
                (currentMemberPage - 1) * MemberPageSize,
                MemberPageSize);
            members.Clear();
            members.AddRange(result.Items);
            totalMembers = result.TotalCount;
        }
        catch (Exception exception)
        {
            errorMessage = exception is OrganizationUnitApiException
                ? L["OrganizationUnit:LoadError"].Value
                : L["OrganizationUnit:NetworkError"].Value;
            members.Clear();
            totalMembers = 0;
        }
        finally
        {
            isMembersLoading = false;
        }
    }
}
