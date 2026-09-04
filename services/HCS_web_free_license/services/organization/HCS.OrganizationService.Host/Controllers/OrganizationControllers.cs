using HCS.OrganizationService.Application;
using HCS.OrganizationService.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;

namespace HCS.OrganizationService.Host.Controllers;

[ApiController]
[IgnoreAntiforgeryToken]
public abstract class OrganizationControllerBase : ControllerBase
{
    protected IOrganizationAppService Service { get; }
    protected OrganizationControllerBase(IOrganizationAppService service) => Service = service;
}

[Route("api/organization/departments")]
[Authorize(OrganizationPermissions.Departments)]
public sealed class DepartmentsController(IOrganizationAppService service) : OrganizationControllerBase(service)
{
    [HttpGet] public Task<PagedResultDto<DepartmentDto>> List([FromQuery] OrganizationListInput input, CancellationToken ct) => Service.GetDepartmentsAsync(input, ct);
    [HttpPost] public Task<DepartmentDto> Create([FromBody] UpsertDepartmentDto input, CancellationToken ct) => Service.CreateDepartmentAsync(input, ct);
    [HttpPut("{id:guid}")] public Task<DepartmentDto> Update(Guid id, [FromBody] UpsertDepartmentDto input, CancellationToken ct) => Service.UpdateDepartmentAsync(id, input, ct);
    [HttpDelete("{id:guid}")] public Task Delete(Guid id, CancellationToken ct) => Service.DeleteDepartmentAsync(id, ct);
}

[Route("api/organization/units")]
[Authorize(OrganizationPermissions.Units)]
public sealed class UnitsController(IOrganizationAppService service) : OrganizationControllerBase(service)
{
    [HttpGet] public Task<PagedResultDto<UnitDto>> List([FromQuery] OrganizationListInput input, CancellationToken ct) => Service.GetUnitsAsync(input, ct);
    [HttpPost] public Task<UnitDto> Create([FromBody] UpsertUnitDto input, CancellationToken ct) => Service.CreateUnitAsync(input, ct);
    [HttpPut("{id:guid}")] public Task<UnitDto> Update(Guid id, [FromBody] UpsertUnitDto input, CancellationToken ct) => Service.UpdateUnitAsync(id, input, ct);
    [HttpDelete("{id:guid}")] public Task Delete(Guid id, CancellationToken ct) => Service.DeleteUnitAsync(id, ct);
}

[Route("api/organization/positions")]
[Authorize(OrganizationPermissions.Positions)]
public sealed class PositionsController(IOrganizationAppService service) : OrganizationControllerBase(service)
{
    [HttpGet] public Task<PagedResultDto<PositionDto>> List([FromQuery] OrganizationListInput input, CancellationToken ct) => Service.GetPositionsAsync(input, ct);
    [HttpPost] public Task<PositionDto> Create([FromBody] UpsertPositionDto input, CancellationToken ct) => Service.CreatePositionAsync(input, ct);
    [HttpPut("{id:guid}")] public Task<PositionDto> Update(Guid id, [FromBody] UpsertPositionDto input, CancellationToken ct) => Service.UpdatePositionAsync(id, input, ct);
    [HttpDelete("{id:guid}")] public Task Delete(Guid id, CancellationToken ct) => Service.DeletePositionAsync(id, ct);
}

[Route("api/organization/master-data")]
[Authorize(OrganizationPermissions.MasterData)]
public sealed class MasterDataController(IOrganizationAppService service) : OrganizationControllerBase(service)
{
    [HttpGet] public Task<PagedResultDto<MasterDataItemDto>> List([FromQuery] string? type, [FromQuery] OrganizationListInput input, CancellationToken ct) => Service.GetMasterDataAsync(type, input, ct);
    [HttpPost] public Task<MasterDataItemDto> Create([FromBody] UpsertMasterDataItemDto input, CancellationToken ct) => Service.CreateMasterDataAsync(input, ct);
    [HttpPut("{id:guid}")] public Task<MasterDataItemDto> Update(Guid id, [FromBody] UpsertMasterDataItemDto input, CancellationToken ct) => Service.UpdateMasterDataAsync(id, input, ct);
    [HttpDelete("{id:guid}")] public Task Delete(Guid id, CancellationToken ct) => Service.DeleteMasterDataAsync(id, ct);
}

