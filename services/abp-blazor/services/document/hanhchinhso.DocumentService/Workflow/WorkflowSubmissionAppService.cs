using System.Security.Cryptography;
using hanhchinhso.DocumentService.Data;
using hanhchinhso.DocumentService.Documents;
using hanhchinhso.DocumentService.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization;
using Volo.Abp.BlobStoring;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Uow;
using UglyToad.PdfPig;

namespace hanhchinhso.DocumentService.Workflows;

[Authorize(DocumentServicePermissions.WorkflowRuntime.Submit)]
public class WorkflowSubmissionAppService :
    ApplicationService,
    IWorkflowSubmissionAppService
{
    private static readonly TimeSpan PreviewLifetime = TimeSpan.FromMinutes(5);
    private readonly DocumentServiceDbContext _dbContext;
    private readonly IWorkflowAssigneeResolver _assigneeResolver;
    private readonly WorkflowPreviewTokenService _tokens;
    private readonly IAbpDistributedLock _distributedLock;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IBlobContainer<DocumentBlobContainer> _documentBlobs;

    public WorkflowSubmissionAppService(
        DocumentServiceDbContext dbContext,
        IWorkflowAssigneeResolver assigneeResolver,
        WorkflowPreviewTokenService tokens,
        IAbpDistributedLock distributedLock,
        IUnitOfWorkManager unitOfWorkManager,
        IBlobContainer<DocumentBlobContainer> documentBlobs)
    {
        _dbContext = dbContext;
        _assigneeResolver = assigneeResolver;
        _tokens = tokens;
        _distributedLock = distributedLock;
        _unitOfWorkManager = unitOfWorkManager;
        _documentBlobs = documentBlobs;
    }

    public async Task<WorkflowSubmitPreviewDto> PreviewAsync(
        WorkflowSubmitPreviewInput input)
    {
        var state = await BuildStateAsync(input, requireSelections: false);
        var expiresAtUtc = Clock.Now.ToUniversalTime().Add(PreviewLifetime);
        var token = _tokens.Protect(new WorkflowPreviewTokenPayload(
            CurrentTenant.Id,
            state.Document.Id,
            state.SourceFile.Id,
            state.SourceFileSha256,
            state.Workflow.Id,
            state.Template.Id,
            state.PreviousInstanceId,
            state.InitiatorUserId,
            state.Template.ConcurrencyStamp,
            state.CandidateHash,
            expiresAtUtc));
        return MapPreview(state, token, expiresAtUtc);
    }

    public async Task<DocumentWorkflowInstanceDto> SubmitAsync(
        WorkflowSubmitInput input)
    {
        var tenantKey = CurrentTenant.Id?.ToString("N") ?? "host";
        await using var catalogHandle = await _distributedLock.TryAcquireAsync(
            $"document-workflow-catalog:{tenantKey}",
            TimeSpan.FromSeconds(30));
        if (catalogHandle is null)
        {
            throw new UserFriendlyException(
                "The workflow catalog is busy. Please retry.");
        }

        await using var documentHandle = await _distributedLock.TryAcquireAsync(
            $"document-workflow-document:{tenantKey}:{input.DocumentId:N}",
            TimeSpan.FromSeconds(30));
        if (documentHandle is null)
        {
            throw new UserFriendlyException(
                "The document workflow is busy. Please retry.");
        }

        using var uow = _unitOfWorkManager.Begin(
            requiresNew: true,
            isTransactional: true);
        var state = await BuildStateAsync(input, requireSelections: true);
        ValidateToken(input.PreviewToken, state);
        if (!string.Equals(
                state.Document.ConcurrencyStamp,
                input.DocumentConcurrencyStamp,
                StringComparison.Ordinal))
        {
            throw new AbpDbConcurrencyException();
        }

        var now = Clock.Now.ToUniversalTime();
        var instance = new DocumentWorkflowInstance(
            GuidGenerator.Create(),
            CurrentTenant.Id,
            state.Document.Id,
            state.SourceFile.Id,
            state.Workflow.Id,
            state.Template.Id,
            state.InitiatorUserId,
            state.SignMode,
            now,
            state.PreviousInstanceId);
        var slaDays = state.Steps
            .Where(x => x.Type != WorkflowStepType.View && x.SlaDays.HasValue)
            .Select(x => x.SlaDays!.Value)
            .ToList();
        if (slaDays.Count > 0)
        {
            var deadlineDays = state.SignMode == WorkflowSignMode.Parallel
                ? slaDays.Max()
                : slaDays.Sum();
            instance.SetDeadline(
                WorkflowBusinessDayCalculator.Add(now, deadlineDays));
        }
        var selectedAssignments = new List<DocumentAssignment>();
        foreach (var previewStep in state.PreviewSteps.OrderBy(x => x.Order))
        {
            var templateStep = state.Steps.Single(
                x => x.Id == previewStep.WorkflowStepTemplateId);
            var committed = new DocumentWorkflowCommittedStep(
                GuidGenerator.Create(),
                CurrentTenant.Id,
                instance.Id,
                templateStep);
            AddCommittedScopes(committed, previewStep);
            instance.AddStep(committed);

            var selected = previewStep.Candidates.SingleOrDefault(x => x.IsSelected);
            if (selected is not null && templateStep.Type != WorkflowStepType.View)
            {
                selectedAssignments.Add(new DocumentAssignment(
                    GuidGenerator.Create(),
                    CurrentTenant.Id,
                    instance.Id,
                    state.Document.Id,
                    committed.Id,
                    selected.UserId,
                    templateStep.Type == WorkflowStepType.Sign
                        ? DocumentAssignmentAction.Sign
                        : DocumentAssignmentAction.Process,
                    now,
                    isCurrent: state.SignMode == WorkflowSignMode.Parallel));
            }
        }

        var firstAssignment = selectedAssignments.First();
        Guid? sequentialCurrentStepId = null;
        if (state.SignMode == WorkflowSignMode.Sequential)
        {
            firstAssignment.MarkCurrent();
            sequentialCurrentStepId = firstAssignment.CommittedStepId;
            selectedAssignments = [firstAssignment];
        }
        foreach (var assignment in selectedAssignments)
        {
            _dbContext.DocumentAssignments.Add(assignment);
        }

        state.Document.SetWorkflowStatus("WORKFLOW_IN_PROGRESS");
        _dbContext.DocumentWorkflowInstances.Add(instance);
        var submitAction = state.PreviousInstanceId.HasValue
            ? WorkflowRuntimeAction.Resubmit
            : WorkflowRuntimeAction.Submit;
        _dbContext.DocumentWorkflowInstanceLogs.Add(
            new DocumentWorkflowInstanceLog(
                GuidGenerator.Create(),
                CurrentTenant.Id,
                instance.Id,
                submitAction,
                CurrentUser.Id,
                DocumentWorkflowStatus.Draft,
                DocumentWorkflowStatus.InProgress,
                now));
        _dbContext.DocumentHistories.Add(new DocumentHistory(
            GuidGenerator.Create(),
            CurrentTenant.Id,
            state.Document.Id,
            instance.Id,
            submitAction,
            state.InitiatorUserId,
            firstAssignment.ReceiverUserId,
            now));
        await _dbContext.SaveChangesAsync();
        if (sequentialCurrentStepId.HasValue)
        {
            instance.SetCurrentStep(sequentialCurrentStepId);
            await _dbContext.SaveChangesAsync();
        }
        await uow.CompleteAsync();
        return MapInstance(instance);
    }

    private void ValidateToken(
        string token,
        WorkflowSubmissionState state)
    {
        var payload = _tokens.Unprotect(token);
        var now = Clock.Now.ToUniversalTime();
        if (payload.ExpiresAtUtc < now ||
            payload.TenantId != CurrentTenant.Id ||
            payload.DocumentId != state.Document.Id ||
            payload.SourceFileId != state.SourceFile.Id ||
            !string.Equals(
                payload.SourceFileSha256,
                state.SourceFileSha256,
                StringComparison.OrdinalIgnoreCase) ||
            payload.WorkflowId != state.Workflow.Id ||
            payload.WorkflowTemplateId != state.Template.Id ||
            payload.PreviousInstanceId != state.PreviousInstanceId ||
            payload.InitiatorUserId != state.InitiatorUserId ||
            payload.TemplateConcurrencyStamp != state.Template.ConcurrencyStamp ||
            payload.CandidateHash != state.CandidateHash)
        {
            throw new UserFriendlyException(
                "The workflow preview is stale. Preview again.");
        }
    }

    private async Task<WorkflowSubmissionState> BuildStateAsync(
        WorkflowSubmitPreviewInput input,
        bool requireSelections)
    {
        var callerUserId = CurrentUser.Id ??
            throw new AbpAuthorizationException(
                "An authenticated user is required.");
        var initiatorUserId = input.InitiatorUserId ?? callerUserId;
        var hasOverride = initiatorUserId != callerUserId;
        if (hasOverride && !await AuthorizationService.IsGrantedAsync(
                DocumentServicePermissions.WorkflowRuntime.SubmitAll))
        {
            throw new AbpAuthorizationException(
                "Cross-user workflow submission is not permitted.");
        }

        var document = await _dbContext.Documents
            .FirstOrDefaultAsync(x => x.Id == input.DocumentId)
            ?? throw new EntityNotFoundException(
                typeof(Document), input.DocumentId);
        if (await _dbContext.DocumentWorkflowInstances.AnyAsync(x =>
                x.DocumentId == document.Id &&
                (x.Status == DocumentWorkflowStatus.InProgress ||
                 x.Status == DocumentWorkflowStatus.Overdue)))
        {
            throw new UserFriendlyException(
                "The document already has an active workflow.");
        }
        Guid? previousInstanceId = null;
        if (input.PreviousInstanceId.HasValue)
        {
            var previous = await _dbContext.DocumentWorkflowInstances
                .AsNoTracking()
                .SingleOrDefaultAsync(x =>
                    x.Id == input.PreviousInstanceId.Value &&
                    x.DocumentId == document.Id)
                ?? throw new EntityNotFoundException(
                    typeof(DocumentWorkflowInstance),
                    input.PreviousInstanceId.Value);
            if (previous.Status != DocumentWorkflowStatus.Returned ||
                document.CurrentStatus != "WORKFLOW_RETURNED")
            {
                throw new UserFriendlyException(
                    "Only a returned workflow can be resubmitted.");
            }
            previousInstanceId = previous.Id;
        }
        else if (IsTerminalStatus(document.CurrentStatus))
        {
            throw new UserFriendlyException(
                "A terminal document workflow cannot be submitted again.");
        }
        var template = await _dbContext.WorkflowTemplates
            .FirstOrDefaultAsync(x =>
                x.Id == input.WorkflowTemplateId && x.IsActive)
            ?? throw new EntityNotFoundException(
                typeof(WorkflowTemplate), input.WorkflowTemplateId);
        if (!template.WordTemplatePath.IsNullOrWhiteSpace() ||
            !template.PdfTemplatePath.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException(
                "Workflow template file materialization is not available until slice 3f.");
        }
        var workflow = await _dbContext.Workflows
            .FirstOrDefaultAsync(x =>
                x.Id == template.WorkflowId && x.IsActive)
            ?? throw new EntityNotFoundException(
                typeof(Workflow), template.WorkflowId);
        if (!await _dbContext.WorkflowDefinitions.AnyAsync(x =>
                x.Id == workflow.WorkflowDefinitionId && x.IsActive))
        {
            throw new UserFriendlyException(
                "The workflow definition is inactive.");
        }

        var steps = await _dbContext.WorkflowStepTemplates
            .Where(x => x.WorkflowTemplateId == template.Id && x.IsActive)
            .OrderBy(x => x.Order)
            .ToListAsync();
        if (steps.Count == 0 ||
            steps.Select(x => x.Order).Distinct().Count() != steps.Count ||
            steps.All(x => x.Type == WorkflowStepType.View))
        {
            throw new UserFriendlyException(
                "The workflow requires unique active steps and at least one actionable step.");
        }
        var stepIds = steps.Select(x => x.Id).ToList();
        var configurations = await _dbContext.WorkflowStepAssignmentConfigurations
            .Include(x => x.Users)
            .Include(x => x.OrganizationUnits)
            .Where(x => stepIds.Contains(x.WorkflowStepTemplateId) && x.IsActive)
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.CreationTime)
            .ThenBy(x => x.Id)
            .ToListAsync();
        if (steps.Any(step => !configurations.Any(
                configuration =>
                    configuration.WorkflowStepTemplateId == step.Id)))
        {
            throw new UserFriendlyException(
                "Every active workflow step requires an active assignee configuration.");
        }

        var resolution = await _assigneeResolver.ResolveAsync(
            initiatorUserId,
            configurations);
        if (!hasOverride &&
            callerUserId != document.FromUserId &&
            callerUserId != document.ReceiverUserId &&
            (!document.OrganizationUnitId.HasValue ||
             !resolution.SubmitterOrganizationUnitIds.Contains(
                 document.OrganizationUnitId.Value)))
        {
            throw new AbpAuthorizationException(
                "The current user cannot submit this document.");
        }

        // Authorization must precede source metadata/blob access. BuildState is
        // also executed under the document lock during SubmitAsync.
        var sourceFile = await _dbContext.DocumentFiles
            .AsNoTracking()
            .SingleOrDefaultAsync(x =>
                x.Id == input.SourceFileId &&
                x.DocumentId == document.Id &&
                !x.BlobDeletionPending)
            ?? throw new EntityNotFoundException(
                typeof(DocumentFile), input.SourceFileId);
        if (!string.Equals(
                sourceFile.MimeType,
                "application/pdf",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException(
                "DocumentService:WorkflowSourceMustBePdf");
        }
        var sourceFileSha256 = await VerifySourceFileAsync(sourceFile);

        var selections = NormalizeSelections(input.Selections);
        if (selections.Keys.Any(id =>
                steps.All(step => step.Id != id || step.Type == WorkflowStepType.View)))
        {
            throw new UserFriendlyException(
                "A workflow selection targets an unknown or VIEW step.");
        }
        var previewSteps = steps.Select(step => BuildPreviewStep(
                step,
                configurations.Where(x =>
                    x.WorkflowStepTemplateId == step.Id).ToList(),
                resolution,
                selections.TryGetValue(step.Id, out var selection)
                    ? selection
                    : null,
                requireSelections))
            .ToList();
        var candidateHash = WorkflowPreviewTokenService.HashCandidates(previewSteps);
        return new WorkflowSubmissionState(
            document,
            sourceFile,
            sourceFileSha256,
            workflow,
            template,
            previousInstanceId,
            template.SignMode ?? WorkflowSignMode.Sequential,
            initiatorUserId,
            steps,
            configurations,
            resolution,
            previewSteps,
            candidateHash);
    }

    private static WorkflowStepSubmitPreviewDto BuildPreviewStep(
        WorkflowStepTemplate step,
        IReadOnlyCollection<WorkflowStepAssignmentConfiguration> configurations,
        hanhchinhso.IdentityService.Internal.WorkflowAssigneeResolutionResult resolution,
        Guid? selectedUserId,
        bool requireSelection)
    {
        var configurationIds = configurations.Select(x => x.Id).ToHashSet();
        var resolved = resolution.Candidates
            .Where(x => configurationIds.Contains(x.ConfigurationId))
            .GroupBy(x => x.UserId)
            .Select(group => group
                .OrderByDescending(x => x.IsPrimaryConfiguration)
                .ThenBy(x => x.OrganizationUnitDepth)
                .ThenBy(x => x.ConfigurationCreationTime)
                .ThenBy(x => x.ConfigurationId)
                .First())
            .OrderByDescending(x => x.IsPrimaryConfiguration)
            .ThenBy(x => x.OrganizationUnitDepth)
            .ThenBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.UserId)
            .ToList();
        if (resolved.Count == 0)
        {
            throw new UserFriendlyException(
                $"Workflow step '{step.Name}' has no enabled assignee.");
        }

        if (step.Type == WorkflowStepType.View)
        {
            return new WorkflowStepSubmitPreviewDto
            {
                WorkflowStepTemplateId = step.Id,
                Order = step.Order,
                Name = step.Name,
                Type = step.Type,
                AllowReturn = step.AllowReturn,
                SlaDays = step.SlaDays,
                PrimaryOrganizationUnitId =
                    resolution.PrimarySubmitterOrganizationUnitId,
                ViewUserIds = resolved.Select(x => x.UserId).Distinct().Order().ToList(),
                ViewOrganizationUnitIds = configurations
                    .SelectMany(x => x.OrganizationUnits)
                    .Select(x => x.OrganizationUnitId)
                    .Distinct().Order().ToList()
            };
        }

        var selected = selectedUserId ??
            (resolved.Count == 1 ? resolved[0].UserId : null);
        if (selected.HasValue && resolved.All(x => x.UserId != selected.Value))
        {
            throw new UserFriendlyException(
                $"Selected assignee for step '{step.Name}' is not an enabled candidate.");
        }
        if (requireSelection && !selected.HasValue)
        {
            throw new UserFriendlyException(
                $"Select exactly one assignee for workflow step '{step.Name}'.");
        }
        return new WorkflowStepSubmitPreviewDto
        {
            WorkflowStepTemplateId = step.Id,
            Order = step.Order,
            Name = step.Name,
            Type = step.Type,
            AllowReturn = step.AllowReturn,
            SlaDays = step.SlaDays,
            PrimaryOrganizationUnitId =
                resolution.PrimarySubmitterOrganizationUnitId,
            Candidates = resolved.Select(x => new WorkflowCandidateDto
            {
                UserId = x.UserId,
                DisplayName = x.DisplayName,
                IsSelected = selected == x.UserId,
                IsPrimary = x.IsPrimaryConfiguration,
                ProvenanceOrganizationUnitId =
                    x.ProvenanceOrganizationUnitId,
                ProvenanceRoleId = x.ProvenanceRoleId
            }).ToList()
        };
    }

    private void AddCommittedScopes(
        DocumentWorkflowCommittedStep committed,
        WorkflowStepSubmitPreviewDto preview)
    {
        foreach (var candidate in preview.Candidates)
        {
            committed.AddReceiver(new DocumentWorkflowCommittedReceiver(
                GuidGenerator.Create(),
                CurrentTenant.Id,
                committed.Id,
                candidate.UserId,
                candidate.IsSelected,
                candidate.IsPrimary,
                candidate.ProvenanceOrganizationUnitId,
                candidate.ProvenanceRoleId));
        }
        foreach (var userId in preview.ViewUserIds)
        {
            committed.AddViewScope(new DocumentWorkflowCommittedViewScope(
                GuidGenerator.Create(),
                CurrentTenant.Id,
                committed.Id,
                null,
                userId));
        }
        foreach (var organizationUnitId in preview.ViewOrganizationUnitIds)
        {
            committed.AddViewScope(new DocumentWorkflowCommittedViewScope(
                GuidGenerator.Create(),
                CurrentTenant.Id,
                committed.Id,
                organizationUnitId,
                null));
        }
    }

    private static Dictionary<Guid, Guid> NormalizeSelections(
        IEnumerable<WorkflowSubmitSelectionDto>? selections)
    {
        var result = new Dictionary<Guid, Guid>();
        foreach (var selection in selections ?? [])
        {
            if (selection.WorkflowStepTemplateId == Guid.Empty ||
                selection.UserId == Guid.Empty ||
                !result.TryAdd(
                    selection.WorkflowStepTemplateId,
                    selection.UserId))
            {
                throw new UserFriendlyException(
                    "Workflow selections must contain one unique user per step.");
            }
        }
        return result;
    }

    private static bool IsTerminalStatus(string? status) =>
        status is "WORKFLOW_COMPLETED" or "WORKFLOW_RETURNED" or
            "WORKFLOW_REJECTED" or "WORKFLOW_CANCELLED";

    private async Task<string> VerifySourceFileAsync(DocumentFile sourceFile)
    {
        if (sourceFile.Hash.IsNullOrWhiteSpace())
        {
            throw new BusinessException(
                "DocumentService:WorkflowSourceHashRequired");
        }
        await using var source = await _documentBlobs.GetAsync(
            sourceFile.BlobName);
        if (sourceFile.Size <= 0 || sourceFile.Size > 104_857_600)
        {
            throw new BusinessException(
                "DocumentService:InvalidWorkflowSourcePdf");
        }
        using var buffer = new MemoryStream((int)Math.Min(
            sourceFile.Size, int.MaxValue));
        await source.CopyToAsync(buffer);
        if (buffer.Length != sourceFile.Size ||
            buffer.Length > 104_857_600)
        {
            throw new BusinessException(
                "DocumentService:InvalidWorkflowSourcePdf");
        }
        var bytes = buffer.ToArray();
        var hash = Convert.ToHexString(
            SHA256.HashData(bytes)).ToLowerInvariant();
        if (!string.Equals(
                sourceFile.Hash,
                hash,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new BusinessException(
                "DocumentService:WorkflowSourceHashMismatch");
        }
        try
        {
            using var pdf = PdfDocument.Open(bytes);
            if (pdf.NumberOfPages < 1)
            {
                throw new BusinessException(
                    "DocumentService:InvalidWorkflowSourcePdf");
            }
        }
        catch (BusinessException)
        {
            throw;
        }
        catch
        {
            throw new BusinessException(
                "DocumentService:InvalidWorkflowSourcePdf");
        }
        return hash;
    }

    private static WorkflowSubmitPreviewDto MapPreview(
        WorkflowSubmissionState state,
        string token,
        DateTime expiresAtUtc) => new()
    {
        DocumentId = state.Document.Id,
        SourceFileId = state.SourceFile.Id,
        SourceFileSha256 = state.SourceFileSha256,
        WorkflowId = state.Workflow.Id,
        WorkflowTemplateId = state.Template.Id,
        SignMode = state.SignMode,
        Steps = state.PreviewSteps.ToList(),
        PreviewToken = token,
        ExpiresAtUtc = expiresAtUtc
    };

    private static DocumentWorkflowInstanceDto MapInstance(
        DocumentWorkflowInstance instance) => new()
    {
        Id = instance.Id,
        DocumentId = instance.DocumentId,
        SourceFileId = instance.SourceFileId,
        WorkflowId = instance.WorkflowId,
        WorkflowTemplateId = instance.WorkflowTemplateId,
        InitiatorUserId = instance.InitiatorUserId,
        SignMode = instance.SignMode,
            Status = instance.Status,
            CurrentCommittedStepId = instance.CurrentCommittedStepId,
            CurrentSignedFileId = instance.CurrentSignedFileId,
            PreviousInstanceId = instance.PreviousInstanceId,
            StartedAtUtc = instance.StartedAtUtc,
            DeadlineAtUtc = instance.DeadlineAtUtc,
            FinishedAtUtc = instance.FinishedAtUtc,
            OverdueAtUtc = instance.OverdueAtUtc,
            ExtensionCount = instance.ExtensionCount,
            TotalExtensionBusinessDays =
                instance.TotalExtensionBusinessDays,
            ConcurrencyStamp = instance.ConcurrencyStamp
        };
}
