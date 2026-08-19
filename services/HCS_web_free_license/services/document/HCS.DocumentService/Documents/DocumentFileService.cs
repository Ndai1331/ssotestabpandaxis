using System.Security.Cryptography;
using HCS.DocumentService.Conversion;
using HCS.DocumentService.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Volo.Abp.BlobStoring;

namespace HCS.DocumentService.Documents;

public sealed class DocumentFileService(DocumentServiceDbContext db, IBlobContainer<DocumentBlobContainer> blobs,
    IHttpContextAccessor httpContext, IDocxToPdfConverter converter, ILogger<DocumentFileService> logger)
{
    public const long MaxFileSize = 50 * 1024 * 1024;
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
        { "application/pdf", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "image/png", "image/jpeg" };

    public async Task<DocumentFileDto> UploadAsync(Guid documentId, string fileName, string contentType, Stream content, long size, CancellationToken cancellationToken)
    {
        var principal = httpContext.HttpContext?.User ?? new System.Security.Claims.ClaimsPrincipal();
        var userId = DocumentAccess.RequireUser(principal);
        DocumentAccess.RequirePermission(principal, DocumentPermissions.ManageFiles);
        if (size is <= 0 or > MaxFileSize) throw new InvalidOperationException("File size is outside the allowed range.");
        if (!AllowedTypes.Contains(contentType)) throw new InvalidOperationException("File type is not allowed.");
        var document = await db.Documents.Include(x => x.Files).Include(x => x.History).SingleOrDefaultAsync(x => x.Id == documentId, cancellationToken)
            ?? throw new KeyNotFoundException("Document not found.");
        await db.Entry(document).Collection(x => x.Assignments).LoadAsync(cancellationToken);
        DocumentAccess.EnsureCanManage(document, userId, principal);
        await using var copy = new MemoryStream();
        await content.CopyToAsync(copy, cancellationToken);
        var bytes = copy.ToArray();
        if (bytes.Length != size) throw new InvalidOperationException("Declared file size does not match content.");
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var fileId = Guid.NewGuid();
        var blobName = BlobNamePolicy.Document(documentId, fileId);
        copy.Position = 0;
        await blobs.SaveAsync(blobName, copy, overrideExisting: false, cancellationToken: cancellationToken);
        try
        {
            var file = document.AddFile(fileId, fileName, contentType, size, hash, blobName, userId, DateTime.UtcNow);
            await TryAttachConvertedPdfAsync(document, file, bytes, userId, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            return Map(file);
        }
        catch
        {
            await blobs.DeleteAsync(blobName, cancellationToken: cancellationToken);
            throw;
        }
    }

    public async Task<(DocumentFile File, Stream Content)> OpenAuthorizedAsync(Guid documentId, Guid fileId, CancellationToken cancellationToken)
    {
        var principal = httpContext.HttpContext?.User ?? new System.Security.Claims.ClaimsPrincipal();
        var userId = DocumentAccess.RequireUser(principal);
        DocumentAccess.RequirePermission(principal, DocumentPermissions.View);
        var document = await db.Documents.AsNoTracking().Include(x => x.Assignments).Include(x => x.History)
            .SingleOrDefaultAsync(x => x.Id == documentId, cancellationToken)
            ?? throw new KeyNotFoundException("Document not found.");
        DocumentAccess.EnsureCanView(document, userId, principal);
        var file = await db.DocumentFiles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == fileId && x.DocumentId == documentId && !x.IsPendingDeletion, cancellationToken)
            ?? throw new KeyNotFoundException("Document file not found.");
        return (file, await blobs.GetAsync(file.BlobName, cancellationToken: cancellationToken));
    }

    public async Task DeleteAsync(Guid documentId, Guid fileId, CancellationToken cancellationToken)
    {
        var principal = httpContext.HttpContext?.User ?? new System.Security.Claims.ClaimsPrincipal();
        var userId = DocumentAccess.RequireUser(principal);
        DocumentAccess.RequirePermission(principal, DocumentPermissions.ManageFiles);
        var document = await db.Documents.Include(x => x.Files).Include(x => x.History)
            .SingleOrDefaultAsync(x => x.Id == documentId, cancellationToken)
            ?? throw new KeyNotFoundException("Document not found.");
        await db.Entry(document).Collection(x => x.Assignments).LoadAsync(cancellationToken);
        DocumentAccess.EnsureCanManage(document, userId, principal);
        var file = document.BeginFileDeletion(fileId, userId, DateTime.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
        await blobs.DeleteAsync(file.BlobName, cancellationToken: cancellationToken);
        document.CompleteFileDeletion(fileId, userId, DateTime.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task CopyFilesAsync(DocumentAggregate source, DocumentAggregate target, Guid? actorUserId, DateTime now,
        CancellationToken cancellationToken)
    {
        foreach (var file in source.Files.Where(x => !x.IsPendingDeletion))
        {
            await using var content = await blobs.GetAsync(file.BlobName, cancellationToken: cancellationToken);
            await using var copy = new MemoryStream();
            await content.CopyToAsync(copy, cancellationToken);
            copy.Position = 0;
            var fileId = Guid.NewGuid();
            var blobName = BlobNamePolicy.Document(target.Id, fileId);
            await blobs.SaveAsync(blobName, copy, overrideExisting: false, cancellationToken: cancellationToken);
            target.AddFile(fileId, file.FileName, file.ContentType, file.Size, file.Sha256, blobName, actorUserId, now);
        }
    }

    public async Task AttachBlobAsync(DocumentAggregate document, string fileName, string contentType, Stream content,
        Guid? actorUserId, DateTime now, CancellationToken cancellationToken)
    {
        await using var copy = new MemoryStream();
        await content.CopyToAsync(copy, cancellationToken);
        var bytes = copy.ToArray();
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var fileId = Guid.NewGuid();
        var blobName = BlobNamePolicy.Document(document.Id, fileId);
        copy.Position = 0;
        await blobs.SaveAsync(blobName, copy, overrideExisting: false, cancellationToken: cancellationToken);
        document.AddFile(fileId, fileName, contentType, bytes.Length, hash, blobName, actorUserId, now);
    }

    private async Task TryAttachConvertedPdfAsync(DocumentAggregate document, DocumentFile wordFile, byte[] docxBytes,
        Guid? actorUserId, CancellationToken cancellationToken)
    {
        if (!IsDocx(wordFile.FileName, wordFile.ContentType)) return;
        if (!converter.IsAvailable)
        {
            logger.LogInformation("Skipping Word-to-PDF conversion because LibreOffice is not available.");
            return;
        }
        var pdfBytes = await converter.ConvertAsync(docxBytes, cancellationToken);
        if (pdfBytes is null or { Length: 0 })
        {
            logger.LogWarning("Word-to-PDF conversion produced no output for {File}.", wordFile.FileName);
            return;
        }
        var pdfId = Guid.NewGuid();
        var pdfName = Path.ChangeExtension(wordFile.FileName, ".pdf");
        var pdfHash = Convert.ToHexString(SHA256.HashData(pdfBytes)).ToLowerInvariant();
        var pdfBlob = BlobNamePolicy.Document(document.Id, pdfId);
        await using var pdfStream = new MemoryStream(pdfBytes);
        await blobs.SaveAsync(pdfBlob, pdfStream, overrideExisting: false, cancellationToken: cancellationToken);
        var pdfFile = document.AddFile(pdfId, pdfName, "application/pdf", pdfBytes.Length, pdfHash, pdfBlob, actorUserId, DateTime.UtcNow);
        wordFile.SetPairedFileId(pdfFile.Id);
        pdfFile.SetPairedFileId(wordFile.Id);
    }

    internal static DocumentFileDto Map(DocumentFile file) =>
        new(file.Id, file.FileName, file.ContentType, file.Size, file.Sha256, file.CreationTime, file.PairedFileId);

    private static bool IsDocx(string fileName, string contentType) =>
        fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase)
        || string.Equals(contentType, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", StringComparison.OrdinalIgnoreCase);
}
