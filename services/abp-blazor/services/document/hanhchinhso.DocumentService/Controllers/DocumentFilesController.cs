using System.IO.Compression;
using System.Security.Cryptography;
using hanhchinhso.DocumentService.Documents;
using hanhchinhso.DocumentService.Permissions;
using hanhchinhso.DocumentService.Workflows;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.BlobStoring;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Users;

namespace hanhchinhso.DocumentService.Controllers;

[ApiController]
[Authorize(DocumentServicePermissions.Files.Default)]
[Route("api/document-management/files")]
public class DocumentFilesController : ControllerBase
{
    private static readonly Dictionary<string, string[]> AllowedTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = ["application/pdf"],
            [".docx"] = ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"]
        };

    private readonly IRepository<Document, Guid> _documents;
    private readonly IRepository<DocumentFile, Guid> _files;
    private readonly IBlobContainer<DocumentBlobContainer> _blobs;
    private readonly DocumentFileManager _fileManager;
    private readonly ICurrentTenant _currentTenant;
    private readonly IConfiguration _configuration;
    private readonly ICurrentUser _currentUser;
    private readonly IWorkflowDocumentAccessService _access;

    public DocumentFilesController(
        IRepository<Document, Guid> documents,
        IRepository<DocumentFile, Guid> files,
        IBlobContainer<DocumentBlobContainer> blobs,
        DocumentFileManager fileManager,
        ICurrentTenant currentTenant,
        IConfiguration configuration,
        ICurrentUser currentUser,
        IWorkflowDocumentAccessService access)
    {
        _documents = documents;
        _files = files;
        _blobs = blobs;
        _fileManager = fileManager;
        _currentTenant = currentTenant;
        _configuration = configuration;
        _currentUser = currentUser;
        _access = access;
    }

    [HttpPost("{documentId:guid}")]
    [Authorize(DocumentServicePermissions.Files.Upload)]
    [RequestFormLimits(MultipartBodyLengthLimit = 104_857_600)]
    public async Task<DocumentFileDto> UploadAsync(
        Guid documentId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        await _documents.GetAsync(documentId, cancellationToken: cancellationToken);
        if (!_currentUser.Id.HasValue ||
            !await _access.CanMutateDocumentAsync(
                documentId, _currentUser.Id.Value))
        {
            throw new Volo.Abp.Authorization.AbpAuthorizationException(
                "The current user cannot upload files to this document.");
        }
        var maxBytes = _configuration.GetValue<long?>("DocumentFiles:MaxUploadBytes") ?? 52_428_800;
        if (file.Length <= 0 || file.Length > maxBytes)
        {
            throw new UserFriendlyException($"File size must be between 1 and {maxBytes} bytes.");
        }

        var displayName = SanitizeDisplayName(file.FileName);
        var extension = Path.GetExtension(displayName);
        if (!AllowedTypes.TryGetValue(extension, out var mimeTypes) ||
            !mimeTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new UserFriendlyException("Only valid PDF and DOCX files are allowed.");
        }

        var fileId = Guid.NewGuid();
        var tenantSegment = _currentTenant.Id?.ToString("N") ?? "host";
        var blobName = $"{tenantSegment}/{documentId:N}/{fileId:N}{extension.ToLowerInvariant()}";
        await using var stream = file.OpenReadStream();
        await EnsureMagicBytesAsync(stream, extension, cancellationToken);
        var hash = await ComputeHashAsync(stream, cancellationToken);
        stream.Position = 0;

        var entity = new DocumentFile(
            fileId, _currentTenant.Id, documentId, displayName, blobName,
            file.ContentType, file.Length, hash);
        await _fileManager.SaveAsync(entity, stream, cancellationToken);
        return DocumentFileAppService.Map(entity);
    }

    [HttpGet("{id:guid}/content")]
    [Authorize(DocumentServicePermissions.Files.Download)]
    public async Task<IActionResult> DownloadAsync(Guid id, CancellationToken cancellationToken)
    {
        var file = await _files.FindAsync(id, cancellationToken: cancellationToken);
        if (file is null || file.BlobDeletionPending)
        {
            return NotFound();
        }
        if (!_currentUser.Id.HasValue ||
            !await _access.CanAccessFileAsync(
                file.Id, _currentUser.Id.Value))
        {
            return Forbid();
        }
        Stream stream;
        try
        {
            stream = await _blobs.GetAsync(file.BlobName, cancellationToken);
        }
        catch
        {
            if (!await _blobs.ExistsAsync(file.BlobName, CancellationToken.None))
            {
                return NotFound();
            }

            throw;
        }

        return File(stream, file.MimeType, file.DisplayName, enableRangeProcessing: true);
    }

    internal static string SanitizeDisplayName(string value)
    {
        var name = Path.GetFileName(value);
        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalid, '_');
        }

        if (string.IsNullOrWhiteSpace(name) || name.Length > DocumentConsts.FileNameMaxLength)
        {
            throw new UserFriendlyException("Invalid file name.");
        }

        return name;
    }

    internal static async Task EnsureMagicBytesAsync(
        Stream stream,
        string extension,
        CancellationToken cancellationToken)
    {
        if (!stream.CanSeek)
        {
            throw new UserFriendlyException("The upload stream must support validation.");
        }

        var header = new byte[4];
        var read = await stream.ReadAsync(header.AsMemory(), cancellationToken);
        stream.Position = 0;
        var valid = extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase)
            ? read >= 4 && header.SequenceEqual("%PDF"u8.ToArray())
            : read >= 2 && header[0] == 0x50 && header[1] == 0x4B;
        if (!valid)
        {
            throw new UserFriendlyException("File content does not match its extension.");
        }

        if (extension.Equals(".docx", StringComparison.OrdinalIgnoreCase))
        {
            ValidateDocxPackage(stream);
            stream.Position = 0;
        }
    }

    private static void ValidateDocxPackage(Stream stream)
    {
        try
        {
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            if (archive.Entries.Count > 2_048 ||
                archive.GetEntry("[Content_Types].xml") is null ||
                archive.GetEntry("word/document.xml") is null)
            {
                throw new UserFriendlyException("Invalid DOCX package.");
            }

            const long maxExpandedBytes = 209_715_200;
            long expandedBytes = 0;
            foreach (var entry in archive.Entries)
            {
                expandedBytes = checked(expandedBytes + entry.Length);
                if (expandedBytes > maxExpandedBytes)
                {
                    throw new UserFriendlyException("DOCX package expands beyond the allowed limit.");
                }
            }
        }
        catch (InvalidDataException)
        {
            throw new UserFriendlyException("Invalid DOCX package.");
        }
        catch (OverflowException)
        {
            throw new UserFriendlyException("DOCX package expands beyond the allowed limit.");
        }
    }

    private static async Task<string> ComputeHashAsync(Stream stream, CancellationToken cancellationToken)
    {
        stream.Position = 0;
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
