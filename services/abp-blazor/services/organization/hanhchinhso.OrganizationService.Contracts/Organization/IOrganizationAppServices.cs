using Volo.Abp.Application.Services;

namespace hanhchinhso.OrganizationService.Organization;

public interface IUnitAppService :
    ICrudAppService<UnitDto, Guid, OrganizationListInput, CreateUpdateUnitDto>;

public interface IPositionAppService :
    ICrudAppService<PositionDto, Guid, PositionListInput, CreateUpdatePositionDto>;

public interface IDepartmentAppService :
    ICrudAppService<DepartmentDto, Guid, DepartmentListInput, CreateUpdateDepartmentDto>;

public interface IUserDepartmentAppService :
    ICrudAppService<UserDepartmentDto, Guid, UserDepartmentListInput, CreateUpdateUserDepartmentDto>;
