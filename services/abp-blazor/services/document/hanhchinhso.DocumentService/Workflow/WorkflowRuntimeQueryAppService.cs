using hanhchinhso.DocumentService.Data;
using hanhchinhso.DocumentService.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Entities;

namespace hanhchinhso.DocumentService.Workflows;

[Authorize(DocumentServicePermissions.WorkflowRuntime.Default)]
public class WorkflowRuntimeQueryAppService :
    ApplicationService,
    IWorkflowRuntimeQueryAppService
{
    private readonly DocumentServiceDbContext _dbContext;
    private readonly IWorkflowIdentityMembershipResolver _membershipResolver;

    public WorkflowRuntimeQueryAppService(
        DocumentServiceDbContext dbContext,
        IWorkflowIdentityMembershipResolver membershipResolver)
    {
        _dbContext = dbContext;
        _membershipResolver = membershipResolver;
    }

    public async Task<WorkflowRuntimeStatusDto> GetAsync(Guid instanceId)
    {
        var userId = CurrentUser.Id ??
            throw new AbpAuthorizationException(
                "An authenticated user is required.");
        var instance = await _dbContext.DocumentWorkflowInstances
            .AsNoTracking()
            .Include(x => x.Steps)
                .ThenInclude(x => x.ViewScopes)
            .SingleOrDefaultAsync(x => x.Id == instanceId)
            ?? throw new EntityNotFoundException(
                typeof(DocumentWorkflowInstance), instanceId);
        var assignments = await _dbContext.DocumentAssignments
            .AsNoTracking()
            .Where(x => x.InstanceId == instance.Id)
            .OrderBy(x => x.AssignedAtUtc)
            .ThenBy(x => x.Id)
            .ToListAsync();
        var unlockOrder = GetSequentialUnlockOrder(instance, assignments);
        var unlockedViewSteps = instance.Steps
            .Where(x =>
                x.Type == WorkflowStepType.View &&
                (instance.SignMode == WorkflowSignMode.Parallel ||
                 x.Order <= unlockOrder))
            .ToList();
        var directAccess =
            instance.InitiatorUserId == userId ||
            assignments.Any(x => x.ReceiverUserId == userId) ||
            unlockedViewSteps.SelectMany(x => x.ViewScopes)
                .Any(x => x.UserId == userId);
        if (!directAccess)
        {
            var scopedOrganizationUnitIds = unlockedViewSteps
                .SelectMany(x => x.ViewScopes)
                .Where(x => x.OrganizationUnitId.HasValue)
                .Select(x => x.OrganizationUnitId!.Value)
                .Distinct()
                .ToList();
            var memberships = scopedOrganizationUnitIds.Count == 0
                ? new HashSet<Guid>()
                : await _membershipResolver.ResolveAsync(
                    userId,
                    scopedOrganizationUnitIds);
            if (memberships.Count == 0)
            {
                throw new AbpAuthorizationException(
                    "The current user cannot view this workflow.");
            }
        }

        return new WorkflowRuntimeStatusDto
        {
            Instance = MapInstance(instance),
            Assignments = assignments.Select(MapAssignment).ToList(),
            Steps = instance.Steps
                .OrderBy(x => x.Order)
                .Select(x => MapStep(
                    x,
                    x.Type == WorkflowStepType.View &&
                    (instance.SignMode == WorkflowSignMode.Parallel ||
                     x.Order <= unlockOrder)))
                .ToList()
        };
    }

    public async Task<PagedResultDto<DocumentAssignmentDto>>
        GetMyAssignmentsAsync(MyWorkflowAssignmentListInput input)
    {
        var userId = CurrentUser.Id ??
            throw new AbpAuthorizationException(
                "An authenticated user is required.");
        var query = _dbContext.DocumentAssignments
            .AsNoTracking()
            .Where(x => x.ReceiverUserId == userId);
        if (input.Status.HasValue)
        {
            query = query.Where(x => x.Status == input.Status.Value);
        }
        if (input.IsCurrent.HasValue)
        {
            query = query.Where(x => x.IsCurrent == input.IsCurrent.Value);
        }
        var totalCount = await query.LongCountAsync();
        var items = await query
            .OrderByDescending(x => x.AssignedAtUtc)
            .ThenBy(x => x.Id)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToListAsync();
        return new PagedResultDto<DocumentAssignmentDto>(
            totalCount,
            items.Select(MapAssignment).ToList());
    }

    private static int GetSequentialUnlockOrder(
        DocumentWorkflowInstance instance,
        IReadOnlyCollection<DocumentAssignment> assignments)
    {
        if (instance.SignMode == WorkflowSignMode.Parallel ||
            instance.Status == DocumentWorkflowStatus.Completed)
        {
            return int.MaxValue;
        }
        if (instance.CurrentCommittedStepId.HasValue)
        {
            return instance.Steps
                .Single(x => x.Id == instance.CurrentCommittedStepId.Value)
                .Order;
        }
        var assignedStepIds = assignments
            .Select(x => x.CommittedStepId)
            .ToHashSet();
        return instance.Steps
            .Where(x => assignedStepIds.Contains(x.Id))
            .Select(x => x.Order)
            .DefaultIfEmpty(0)
            .Max();
    }

    private static DocumentAssignmentDto MapAssignment(
        DocumentAssignment assignment) =>
        new()
        {
            Id = assignment.Id,
            InstanceId = assignment.InstanceId,
            DocumentId = assignment.DocumentId,
            CommittedStepId = assignment.CommittedStepId,
            ReceiverUserId = assignment.ReceiverUserId,
            Action = assignment.Action,
            Status = assignment.Status,
            AssignedAtUtc = assignment.AssignedAtUtc,
            IsCurrent = assignment.IsCurrent,
            ProcessedAtUtc = assignment.ProcessedAtUtc,
            DocumentFileResultId = assignment.DocumentFileResultId,
            ConcurrencyStamp = assignment.ConcurrencyStamp
        };

    private static WorkflowCommittedStepStatusDto MapStep(
        DocumentWorkflowCommittedStep step,
        bool isViewUnlocked) =>
        new()
        {
            Id = step.Id,
            Order = step.Order,
            Name = step.Name,
            Type = step.Type,
            AllowReturn = step.AllowReturn,
            IsViewUnlocked = isViewUnlocked,
            ViewUserIds = isViewUnlocked
                ? step.ViewScopes
                    .Where(x => x.UserId.HasValue)
                    .Select(x => x.UserId!.Value)
                    .Order()
                    .ToList()
                : [],
            ViewOrganizationUnitIds = isViewUnlocked
                ? step.ViewScopes
                    .Where(x => x.OrganizationUnitId.HasValue)
                    .Select(x => x.OrganizationUnitId!.Value)
                    .Order()
                    .ToList()
                : []
        };

    private static DocumentWorkflowInstanceDto MapInstance(
        DocumentWorkflowInstance instance) =>
        new()
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
