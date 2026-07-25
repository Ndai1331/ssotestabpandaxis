using System.Text.Json;
using hanhchinhso.DocumentService.Signing;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Security.Claims;
using System.Security.Claims;
using hanhchinhso.DocumentService.Data;
using hanhchinhso.DocumentService.Controllers;
using Volo.Abp.Domain.Repositories;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace hanhchinhso.DocumentService.Tests.Signing;

public class SigningMetadataTests : DocumentServiceIntegrationTestBase
{
    [Fact]
    public void Should_Normalize_Provider_And_Reject_Invalid_Endpoint()
    {
        var entity = new SignatureSetting(
            Guid.NewGuid(),
            null,
            CreateSetting(" remote_ca "));

        entity.ProviderCode.ShouldBe("REMOTE_CA");
        entity.ApiEndpoint.ShouldBe("https://sign.example.test/api");

        var invalid = CreateSetting("other");
        invalid.ApiEndpoint = "not-an-endpoint";
        Should.Throw<BusinessException>(() =>
            new SignatureSetting(Guid.NewGuid(), null, invalid));
    }

    [Fact]
    public void Should_Require_Digital_Credential_And_Valid_Window()
    {
        var input = CreateUserSignature(SignatureType.Digital);
        Should.Throw<BusinessException>(() =>
            new UserSignature(
                Guid.NewGuid(), null, Guid.NewGuid(), Guid.NewGuid(), input, null));

        input.Secret = "write-only";
        input.ValidFromUtc = new DateTime(2026, 7, 25, 0, 0, 0, DateTimeKind.Utc);
        input.ValidToUtc = input.ValidFromUtc.Value.AddDays(-1);
        Should.Throw<BusinessException>(() =>
            new UserSignature(
                Guid.NewGuid(), null, Guid.NewGuid(), Guid.NewGuid(), input, "protected"));
    }

    [Fact]
    public void Should_Protect_Secret_With_Record_Scoped_Purpose()
    {
        var protector = new UserSignatureSecretProtector(
            GetRequiredService<IDataProtectionProvider>());
        var firstId = Guid.NewGuid();
        var protectedValue = protector.Protect(
            null, firstId, "remote_ca", "sensitive");

        protectedValue.ShouldNotContain("sensitive");
        protector.Unprotect(
            null, firstId, "REMOTE_CA", protectedValue)
            .ShouldBe("sensitive");
        Should.Throw<Exception>(() =>
            protector.Unprotect(
                null, Guid.NewGuid(), "REMOTE_CA", protectedValue));
    }

    [Fact]
    public void Should_Never_Expose_Protected_Secret_In_Read_Dto()
    {
        var json = JsonSerializer.Serialize(new UserSignatureDto
        {
            Id = Guid.NewGuid(),
            IdentityUserId = Guid.NewGuid(),
            ProviderCode = "REMOTE_CA",
            SignatureType = SignatureType.Digital,
            SignatureAssetId = Guid.NewGuid(),
            HasSecret = true
        });

        json.ShouldContain("\"HasSecret\":true");
        json.ShouldNotContain("ProtectedSecret");
        json.ShouldNotContain("\"Secret\"");
    }

