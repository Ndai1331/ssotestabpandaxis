using hanhchinhso.DocumentService.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.DistributedLocking;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using Volo.Abp.Uow;

namespace hanhchinhso.DocumentService.Workflows;

[Authorize(DocumentServicePermissions.WorkflowStepAssignments.Default)]
public class WorkflowStepAssignmentConfigurationAppService :
    ApplicationService,
    IWorkflowStepAssignmentConfigurationAppService
{
    private readonly IRepository<WorkflowStepAssignmentConfiguration, Guid> _configurations;
    private readonly IRepository<WorkflowStepTemplate, Guid> _steps;
    private readonly IWorkflowIdentityReferenceValidator _identityReferences;
    private readonly IAbpDistributedLock _distributedLock;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public WorkflowStepAssignmentConfigurationAppService(
        IRepository<WorkflowStepAssignmentConfiguration, Guid> configurations,
        IRepository<WorkflowStepTemplate, Guid> steps,
        IWorkflowIdentityReferenceValidator identityReferences,
        IAbpDistributedLock distributedLock,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _configurations = configurations;
        _steps = steps;
        _identityReferences = identityReferences;
        _distributedLock = distributedLock;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public async Task<WorkflowStepAssignmentConfigurationDto> GetAsync(Guid id)
    {
        var query = await WithDetailsAsync();
        var entity = await AsyncExecuter.FirstOrDefaultAsync(query.Where(x => x.Id == id))
            ?? throw new EntityNotFoundException(
                typeof(WorkflowStepAssignmentConfiguration), id);
        return Map(entity);
    }

    public async Task<PagedResultDto<WorkflowStepAssignmentConfigurationDto>> GetListAsync(
        WorkflowStepAssignmentConfigurationListInput input)
    {
        var query = await WithDetailsAsync();
        query = query
            .WhereIf(input.WorkflowStepTemplateId.HasValue,
                x => x.WorkflowStepTemplateId == input.WorkflowStepTemplateId)
            .WhereIf(input.AssigneeType.HasValue,
                x => x.AssigneeType == input.AssigneeType)
            .WhereIf(input.IsActive.HasValue, x => x.IsActive == input.IsActive);
        var total = await AsyncExecuter.CountAsync(query);
        var entities = await AsyncExecuter.ToListAsync(
            query.OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.CreationTime)
                .PageBy(input.SkipCount, input.MaxResultCount));
        return new PagedResultDto<WorkflowStepAssignmentConfigurationDto>(
            total, entities.Select(Map).ToList());
    }

    [Authorize(DocumentServicePermissions.WorkflowStepAssignments.Create)]
    public async Task<WorkflowStepAssignmentConfigurationDto> CreateAsync(
        CreateUpdateWorkflowStepAssignmentConfigurationDto input)
    {
        var id = await MutateAsync(async () =>
        {
            await ValidateReferencesAsync(input);
            var entity = new WorkflowStepAssignmentConfiguration(
                GuidGenerator.Create(),
                CurrentTenant.Id,
                input,
                GuidGenerator.Create);
            await _configurations.InsertAsync(entity, autoSave: true);
            return entity.Id;
        });
        return await GetAsync(id);
    }

    [Authorize(DocumentServicePermissions.WorkflowStepAssignments.Update)]
    public async Task<WorkflowStepAssignmentConfigurationDto> UpdateAsync(
        Guid id,
        CreateUpdateWorkflowStepAssignmentConfigurationDto input)
    {
        await MutateAsync(async () =>
        {
            await ValidateReferencesAsync(input);
            var query = await WithDetailsAsync();
            var entity = await AsyncExecuter.FirstOrDefaultAsync(query.Where(x => x.Id == id))
                ?? throw new EntityNotFoundException(
                    typeof(WorkflowStepAssignmentConfiguration), id);
            if (!string.Equals(
                    entity.ConcurrencyStamp,
                    input.ConcurrencyStamp,
                    StringComparison.Ordinal))
            {
                throw new AbpDbConcurrencyException();
            }
            entity.Update(input, GuidGenerator.Create);
            await _configurations.UpdateAsync(entity, autoSave: true);
            return true;
        });
        return await GetAsync(id);
    }

    [Authorize(DocumentServicePermissions.WorkflowStepAssignments.Delete)]
    public async Task DeleteAsync(Guid id, string concurrencyStamp)
    {
        await MutateAsync(async () =>
        {
            var entity = await _configurations.GetAsync(id);
            if (!string.Equals(
                    entity.ConcurrencyStamp,
                    concurrencyStamp,
                    StringComparison.Ordinal))
            {
                throw new AbpDbConcurrencyException();
            }
            await _configurations.DeleteAsync(entity, autoSave: true);
            return true;
        });
    }

    private async Task ValidateReferencesAsync(
        CreateUpdateWorkflowStepAssignmentConfigurationDto input)
    {
        if (!await _steps.AnyAsync(x => x.Id == input.WorkflowStepTemplateId))
        {
            throw new EntityNotFoundException(
                typeof(WorkflowStepTemplate),
                input.WorkflowStepTemplateId);
        }

        // Construct once before remote calls so mode/cardinality errors fail locally.
        _ = new WorkflowStepAssignmentConfiguration(
            GuidGenerator.Create(),
            CurrentTenant.Id,
            input,
            GuidGenerator.Create);

        await _identityReferences.ValidateAsync(
            input.UserIds,
            input.OrganizationUnitIds,
            input.RoleId);
    }

    private async Task<IQueryable<WorkflowStepAssignmentConfiguration>> WithDetailsAsync()
    {
        var query = await _configurations.GetQueryableAsync();
        return query
            .Include(x => x.Users)
            .Include(x => x.OrganizationUnits);
    }

    private async Task<TResult> MutateAsync<TResult>(Func<Task<TResult>> action)
    {
        var tenantKey = CurrentTenant.Id?.ToString("N") ?? "host";
        await using var handle = await _distributedLock.TryAcquireAsync(
            $"document-workflow-catalog:{tenantKey}",
            TimeSpan.FromSeconds(30));
        if (handle is null)
        {
            throw new UserFriendlyException(
                "The workflow catalog is busy. Please retry the operation.");
        }

        using var uow = _unitOfWorkManager.Begin(requiresNew: true, isTransactional: true);
        var result = await action();
        await uow.CompleteAsync();
        return result;
    }

    private static WorkflowStepAssignmentConfigurationDto Map(
        WorkflowStepAssignmentConfiguration entity) => new()
    {
        Id = entity.Id,
        WorkflowStepTemplateId = entity.WorkflowStepTemplateId,
        AssigneeType = entity.AssigneeType,
        RoleId = entity.RoleId,
        IsPrimary = entity.IsPrimary,
        IsActive = entity.IsActive,
        UserIds = entity.Users.Select(x => x.UserId).Order().ToList(),
        OrganizationUnitIds = entity.OrganizationUnits
            .Select(x => x.OrganizationUnitId).Order().ToList(),
        CreationTime = entity.CreationTime,
        CreatorId = entity.CreatorId,
        LastModificationTime = entity.LastModificationTime,
        LastModifierId = entity.LastModifierId,
        ConcurrencyStamp = entity.ConcurrencyStamp
    };
}
