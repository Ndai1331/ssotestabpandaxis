using hanhchinhso.OrganizationService.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Data;
using Volo.Abp.Authorization;

namespace hanhchinhso.OrganizationService.Organization;

[Authorize(OrganizationServicePermissions.Units.Default)]
public class UnitAppService :
    CrudAppService<Unit, UnitDto, Guid, OrganizationListInput, CreateUpdateUnitDto>,
    IUnitAppService
{
    public UnitAppService(IRepository<Unit, Guid> repository) : base(repository)
    {
        CreatePolicyName = OrganizationServicePermissions.Units.Create;
        UpdatePolicyName = OrganizationServicePermissions.Units.Update;
        DeletePolicyName = OrganizationServicePermissions.Units.Delete;
    }

    protected override async Task<IQueryable<Unit>> CreateFilteredQueryAsync(OrganizationListInput input)
    {
        var query = await Repository.GetQueryableAsync();
        return query
            .WhereIf(!input.FilterText.IsNullOrWhiteSpace(),
                x => x.Code.Contains(input.FilterText!) || x.Name.Contains(input.FilterText!))
            .WhereIf(!input.Code.IsNullOrWhiteSpace(), x => x.Code == input.Code)
            .WhereIf(!input.Name.IsNullOrWhiteSpace(), x => x.Name.Contains(input.Name!))
            .WhereIf(input.SortOrderMin.HasValue, x => x.SortOrder >= input.SortOrderMin)
            .WhereIf(input.SortOrderMax.HasValue, x => x.SortOrder <= input.SortOrderMax)
            .WhereIf(input.IsActive.HasValue, x => x.IsActive == input.IsActive);
    }

    protected override Task<Unit> MapToEntityAsync(CreateUpdateUnitDto input) =>
        Task.FromResult(new Unit(GuidGenerator.Create(), CurrentTenant.Id, input.Code, input.Name,
            input.SortOrder, input.IsActive));

    protected override Task MapToEntityAsync(CreateUpdateUnitDto input, Unit entity)
    {
        entity.Update(input.Code, input.Name, input.SortOrder, input.IsActive);
        entity.SetConcurrencyStampIfNotNull(input.ConcurrencyStamp);
        return Task.CompletedTask;
    }

    protected override UnitDto MapToGetOutputDto(Unit entity) => new()
    {
        Id = entity.Id, Code = entity.Code, Name = entity.Name, SortOrder = entity.SortOrder,
        IsActive = entity.IsActive, CreationTime = entity.CreationTime,
        ConcurrencyStamp = entity.ConcurrencyStamp
    };
}

[Authorize(OrganizationServicePermissions.Positions.Default)]
public class PositionAppService :
    CrudAppService<Position, PositionDto, Guid, PositionListInput, CreateUpdatePositionDto>,
    IPositionAppService
{
    public PositionAppService(IRepository<Position, Guid> repository) : base(repository)
    {
        CreatePolicyName = OrganizationServicePermissions.Positions.Create;
        UpdatePolicyName = OrganizationServicePermissions.Positions.Update;
        DeletePolicyName = OrganizationServicePermissions.Positions.Delete;
    }

    protected override async Task<IQueryable<Position>> CreateFilteredQueryAsync(PositionListInput input)
    {
        var query = await Repository.GetQueryableAsync();
        return query
            .WhereIf(!input.FilterText.IsNullOrWhiteSpace(),
                x => x.Code.Contains(input.FilterText!) || x.Name.Contains(input.FilterText!))
            .WhereIf(!input.Code.IsNullOrWhiteSpace(), x => x.Code == input.Code)
            .WhereIf(!input.Name.IsNullOrWhiteSpace(), x => x.Name.Contains(input.Name!))
            .WhereIf(input.SignOrderMin.HasValue, x => x.SignOrder >= input.SignOrderMin)
            .WhereIf(input.SignOrderMax.HasValue, x => x.SignOrder <= input.SignOrderMax)
            .WhereIf(input.IsActive.HasValue, x => x.IsActive == input.IsActive);
    }

    protected override Task<Position> MapToEntityAsync(CreateUpdatePositionDto input) =>
        Task.FromResult(new Position(GuidGenerator.Create(), CurrentTenant.Id, input.Code, input.Name,
            input.SignOrder, input.IsActive));

    protected override Task MapToEntityAsync(CreateUpdatePositionDto input, Position entity)
    {
        entity.Update(input.Code, input.Name, input.SignOrder, input.IsActive);
        entity.SetConcurrencyStampIfNotNull(input.ConcurrencyStamp);
        return Task.CompletedTask;
    }

    protected override PositionDto MapToGetOutputDto(Position entity) => new()
    {
        Id = entity.Id, Code = entity.Code, Name = entity.Name, SignOrder = entity.SignOrder,
        IsActive = entity.IsActive, CreationTime = entity.CreationTime,
        ConcurrencyStamp = entity.ConcurrencyStamp
    };
}