[Route("api/organization/icd10")]
[Authorize(OrganizationPermissions.Icd10)]
public sealed class Icd10Controller(IOrganizationAppService service) : OrganizationControllerBase(service)
{
    [HttpGet] public Task<PagedResultDto<Icd10Dto>> List([FromQuery] OrganizationListInput input, CancellationToken ct) => Service.GetIcd10Async(input, ct);
    [HttpPost] public Task<Icd10Dto> Create([FromBody] UpsertIcd10Dto input, CancellationToken ct) => Service.CreateIcd10Async(input, ct);
    [HttpPut("{id:guid}")] public Task<Icd10Dto> Update(Guid id, [FromBody] UpsertIcd10Dto input, CancellationToken ct) => Service.UpdateIcd10Async(id, input, ct);
    [HttpDelete("{id:guid}")] public Task Delete(Guid id, CancellationToken ct) => Service.DeleteIcd10Async(id, ct);
}

[Route("api/organization/blood-pressure")]
[Authorize(OrganizationPermissions.BloodPressure)]
public sealed class BloodPressureController(IOrganizationAppService service) : OrganizationControllerBase(service)
{
    [HttpGet] public Task<PagedResultDto<BloodPressureRangeDto>> List([FromQuery] OrganizationListInput input, CancellationToken ct) => Service.GetBloodPressureRangesAsync(input, ct);
    [HttpPost] public Task<BloodPressureRangeDto> Create([FromBody] UpsertBloodPressureRangeDto input, CancellationToken ct) => Service.CreateBloodPressureRangeAsync(input, ct);
    [HttpPut("{id:guid}")] public Task<BloodPressureRangeDto> Update(Guid id, [FromBody] UpsertBloodPressureRangeDto input, CancellationToken ct) => Service.UpdateBloodPressureRangeAsync(id, input, ct);
    [HttpDelete("{id:guid}")] public Task Delete(Guid id, CancellationToken ct) => Service.DeleteBloodPressureRangeAsync(id, ct);
}

[Route("api/organization/blood-glucose")]
[Authorize(OrganizationPermissions.BloodGlucose)]
public sealed class BloodGlucoseController(IOrganizationAppService service) : OrganizationControllerBase(service)
{
    [HttpGet] public Task<PagedResultDto<BloodGlucoseRangeDto>> List([FromQuery] OrganizationListInput input, CancellationToken ct) => Service.GetBloodGlucoseRangesAsync(input, ct);
    [HttpPost] public Task<BloodGlucoseRangeDto> Create([FromBody] UpsertBloodGlucoseRangeDto input, CancellationToken ct) => Service.CreateBloodGlucoseRangeAsync(input, ct);
    [HttpPut("{id:guid}")] public Task<BloodGlucoseRangeDto> Update(Guid id, [FromBody] UpsertBloodGlucoseRangeDto input, CancellationToken ct) => Service.UpdateBloodGlucoseRangeAsync(id, input, ct);
    [HttpDelete("{id:guid}")] public Task Delete(Guid id, CancellationToken ct) => Service.DeleteBloodGlucoseRangeAsync(id, ct);
}

[Route("api/organization/bmi")]
[Authorize(OrganizationPermissions.Bmi)]
public sealed class BmiController(IOrganizationAppService service) : OrganizationControllerBase(service)
{
    [HttpGet] public Task<PagedResultDto<BmiRangeDto>> List([FromQuery] OrganizationListInput input, CancellationToken ct) => Service.GetBmiRangesAsync(input, ct);
    [HttpPost] public Task<BmiRangeDto> Create([FromBody] UpsertBmiRangeDto input, CancellationToken ct) => Service.CreateBmiRangeAsync(input, ct);
    [HttpPut("{id:guid}")] public Task<BmiRangeDto> Update(Guid id, [FromBody] UpsertBmiRangeDto input, CancellationToken ct) => Service.UpdateBmiRangeAsync(id, input, ct);
    [HttpDelete("{id:guid}")] public Task Delete(Guid id, CancellationToken ct) => Service.DeleteBmiRangeAsync(id, ct);
}

