using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace hanhchinhso.OrganizationService.MasterData;

public interface IMasterDataAppService :
    ICrudAppService<
        MasterDataItemDto,
        Guid,
        MasterDataListInput,
        CreateUpdateMasterDataItemDto>
{
}
