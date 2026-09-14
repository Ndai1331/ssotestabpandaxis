using Microsoft.EntityFrameworkCore;

namespace HCS.DocumentService.Workflows;

internal static class WorkflowCatalogQueries
{
    internal static IQueryable<WorkflowKind> WhereActiveWorkflowKinds(this IQueryable<WorkflowKind> query) =>
        query.Where(x => x.IsActive);

    internal static IQueryable<WorkflowDefinition> WhereActiveWorkflowDefinitions(this IQueryable<WorkflowDefinition> query) =>
        query.Where(x => x.IsActive);

    internal static IQueryable<WorkflowTemplate> WhereActiveWorkflowTemplates(this IQueryable<WorkflowTemplate> query) =>
        query.Where(x => x.IsActive);
}
