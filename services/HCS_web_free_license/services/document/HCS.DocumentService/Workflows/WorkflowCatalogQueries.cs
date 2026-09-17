namespace HCS.DocumentService.Workflows;

internal static class WorkflowCatalogQueries
{
    internal static IQueryable<WorkflowKind> WhereActiveWorkflowKinds(this IQueryable<WorkflowKind> query) =>
        query.Where(x => x.IsActive);

    internal static IQueryable<WorkflowDefinition> WhereVisibleWorkflowDefinitions(this IQueryable<WorkflowDefinition> query) =>
        query.Where(x => !x.IsDeleted);

    internal static IQueryable<WorkflowDefinition> WhereActiveWorkflowDefinitions(this IQueryable<WorkflowDefinition> query) =>
        query.Where(x => x.IsActive && !x.IsDeleted);

    internal static IQueryable<WorkflowTemplate> WhereVisibleWorkflowTemplates(
        this IQueryable<WorkflowTemplate> query, IQueryable<WorkflowDefinition> definitions) =>
        query.Where(x => x.IsActive && definitions.Any(definition =>
            definition.Id == x.DefinitionId && definition.IsActive && !definition.IsDeleted));
}
