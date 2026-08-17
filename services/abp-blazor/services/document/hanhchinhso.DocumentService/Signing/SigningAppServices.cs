using hanhchinhso.DocumentService.Permissions;
using hanhchinhso.DocumentService.Workflows;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace hanhchinhso.DocumentService.Signing;

[Authorize(DocumentServicePermissions.SignatureSettings.Default)]
public class SignatureSettingAppService :
    ApplicationService,
    ISignatureSettingAppService
{
    private readonly IRepository<SignatureSetting, Guid> _repository;
    private readonly IRepository<UserSignature, Guid> _userSignatures;
    private readonly ISigningEndpointPolicy _endpointPolicy;
    private readonly ISigningMutationCoordinator _mutationCoordinator;
    private readonly IRepository<SigningAsset, Guid> _assets;
    private readonly ISigningAssetLock _assetLock;

    public SignatureSettingAppService(
        IRepository<SignatureSetting, Guid> repository,
        IRepository<UserSignature, Guid> userSignatures,
        ISigningEndpointPolicy endpointPolicy,
        ISigningMutationCoordinator mutationCoordinator,
        IRepository<SigningAsset, Guid> assets,
        ISigningAssetLock assetLock)
    {
        _repository = repository;
        _userSignatures = userSignatures;
        _endpointPolicy = endpointPolicy;
        _mutationCoordinator = mutationCoordinator;
        _assets = assets;
        _assetLock = assetLock;
    }

    public async Task<SignatureSettingDto> GetAsync(Guid id) =>
        Map(await _repository.GetAsync(id));

    public async Task<PagedResultDto<SignatureSettingDto>> GetListAsync(
        SigningListInput input)
    {
        var query = (await _repository.GetQueryableAsync())
            .WhereIf(!input.FilterText.IsNullOrWhiteSpace(),
                x => x.ProviderCode.Contains(input.FilterText!))
            .WhereIf(input.IsActive.HasValue,
                x => x.IsActive == input.IsActive);
        var total = await AsyncExecuter.CountAsync(query);
        var rows = await AsyncExecuter.ToListAsync(
            query.OrderBy(x => x.ProviderCode)
                .PageBy(input.SkipCount, input.MaxResultCount));
        return new(total, rows.Select(Map).ToList());
    }

    [Authorize(DocumentServicePermissions.SignatureSettings.Create)]
    public async Task<SignatureSettingDto> CreateAsync(
        CreateUpdateSignatureSettingDto input)
    {
        _endpointPolicy.Validate(input.ApiEndpoint);
        return await _assetLock.ExecuteAsync(
            CurrentTenant.Id,
            GetAssetIds(input.LayoutAssetId),
            () => _mutationCoordinator.ExecuteAsync(
                CurrentTenant.Id,
                input.ProviderCode,
                async () =>
                {
                    await ValidateLayoutAssetAsync(input.LayoutAssetId);
                    var entity = new SignatureSetting(
                        GuidGenerator.Create(), CurrentTenant.Id, input);
                    await _repository.InsertAsync(entity, autoSave: true);
                    return Map(entity);
                }));
    }

    [Authorize(DocumentServicePermissions.SignatureSettings.Update)]
    public async Task<SignatureSettingDto> UpdateAsync(
        Guid id,
        CreateUpdateSignatureSettingDto input)
    {
        _endpointPolicy.Validate(input.ApiEndpoint);
        return await _assetLock.ExecuteAsync(
            CurrentTenant.Id,
            GetAssetIds(input.LayoutAssetId),
            () => _mutationCoordinator.ExecuteAsync(
                CurrentTenant.Id,
                input.ProviderCode,
                async () =>
                {
                    await ValidateLayoutAssetAsync(input.LayoutAssetId);
                    var entity = await _repository.GetAsync(id);
                    EnsureConcurrency(
                        entity.ConcurrencyStamp, input.ConcurrencyStamp);
                    if (!string.Equals(
                            entity.ProviderCode,
                            SignatureSetting.NormalizeProviderCode(
                                input.ProviderCode),
                            StringComparison.Ordinal))
                    {
                        throw new BusinessException(
                            "DocumentService:SignatureProviderCodeImmutable");
                    }
                    entity.Update(input);
                    await _repository.UpdateAsync(entity, autoSave: true);
                    return Map(entity);
                }));
    }

    [Authorize(DocumentServicePermissions.SignatureSettings.Delete)]
    public async Task DeleteAsync(Guid id, string concurrencyStamp)
    {
        var query = await _repository.GetQueryableAsync();
        var providerCode = await AsyncExecuter.FirstOrDefaultAsync(
            query.AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => x.ProviderCode));
        if (providerCode.IsNullOrWhiteSpace())
        {
            throw new EntityNotFoundException(
                typeof(SignatureSetting), id);
        }
        await _mutationCoordinator.ExecuteAsync(
            CurrentTenant.Id,
            providerCode!,
            async () =>
            {
                var entity = await _repository.GetAsync(id);
                EnsureConcurrency(
                    entity.ConcurrencyStamp, concurrencyStamp);
                if (await _userSignatures.AnyAsync(
                        x => x.SignatureSettingId == id))
                {
                    throw new BusinessException(
                        "DocumentService:SignatureProviderInUse");
                }
                await _repository.DeleteAsync(entity, autoSave: true);
                return true;
            });
    }

    private static SignatureSettingDto Map(SignatureSetting x) => new()
    {
        Id = x.Id,
        ProviderCode = x.ProviderCode,
        ProviderType = x.ProviderType,
        ApiEndpoint = x.ApiEndpoint,
        LayoutAssetId = x.LayoutAssetId,
        ApiTimeoutSeconds = x.ApiTimeoutSeconds,
        DefaultSignatureType = x.DefaultSignatureType,
        AllowElectronicSign = x.AllowElectronicSign,
        AllowDigitalSign = x.AllowDigitalSign,
        RequireOtp = x.RequireOtp,
        SignWidth = x.SignWidth,
        SignHeight = x.SignHeight,
        SignedFileSuffix = x.SignedFileSuffix,
        KeepOriginalFile = x.KeepOriginalFile,
        OverwriteSignedFile = x.OverwriteSignedFile,
        EnableSignLog = x.EnableSignLog,
        IsActive = x.IsActive,
        CreationTime = x.CreationTime,
        LastModificationTime = x.LastModificationTime,
        ConcurrencyStamp = x.ConcurrencyStamp
    };

    internal static void EnsureConcurrency(string actual, string supplied)
    {
        if (!string.Equals(actual, supplied, StringComparison.Ordinal))
        {
            throw new AbpDbConcurrencyException();
        }
    }

    private async Task ValidateLayoutAssetAsync(Guid? assetId)
    {
        if (!assetId.HasValue)
        {
            return;
        }
        var query = await _assets.GetQueryableAsync();
        if (!await AsyncExecuter.AnyAsync(query.AsNoTracking().Where(x =>
                x.Id == assetId.Value &&
                x.Kind == SigningAssetKind.LayoutImage &&
                !x.BlobDeletionPending)))
        {
            throw new EntityNotFoundException(
                typeof(SigningAsset), assetId.Value);
        }
    }

    private static IEnumerable<Guid> GetAssetIds(Guid? assetId) =>
        assetId.HasValue ? [assetId.Value] : [];
}

