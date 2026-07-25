using hanhchinhso.DocumentService.Permissions;
using hanhchinhso.DocumentService.Workflows;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.BlobStoring;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Identity;
using Volo.Abp.Linq;
using Volo.Abp.Uow;
using Volo.Abp.Users;

namespace hanhchinhso.DocumentService.Documents;

[Authorize(DocumentServicePermissions.Documents.Default)]
public class DocumentAppService :
    CrudAppService<Document, DocumentDto, Guid, DocumentListInput, CreateUpdateDocumentDto>,
    IDocumentAppService
{
    private const int MaxHierarchyDepth = 100;
    private readonly IOrganizationUnitAppService _organizationUnits;
    private readonly IAbpDistributedLock _distributedLock;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public DocumentAppService(
        IRepository<Document, Guid> repository,
        IOrganizationUnitAppService organizationUnits,
        IAbpDistributedLock distributedLock,
        IUnitOfWorkManager unitOfWorkManager) : base(repository)
    {
        _organizationUnits = organizationUnits;
        _distributedLock = distributedLock;
        _unitOfWorkManager = unitOfWorkManager;
        CreatePolicyName = DocumentServicePermissions.Documents.Create;
        UpdatePolicyName = DocumentServicePermissions.Documents.Update;
        DeletePolicyName = DocumentServicePermissions.Documents.Delete;
    }

    public override async Task<DocumentDto> CreateAsync(CreateUpdateDocumentDto input)
    {
        await ValidateInputAsync(input);
        return await base.CreateAsync(input);
    }

    public override async Task<DocumentDto> UpdateAsync(Guid id, CreateUpdateDocumentDto input)
    {
        var tenantKey = CurrentTenant.Id?.ToString("N") ?? "host";
        await using var handle = await _distributedLock.TryAcquireAsync(
            $"document-hierarchy:{tenantKey}",
            TimeSpan.FromSeconds(30));
        if (handle is null)
        {
            throw new Volo.Abp.UserFriendlyException(
                "The document hierarchy is busy. Please retry the update.");
        }

        using var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);
        await ValidateInputAsync(input, id);
        var result = await base.UpdateAsync(id, input);
        await uow.CompleteAsync();
        return result;
    }

    protected override async Task<IQueryable<Document>> CreateFilteredQueryAsync(DocumentListInput input)
    {
        var query = await Repository.GetQueryableAsync();
        return query
            .WhereIf(!input.FilterText.IsNullOrWhiteSpace(),
                x => (x.Number != null && x.Number.Contains(input.FilterText!)) ||
                     x.Title.Contains(input.FilterText!) ||
                     x.StorageNumber.Contains(input.FilterText!))
            .WhereIf(!input.Number.IsNullOrWhiteSpace(), x => x.Number == input.Number)
            .WhereIf(!input.CurrentStatus.IsNullOrWhiteSpace(), x => x.CurrentStatus == input.CurrentStatus)
            .WhereIf(input.OrganizationUnitId.HasValue,
                x => x.OrganizationUnitId == input.OrganizationUnitId)
            .WhereIf(input.SourceType.HasValue, x => x.SourceType == input.SourceType);
    }

    protected override Task<Document> MapToEntityAsync(CreateUpdateDocumentDto input) =>
        Task.FromResult(new Document(GuidGenerator.Create(), CurrentTenant.Id, input, CurrentUser.Id));

    protected override Task MapToEntityAsync(CreateUpdateDocumentDto input, Document entity)
    {
        entity.Update(input);
        entity.SetConcurrencyStampIfNotNull(input.ConcurrencyStamp);
        return Task.CompletedTask;
    }

    protected override DocumentDto MapToGetOutputDto(Document entity) => new()
    {
        Id = entity.Id,
        Number = entity.Number,
        Title = entity.Title,
        CurrentStatus = entity.CurrentStatus,
        CompletedTime = entity.CompletedTime,
        StorageNumber = entity.StorageNumber,
        IncomingDate = entity.IncomingDate,
        FieldId = entity.FieldId,
        UnitId = entity.UnitId,
        StatusId = entity.StatusId,
        TypeId = entity.TypeId,
        UrgencyLevelId = entity.UrgencyLevelId,
        SecrecyLevelId = entity.SecrecyLevelId,
        SourceType = entity.SourceType,
        OrganizationUnitId = entity.OrganizationUnitId,
        FromUserId = entity.FromUserId,
        ReceiverUserId = entity.ReceiverUserId,
        ParentDocumentId = entity.ParentDocumentId,
        CreationTime = entity.CreationTime,
        ConcurrencyStamp = entity.ConcurrencyStamp
    };

    private async Task ValidateOrganizationUnitAsync(Guid? organizationUnitId)
    {
        if (organizationUnitId.HasValue)
        {
            await _organizationUnits.GetAsync(organizationUnitId.Value);
        }
    }

    private async Task ValidateInputAsync(
        CreateUpdateDocumentDto input,
        Guid? currentDocumentId = null)
    {
        if (!Enum.IsDefined(input.SourceType))
        {
            throw new Volo.Abp.UserFriendlyException("Invalid document source type.");
        }

        if (input.IncomingDate == default)
        {
            throw new Volo.Abp.UserFriendlyException("Incoming date is required.");
        }

        if (input.ParentDocumentId.HasValue)
        {
            if (input.ParentDocumentId == currentDocumentId)
            {
                throw new Volo.Abp.UserFriendlyException("A document cannot be its own parent.");
            }

            var parent = await Repository.GetAsync(input.ParentDocumentId.Value);
            var visited = new HashSet<Guid>();
            var depth = 0;
            while (parent.ParentDocumentId.HasValue)
            {
                if (++depth > MaxHierarchyDepth)
                {
                    throw new Volo.Abp.UserFriendlyException(
                        $"Document hierarchy cannot exceed {MaxHierarchyDepth} levels.");
                }

                if (!visited.Add(parent.Id) || parent.ParentDocumentId == currentDocumentId)
                {
                    throw new Volo.Abp.UserFriendlyException(
                        "The selected parent would create a document hierarchy cycle.");
                }

                parent = await Repository.GetAsync(parent.ParentDocumentId.Value);
            }
        }

        await ValidateOrganizationUnitAsync(input.OrganizationUnitId);
    }
}