[Authorize(OrganizationServicePermissions.Departments.Default)]
public class DepartmentAppService :
    CrudAppService<Department, DepartmentDto, Guid, DepartmentListInput, CreateUpdateDepartmentDto>,
    IDepartmentAppService
{
    public DepartmentAppService(IRepository<Department, Guid> repository) : base(repository)
    {
        CreatePolicyName = OrganizationServicePermissions.Departments.Create;
        UpdatePolicyName = OrganizationServicePermissions.Departments.Update;
        DeletePolicyName = OrganizationServicePermissions.Departments.Delete;
    }

    protected override async Task<IQueryable<Department>> CreateFilteredQueryAsync(DepartmentListInput input)
    {
        var query = await Repository.GetQueryableAsync();
        return query
            .WhereIf(!input.FilterText.IsNullOrWhiteSpace(),
                x => x.Code.Contains(input.FilterText!) || x.Name.Contains(input.FilterText!))
            .WhereIf(!input.Code.IsNullOrWhiteSpace(), x => x.Code == input.Code)
            .WhereIf(!input.Name.IsNullOrWhiteSpace(), x => x.Name.Contains(input.Name!))
            .WhereIf(input.LevelMin.HasValue, x => x.Level >= input.LevelMin)
            .WhereIf(input.LevelMax.HasValue, x => x.Level <= input.LevelMax)
            .WhereIf(input.SortOrderMin.HasValue, x => x.SortOrder >= input.SortOrderMin)
            .WhereIf(input.SortOrderMax.HasValue, x => x.SortOrder <= input.SortOrderMax)
            .WhereIf(input.IsActive.HasValue, x => x.IsActive == input.IsActive)
            .WhereIf(!input.ParentId.IsNullOrWhiteSpace(), x => x.ParentId == input.ParentId)
            .WhereIf(input.LeaderUserId.HasValue, x => x.LeaderUserId == input.LeaderUserId);
    }

    protected override Task<Department> MapToEntityAsync(CreateUpdateDepartmentDto input) =>
        Task.FromResult(new Department(GuidGenerator.Create(), CurrentTenant.Id, input.Code, input.Name,
            input.ParentId, input.Level, input.SortOrder, input.IsActive, input.LeaderUserId));

    protected override Task MapToEntityAsync(CreateUpdateDepartmentDto input, Department entity)
    {
        entity.Update(input.Code, input.Name, input.ParentId, input.Level, input.SortOrder,
            input.IsActive, input.LeaderUserId);
        entity.SetConcurrencyStampIfNotNull(input.ConcurrencyStamp);
        return Task.CompletedTask;
    }

    protected override DepartmentDto MapToGetOutputDto(Department entity) => new()
    {
        Id = entity.Id, Code = entity.Code, Name = entity.Name, ParentId = entity.ParentId,
        Level = entity.Level, SortOrder = entity.SortOrder, IsActive = entity.IsActive,
        LeaderUserId = entity.LeaderUserId, CreationTime = entity.CreationTime
        , ConcurrencyStamp = entity.ConcurrencyStamp
    };
}

