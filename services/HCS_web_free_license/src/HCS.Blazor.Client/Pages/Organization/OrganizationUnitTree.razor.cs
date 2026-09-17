using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace HCS.Blazor.Client.Pages.Organization;

public partial class OrganizationUnitTree
{
    [Parameter, EditorRequired]
    public IReadOnlyList<OrganizationUnitTreeNode> Nodes { get; set; } = [];

    [Parameter]
    public Guid? SelectedId { get; set; }

    [Parameter]
    public bool CanCreate { get; set; }

    [Parameter]
    public bool CanUpdate { get; set; }

    [Parameter]
    public bool CanDelete { get; set; }

    [Parameter]
    public Guid? OpenMenuId { get; set; }

    [Parameter]
    public EventCallback<Guid?> OpenMenuIdChanged { get; set; }

    [Parameter]
    public EventCallback<OrganizationUnitTreeNode> NodeSelected { get; set; }

    [Parameter]
    public EventCallback<OrganizationUnitActionRequest> ActionRequested { get; set; }

    private static Task ToggleAsync(OrganizationUnitTreeNode node)
    {
        node.IsExpanded = !node.IsExpanded;
        return Task.CompletedTask;
    }

    private Task ToggleMenuAsync(Guid id) =>
        OpenMenuIdChanged.InvokeAsync(OpenMenuId == id ? null : id);

    private Task CloseMenuAsync() => OpenMenuIdChanged.InvokeAsync(null);

    private async Task SelectAsync(OrganizationUnitTreeNode node)
    {
        if (OpenMenuId is not null)
        {
            await CloseMenuAsync();
        }

        await NodeSelected.InvokeAsync(node);
    }

    private async Task RequestAsync(OrganizationUnitTreeNode node, OrganizationUnitAction action)
    {
        await CloseMenuAsync();
        await ActionRequested.InvokeAsync(new OrganizationUnitActionRequest(node, action));
    }
}
