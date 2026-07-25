using Volo.Abp.Application.Services;

namespace hanhchinhso.OrganizationService.Organization;

public interface IUnitAppService :
    ICrudAppService<UnitDto, Guid, OrganizationListInput, CreateUpdateUnitDto>;

public interface IPositionAppService :
    ICrudAppService<PositionDto, Guid, PositionListInput, CreateUpdatePositionDto>;