[Route("api/organization/countries")]
[Authorize(OrganizationPermissions.Countries)]
public sealed class CountriesController(IOrganizationAppService service) : OrganizationControllerBase(service)
{
    [HttpGet] public Task<PagedResultDto<CountryDto>> List([FromQuery] OrganizationListInput input, CancellationToken ct) => Service.GetCountriesAsync(input, ct);
    [HttpPost] public Task<CountryDto> Create([FromBody] UpsertCountryDto input, CancellationToken ct) => Service.CreateCountryAsync(input, ct);
    [HttpPut("{id:guid}")] public Task<CountryDto> Update(Guid id, [FromBody] UpsertCountryDto input, CancellationToken ct) => Service.UpdateCountryAsync(id, input, ct);
    [HttpDelete("{id:guid}")] public Task Delete(Guid id, CancellationToken ct) => Service.DeleteCountryAsync(id, ct);
}

[Route("api/organization/provinces")]
[Authorize(OrganizationPermissions.Provinces)]
public sealed class ProvincesController(IOrganizationAppService service) : OrganizationControllerBase(service)
{
    [HttpGet] public Task<PagedResultDto<ProvinceDto>> List([FromQuery] OrganizationListInput input, CancellationToken ct) => Service.GetProvincesAsync(input, ct);
    [HttpPost] public Task<ProvinceDto> Create([FromBody] UpsertProvinceDto input, CancellationToken ct) => Service.CreateProvinceAsync(input, ct);
    [HttpPut("{id:guid}")] public Task<ProvinceDto> Update(Guid id, [FromBody] UpsertProvinceDto input, CancellationToken ct) => Service.UpdateProvinceAsync(id, input, ct);
    [HttpDelete("{id:guid}")] public Task Delete(Guid id, CancellationToken ct) => Service.DeleteProvinceAsync(id, ct);
}

[Route("api/organization/communes")]
[Authorize(OrganizationPermissions.Communes)]
public sealed class CommunesController(IOrganizationAppService service) : OrganizationControllerBase(service)
{
    [HttpGet] public Task<PagedResultDto<CommuneDto>> List([FromQuery] OrganizationListInput input, CancellationToken ct) => Service.GetCommunesAsync(input, ct);
    [HttpPost] public Task<CommuneDto> Create([FromBody] UpsertCommuneDto input, CancellationToken ct) => Service.CreateCommuneAsync(input, ct);
    [HttpPut("{id:guid}")] public Task<CommuneDto> Update(Guid id, [FromBody] UpsertCommuneDto input, CancellationToken ct) => Service.UpdateCommuneAsync(id, input, ct);
    [HttpDelete("{id:guid}")] public Task Delete(Guid id, CancellationToken ct) => Service.DeleteCommuneAsync(id, ct);
}

[Route("api/organization/user-mappings")]
[Authorize(OrganizationPermissions.UserMappings)]
public sealed class UserMappingsController(IOrganizationAppService service) : OrganizationControllerBase(service)
{
    [HttpGet] public Task<PagedResultDto<UserOrganizationMappingDto>> List([FromQuery] Guid? userId, [FromQuery] int skipCount = 0, [FromQuery] int maxResultCount = 20, CancellationToken ct = default) => Service.GetUserMappingsAsync(userId, skipCount, maxResultCount, ct);
    [HttpPost] public Task<UserOrganizationMappingDto> Create([FromBody] UpsertUserOrganizationMappingDto input, CancellationToken ct) => Service.CreateUserMappingAsync(input, ct);
    [HttpPut("{id:guid}")] public Task<UserOrganizationMappingDto> Update(Guid id, [FromBody] UpsertUserOrganizationMappingDto input, CancellationToken ct) => Service.UpdateUserMappingAsync(id, input, ct);
    [HttpDelete("{id:guid}")] public Task Delete(Guid id, CancellationToken ct) => Service.DeleteUserMappingAsync(id, ct);
}

[Route("api/organization/user-departments")]
[Authorize]
public sealed class UserDepartmentLookupController(IOrganizationAppService service) : OrganizationControllerBase(service)
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserDepartmentLookupDto>>> List(
        [FromQuery] Guid[] userIds, CancellationToken ct)
    {
        if (!CanReadLookup()) return Forbid();
        return Ok(await Service.GetUserDepartmentsAsync(userIds, ct));
    }

    private bool CanReadLookup() =>
        User.IsInRole("admin")
        || User.IsInRole("lanhdao")
        || User.IsInRole("bacsi")
        || User.HasClaim("permission", "Documents.Signing.Execute")
        || User.HasClaim("permission", "Documents.Workflow.Start")
        || User.HasClaim("permission", "Documents.Workflow.View")
        || User.HasClaim("permission", OrganizationPermissions.UserMappings);
}