[Authorize(DocumentServicePermissions.Files.Default)]
public class DocumentFileAppService :
    ApplicationService,
    IDocumentFileAppService
{
    private readonly IRepository<DocumentFile, Guid> _files;
    private readonly DocumentFileManager _fileManager;
    private readonly IWorkflowDocumentAccessService _access;
    private readonly ICurrentUser _currentUser;

    public DocumentFileAppService(
        IRepository<DocumentFile, Guid> files,
        DocumentFileManager fileManager,
        IWorkflowDocumentAccessService access,
        ICurrentUser currentUser)
    {
        _files = files;
        _fileManager = fileManager;
        _access = access;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<DocumentFileDto>> GetListAsync(Guid documentId)
    {
        var userId = _currentUser.Id ??
            throw new Volo.Abp.Authorization.AbpAuthorizationException();
        if (!await _access.CanAccessDocumentAsync(documentId, userId))
        {
            throw new Volo.Abp.Authorization.AbpAuthorizationException();
        }
        var accessibleFileIds = await _access.GetAccessibleFileIdsAsync(
            documentId, userId);
        var files = await _files.GetListAsync(x =>
            x.DocumentId == documentId &&
            !x.BlobDeletionPending &&
            accessibleFileIds.Contains(x.Id));
        return files
            .OrderByDescending(x => x.UploadedAt)
            .Select(Map)
            .ToList();
    }

    [Authorize(DocumentServicePermissions.Files.Delete)]
    public async Task DeleteAsync(Guid id, string concurrencyStamp)
    {
        var userId = _currentUser.Id ??
            throw new Volo.Abp.Authorization.AbpAuthorizationException();
        if (!await _access.CanAccessFileAsync(id, userId))
        {
            throw new Volo.Abp.Authorization.AbpAuthorizationException();
        }
        await _fileManager.RequestDeleteAsync(id, concurrencyStamp);
    }

    internal static DocumentFileDto Map(DocumentFile file) => new()
    {
        Id = file.Id,
        DocumentId = file.DocumentId,
        DisplayName = file.DisplayName,
        MimeType = file.MimeType,
        Size = file.Size,
        Hash = file.Hash,
        IsSigned = file.IsSigned,
        UploadedAt = file.UploadedAt,
        CreationTime = file.CreationTime,
        ConcurrencyStamp = file.ConcurrencyStamp
    };
}