[Authorize(OrganizationServicePermissions.UserDepartments.Default)]
public class UserDepartmentAppService :
    CrudAppService<UserDepartment, UserDepartmentDto, Guid, UserDepartmentListInput,
        CreateUpdateUserDepartmentDto>,
    IUserDepartmentAppService
{
    private readonly IRepository<Department, Guid> _departmentRepository;
    private readonly IIdentityReferenceValidator _identityReferenceValidator;

    public UserDepartmentAppService(
        IRepository<UserDepartment, Guid> repository,
        IRepository<Department, Guid> departmentRepository,
        IIdentityReferenceValidator identityReferenceValidator) : base(repository)
    {
        _departmentRepository = departmentRepository;
        _identityReferenceValidator = identityReferenceValidator;
        CreatePolicyName = OrganizationServicePermissions.UserDepartments.Create;
        UpdatePolicyName = OrganizationServicePermissions.UserDepartments.Update;
        DeletePolicyName = OrganizationServicePermissions.UserDepartments.Delete;
    }

    protected override async Task<IQueryable<UserDepartment>> CreateFilteredQueryAsync(UserDepartmentListInput input)
    {
        var query = await Repository.GetQueryableAsync();
        var userId = input.UserId;
        if (!CurrentUser.IsInRole("admin") && CurrentUser.Id.HasValue)
        {
            userId = CurrentUser.Id;
        }

        return query
            .WhereIf(input.DepartmentId.HasValue, x => x.DepartmentId == input.DepartmentId)
            .WhereIf(userId.HasValue, x => x.UserId == userId)
            .WhereIf(input.IsPrimary.HasValue, x => x.IsPrimary == input.IsPrimary)
            .WhereIf(input.IsActive.HasValue, x => x.IsActive == input.IsActive);
    }

    public override async Task<UserDepartmentDto> CreateAsync(CreateUpdateUserDepartmentDto input)
    {
        EnsureCurrentUserCanManage(input.UserId);
        await ValidateInputAsync(input);
        var result = await base.CreateAsync(input);
        if (input.IsPrimary)
        {
            await DemoteOtherPrimaryDepartmentsAsync(input.UserId, result.Id);
        }

        return result;
    }

    public override async Task<UserDepartmentDto> UpdateAsync(Guid id, CreateUpdateUserDepartmentDto input)
    {
        await EnsureCanAccessAsync(id);
        EnsureCurrentUserCanManage(input.UserId);
        await ValidateInputAsync(input, id);
        var result = await base.UpdateAsync(id, input);
        if (input.IsPrimary)
        {
            await DemoteOtherPrimaryDepartmentsAsync(input.UserId, id);
        }

        return result;
    }

    public override async Task<UserDepartmentDto> GetAsync(Guid id)
    {
        await EnsureCanAccessAsync(id);
        return await base.GetAsync(id);
    }

    public override async Task DeleteAsync(Guid id)
    {
        await EnsureCanAccessAsync(id);
        await base.DeleteAsync(id);
    }

    protected override Task<UserDepartment> MapToEntityAsync(CreateUpdateUserDepartmentDto input) =>
        Task.FromResult(new UserDepartment(GuidGenerator.Create(), CurrentTenant.Id, input.DepartmentId,
            input.UserId, input.IsPrimary, input.IsActive));

    protected override Task MapToEntityAsync(CreateUpdateUserDepartmentDto input, UserDepartment entity)
    {
        entity.Update(input.DepartmentId, input.UserId, input.IsPrimary, input.IsActive);
        entity.SetConcurrencyStampIfNotNull(input.ConcurrencyStamp);
        return Task.CompletedTask;
    }

    protected override UserDepartmentDto MapToGetOutputDto(UserDepartment entity) => new()
    {
        Id = entity.Id, DepartmentId = entity.DepartmentId, UserId = entity.UserId,
        IsPrimary = entity.IsPrimary, IsActive = entity.IsActive, CreationTime = entity.CreationTime
        , ConcurrencyStamp = entity.ConcurrencyStamp
    };

    private async Task ValidateInputAsync(CreateUpdateUserDepartmentDto input, Guid? excludedId = null)
    {
        if (input.DepartmentId == Guid.Empty || input.UserId == Guid.Empty)
        {
            throw new UserFriendlyException("DepartmentId and UserId are required.");
        }

        await _departmentRepository.GetAsync(input.DepartmentId);
        await _identityReferenceValidator.EnsureUserExistsAsync(input.UserId);
        var query = await Repository.GetQueryableAsync();
        if (await AsyncExecuter.AnyAsync(query.Where(x =>
                x.DepartmentId == input.DepartmentId &&
                x.UserId == input.UserId &&
                (!excludedId.HasValue || x.Id != excludedId.Value))))
        {
            throw new UserFriendlyException("This user is already assigned to the department.");
        }
    }

    private async Task EnsureCanAccessAsync(Guid id)
    {
        if (CurrentUser.IsInRole("admin"))
        {
            return;
        }

        var membership = await Repository.GetAsync(id);
        if (!CurrentUser.Id.HasValue || membership.UserId != CurrentUser.Id.Value)
        {
            throw new AbpAuthorizationException("You can only access your own department assignments.");
        }
    }

    private void EnsureCurrentUserCanManage(Guid userId)
    {
        if (!CurrentUser.IsInRole("admin") &&
            (!CurrentUser.Id.HasValue || CurrentUser.Id.Value != userId))
        {
            throw new AbpAuthorizationException("You can only manage your own department assignments.");
        }
    }

    private async Task DemoteOtherPrimaryDepartmentsAsync(Guid userId, Guid keepId)
    {
        var query = await Repository.GetQueryableAsync();
        var others = await AsyncExecuter.ToListAsync(
            query.Where(x => x.UserId == userId && x.IsPrimary && x.Id != keepId));
        foreach (var membership in others)
        {
            membership.SetPrimary(false);
            await Repository.UpdateAsync(membership);
        }
    }
}
