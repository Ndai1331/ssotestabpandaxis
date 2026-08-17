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

public abstract class WorkflowCatalogAppService<TEntity, TDto, TInput> :
    ApplicationService,
    IWorkflowCatalogAppService<TDto, TInput>
    where TEntity : WorkflowCatalogAggregate
{
    protected readonly IRepository<TEntity, Guid> Repository;
    private readonly IAbpDistributedLock _distributedLock;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    protected abstract string ReadPermission { get; }
    protected abstract string CreatePermission { get; }
    protected abstract string UpdatePermission { get; }
    protected abstract string DeletePermission { get; }

    protected WorkflowCatalogAppService(
        IRepository<TEntity, Guid> repository,
        IAbpDistributedLock distributedLock,
        IUnitOfWorkManager unitOfWorkManager)
    {
        Repository = repository;
        _distributedLock = distributedLock;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public async Task<TDto> GetAsync(Guid id)
    {
        await AuthorizationService.CheckAsync(ReadPermission);
        return Map(await Repository.GetAsync(id));
    }

    public async Task<PagedResultDto<TDto>> GetListAsync(WorkflowCatalogListInput input)
    {
        await AuthorizationService.CheckAsync(ReadPermission);
        var query = await CreateFilteredQueryAsync(input);
        var total = await AsyncExecuter.CountAsync(query);
        var entities = await AsyncExecuter.ToListAsync(
            query.OrderByDescending(x => x.CreationTime)
                .PageBy(input.SkipCount, input.MaxResultCount));
        return new PagedResultDto<TDto>(total, entities.Select(Map).ToList());
    }

    public async Task<TDto> CreateAsync(TInput input)
    {
        await AuthorizationService.CheckAsync(CreatePermission);
        return await MutateAsync(async () =>
        {
            await ValidateParentAsync(input);
            var entity = CreateEntity(input);
            await Repository.InsertAsync(entity, autoSave: true);
            return Map(entity);
        });
    }

    public async Task<TDto> UpdateAsync(Guid id, TInput input)
    {
        await AuthorizationService.CheckAsync(UpdatePermission);
        return await MutateAsync(async () =>
        {
            await ValidateParentAsync(input);
            var entity = await Repository.GetAsync(id);
            if (!string.Equals(
                    entity.ConcurrencyStamp,
                    GetConcurrencyStamp(input),
                    StringComparison.Ordinal))
            {
                throw new AbpDbConcurrencyException();
            }
            UpdateEntity(entity, input);
            await Repository.UpdateAsync(entity, autoSave: true);
            return Map(entity);
        });
    }

    public async Task DeleteAsync(Guid id, string concurrencyStamp)
    {
        await AuthorizationService.CheckAsync(DeletePermission);
        await MutateAsync(async () =>
        {
            var entity = await Repository.GetAsync(id);
            if (!string.Equals(entity.ConcurrencyStamp, concurrencyStamp, StringComparison.Ordinal))
            {
                throw new AbpDbConcurrencyException();
            }
            if (await HasLiveChildrenAsync(id))
            {
                throw new UserFriendlyException(
                    "This workflow catalog item cannot be deleted while active children exist.");
            }

            await Repository.DeleteAsync(entity, autoSave: true);
            return true;
        });
    }

    protected virtual Task ValidateParentAsync(TInput input) => Task.CompletedTask;
    protected virtual Task<bool> HasLiveChildrenAsync(Guid id) => Task.FromResult(false);
    protected abstract IQueryable<TEntity> ApplyFilter(
        IQueryable<TEntity> query,
        WorkflowCatalogListInput input);
    protected abstract TEntity CreateEntity(TInput input);
    protected abstract void UpdateEntity(TEntity entity, TInput input);
    protected abstract string GetConcurrencyStamp(TInput input);
    protected abstract TDto Map(TEntity entity);

    private async Task<IQueryable<TEntity>> CreateFilteredQueryAsync(WorkflowCatalogListInput input)
    {
        var query = await Repository.GetQueryableAsync();
        query = query
            .WhereIf(!input.FilterText.IsNullOrWhiteSpace(),
                x => x.Code.Contains(input.FilterText!) ||
                     x.Name.Contains(input.FilterText!))
            .WhereIf(input.IsActive.HasValue, x => x.IsActive == input.IsActive);
        return ApplyFilter(query, input);
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
}

[Authorize]
public class WorkflowDefinitionAppService :
    WorkflowCatalogAppService<WorkflowDefinition, WorkflowDefinitionDto, CreateUpdateWorkflowDefinitionDto>,
    IWorkflowDefinitionAppService
{
    private readonly IRepository<Workflow, Guid> _workflows;
    protected override string ReadPermission => DocumentServicePermissions.WorkflowDefinitions.Default;
    protected override string CreatePermission => DocumentServicePermissions.WorkflowDefinitions.Create;
    protected override string UpdatePermission => DocumentServicePermissions.WorkflowDefinitions.Update;
    protected override string DeletePermission => DocumentServicePermissions.WorkflowDefinitions.Delete;

    public WorkflowDefinitionAppService(
        IRepository<WorkflowDefinition, Guid> repository,
        IRepository<Workflow, Guid> workflows,
        IAbpDistributedLock distributedLock,
        IUnitOfWorkManager unitOfWorkManager) :
        base(repository, distributedLock, unitOfWorkManager) => _workflows = workflows;

    protected override IQueryable<WorkflowDefinition> ApplyFilter(
        IQueryable<WorkflowDefinition> query,
        WorkflowCatalogListInput input) => query;
    protected override WorkflowDefinition CreateEntity(CreateUpdateWorkflowDefinitionDto input) =>
        new(GuidGenerator.Create(), CurrentTenant.Id, input);
    protected override void UpdateEntity(
        WorkflowDefinition entity,
        CreateUpdateWorkflowDefinitionDto input) => entity.Update(input);
    protected override string GetConcurrencyStamp(CreateUpdateWorkflowDefinitionDto input) =>
        input.ConcurrencyStamp;
    protected override Task<bool> HasLiveChildrenAsync(Guid id) =>
        _workflows.AnyAsync(x => x.WorkflowDefinitionId == id);
    protected override WorkflowDefinitionDto Map(WorkflowDefinition entity) => new()
    {
        Id = entity.Id, Code = entity.Code, Name = entity.Name,
        Description = entity.Description, IsActive = entity.IsActive,
        CreationTime = entity.CreationTime, ConcurrencyStamp = entity.ConcurrencyStamp
    };
}

[Authorize]
public class WorkflowAppService :
    WorkflowCatalogAppService<Workflow, WorkflowDto, CreateUpdateWorkflowDto>,
    IWorkflowAppService
{
    private readonly IRepository<WorkflowDefinition, Guid> _definitions;
    private readonly IRepository<WorkflowTemplate, Guid> _templates;
    protected override string ReadPermission => DocumentServicePermissions.Workflows.Default;
    protected override string CreatePermission => DocumentServicePermissions.Workflows.Create;
    protected override string UpdatePermission => DocumentServicePermissions.Workflows.Update;
    protected override string DeletePermission => DocumentServicePermissions.Workflows.Delete;

    public WorkflowAppService(
        IRepository<Workflow, Guid> repository,
        IRepository<WorkflowDefinition, Guid> definitions,
        IRepository<WorkflowTemplate, Guid> templates,
        IAbpDistributedLock distributedLock,
        IUnitOfWorkManager unitOfWorkManager) :
        base(repository, distributedLock, unitOfWorkManager)
    {
        _definitions = definitions;
        _templates = templates;
    }

    protected override async Task ValidateParentAsync(CreateUpdateWorkflowDto input)
    {
        if (!await _definitions.AnyAsync(x => x.Id == input.WorkflowDefinitionId))
        {
            throw new EntityNotFoundException(
                typeof(WorkflowDefinition),
                input.WorkflowDefinitionId);
        }
    }
    protected override IQueryable<Workflow> ApplyFilter(
        IQueryable<Workflow> query,
        WorkflowCatalogListInput input) =>
        query.WhereIf(input.ParentId.HasValue,
            x => x.WorkflowDefinitionId == input.ParentId);
    protected override Workflow CreateEntity(CreateUpdateWorkflowDto input) =>
        new(GuidGenerator.Create(), CurrentTenant.Id, input);
    protected override void UpdateEntity(Workflow entity, CreateUpdateWorkflowDto input) =>
        entity.Update(input);
    protected override string GetConcurrencyStamp(CreateUpdateWorkflowDto input) =>
        input.ConcurrencyStamp;
    protected override Task<bool> HasLiveChildrenAsync(Guid id) =>
        _templates.AnyAsync(x => x.WorkflowId == id);
    protected override WorkflowDto Map(Workflow entity) => new()
    {
        Id = entity.Id, Code = entity.Code, Name = entity.Name,
        Description = entity.Description, IsActive = entity.IsActive,
        WorkflowDefinitionId = entity.WorkflowDefinitionId,
        CreationTime = entity.CreationTime, ConcurrencyStamp = entity.ConcurrencyStamp
    };
}

[Authorize]
public class WorkflowTemplateAppService :
    WorkflowCatalogAppService<WorkflowTemplate, WorkflowTemplateDto, CreateUpdateWorkflowTemplateDto>,
    IWorkflowTemplateAppService
{
    private readonly IRepository<Workflow, Guid> _workflows;
    private readonly IRepository<WorkflowStepTemplate, Guid> _steps;
    protected override string ReadPermission => DocumentServicePermissions.WorkflowTemplates.Default;
    protected override string CreatePermission => DocumentServicePermissions.WorkflowTemplates.Create;
    protected override string UpdatePermission => DocumentServicePermissions.WorkflowTemplates.Update;
    protected override string DeletePermission => DocumentServicePermissions.WorkflowTemplates.Delete;

    public WorkflowTemplateAppService(
        IRepository<WorkflowTemplate, Guid> repository,
        IRepository<Workflow, Guid> workflows,
        IRepository<WorkflowStepTemplate, Guid> steps,
        IAbpDistributedLock distributedLock,
        IUnitOfWorkManager unitOfWorkManager) :
        base(repository, distributedLock, unitOfWorkManager)
    {
        _workflows = workflows;
        _steps = steps;
    }

    protected override async Task ValidateParentAsync(CreateUpdateWorkflowTemplateDto input)
    {
        if (!await _workflows.AnyAsync(x => x.Id == input.WorkflowId))
        {
            throw new EntityNotFoundException(typeof(Workflow), input.WorkflowId);
        }
    }
    protected override IQueryable<WorkflowTemplate> ApplyFilter(
        IQueryable<WorkflowTemplate> query,
        WorkflowCatalogListInput input) =>
        query.WhereIf(input.ParentId.HasValue, x => x.WorkflowId == input.ParentId);
    protected override WorkflowTemplate CreateEntity(CreateUpdateWorkflowTemplateDto input) =>
        new(GuidGenerator.Create(), CurrentTenant.Id, input);
    protected override void UpdateEntity(
        WorkflowTemplate entity,
        CreateUpdateWorkflowTemplateDto input) => entity.Update(input);
    protected override string GetConcurrencyStamp(CreateUpdateWorkflowTemplateDto input) =>
        input.ConcurrencyStamp;
    protected override Task<bool> HasLiveChildrenAsync(Guid id) =>
        _steps.AnyAsync(x => x.WorkflowTemplateId == id);
    protected override WorkflowTemplateDto Map(WorkflowTemplate entity) => new()
    {
        Id = entity.Id, Code = entity.Code, Name = entity.Name,
        WordTemplatePath = entity.WordTemplatePath,
        PdfTemplatePath = entity.PdfTemplatePath,
        ContentSchema = entity.ContentSchema,
        OutputFormat = entity.OutputFormat, SignMode = entity.SignMode,
        WorkflowId = entity.WorkflowId, IsActive = entity.IsActive,
        CreationTime = entity.CreationTime, ConcurrencyStamp = entity.ConcurrencyStamp
    };
}

[Authorize]
public class WorkflowStepTemplateAppService :
    ApplicationService,
    IWorkflowStepTemplateAppService
{
    private readonly IRepository<WorkflowStepTemplate, Guid> _steps;
    private readonly IRepository<WorkflowTemplate, Guid> _templates;
    private readonly IRepository<WorkflowStepAssignmentConfiguration, Guid> _assignmentConfigurations;
    private readonly IAbpDistributedLock _distributedLock;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public WorkflowStepTemplateAppService(
        IRepository<WorkflowStepTemplate, Guid> steps,
        IRepository<WorkflowTemplate, Guid> templates,
        IRepository<WorkflowStepAssignmentConfiguration, Guid> assignmentConfigurations,
        IAbpDistributedLock distributedLock,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _steps = steps;
        _templates = templates;
        _assignmentConfigurations = assignmentConfigurations;
        _distributedLock = distributedLock;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public async Task<WorkflowStepTemplateDto> GetAsync(Guid id)
    {
        await AuthorizationService.CheckAsync(DocumentServicePermissions.WorkflowStepTemplates.Default);
        return Map(await _steps.GetAsync(id));
    }

    public async Task<PagedResultDto<WorkflowStepTemplateDto>> GetListAsync(
        WorkflowCatalogListInput input)
    {
        await AuthorizationService.CheckAsync(DocumentServicePermissions.WorkflowStepTemplates.Default);
        var query = await _steps.GetQueryableAsync();
        query = query
            .WhereIf(!input.FilterText.IsNullOrWhiteSpace(),
                x => x.Name.Contains(input.FilterText!))
            .WhereIf(input.IsActive.HasValue, x => x.IsActive == input.IsActive)
            .WhereIf(input.ParentId.HasValue, x => x.WorkflowTemplateId == input.ParentId);
        var total = await AsyncExecuter.CountAsync(query);
        var entities = await AsyncExecuter.ToListAsync(
            query.OrderBy(x => x.Order).PageBy(input.SkipCount, input.MaxResultCount));
        return new PagedResultDto<WorkflowStepTemplateDto>(
            total, entities.Select(Map).ToList());
    }

    public Task<WorkflowStepTemplateDto> CreateAsync(CreateUpdateWorkflowStepTemplateDto input) =>
        MutateAsync(async () =>
        {
            await AuthorizationService.CheckAsync(DocumentServicePermissions.WorkflowStepTemplates.Create);
            await EnsureTemplateExistsAsync(input.WorkflowTemplateId);
            var entity = new WorkflowStepTemplate(
                GuidGenerator.Create(), CurrentTenant.Id, input);
            await _steps.InsertAsync(entity, autoSave: true);
            return Map(entity);
        });

    public Task<WorkflowStepTemplateDto> UpdateAsync(
        Guid id,
        CreateUpdateWorkflowStepTemplateDto input) =>
        MutateAsync(async () =>
        {
            await AuthorizationService.CheckAsync(DocumentServicePermissions.WorkflowStepTemplates.Update);
            await EnsureTemplateExistsAsync(input.WorkflowTemplateId);
            var entity = await _steps.GetAsync(id);
            if (!string.Equals(
                    entity.ConcurrencyStamp,
                    input.ConcurrencyStamp,
                    StringComparison.Ordinal))
            {
                throw new AbpDbConcurrencyException();
            }
            entity.Update(input);
            await _steps.UpdateAsync(entity, autoSave: true);
            return Map(entity);
        });

    public async Task DeleteAsync(Guid id, string concurrencyStamp)
    {
        await AuthorizationService.CheckAsync(DocumentServicePermissions.WorkflowStepTemplates.Delete);
        await MutateAsync(async () =>
        {
            var entity = await _steps.GetAsync(id);
            if (!string.Equals(entity.ConcurrencyStamp, concurrencyStamp, StringComparison.Ordinal))
            {
                throw new AbpDbConcurrencyException();
            }
            if (await _assignmentConfigurations.AnyAsync(
                    x => x.WorkflowStepTemplateId == id))
            {
                throw new UserFriendlyException(
                    "A workflow step cannot be deleted while active assignment configurations exist.");
            }
            await _steps.DeleteAsync(entity, autoSave: true);
            return true;
        });
    }

    private async Task<TResult> MutateAsync<TResult>(Func<Task<TResult>> action)
    {
        var tenantKey = CurrentTenant.Id?.ToString("N") ?? "host";
        await using var handle = await _distributedLock.TryAcquireAsync(
            $"document-workflow-catalog:{tenantKey}", TimeSpan.FromSeconds(30));
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

    private async Task EnsureTemplateExistsAsync(Guid workflowTemplateId)
    {
        if (!await _templates.AnyAsync(x => x.Id == workflowTemplateId))
        {
            throw new EntityNotFoundException(
                typeof(WorkflowTemplate),
                workflowTemplateId);
        }
    }

    private static WorkflowStepTemplateDto Map(WorkflowStepTemplate entity) => new()
    {
        Id = entity.Id, Order = entity.Order, Name = entity.Name,
        Type = entity.Type, SlaDays = entity.SlaDays,
        AllowReturn = entity.AllowReturn, IsActive = entity.IsActive,
        WorkflowTemplateId = entity.WorkflowTemplateId,
        CreationTime = entity.CreationTime, ConcurrencyStamp = entity.ConcurrencyStamp
    };
}
