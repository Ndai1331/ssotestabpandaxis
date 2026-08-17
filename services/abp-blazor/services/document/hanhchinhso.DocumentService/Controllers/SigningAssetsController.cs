using System.Security.Cryptography;
using hanhchinhso.DocumentService.Permissions;
using hanhchinhso.DocumentService.Signing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.BlobStoring;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Users;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;

namespace hanhchinhso.DocumentService.Controllers;

[ApiController]
[Authorize(DocumentServicePermissions.SigningAssets.Default)]
[Route("api/document-management/signing-assets")]
public class SigningAssetsController : ControllerBase
{
    private readonly IRepository<SigningAsset, Guid> _assets;
    private readonly IBlobContainer<SigningBlobContainer> _blobs;
    private readonly SigningAssetManager _manager;
    private readonly ICurrentTenant _currentTenant;
    private readonly ICurrentUser _currentUser;
    private readonly IAuthorizationService _authorization;
    private readonly IConfiguration _configuration;

    public SigningAssetsController(
        IRepository<SigningAsset, Guid> assets,
        IBlobContainer<SigningBlobContainer> blobs,
        SigningAssetManager manager,
        ICurrentTenant currentTenant,
        ICurrentUser currentUser,
        IAuthorizationService authorization,
        IConfiguration configuration)
    {
        _assets = assets;
        _blobs = blobs;
        _manager = manager;
        _currentTenant = currentTenant;
        _currentUser = currentUser;
        _authorization = authorization;
        _configuration = configuration;
    }

    [HttpPost]
    [Authorize(DocumentServicePermissions.SigningAssets.Upload)]
    [RequestFormLimits(MultipartBodyLengthLimit = 10_485_760)]
    public async Task<SigningAssetDto> UploadAsync(
        SigningAssetKind kind,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.Id ??
            throw new Volo.Abp.Authorization.AbpAuthorizationException();
        if (!Enum.IsDefined(kind))
        {
            throw new UserFriendlyException("Invalid signing asset kind.");
        }
        if (kind == SigningAssetKind.LayoutImage)
        {
            await _authorization.CheckAsync(
                DocumentServicePermissions.SigningAssets.ManageLayouts);
        }
        var maxBytes = _configuration.GetValue<long?>(
            "SigningAssets:MaxUploadBytes") ?? 5_242_880;
        if (file.Length <= 0 || file.Length > maxBytes)
        {
            throw new UserFriendlyException(
                $"Signing image size must be between 1 and {maxBytes} bytes.");
        }
        var mime = file.ContentType.ToLowerInvariant();
        var extension = mime switch
        {
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            _ => throw new UserFriendlyException(
                "Only PNG and JPEG signing images are allowed.")
        };
        await using var source = file.OpenReadStream();
        await using var stream = await NormalizeImageAsync(
            source, extension, cancellationToken);
        if (stream.Length > maxBytes)
        {
            throw new UserFriendlyException(
                $"Normalized signing image must not exceed {maxBytes} bytes.");
        }
        var hash = Convert.ToHexString(
            await SHA256.HashDataAsync(stream, cancellationToken))
            .ToLowerInvariant();
        stream.Position = 0;
        var id = Guid.NewGuid();
        var tenant = _currentTenant.Id?.ToString("N") ?? "host";
        var blobName =
            $"{tenant}/{kind.ToString().ToLowerInvariant()}/{id:N}{extension}";
        var asset = new SigningAsset(
            id,
            _currentTenant.Id,
            kind,
            kind == SigningAssetKind.LayoutImage ? null : userId,
            SanitizeName(file.FileName),
            blobName,
            mime,
            stream.Length,
            hash);
        await _manager.SaveAsync(asset, stream, cancellationToken);
        return Map(asset);
    }