    [Fact]
    public async Task Should_Persist_Encrypted_Secret_And_Preserve_On_Blank_Update()
    {
        var userId = Guid.NewGuid();
        using var principal = ChangeUser(userId);
        var settings = GetRequiredService<ISignatureSettingAppService>();
        var signatures = GetRequiredService<IUserSignatureAppService>();
        var setting = await settings.CreateAsync(CreateSetting("remote_ca"));
        var signatureAssetId = await CreateAssetAsync(
            userId, SigningAssetKind.SignatureImage);

        var created = await signatures.CreateAsync(new()
        {
            SignatureType = SignatureType.Digital,
            ProviderCode = "remote_ca",
            TokenReference = "token-1",
            Secret = "plain-secret",
            SignatureAssetId = signatureAssetId,
            IsActive = true
        });

        created.SignatureSettingId.ShouldBe(setting.Id);
        created.HasSecret.ShouldBeTrue();
        var firstCiphertext = await WithUnitOfWorkAsync(async () =>
        {
            var row = await GetRequiredService<DocumentServiceDbContext>()
                .UserSignatures.SingleAsync(x => x.Id == created.Id);
            row.ProtectedSecret.ShouldNotBeNull();
            row.ProtectedSecret.ShouldNotContain("plain-secret");
            return row.ProtectedSecret;
        });

        var updated = await signatures.UpdateAsync(created.Id, new()
        {
            IdentityUserId = userId,
            SignatureType = SignatureType.Digital,
            ProviderCode = "REMOTE_CA",
            TokenReference = "token-2",
            Secret = null,
            SignatureAssetId = signatureAssetId,
            IsActive = true,
            ConcurrencyStamp = created.ConcurrencyStamp
        });
        updated.HasSecret.ShouldBeTrue();
        await WithUnitOfWorkAsync(async () =>
            (await GetRequiredService<DocumentServiceDbContext>()
                .UserSignatures.SingleAsync(x => x.Id == created.Id))
            .ProtectedSecret.ShouldBe(firstCiphertext));

        var revoked = await signatures.RevokeCredentialAsync(
            created.Id, updated.ConcurrencyStamp);
        revoked.IsActive.ShouldBeFalse();
        revoked.HasSecret.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Block_Provider_Delete_And_Stale_Update_When_Referenced()
    {
        var userId = Guid.NewGuid();
        using var principal = ChangeUser(userId);
        var settings = GetRequiredService<ISignatureSettingAppService>();
        var signatures = GetRequiredService<IUserSignatureAppService>();
        var setting = await settings.CreateAsync(CreateSetting("remote_ca_2"));
        var signatureAssetId = await CreateAssetAsync(
            userId, SigningAssetKind.SignatureImage);
        await signatures.CreateAsync(new()
        {
            SignatureType = SignatureType.Electronic,
            ProviderCode = setting.ProviderCode,
            SignatureAssetId = signatureAssetId,
            IsActive = true
        });

        await Should.ThrowAsync<BusinessException>(() =>
            settings.DeleteAsync(setting.Id, setting.ConcurrencyStamp));
        var update = CreateSetting(setting.ProviderCode);
        update.ConcurrencyStamp = "stale";
        await Should.ThrowAsync<Volo.Abp.Data.AbpDbConcurrencyException>(() =>
            settings.UpdateAsync(setting.Id, update));
    }

    [Fact]
    public void Should_Fail_Closed_Endpoint_Policy_Without_Allowlist()
    {
        var empty = new ConfigurationBuilder().Build();
        var policy = new SigningEndpointPolicy(empty);
        Should.Throw<BusinessException>(() =>
            policy.Validate("https://sign.example.test"));

        var configured = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Signing:AllowedHosts:0"] = "sign.example.test"
            })
            .Build();
        new SigningEndpointPolicy(configured)
            .Validate("https://sign.example.test/api");
        Should.Throw<BusinessException>(() =>
            new SigningEndpointPolicy(configured)
                .Validate("http://sign.example.test/api"));
        Should.Throw<BusinessException>(() =>
            new SigningEndpointPolicy(configured)
                .Validate("https://127.0.0.1/api"));
        Should.Throw<BusinessException>(() =>
            new SigningEndpointPolicy(configured)
                .Validate("https://user:secret@sign.example.test/api"));
        Should.Throw<BusinessException>(() =>
            new SigningEndpointPolicy(configured)
                .Validate("https://sign.example.test/api?target=x"));

