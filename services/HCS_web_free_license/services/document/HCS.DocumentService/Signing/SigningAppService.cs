using System.Security.Claims;
using HCS.DocumentService.Integration;
using HCS.DocumentService.Documents;
using HCS.DocumentService.Storage;
using HCS.IntegrationEvents.Documents;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Authorization;
using Volo.Abp.BlobStoring;

namespace HCS.DocumentService.Signing;

public sealed class SigningAppService(
    DocumentServiceDbContext db,
    IHttpContextAccessor httpContext,
    IConfiguration configuration,
    ISigningSecretProtector secretProtector,
    IEnumerable<IDigitalSigningAdapter> adapters,
    IBlobContainer<DocumentBlobContainer> documentBlobs,
    IBlobContainer<SigningBlobContainer> signingBlobs) : ISigningAppService
{
    private const long MaxSignatureSize = 2 * 1024 * 1024;
    private static readonly HashSet<string> AllowedSignatureContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif"
    };

    public async Task<IReadOnlyList<SigningCredentialDto>> GetCredentialsAsync(Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var principal = Principal;
        var targetUserId = ResolveTargetUser(userId, DocumentPermissions.SigningConfigure);
        var credentials = await db.SigningCredentials.AsNoTracking().Where(x => x.UserId == targetUserId)
            .OrderBy(x => x.Kind).ToListAsync(cancellationToken);
        return credentials.Select(Map).ToList();
    }

    public async Task<SigningCredentialDto> ConfigureCredentialAsync(ConfigureSigningCredentialRequest input, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var principal = Principal;
        var targetUserId = ResolveTargetUser(userId, DocumentPermissions.SigningConfigure);
        if (!Uri.TryCreate(input.Endpoint, UriKind.Absolute, out var endpoint) || endpoint.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException("Signing endpoint must be an absolute HTTPS URI.");
        EnsureEndpointAllowed(endpoint);
        var protectedSecret = secretProtector.Protect(input.ConsumeSecret());
        var credential = await db.SigningCredentials.SingleOrDefaultAsync(x => x.UserId == targetUserId && x.Kind == input.Kind, cancellationToken);
        if (credential is null)
        {
            credential = new SigningCredential(Guid.NewGuid(), targetUserId, input.Kind, endpoint.ToString(), protectedSecret, DateTime.UtcNow);
            db.SigningCredentials.Add(credential);
        }
        else credential.Replace(endpoint.ToString(), protectedSecret, DateTime.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
        return Map(credential);
    }

    public async Task<SigningAttemptDto> SignAsync(SignDocumentRequest input, CancellationToken cancellationToken = default)
    {
        var principal = Principal;
        var userId = DocumentAccess.RequireUser(principal);
        DocumentAccess.RequirePermission(principal, DocumentPermissions.SigningExecute);
        var key = input.IdempotencyKey?.Trim();
        if (string.IsNullOrWhiteSpace(key) || key.Length > 128)
            throw new ArgumentException("A valid idempotency key is required.", nameof(input));
        var document = await db.Documents.AsNoTracking().Include(x => x.Assignments).Include(x => x.History)
            .SingleOrDefaultAsync(x => x.Id == input.DocumentId, cancellationToken)
            ?? throw new KeyNotFoundException("Document not found.");
        DocumentAccess.EnsureCanView(document, userId, principal);
        var existing = await FindAttemptAsync(userId, input, key, cancellationToken);
        if (existing is not null) return Map(existing);
        var file = await db.DocumentFiles.AsNoTracking().SingleOrDefaultAsync(x => x.Id == input.FileId && x.DocumentId == input.DocumentId && !x.IsPendingDeletion, cancellationToken)
            ?? throw new KeyNotFoundException("Document file not found.");
        var credential = await db.SigningCredentials.AsNoTracking().SingleOrDefaultAsync(x => x.UserId == userId && x.Kind == input.Kind, cancellationToken);
        if (input.Kind != SigningKind.Electronic && credential is null) throw new InvalidOperationException("Signing credential is not configured.");
        var attempt = new SigningAttempt(Guid.NewGuid(), input.DocumentId, input.FileId, userId, input.Kind, file.Sha256, key, DateTime.UtcNow);
        db.SigningAttempts.Add(attempt);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            db.Entry(attempt).State = EntityState.Detached;
            var concurrent = await FindAttemptAsync(userId, input, key, cancellationToken);
            if (concurrent is not null) return Map(concurrent);
            throw;
        }
        try
        {
            await using var inputStream = await documentBlobs.GetAsync(file.BlobName, cancellationToken: cancellationToken);
            await using var buffer = new MemoryStream();
            await inputStream.CopyToAsync(buffer, cancellationToken);
            var bytes = buffer.ToArray();
            var actualInputHash = ContentHash.Sha256(bytes);
            if (!actualInputHash.Equals(file.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Stored file hash does not match its immutable metadata.");
            var adapter = adapters.SingleOrDefault(x => x.Kind == input.Kind)
                ?? throw new NotSupportedException($"No adapter registered for {input.Kind}.");
            var result = await adapter.SignAsync(new SigningAdapterRequest(bytes, actualInputHash,
                credential?.Endpoint ?? "https://electronic.local", credential is null ? string.Empty : secretProtector.Unprotect(credential.ProtectedSecret)), cancellationToken);
            var outputHash = ContentHash.Sha256(result.SignedContent);
            var blobName = BlobNamePolicy.Signing(input.DocumentId, attempt.Id);
            await signingBlobs.SaveAsync(blobName, new MemoryStream(result.SignedContent), overrideExisting: false, cancellationToken: cancellationToken);
            attempt.Complete(outputHash, blobName, DateTime.UtcNow);
            var integrationEvent = new DocumentSignedEto(Guid.NewGuid(), DateTimeOffset.UtcNow, CorrelationId, input.DocumentId,
                input.FileId, actualInputHash, outputHash, result.AdapterId);
            db.OutboxMessages.Add(OutboxFactory.CreateCanonical(integrationEvent, CorrelationId, DateTime.UtcNow));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            attempt.Fail(SigningFailureSanitizer.ToPublicMessage(exception), DateTime.UtcNow);
        }
        await db.SaveChangesAsync(cancellationToken);
        return Map(attempt);
    }

    public async Task<SigningReportDto> GetReportAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var principal = Principal;
        var userId = DocumentAccess.RequireUser(principal);
        DocumentAccess.RequirePermission(principal, DocumentPermissions.SigningReport);
        var document = await db.Documents.AsNoTracking().Include(x => x.Assignments).Include(x => x.History)
            .SingleOrDefaultAsync(x => x.Id == documentId, cancellationToken)
            ?? throw new KeyNotFoundException("Document not found.");
        DocumentAccess.EnsureCanView(document, userId, principal);
        var attempts = await db.SigningAttempts.AsNoTracking().Where(x => x.DocumentId == documentId)
            .OrderByDescending(x => x.CreationTime).ToListAsync(cancellationToken);
        return new SigningReportDto(documentId, attempts.Count(x => x.Status == SigningStatus.Completed),
            attempts.Count(x => x.Status == SigningStatus.Failed), attempts.Select(Map).ToList());
    }

    public async Task<IReadOnlyList<UserSignatureDto>> GetSignaturesAsync(Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var targetUserId = ResolveTargetUser(userId, DocumentPermissions.SigningExecute);
        var items = await db.UserSignatures.AsNoTracking().Where(x => x.UserId == targetUserId)
            .OrderByDescending(x => x.IsDefault).ThenByDescending(x => x.CreationTime).ToListAsync(cancellationToken);
        return items.Select(MapSignature).ToList();
    }

    public async Task<UserSignatureDto> UploadSignatureAsync(string fileName, string contentType, Stream content, long size, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var targetUserId = ResolveTargetUser(userId, DocumentPermissions.SigningExecute);
        ValidateSignatureFile(fileName, contentType, size);
        var id = Guid.NewGuid();
        var blobName = BlobNamePolicy.UserSignature(targetUserId, id);
        await signingBlobs.SaveAsync(blobName, content, overrideExisting: false, cancellationToken: cancellationToken);
        var signature = new UserSignature(id, targetUserId, Path.GetFileName(fileName), NormalizeSignatureContentType(contentType), blobName, size, DateTime.UtcNow);
        if (!await db.UserSignatures.AnyAsync(x => x.UserId == targetUserId, cancellationToken)) signature.MarkDefault();
        db.UserSignatures.Add(signature);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch { await signingBlobs.DeleteAsync(blobName, cancellationToken: cancellationToken); throw; }
        return MapSignature(signature);
    }

    public async Task<UserSignatureDto> UpdateSignatureAsync(Guid id, string? fileName, string? contentType, Stream? content, long? size,
        Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var targetUserId = ResolveTargetUser(userId, DocumentPermissions.SigningExecute);
        var signature = await db.UserSignatures.SingleOrDefaultAsync(x => x.Id == id && x.UserId == targetUserId, cancellationToken)
            ?? throw new KeyNotFoundException("Signature not found.");

        var normalizedFileName = string.IsNullOrWhiteSpace(fileName) ? signature.FileName : Path.GetFileName(fileName.Replace('\\', '/'));
        if (content is null)
        {
            if (string.Equals(normalizedFileName, signature.FileName, StringComparison.Ordinal))
                throw new ArgumentException("A file name or replacement image is required.");

            signature.Rename(normalizedFileName);
            await db.SaveChangesAsync(cancellationToken);
            return MapSignature(signature);
        }

        var uploadSize = size ?? 0;
        ValidateSignatureFile(normalizedFileName, contentType, uploadSize);
        var normalizedContentType = NormalizeSignatureContentType(contentType);
        var oldBlobName = signature.BlobName;
        var newBlobName = BlobNamePolicy.UserSignature(targetUserId, Guid.NewGuid());
        await signingBlobs.SaveAsync(newBlobName, content, overrideExisting: false, cancellationToken: cancellationToken);
        signature.ReplaceContent(normalizedFileName, normalizedContentType, newBlobName, uploadSize);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await signingBlobs.DeleteAsync(newBlobName, cancellationToken: cancellationToken);
            throw;
        }

        await signingBlobs.DeleteAsync(oldBlobName, cancellationToken: cancellationToken);
        return MapSignature(signature);
    }

    public async Task<UserSignatureDto> SetDefaultSignatureAsync(Guid id, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var targetUserId = ResolveTargetUser(userId, DocumentPermissions.SigningExecute);
        var signature = await db.UserSignatures.SingleOrDefaultAsync(x => x.Id == id && x.UserId == targetUserId, cancellationToken)
            ?? throw new KeyNotFoundException("Signature not found.");
        var signatures = await db.UserSignatures.Where(x => x.UserId == targetUserId).ToListAsync(cancellationToken);
        foreach (var item in signatures)
        {
            if (item.Id == signature.Id) item.MarkDefault();
            else item.ClearDefault();
        }

        await db.SaveChangesAsync(cancellationToken);
        return MapSignature(signature);
    }

    public async Task DeleteSignatureAsync(Guid id, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var targetUserId = ResolveTargetUser(userId, DocumentPermissions.SigningExecute);
        var signature = await db.UserSignatures.SingleOrDefaultAsync(x => x.Id == id && x.UserId == targetUserId, cancellationToken)
            ?? throw new KeyNotFoundException("Signature not found.");
        List<UserSignature> siblings = signature.IsDefault
            ? await db.UserSignatures.Where(x => x.UserId == targetUserId && x.Id != id)
                .OrderByDescending(x => x.CreationTime).ToListAsync(cancellationToken)
            : [];
        var replacement = siblings.FirstOrDefault();
        db.UserSignatures.Remove(signature);
        if (signature.IsDefault)
        {
            foreach (var sibling in siblings)
            {
                if (sibling.Id == replacement?.Id) sibling.MarkDefault();
                else sibling.ClearDefault();
            }
        }
        await db.SaveChangesAsync(cancellationToken);
        await signingBlobs.DeleteAsync(signature.BlobName, cancellationToken: cancellationToken);
    }

    public async Task<(Stream Content, string ContentType, string FileName)> OpenSignatureContentAsync(
        Guid id, Guid? userId = null, CancellationToken cancellationToken = default)
    {
        var targetUserId = ResolveTargetUser(userId, DocumentPermissions.SigningExecute);
        var signature = await db.UserSignatures.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == id && x.UserId == targetUserId, cancellationToken)
            ?? throw new KeyNotFoundException("Signature not found.");
        var stream = await signingBlobs.GetAsync(signature.BlobName, cancellationToken);
        return (stream, signature.ContentType, signature.FileName);
    }

    private Guid ResolveTargetUser(Guid? userId, string permission)
    {
        var principal = Principal;
        var current = DocumentAccess.RequireUser(principal);
        if (userId is null || userId == current)
        {
            return current;
        }

        DocumentAccess.RequirePermission(principal, permission);
        if (!DocumentAccess.IsElevated(principal))
        {
            throw new AbpAuthorizationException("Managing another user's signatures requires an administrator.");
        }

        return userId.Value;
    }

    private ClaimsPrincipal Principal => httpContext.HttpContext?.User ?? new ClaimsPrincipal();

    private static void ValidateSignatureFile(string fileName, string? contentType, long size)
    {
        if (size is <= 0 or > MaxSignatureSize) throw new ArgumentOutOfRangeException(nameof(size));
        var normalizedFileName = Path.GetFileName(fileName.Replace('\\', '/')).Trim();
        if (string.IsNullOrWhiteSpace(normalizedFileName)) throw new ArgumentException("A file name is required.", nameof(fileName));
        if (normalizedFileName.Length > 256) throw new ArgumentException("The file name is too long.", nameof(fileName));
        if (!AllowedSignatureContentTypes.Contains(contentType ?? string.Empty))
            throw new InvalidDataException("Signature files must be JPEG, PNG, WebP, or GIF images.");
    }

    private static string NormalizeSignatureContentType(string? contentType) =>
        contentType?.Trim().ToLowerInvariant() ?? throw new InvalidDataException("A signature image content type is required.");

    private Task<SigningAttempt?> FindAttemptAsync(Guid userId, SignDocumentRequest input, string key,
        CancellationToken cancellationToken) => db.SigningAttempts.AsNoTracking().SingleOrDefaultAsync(x =>
        x.UserId == userId && x.DocumentId == input.DocumentId && x.FileId == input.FileId &&
        x.Kind == input.Kind && x.IdempotencyKey == key, cancellationToken);
    private void EnsureEndpointAllowed(Uri endpoint)
    {
        var hosts = configuration.GetSection("Signing:AllowedEndpointHosts").Get<string[]>() ?? [];
        if (hosts.Length == 0 || !hosts.Any(host => string.Equals(host?.Trim(), endpoint.Host,
                StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("Signing endpoint is not in the configured allowlist.");
    }
    private string CorrelationId => httpContext.HttpContext?.TraceIdentifier ?? Guid.NewGuid().ToString("N");
    private static SigningCredentialDto Map(SigningCredential x) => new(x.Id, x.Kind, x.Endpoint, "********", x.UpdatedAt);
    private static SigningAttemptDto Map(SigningAttempt x) => new(x.Id, x.DocumentId, x.FileId, x.Kind, x.Status,
        x.InputSha256, x.OutputSha256, x.Error, x.CreationTime, x.CompletedAt);
    private static UserSignatureDto MapSignature(UserSignature x) =>
        new(x.Id, x.FileName, x.ContentType, x.Size, x.IsDefault, x.CreationTime);
}

internal static class SigningFailureSanitizer
{
    public static string ToPublicMessage(Exception exception) => exception switch
    {
        NotSupportedException => "The selected signing method is not available.",
        InvalidDataException => "The source document failed integrity verification.",
        System.Security.Cryptography.CryptographicException => "The signing operation failed cryptographic verification.",
        _ => "The signing operation failed. Use the correlation id to investigate server logs."
    };
}
