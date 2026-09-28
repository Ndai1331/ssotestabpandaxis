using HCS.CollaborationService.Application;
using HCS.CollaborationService.Contracts;
using HCS.CollaborationService.Data;
using HCS.CollaborationService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.BlobStoring;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Volo.Abp.Users;

namespace HCS.CollaborationService.Storage;

[BlobContainerName("hcs-collaboration")]
public sealed class CollaborationAttachmentContainer;

public sealed class CollaborationAttachmentStore(
    IBlobContainer<CollaborationAttachmentContainer> container,
    CollaborationDbContext db,
    ICurrentUser currentUser,
    IGuidGenerator guidGenerator,
    ChatAttachmentLimitStore limits,
    IChatRealtimeNotifier notifier,
    ILogger<CollaborationAttachmentStore> logger) : ITransientDependency
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/webp", "application/pdf", "text/plain",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "video/mp4", "audio/mpeg"
    };

    public async Task<UploadAttachmentResult> UploadAsync(Guid conversationId, string fileName,
        string contentType, Stream content, long size, CancellationToken ct = default)
    {
        var userId = currentUser.Id ?? throw new AbpAuthorizationException();
        if (!await db.ConversationMembers.AnyAsync(x => x.ConversationId == conversationId && x.UserId == userId &&
                db.Conversations.Any(c => c.Id == conversationId && !c.IsDeleted), ct))
            throw new AbpAuthorizationException();
        var maxBytes = limits.GetMaxBytes();
        if (size <= 0 || size > maxBytes) throw new BusinessException("Collaboration:InvalidAttachmentSize");
        if (string.IsNullOrWhiteSpace(contentType) || contentType.Length > 128)
            throw new BusinessException("Collaboration:InvalidAttachmentType");
        if (!AllowedTypes.Contains(contentType)) throw new BusinessException("Collaboration:InvalidAttachmentType");
        var safeName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(safeName) || safeName.Length > 256) throw new BusinessException("Collaboration:InvalidFileName");
        var id = guidGenerator.Create();
        var blobName = $"conversations/{conversationId:N}/{id:N}";
        await using var buffer = await AttachmentContent.BufferAsync(content, size, ct);
        if (buffer.Length != size)
            throw new BusinessException("Collaboration:InvalidAttachmentSize");
        await container.SaveAsync(blobName, buffer, overrideExisting: false, cancellationToken: ct);
        var kind = contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ? AttachmentKind.Image
            : contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase) ? AttachmentKind.Video
            : contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) ? AttachmentKind.Audio : AttachmentKind.File;
        var attachment = new MessageAttachment(id, conversationId, userId, blobName, safeName, contentType, size, kind);
        db.Attachments.Add(attachment);
        try { await db.SaveChangesAsync(ct); }
        catch { await container.DeleteAsync(blobName, ct); throw; }
        return new(id, safeName, contentType, size, kind);
    }

    public async Task<AuthorizedDownload> DownloadAsync(Guid attachmentId, CancellationToken ct = default)
    {
        var userId = currentUser.Id ?? throw new AbpAuthorizationException();
        var attachment = await db.Attachments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == attachmentId, ct)
            ?? throw new BusinessException("Collaboration:AttachmentNotFound");
        if (!await db.ConversationMembers.AnyAsync(x => x.ConversationId == attachment.ConversationId && x.UserId == userId, ct))
            throw new AbpAuthorizationException();
        if (attachment.MessageId.HasValue && await db.Messages.AnyAsync(x => x.Id == attachment.MessageId && x.RecalledAt != null, ct))
            throw new BusinessException("Collaboration:AttachmentNotFound");
        return new AuthorizedDownload(attachment.FileName, attachment.ContentType, await container.GetAsync(attachment.BlobName, ct));
    }

    public async Task<ConversationDto> UploadAvatarAsync(Guid conversationId, string contentType, Stream content, long size,
        CancellationToken ct = default)
    {
        var userId = currentUser.Id ?? throw new AbpAuthorizationException();
        var conversation = await RequireManageableAsync(conversationId, userId, ct);
        if (size <= 0 || size > ChatModerationRules.MaxAvatarBytes) throw new BusinessException("Collaboration:InvalidAvatarSize");
        if (string.IsNullOrWhiteSpace(contentType) || !ChatModerationRules.AvatarContentTypes.Contains(contentType))
            throw new BusinessException("Collaboration:InvalidAvatarType");
        var blobName = $"conversations/{conversationId:N}/avatar/{guidGenerator.Create():N}";
        await using var buffer = await AttachmentContent.BufferAsync(content, size, ct);
        if (buffer.Length != size) throw new BusinessException("Collaboration:InvalidAvatarSize");
        await container.SaveAsync(blobName, buffer, overrideExisting: false, cancellationToken: ct);
        var previous = conversation.AvatarBlobName;
        conversation.SetAvatar(blobName, contentType.ToLowerInvariant());
        try { await db.SaveChangesAsync(ct); }
        catch { await container.DeleteAsync(blobName, ct); throw; }
        await DeleteBlobQuietlyAsync(previous, ct);
        await NotifyUpdatedAsync(conversation, ct);
        return CollaborationAppService.MapConversation(conversation, userId);
    }

    public async Task RemoveAvatarAsync(Guid conversationId, CancellationToken ct = default)
    {
        var userId = currentUser.Id ?? throw new AbpAuthorizationException();
        var conversation = await RequireManageableAsync(conversationId, userId, ct);
        var previous = conversation.AvatarBlobName;
        if (previous is null) return;
        conversation.ClearAvatar();
        await db.SaveChangesAsync(ct);
        await DeleteBlobQuietlyAsync(previous, ct);
        await NotifyUpdatedAsync(conversation, ct);
    }

    public async Task<AuthorizedDownload?> DownloadAvatarAsync(Guid conversationId, CancellationToken ct = default)
    {
        var userId = currentUser.Id ?? throw new AbpAuthorizationException();
        var conversation = await db.Conversations.AsNoTracking()
            .Where(x => x.Id == conversationId && !x.IsDeleted && x.Members.Any(m => m.UserId == userId))
            .Select(x => new { x.AvatarBlobName, x.AvatarContentType })
            .SingleOrDefaultAsync(ct) ?? throw new AbpAuthorizationException();
        if (conversation.AvatarBlobName is null || conversation.AvatarContentType is null) return null;
        var stream = await container.GetOrNullAsync(conversation.AvatarBlobName, ct);
        return stream is null ? null : new AuthorizedDownload("avatar", conversation.AvatarContentType, stream);
    }

    private async Task<Conversation> RequireManageableAsync(Guid conversationId, Guid userId, CancellationToken ct)
    {
        var conversation = await db.Conversations.Include(x => x.Members)
            .SingleOrDefaultAsync(x => x.Id == conversationId && !x.IsDeleted && x.Members.Any(m => m.UserId == userId), ct)
            ?? throw new AbpAuthorizationException("Conversation membership required.");
        var isSystemAdmin = ChatModerationRules.IsSystemAdmin(currentUser.IsInRole("admin"), currentUser.IsInRole("bd-admin"));
        if (!isSystemAdmin && conversation.Members.Single(x => x.UserId == userId).Role != ConversationMemberRole.Admin)
            throw new AbpAuthorizationException("Conversation admin required.");
        return conversation;
    }

    private async Task DeleteBlobQuietlyAsync(string? blobName, CancellationToken ct)
    {
        if (blobName is null) return;
        try { await container.DeleteAsync(blobName, ct); }
        catch (Exception exception) { logger.LogWarning(exception, "Failed to delete replaced conversation avatar {BlobName}", blobName); }
    }

    private async Task NotifyUpdatedAsync(Conversation conversation, CancellationToken ct)
    {
        try { await notifier.ConversationUpdatedAsync(conversation.Id, conversation.Members.Select(x => x.UserId).ToArray(), ct); }
        catch (Exception exception) { logger.LogWarning(exception, "Realtime conversation update failed for {ConversationId}", conversation.Id); }
    }

    public async Task DeleteAsync(Guid attachmentId, CancellationToken ct = default)
    {
        var userId = currentUser.Id ?? throw new AbpAuthorizationException();
        var attachment = await db.Attachments.SingleOrDefaultAsync(x => x.Id == attachmentId, ct)
            ?? throw new BusinessException("Collaboration:AttachmentNotFound");
        if (attachment.UploadedByUserId != userId || attachment.MessageId.HasValue) throw new AbpAuthorizationException();
        db.Attachments.Remove(attachment); await db.SaveChangesAsync(ct); await container.DeleteAsync(attachment.BlobName, ct);
    }
}
