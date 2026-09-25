namespace HCS.DocumentService.Documents;

internal static class DocumentStatusDisplay
{
    public static DocumentStatus Resolve(DocumentStatus stored, bool isSent, DocumentStatus? workflowChildStatus)
    {
        if (stored != DocumentStatus.Draft)
            return stored;
        if (workflowChildStatus is { } child)
            return child;
        return isSent ? DocumentStatus.Submitted : stored;
    }
}
