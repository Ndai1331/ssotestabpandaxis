using hanhchinhso.DocumentService.Data;
using hanhchinhso.DocumentService.Documents;
using hanhchinhso.DocumentService.Permissions;
using hanhchinhso.DocumentService.Signing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization;

namespace hanhchinhso.DocumentService.Workflows;

[Authorize(DocumentServicePermissions.WorkflowRuntime.Default)]
public class MobileWorkflowQueryAppService :
    ApplicationService,
    IMobileWorkflowQueryAppService
{
    private const int MaxPageSize = 100;
    private readonly DocumentServiceDbContext _db;
    private readonly IWorkflowDocumentAccessService _access;
    private readonly IWorkflowRuntimeQueryAppService _runtime;

    public MobileWorkflowQueryAppService(
        DocumentServiceDbContext db,
        IWorkflowDocumentAccessService access,
        IWorkflowRuntimeQueryAppService runtime)
    {
        _db = db;
        _access = access;
        _runtime = runtime;
    }

    public async Task<MobileSigningPageResultDto> GetSigningListAsync(
        MobileSigningListInput input)
    {
        var userId = CurrentUser.Id ??
            throw new AbpAuthorizationException();
        var accessible = await _access.GetAccessibleInstancesAsync(userId);
        var received = await _access.GetReceivedInstancesAsync(userId);
        if (!input.FilterText.IsNullOrWhiteSpace())
        {
            accessible = _access.ApplyDocumentFilter(
                accessible, input.FilterText);
            received = _access.ApplyDocumentFilter(
                received, input.FilterText);
        }
        var filtered = ApplyFilters(accessible, input);
        var filteredIds = filtered.Select(x => x.Id);
        var receivedIds = received
            .Where(x => filteredIds.Contains(x.Id))
            .Select(x => x.Id);
        var sentByMeIds = filtered
            .Where(x => x.InitiatorUserId == userId)
            .Select(x => x.Id);

        var allCount = await filtered.CountAsync();
        var sentToMeCount = await receivedIds.CountAsync();
        var sentByMeCount = await sentByMeIds.CountAsync();
        IQueryable<DocumentWorkflowInstance> modeQuery =
            input.FilterMode switch
            {
                MobileSigningFilterMode.SentToMe =>
                    filtered.Where(x => receivedIds.Contains(x.Id)),
                MobileSigningFilterMode.SentByMe =>
                    filtered.Where(x => x.InitiatorUserId == userId),
                MobileSigningFilterMode.Following =>
                    filtered.Where(_ => false),
                _ => filtered
            };
        var totalCount = await modeQuery.LongCountAsync();
        var pageSize = Math.Clamp(input.MaxResultCount, 1, MaxPageSize);
        var pageIds = await ApplySorting(modeQuery, input.Sorting)
            .Skip(Math.Max(input.SkipCount, 0))
            .Take(pageSize)
            .Select(x => x.Id)
            .ToListAsync();
        if (pageIds.Count == 0)
        {
            return new MobileSigningPageResultDto
            {
                TotalCount = totalCount,
                AllCount = allCount,
                SentToMeCount = sentToMeCount,
                SentByMeCount = sentByMeCount,
                FollowingCount = 0
            };
        }

        var rows = await _db.DocumentWorkflowInstances
            .AsNoTracking()
            .Include(x => x.Steps)
            .Where(x => pageIds.Contains(x.Id))
            .ToListAsync();
        var documentIds = rows.Select(x => x.DocumentId).Distinct().ToList();
        var documents = await _db.Documents
            .AsNoTracking()
            .Where(x => documentIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id);
        var assignments = await _db.DocumentAssignments
            .AsNoTracking()
            .Where(x =>
                pageIds.Contains(x.InstanceId) &&
                x.ReceiverUserId == userId)
            .OrderByDescending(x => x.IsCurrent)
            .ThenByDescending(x => x.AssignedAtUtc)
            .ThenBy(x => x.Id)
            .ToListAsync();
        var assignmentByInstance = assignments
            .GroupBy(x => x.InstanceId)
            .ToDictionary(x => x.Key, x => x.First());
        var rowById = rows.ToDictionary(x => x.Id);
        var items = pageIds.Select(id =>
        {
            var instance = rowById[id];
            var document = documents[instance.DocumentId];
            assignmentByInstance.TryGetValue(id, out var assignment);
            var current = instance.CurrentCommittedStepId.HasValue
                ? instance.Steps.SingleOrDefault(x =>
                    x.Id == instance.CurrentCommittedStepId.Value)
                : null;
            return new MobileSigningItemDto
            {
                DocumentId = document.Id,
                DocumentNumber = document.Number,
                DocumentTitle = document.Title,
                StorageNumber = document.StorageNumber,
                WorkflowInstanceId = instance.Id,
                WorkflowStatus = instance.Status,
                CurrentStepName = current?.Name,
                CurrentStepOrder = current?.Order,
                TotalSteps = instance.Steps.Count,
                MyAssignmentId = assignment?.Id,
                MyAssignmentStatus = assignment?.Status,
                CanAct = assignment is
                {
                    IsCurrent: true,
                    Status: DocumentAssignmentStatus.Pending
                } && instance.Status is (
                    DocumentWorkflowStatus.InProgress or
                    DocumentWorkflowStatus.Overdue),
                CanResubmit =
                    instance.InitiatorUserId == userId &&
                    instance.Status == DocumentWorkflowStatus.Returned,
                StartedAtUtc = instance.StartedAtUtc,
                DeadlineAtUtc = instance.DeadlineAtUtc,
                FinishedAtUtc = instance.FinishedAtUtc
            };
        }).ToList();
        return new MobileSigningPageResultDto
        {
            TotalCount = totalCount,
            AllCount = allCount,
            SentToMeCount = sentToMeCount,
            SentByMeCount = sentByMeCount,
            FollowingCount = 0,
            Items = items
        };
    }

    public async Task<MobileWorkflowDetailDto> GetDetailAsync(
        Guid instanceId)
    {
        var runtime = await _runtime.GetAsync(instanceId);
        var document = await _db.Documents
            .AsNoTracking()
            .SingleAsync(x => x.Id == runtime.Instance.DocumentId);
        var logs = await _db.DocumentWorkflowInstanceLogs
            .AsNoTracking()
            .Where(x => x.InstanceId == instanceId)
            .OrderBy(x => x.OccurredAtUtc)
            .ThenBy(x => x.Id)
            .Select(x => new MobileWorkflowLogDto
            {
                Id = x.Id,
                AssignmentId = x.AssignmentId,
                Action = x.Action,
                ActorUserId = x.ActorUserId,
                FromStatus = x.FromStatus,
                ToStatus = x.ToStatus,
                OccurredAtUtc = x.OccurredAtUtc,
                Note = x.Note
            })
            .ToListAsync();
        var history = await _db.DocumentHistories
            .AsNoTracking()
            .Where(x => x.InstanceId == instanceId)
            .OrderBy(x => x.OccurredAtUtc)
            .ThenBy(x => x.Id)
            .Select(x => new MobileDocumentHistoryDto
            {
                Id = x.Id,
                Action = x.Action,
                FromUserId = x.FromUserId,
                ToUserId = x.ToUserId,
                OccurredAtUtc = x.OccurredAtUtc,
                Comment = x.Comment
            })
            .ToListAsync();
        var fileIds = runtime.Assignments
            .Where(x => x.DocumentFileResultId.HasValue)
            .Select(x => x.DocumentFileResultId!.Value)
            .Append(runtime.Instance.SourceFileId)
            .Concat(runtime.Instance.CurrentSignedFileId.HasValue
                ? [runtime.Instance.CurrentSignedFileId.Value]
                : [])
            .Distinct()
            .ToList();
        var files = await _db.DocumentFiles
            .AsNoTracking()
            .Where(x =>
                fileIds.Contains(x.Id) &&
                !x.BlobDeletionPending)
            .OrderBy(x => x.UploadedAt)
            .ThenBy(x => x.Id)
            .Select(x => new DocumentFileDto
            {
                Id = x.Id,
                DocumentId = x.DocumentId,
                DisplayName = x.DisplayName,
                MimeType = x.MimeType,
                Size = x.Size,
                Hash = x.Hash,
                IsSigned = x.IsSigned,
                UploadedAt = x.UploadedAt,
                ConcurrencyStamp = x.ConcurrencyStamp
            })
            .ToListAsync();
        return new MobileWorkflowDetailDto
        {
            Runtime = runtime,
            Document = MapDocument(document),
            Logs = logs,
            History = history,
            Files = files
        };
    }

    public async Task<ListResultDto<UserSignatureDto>> GetEligibleSignaturesAsync(
        MobileEligibleSignatureListInput input)
    {
        var userId = CurrentUser.Id ??
            throw new AbpAuthorizationException();
        var now = Clock.Now.ToUniversalTime();
        var signatureType = input.SignatureType;
        var isElectronic = signatureType == SignatureType.Electronic;
        var rows = await (
            from signature in _db.UserSignatures.AsNoTracking()
            join setting in _db.SignatureSettings.AsNoTracking()
                on signature.SignatureSettingId equals setting.Id
            where signature.IdentityUserId == userId &&
                  signature.IsActive &&
                  signature.SignatureType == signatureType &&
                  setting.IsActive &&
                  setting.ProviderCode == signature.ProviderCode &&
                  (isElectronic
                      ? setting.AllowElectronicSign
                      : setting.AllowDigitalSign) &&
                  (signature.ValidFromUtc == null ||
                   signature.ValidFromUtc <= now) &&
                  (signature.ValidToUtc == null ||
                   signature.ValidToUtc >= now)
            orderby signature.CreationTime descending, signature.Id
            select signature).ToListAsync();
        return new ListResultDto<UserSignatureDto>(
            rows.Select(MapSignature).ToList());
    }

    private static UserSignatureDto MapSignature(UserSignature x) =>
        new()
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

    private IQueryable<DocumentWorkflowInstance> ApplyFilters(
        IQueryable<DocumentWorkflowInstance> query,
        MobileSigningListInput input)
    {
        if (input.FromDateUtc.HasValue)
        {
            query = query.Where(x =>
                x.StartedAtUtc >= input.FromDateUtc.Value);
        }
        if (input.ToDateUtc.HasValue)
        {
            query = query.Where(x =>
                x.StartedAtUtc <= input.ToDateUtc.Value);
        }
        if (input.Status.HasValue)
        {
            query = query.Where(x => x.Status == input.Status.Value);
        }
        return query;
    }

    private static IOrderedQueryable<DocumentWorkflowInstance> ApplySorting(
        IQueryable<DocumentWorkflowInstance> query,
        string? sorting) =>
        sorting?.Trim().ToLowerInvariant() switch
        {
            "startedatutc" => query
                .OrderBy(x => x.StartedAtUtc)
                .ThenBy(x => x.Id),
            "deadlineatutc" => query
                .OrderBy(x => x.DeadlineAtUtc)
                .ThenBy(x => x.Id),
            "status" => query
                .OrderBy(x => x.Status)
                .ThenBy(x => x.Id),
            _ => query
                .OrderByDescending(x => x.StartedAtUtc)
                .ThenByDescending(x => x.Id)
        };

    private static DocumentDto MapDocument(Document document) => new()
    {
        Id = document.Id,
        Number = document.Number,
        Title = document.Title,
        CurrentStatus = document.CurrentStatus,
        CompletedTime = document.CompletedTime,
        StorageNumber = document.StorageNumber,
        IncomingDate = document.IncomingDate,
        FieldId = document.FieldId,
        UnitId = document.UnitId,
        StatusId = document.StatusId,
        TypeId = document.TypeId,
        UrgencyLevelId = document.UrgencyLevelId,
        SecrecyLevelId = document.SecrecyLevelId,
        SourceType = document.SourceType,
        OrganizationUnitId = document.OrganizationUnitId,
        FromUserId = document.FromUserId,
        ReceiverUserId = document.ReceiverUserId,
        ParentDocumentId = document.ParentDocumentId,
        ConcurrencyStamp = document.ConcurrencyStamp,
        CreationTime = document.CreationTime,
        CreatorId = document.CreatorId,
        LastModificationTime = document.LastModificationTime,
        LastModifierId = document.LastModifierId
    };
}
