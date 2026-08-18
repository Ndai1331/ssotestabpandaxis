namespace HCS.DocumentService.Signing;

public sealed class SigningCredential
{
    private SigningCredential() { }
    public SigningCredential(Guid id, Guid userId, SigningKind kind, string endpoint, string protectedSecret, DateTime now)
        => (Id, UserId, Kind, Endpoint, ProtectedSecret, UpdatedAt) =
            (id, userId, kind, endpoint.Trim(), protectedSecret, now);
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public SigningKind Kind { get; private set; }
    public string Endpoint { get; private set; } = string.Empty;
    public string ProtectedSecret { get; private set; } = string.Empty;
    public DateTime UpdatedAt { get; private set; }
    public void Replace(string endpoint, string protectedSecret, DateTime now)
        => (Endpoint, ProtectedSecret, UpdatedAt) = (endpoint.Trim(), protectedSecret, now);
}

public sealed class UserSignature
{
    private UserSignature() { }
    public UserSignature(Guid id, Guid userId, string fileName, string contentType, string blobName, long size, DateTime now)
    {
        if (userId == Guid.Empty) throw new ArgumentException("User is required.", nameof(userId));
        if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
        Id = id;
        UserId = userId;
        FileName = fileName.Trim();
        ContentType = contentType.Trim();
        BlobName = blobName;
        Size = size;
        IsDefault = false;
        CreationTime = now;
    }
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public string BlobName { get; private set; } = string.Empty;
    public long Size { get; private set; }
    public bool IsDefault { get; private set; }
    public DateTime CreationTime { get; private set; }
    public void MarkDefault() => IsDefault = true;
    public void ClearDefault() => IsDefault = false;
}

public sealed class SigningAttempt
{
    private SigningAttempt() { }
    public SigningAttempt(Guid id, Guid documentId, Guid fileId, Guid userId, SigningKind kind,
        string inputSha256, string idempotencyKey, DateTime now)
    {
        if (documentId == Guid.Empty || fileId == Guid.Empty || userId == Guid.Empty)
            throw new ArgumentException("Document, file, and user identifiers are required.");
        if (!Documents.Hashing.IsSha256(inputSha256)) throw new ArgumentException("Invalid input hash.");
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Trim().Length > 128)
            throw new ArgumentException("A valid idempotency key is required.", nameof(idempotencyKey));
        (Id, DocumentId, FileId, UserId, Kind, InputSha256, IdempotencyKey, Status, CreationTime) =
            (id, documentId, fileId, userId, kind, inputSha256.ToLowerInvariant(), idempotencyKey.Trim(), SigningStatus.Pending, now);
    }
    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public Guid FileId { get; private set; }
    public Guid UserId { get; private set; }
    public SigningKind Kind { get; private set; }
    public SigningStatus Status { get; private set; }
    public string InputSha256 { get; private set; } = string.Empty;
    public string? OutputSha256 { get; private set; }
    public string? OutputBlobName { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string? Error { get; private set; }
    public DateTime CreationTime { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public void Complete(string outputSha256, string outputBlobName, DateTime now)
    {
        if (!Documents.Hashing.IsSha256(outputSha256)) throw new ArgumentException("Invalid signed content hash.");
        Status = SigningStatus.Completed;
        OutputSha256 = outputSha256.ToLowerInvariant();
        OutputBlobName = outputBlobName;
        Error = null;
        CompletedAt = now;
    }
    public void Fail(string error, DateTime now)
    {
        Status = SigningStatus.Failed;
        Error = error.Length > 1000 ? error[..1000] : error;
        CompletedAt = now;
    }
}
