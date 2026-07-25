using hanhchinhso.OrganizationService.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Data;

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
