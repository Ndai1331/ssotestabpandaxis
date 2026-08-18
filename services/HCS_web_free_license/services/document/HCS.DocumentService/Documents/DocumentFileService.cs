using System.Security.Cryptography;
using HCS.DocumentService.Storage;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.BlobStoring;

namespace HCS.DocumentService.Documents;

public sealed class DocumentFileService(DocumentServiceDbContext db, IBlobContainer<DocumentBlobContainer> blobs,
    IHttpContextAccessor httpContext)
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
        if (copy.Length != size) throw new InvalidOperationException("Declared file size does not match content.");
        var hash = Convert.ToHexString(SHA256.HashData(copy.ToArray())).ToLowerInvariant();
        var fileId = Guid.NewGuid();
        var blobName = BlobNamePolicy.Document(documentId, fileId);
        copy.Position = 0;
        await blobs.SaveAsync(blobName, copy, overrideExisting: false, cancellationToken: cancellationToken);
        try
        {
            var file = document.AddFile(fileId, fileName, contentType, size, hash, blobName, userId, DateTime.UtcNow);
            await db.SaveChangesAsync(cancellationToken);
            return new DocumentFileDto(file.Id, file.FileName, file.ContentType, file.Size, file.Sha256, file.CreationTime);
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
}