    [HttpGet("{id:guid}/content")]
    [Authorize(DocumentServicePermissions.SigningAssets.Download)]
    public async Task<IActionResult> DownloadAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var asset = await _assets.GetAsync(
            id, cancellationToken: cancellationToken);
        if (asset.BlobDeletionPending)
        {
            return NotFound();
        }
        await EnsureCanReadAsync(asset);
        var stream = await _blobs.GetAsync(
            asset.BlobName, cancellationToken);
        return File(
            stream,
            asset.MimeType,
            asset.DisplayName,
            enableRangeProcessing: true);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(DocumentServicePermissions.SigningAssets.Delete)]
    public async Task<IActionResult> DeleteAsync(
        Guid id,
        [FromQuery] string concurrencyStamp,
        CancellationToken cancellationToken)
    {
        var asset = await _assets.GetAsync(
            id, cancellationToken: cancellationToken);
        await EnsureCanDeleteAsync(asset);
        await _manager.RequestDeleteAsync(
            id, concurrencyStamp, cancellationToken);
        return NoContent();
    }

    private async Task EnsureCanReadAsync(SigningAsset asset)
    {
        if (asset.Kind == SigningAssetKind.LayoutImage ||
            asset.OwnerUserId == _currentUser.Id ||
            await _authorization.IsGrantedAsync(
                DocumentServicePermissions.UserSignatures.ManageAll))
        {
            return;
        }
        throw new Volo.Abp.Authorization.AbpAuthorizationException();
    }

    private async Task EnsureCanDeleteAsync(SigningAsset asset)
    {
        if (asset.Kind == SigningAssetKind.LayoutImage)
        {
            await _authorization.CheckAsync(
                DocumentServicePermissions.SigningAssets.ManageLayouts);
            return;
        }
        if (asset.OwnerUserId == _currentUser.Id ||
            await _authorization.IsGrantedAsync(
                DocumentServicePermissions.UserSignatures.ManageAll))
        {
            return;
        }
        throw new Volo.Abp.Authorization.AbpAuthorizationException();
    }

    internal static async Task<MemoryStream> NormalizeImageAsync(
        Stream stream,
        string extension,
        CancellationToken cancellationToken)
    {
        if (!stream.CanSeek)
        {
            throw new UserFriendlyException(
                "The upload stream must support validation.");
        }
        try
        {
            var format = await Image.DetectFormatAsync(
                stream, cancellationToken);
            var expected = extension == ".png" ? "PNG" : "JPEG";
            if (!string.Equals(
                    format.Name, expected, StringComparison.OrdinalIgnoreCase))
            {
                throw new UserFriendlyException(
                    "Image content does not match its MIME type.");
            }
            stream.Position = 0;
            var info = await Image.IdentifyAsync(
                stream, cancellationToken);
            if (info is null ||
                info.Width is < 1 or > 4096 ||
                info.Height is < 1 or > 4096 ||
                (long)info.Width * info.Height > 16_000_000)
            {
                throw new UserFriendlyException(
                    "Signing image dimensions are invalid or too large.");
            }
            stream.Position = 0;
            using var image = await Image.LoadAsync(
                stream, cancellationToken);
            var normalized = new MemoryStream();
            if (extension == ".png")
            {
                await image.SaveAsPngAsync(
                    normalized,
                    new PngEncoder
                    {
                        CompressionLevel =
                            PngCompressionLevel.DefaultCompression
                    },
                    cancellationToken);
            }
            else
            {
                await image.SaveAsJpegAsync(
                    normalized,
                    new JpegEncoder { Quality = 90 },
                    cancellationToken);
            }
            normalized.Position = 0;
            return normalized;
        }
        catch (UnknownImageFormatException)
        {
            throw new UserFriendlyException(
                "The signing image is malformed or unsupported.");
        }
        catch (InvalidImageContentException)
        {
            throw new UserFriendlyException(
                "The signing image is malformed or unsupported.");
        }
    }

    private static string SanitizeName(string name)
    {
        var value = Path.GetFileName(name);
        if (value.IsNullOrWhiteSpace() || value.Length > 255)
        {
            throw new UserFriendlyException("Invalid image file name.");
        }
        return value;
    }

    internal static SigningAssetDto Map(SigningAsset x) => new()
    {
        Id = x.Id,
        Kind = x.Kind,
        OwnerUserId = x.OwnerUserId,
        DisplayName = x.DisplayName,
        MimeType = x.MimeType,
        Size = x.Size,
        Sha256 = x.Sha256,
        ConcurrencyStamp = x.ConcurrencyStamp,
        CreationTime = x.CreationTime,
        LastModificationTime = x.LastModificationTime
    };
}
