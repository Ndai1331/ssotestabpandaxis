namespace HCS.DocumentService.Documents;

internal static class WorkflowFileSelection
{
    internal static Guid? Resolve(DocumentAggregate document)
    {
        if (document.SourceType != DocumentSourceType.Workflow) return null;
        var files = document.Files.Where(x => !x.IsPendingDeletion).ToList();
        if (document.WorkflowFileId is { } selected)
            return files.Any(x => x.Id == selected) ? selected : null;

        // Compatibility for submissions created before WorkflowFileId was stored:
        // follow the original PDF's generated prepared/signed versions, not later uploads.
        var original = files.Where(IsPdf).OrderBy(x => x.CreationTime)
            .ThenByDescending(x => x.PairedFileId.HasValue).ThenBy(x => x.Id).FirstOrDefault();
        if (original is null) return null;
        var stem = Path.GetFileNameWithoutExtension(original.FileName);
        return files.Where(IsPdf).Where(x => x.Id == original.Id ||
                System.Text.RegularExpressions.Regex.IsMatch(Path.GetFileNameWithoutExtension(x.FileName),
                    "^" + System.Text.RegularExpressions.Regex.Escape(stem) + @"(?:-prepared)?(?:-Sign\d+)*$"))
            .OrderByDescending(x => x.CreationTime).ThenByDescending(x => x.Id).First().Id;
    }

    internal static bool IsPdf(DocumentFile file) =>
        file.ContentType.Contains("pdf", StringComparison.OrdinalIgnoreCase)
        || file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
}