[Authorize(DocumentServicePermissions.UserSignatures.Default)]
public class UserSignatureAppService :
    ApplicationService,
    IUserSignatureAppService
{
    private readonly IRepository<UserSignature, Guid> _repository;
    private readonly IRepository<SignatureSetting, Guid> _settings;
    private readonly IWorkflowIdentityReferenceValidator _identityValidator;
    private readonly IUserSignatureSecretProtector _secretProtector;
    private readonly ISigningMutationCoordinator _mutationCoordinator;
    private readonly IRepository<SigningAsset, Guid> _assets;
    private readonly ISigningAssetLock _assetLock;

    public UserSignatureAppService(
        IRepository<UserSignature, Guid> repository,
        IRepository<SignatureSetting, Guid> settings,
        IWorkflowIdentityReferenceValidator identityValidator,
        IUserSignatureSecretProtector secretProtector,
        ISigningMutationCoordinator mutationCoordinator,
        IRepository<SigningAsset, Guid> assets,
        ISigningAssetLock assetLock)
    {
        _repository = repository;
        _settings = settings;
        _identityValidator = identityValidator;
        _secretProtector = secretProtector;
        _mutationCoordinator = mutationCoordinator;
        _assets = assets;
        _assetLock = assetLock;
    }

    public async Task<UserSignatureDto> GetAsync(Guid id)
    {
        var entity = await _repository.GetAsync(id);
        await EnsureOwnerOrAdminAsync(entity.IdentityUserId);
        return Map(entity);
    }

    public async Task<PagedResultDto<UserSignatureDto>> GetListAsync(
        SigningListInput input)
    {
        var query = await _repository.GetQueryableAsync();
        if (!await AuthorizationService.IsGrantedAsync(
                DocumentServicePermissions.UserSignatures.ManageAll))
        {
            query = query.Where(x => x.IdentityUserId == RequireCurrentUser());
        }
        query = query
            .WhereIf(!input.FilterText.IsNullOrWhiteSpace(),
                x => x.ProviderCode.Contains(input.FilterText!))
            .WhereIf(input.IsActive.HasValue,
                x => x.IsActive == input.IsActive);
        var total = await AsyncExecuter.CountAsync(query);
        var rows = await AsyncExecuter.ToListAsync(
            query.OrderByDescending(x => x.CreationTime)
                .PageBy(input.SkipCount, input.MaxResultCount));
        return new(total, rows.Select(Map).ToList());
    }

    [Authorize(DocumentServicePermissions.UserSignatures.Create)]
    public async Task<UserSignatureDto> CreateAsync(CreateUserSignatureDto input)
    {
        var id = GuidGenerator.Create();
        var userId = await ResolveTargetUserAsync(input.IdentityUserId);
        await ValidateUserAsync(userId);
        return await _assetLock.ExecuteAsync(
            CurrentTenant.Id,
            GetAssetIds(input),
            () => _mutationCoordinator.ExecuteAsync(
                CurrentTenant.Id,
                input.ProviderCode,
                async () =>
                {
                    await ValidateAssetsAsync(
                        userId,
                        input.SignatureAssetId,
                        input.SealAssetId);
                    var settingId =
                        await ResolveSettingIdAsync(input.ProviderCode);
                    var protectedSecret = input.Secret.IsNullOrWhiteSpace()
                        ? null
                        : _secretProtector.Protect(
                            CurrentTenant.Id,
                            id,
                            input.ProviderCode,
                            input.Secret!);
                    var entity = new UserSignature(
                        id,
                        CurrentTenant.Id,
                        settingId,
                        userId,
                        input,
                        protectedSecret);
                    await _repository.InsertAsync(
                        entity, autoSave: true);
                    return Map(entity);
                }));
    }

    [Authorize(DocumentServicePermissions.UserSignatures.Update)]
    public async Task<UserSignatureDto> UpdateAsync(
        Guid id,
        UpdateUserSignatureDto input)
    {
        var userId = await ResolveTargetUserAsync(input.IdentityUserId);
        await ValidateUserAsync(userId);
        return await _assetLock.ExecuteAsync(
            CurrentTenant.Id,
            GetAssetIds(input),
            () => _mutationCoordinator.ExecuteAsync(
                CurrentTenant.Id,
                input.ProviderCode,
                async () =>
                {
                    var entity = await _repository.GetAsync(id);
                    await EnsureOwnerOrAdminAsync(entity.IdentityUserId);
                    SignatureSettingAppService.EnsureConcurrency(
                        entity.ConcurrencyStamp,
                        input.ConcurrencyStamp);
                    await ValidateAssetsAsync(
                        userId,
                        input.SignatureAssetId,
                        input.SealAssetId);
                    var settingId =
                        await ResolveSettingIdAsync(input.ProviderCode);
                    if (!string.Equals(
                            entity.ProviderCode,
                            SignatureSetting.NormalizeProviderCode(
                                input.ProviderCode),
                            StringComparison.Ordinal) &&
                        input.Secret.IsNullOrWhiteSpace())
                    {
                        throw new BusinessException(
                            "DocumentService:SecretRequiredWhenProviderChanges");
                    }
                    var protectedSecret = input.Secret.IsNullOrWhiteSpace()
                        ? null
                        : _secretProtector.Protect(
                            CurrentTenant.Id,
                            id,
                            input.ProviderCode,
                            input.Secret!);
                    entity.Update(
                        settingId,
                        userId,
                        input,
                        protectedSecret);
                    await _repository.UpdateAsync(
                        entity, autoSave: true);
                    return Map(entity);
                }));
    }

    [Authorize(DocumentServicePermissions.UserSignatures.Delete)]
    public async Task DeleteAsync(Guid id, string concurrencyStamp)
    {
        await _mutationCoordinator.ExecuteAsync(
            CurrentTenant.Id,
            string.Empty,
            async () =>
            {
                var entity = await _repository.GetAsync(id);
                await EnsureOwnerOrAdminAsync(entity.IdentityUserId);
                SignatureSettingAppService.EnsureConcurrency(
                    entity.ConcurrencyStamp, concurrencyStamp);
                await _repository.DeleteAsync(entity, autoSave: true);
                return true;
            });
    }

    [Authorize(DocumentServicePermissions.UserSignatures.RevokeCredential)]
    public Task<UserSignatureDto> RevokeCredentialAsync(
        Guid id,
        string concurrencyStamp) =>
        _mutationCoordinator.ExecuteAsync(
            CurrentTenant.Id,
            string.Empty,
            async () =>
            {
                var entity = await _repository.GetAsync(id);
                await EnsureOwnerOrAdminAsync(entity.IdentityUserId);
                SignatureSettingAppService.EnsureConcurrency(
                    entity.ConcurrencyStamp, concurrencyStamp);
                entity.RevokeCredential();
                await _repository.UpdateAsync(entity, autoSave: true);
                return Map(entity);
            });

    private Task ValidateUserAsync(Guid userId) =>
        _identityValidator.ValidateAsync([userId], [], null);

    private async Task<Guid> ResolveSettingIdAsync(
        string providerCode)
    {
        var normalizedCode =
            SignatureSetting.NormalizeProviderCode(providerCode);
        var query = await _settings.GetQueryableAsync();
        var id = await AsyncExecuter.FirstOrDefaultAsync(
            query.AsNoTracking()
                .Where(x =>
                    x.ProviderCode == normalizedCode &&
                    x.IsActive)
                .Select(x => x.Id));
        if (id == Guid.Empty)
        {
            throw new EntityNotFoundException(
                typeof(SignatureSetting), normalizedCode);
        }
        return id;
    }

    private async Task<Guid> ResolveTargetUserAsync(Guid? requestedUserId)
    {
        var currentUserId = RequireCurrentUser();
        var target = requestedUserId.GetValueOrDefault(currentUserId);
        if (target != currentUserId &&
            !await AuthorizationService.IsGrantedAsync(
                DocumentServicePermissions.UserSignatures.ManageAll))
        {
            throw new AbpAuthorizationException(
                "Managing another user's signature requires ManageAll.");
        }
        return target;
    }

    private async Task EnsureOwnerOrAdminAsync(Guid ownerId)
    {
        if (ownerId != RequireCurrentUser() &&
            !await AuthorizationService.IsGrantedAsync(
                DocumentServicePermissions.UserSignatures.ManageAll))
        {
            throw new AbpAuthorizationException(
                "The user signature belongs to another user.");
        }
    }

    private Guid RequireCurrentUser() =>
        CurrentUser.Id ?? throw new AbpAuthorizationException();

    private static UserSignatureDto Map(UserSignature x) => new()
    {
        Id = x.Id,
        SignatureSettingId = x.SignatureSettingId,
        IdentityUserId = x.IdentityUserId,
        SignatureType = x.SignatureType,
        ProviderCode = x.ProviderCode,
        TokenReference = x.TokenReference,
        HasSecret = x.HasSecret,
        SealAssetId = x.SealAssetId,
        SignatureAssetId = x.SignatureAssetId,
        ValidFromUtc = x.ValidFromUtc,
        ValidToUtc = x.ValidToUtc,
        IsActive = x.IsActive,
        CreationTime = x.CreationTime,
        LastModificationTime = x.LastModificationTime,
        ConcurrencyStamp = x.ConcurrencyStamp
    };

    private async Task ValidateAssetsAsync(
        Guid ownerUserId,
        Guid signatureAssetId,
        Guid? sealAssetId)
    {
        if (signatureAssetId == Guid.Empty)
        {
            throw new EntityNotFoundException(
                typeof(SigningAsset), signatureAssetId);
        }
        var ids = sealAssetId.HasValue
            ? new[] { signatureAssetId, sealAssetId.Value }
            : new[] { signatureAssetId };
        var query = await _assets.GetQueryableAsync();
        var assets = await AsyncExecuter.ToListAsync(
            query.AsNoTracking().Where(x =>
                ids.Contains(x.Id) &&
                !x.BlobDeletionPending));
        var signature = assets.SingleOrDefault(
            x => x.Id == signatureAssetId);
        if (signature is null ||
            signature.Kind != SigningAssetKind.SignatureImage ||
            signature.OwnerUserId != ownerUserId)
        {
            throw new EntityNotFoundException(
                typeof(SigningAsset), signatureAssetId);
        }
        if (sealAssetId.HasValue)
        {
            var seal = assets.SingleOrDefault(
                x => x.Id == sealAssetId.Value);
            if (seal is null ||
                seal.Kind != SigningAssetKind.SealImage ||
                seal.OwnerUserId != ownerUserId)
            {
                throw new EntityNotFoundException(
                    typeof(SigningAsset), sealAssetId.Value);
            }
        }
    }

    private static IEnumerable<Guid> GetAssetIds(
        CreateUserSignatureDto input)
    {
        yield return input.SignatureAssetId;
        if (input.SealAssetId.HasValue)
        {
            yield return input.SealAssetId.Value;
        }
    }
}