        var privateConfigured = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Signing:AllowedHosts:0"] = "127.0.0.1",
                ["Signing:AllowedPrivateHosts:0"] = "127.0.0.1"
            })
            .Build();
        new SigningEndpointPolicy(privateConfigured)
            .Validate("https://127.0.0.1/api");

        Should.Throw<HttpRequestException>(() =>
            SigningNetworkPolicy.EnsureAddressAllowed(
                "sign.example.test",
                System.Net.IPAddress.Loopback,
                new HashSet<string>()));
        SigningNetworkPolicy.IsPrivateOrLocal(
                System.Net.IPAddress.Parse("::ffff:127.0.0.1"))
            .ShouldBeTrue();
        SigningNetworkPolicy.IsPrivateOrLocal(
                System.Net.IPAddress.Parse("::ffff:10.0.0.1"))
            .ShouldBeTrue();
        SigningNetworkPolicy.IsPrivateOrLocal(
                System.Net.IPAddress.Any)
            .ShouldBeTrue();
        SigningNetworkPolicy.IsPrivateOrLocal(
                System.Net.IPAddress.IPv6Any)
            .ShouldBeTrue();
        SigningNetworkPolicy.EnsureAddressAllowed(
            "private-sign.example.test",
            System.Net.IPAddress.Parse("10.0.0.2"),
            new HashSet<string>(
                ["private-sign.example.test"],
                StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Should_Reject_Cross_User_And_Wrong_Kind_Assets()
    {
        var userId = Guid.NewGuid();
        using var principal = ChangeUser(userId);
        var foreignAssetId = await CreateAssetAsync(
            Guid.NewGuid(), SigningAssetKind.SignatureImage);
        var wrongKindId = await CreateAssetAsync(
            userId, SigningAssetKind.SealImage);
        var settings = GetRequiredService<ISignatureSettingAppService>();
        var setting = await settings.CreateAsync(
            CreateSetting("asset_validation"));
        var signatures = GetRequiredService<IUserSignatureAppService>();

        await Should.ThrowAsync<EntityNotFoundException>(() =>
            signatures.CreateAsync(new()
            {
                SignatureType = SignatureType.Electronic,
                ProviderCode = setting.ProviderCode,
                SignatureAssetId = foreignAssetId,
                IsActive = true
            }));
        await Should.ThrowAsync<EntityNotFoundException>(() =>
            signatures.CreateAsync(new()
            {
                SignatureType = SignatureType.Electronic,
                ProviderCode = setting.ProviderCode,
                SignatureAssetId = wrongKindId,
                IsActive = true
            }));

        var invalidLayout = CreateSetting("layout_validation");
        invalidLayout.LayoutAssetId = wrongKindId;
        await Should.ThrowAsync<EntityNotFoundException>(() =>
            settings.CreateAsync(invalidLayout));
    }

    [Fact]
    public async Task Should_Decode_And_Normalize_Signing_Image()
    {
        await using var source = new MemoryStream();
        using (var image = new Image<Rgba32>(2, 2))
        {
            await image.SaveAsPngAsync(source);
        }
        var trailer = "untrusted-trailer"u8.ToArray();
        await source.WriteAsync(trailer);
        source.Position = 0;

        await using var normalized =
            await SigningAssetsController.NormalizeImageAsync(
                source, ".png", CancellationToken.None);
        var bytes = normalized.ToArray();

        bytes.AsSpan().EndsWith(trailer).ShouldBeFalse();
        using var decoded = await Image.LoadAsync(normalized);
        decoded.Width.ShouldBe(2);
        decoded.Height.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Reject_Malformed_And_Oversized_Signing_Image()
    {
        await using var malformed = new MemoryStream(
            [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a]);
        await Should.ThrowAsync<UserFriendlyException>(() =>
            SigningAssetsController.NormalizeImageAsync(
                malformed, ".png", CancellationToken.None));

        await using var oversized = new MemoryStream();
        using (var image = new Image<Rgba32>(4097, 1))
        {
            await image.SaveAsPngAsync(oversized);
        }
        oversized.Position = 0;
        await Should.ThrowAsync<UserFriendlyException>(() =>
            SigningAssetsController.NormalizeImageAsync(
                oversized, ".png", CancellationToken.None));
    }

    [Fact]
    public async Task Should_Block_Deleting_Referenced_Signing_Asset()
    {
        var userId = Guid.NewGuid();
        using var principal = ChangeUser(userId);
        var settings = GetRequiredService<ISignatureSettingAppService>();
        var signatures = GetRequiredService<IUserSignatureAppService>();
        var setting = await settings.CreateAsync(
            CreateSetting("asset_delete_guard"));
        var assetId = await CreateAssetAsync(
            userId, SigningAssetKind.SignatureImage);
        await signatures.CreateAsync(new()
        {
            SignatureType = SignatureType.Electronic,
            ProviderCode = setting.ProviderCode,
            SignatureAssetId = assetId,
            IsActive = true
        });
        var asset = await WithUnitOfWorkAsync(async () =>
            await GetRequiredService<IRepository<SigningAsset, Guid>>()
                .GetAsync(assetId));

        await Should.ThrowAsync<BusinessException>(() =>
            GetRequiredService<SigningAssetManager>()
                .RequestDeleteAsync(
                    assetId,
                    asset.ConcurrencyStamp,
                    CancellationToken.None));
    }

    private static CreateUpdateSignatureSettingDto CreateSetting(
        string providerCode) => new()
    {
        ProviderCode = providerCode,
        ProviderType = SignatureProviderType.RemoteCa,
        ApiEndpoint = "https://sign.example.test/api",
        DefaultSignatureType = SignatureType.Digital,
        AllowDigitalSign = true,
        SignWidth = 150,
        SignHeight = 70,
        SignedFileSuffix = "-signed"
    };

    private static CreateUserSignatureDto CreateUserSignature(
        SignatureType signatureType) => new()
    {
        SignatureType = signatureType,
        ProviderCode = "REMOTE_CA",
        TokenReference = "token-ref",
        SignatureAssetId = Guid.NewGuid(),
        IsActive = true
    };

    private async Task<Guid> CreateAssetAsync(
        Guid ownerId,
        SigningAssetKind kind)
    {
        var id = Guid.NewGuid();
        await WithUnitOfWorkAsync(async () =>
            await GetRequiredService<IRepository<SigningAsset, Guid>>()
                .InsertAsync(new SigningAsset(
                    id,
                    null,
                    kind,
                    kind == SigningAssetKind.LayoutImage ? null : ownerId,
                    "test.png",
                    $"host/{kind}/{id:N}.png",
                    "image/png",
                    8,
                    new string('a', 64)),
                    autoSave: true));
        return id;
    }

    private IDisposable ChangeUser(Guid userId)
    {
        var accessor = GetRequiredService<ICurrentPrincipalAccessor>();
        return accessor.Change(new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(AbpClaimTypes.UserId, userId.ToString()),
                new Claim(AbpClaimTypes.UserName, $"user-{userId:N}")
            ],
            "Test")));
    }
}
