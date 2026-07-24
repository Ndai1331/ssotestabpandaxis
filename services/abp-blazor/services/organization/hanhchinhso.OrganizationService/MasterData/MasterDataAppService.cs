using hanhchinhso.OrganizationService.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Data;

namespace hanhchinhso.OrganizationService.MasterData;

[Authorize(OrganizationServicePermissions.MasterData)]
public class MasterDataAppService :
    CrudAppService<
        MasterDataItem,
        MasterDataItemDto,
        Guid,
        MasterDataListInput,
        CreateUpdateMasterDataItemDto>,
    IMasterDataAppService
{
    public MasterDataAppService(IRepository<MasterDataItem, Guid> repository)
        : base(repository)
    {
        CreatePolicyName = OrganizationServicePermissions.Create;
        UpdatePolicyName = OrganizationServicePermissions.Update;
        DeletePolicyName = OrganizationServicePermissions.Delete;
    }

    protected override async Task<IQueryable<MasterDataItem>> CreateFilteredQueryAsync(MasterDataListInput input)
    {
        var query = await Repository.GetQueryableAsync();
        return query
            .WhereIf(!input.FilterText.IsNullOrWhiteSpace(), x =>
                x.Type.Contains(input.FilterText!) || x.Code.Contains(input.FilterText!) ||
                x.Name.Contains(input.FilterText!))
            .WhereIf(!input.Type.IsNullOrWhiteSpace(), x => x.Type == input.Type)
            .WhereIf(!input.Code.IsNullOrWhiteSpace(), x => x.Code == input.Code)
            .WhereIf(!input.Name.IsNullOrWhiteSpace(), x => x.Name.Contains(input.Name!))
            .WhereIf(input.SortOrderMin.HasValue, x => x.SortOrder >= input.SortOrderMin)
            .WhereIf(input.SortOrderMax.HasValue, x => x.SortOrder <= input.SortOrderMax)
            .WhereIf(input.IsActive.HasValue, x => x.IsActive == input.IsActive);
    }

    protected override Task<MasterDataItem> MapToEntityAsync(CreateUpdateMasterDataItemDto input)
    {
        return Task.FromResult(new MasterDataItem(
            GuidGenerator.Create(),
            CurrentTenant.Id,
            input.Type,
            input.Code,
            input.Name,
            input.SortOrder,
            input.IsActive));
    }

    protected override Task MapToEntityAsync(
        CreateUpdateMasterDataItemDto input,
        MasterDataItem entity)
    {
        entity.Update(input.Type, input.Code, input.Name, input.SortOrder, input.IsActive);
        entity.SetConcurrencyStampIfNotNull(input.ConcurrencyStamp);
        return Task.CompletedTask;
    }

    protected override MasterDataItemDto MapToGetOutputDto(MasterDataItem entity)
    {
        return new MasterDataItemDto
        {
            Id = entity.Id,
            Type = entity.Type,
            Code = entity.Code,
            Name = entity.Name,
            SortOrder = entity.SortOrder,
            IsActive = entity.IsActive,
            ConcurrencyStamp = entity.ConcurrencyStamp,
            CreationTime = entity.CreationTime,
            CreatorId = entity.CreatorId,
            LastModificationTime = entity.LastModificationTime,
            LastModifierId = entity.LastModifierId
        };
    }
}
