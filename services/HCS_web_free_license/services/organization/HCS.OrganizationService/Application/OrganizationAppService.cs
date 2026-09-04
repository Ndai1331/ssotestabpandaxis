using HCS.OrganizationService.Contracts;
using HCS.OrganizationService.Data;
using HCS.OrganizationService.Domain;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Guids;
using Volo.Abp.Uow;

namespace HCS.OrganizationService.Application;

[UnitOfWork(false)]
public class OrganizationAppService : ApplicationService, IOrganizationAppService
{
    private readonly OrganizationDbContext _db;
    private readonly IGuidGenerator _guidGenerator;

    public OrganizationAppService(OrganizationDbContext db, IGuidGenerator guidGenerator)
    {
        _db = db;
        _guidGenerator = guidGenerator;
    }

    public virtual async Task<PagedResultDto<DepartmentDto>> GetDepartmentsAsync(OrganizationListInput input, CancellationToken ct = default)
    {
        var query = Filter(_db.Departments.AsNoTracking(), input);
        return await PageAsync(query, input, x => new DepartmentDto(x.Id, x.Code, x.Name, x.ParentId, x.SortOrder, x.IsActive), ct);
    }

    public virtual async Task<DepartmentDto> CreateDepartmentAsync(UpsertDepartmentDto input, CancellationToken ct = default)
    {
        await EnsureUniqueCodeAsync(_db.Departments, input.Code, null, ct);
        await EnsureDepartmentParentAsync(null, input.ParentId, ct);
        var entity = new Department(_guidGenerator.Create(), input.Code, input.Name, input.ParentId, input.SortOrder, input.IsActive);
        _db.Departments.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public virtual async Task<DepartmentDto> UpdateDepartmentAsync(Guid id, UpsertDepartmentDto input, CancellationToken ct = default)
    {
        var entity = await _db.Departments.FindAsync([id], ct) ?? throw new EntityNotFoundException(typeof(Department), id);
        await EnsureUniqueCodeAsync(_db.Departments, input.Code, id, ct);
        await EnsureDepartmentParentAsync(id, input.ParentId, ct);
        entity.Update(input.Code, input.Name, input.ParentId, input.SortOrder, input.IsActive);
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public virtual Task DeleteDepartmentAsync(Guid id, CancellationToken ct = default) => DeleteAsync(_db.Departments, id, ct);

    public virtual async Task<PagedResultDto<UnitDto>> GetUnitsAsync(OrganizationListInput input, CancellationToken ct = default)
    {
        var query = Filter(_db.Units.AsNoTracking(), input);
        return await PageAsync(query, input, x => new UnitDto(x.Id, x.DepartmentId, x.Code, x.Name, x.SortOrder, x.IsActive), ct);
    }

    public virtual async Task<UnitDto> CreateUnitAsync(UpsertUnitDto input, CancellationToken ct = default)
    {
        await EnsureDepartmentExistsAsync(input.DepartmentId, ct);
        await EnsureUniqueCodeAsync(_db.Units, input.Code, null, ct);
        var entity = new Unit(_guidGenerator.Create(), input.DepartmentId, input.Code, input.Name, input.SortOrder, input.IsActive);
        _db.Units.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public virtual async Task<UnitDto> UpdateUnitAsync(Guid id, UpsertUnitDto input, CancellationToken ct = default)
    {
        var entity = await _db.Units.FindAsync([id], ct) ?? throw new EntityNotFoundException(typeof(Unit), id);
        await EnsureDepartmentExistsAsync(input.DepartmentId, ct);
        await EnsureUniqueCodeAsync(_db.Units, input.Code, id, ct);
        entity.Update(input.DepartmentId, input.Code, input.Name, input.SortOrder, input.IsActive);
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public virtual Task DeleteUnitAsync(Guid id, CancellationToken ct = default) => DeleteAsync(_db.Units, id, ct);

    public virtual async Task<PagedResultDto<PositionDto>> GetPositionsAsync(OrganizationListInput input, CancellationToken ct = default)
    {
        var query = Filter(_db.Positions.AsNoTracking(), input);
        return await PageAsync(query, input, x => new PositionDto(x.Id, x.Code, x.Name, x.SignOrder, x.SortOrder, x.IsActive), ct);
    }

    public virtual async Task<PositionDto> CreatePositionAsync(UpsertPositionDto input, CancellationToken ct = default)
    {
        await EnsureUniqueCodeAsync(_db.Positions, input.Code, null, ct);
        var entity = new Position(_guidGenerator.Create(), input.Code, input.Name, input.SignOrder, input.SortOrder, input.IsActive);
        _db.Positions.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public virtual async Task<PositionDto> UpdatePositionAsync(Guid id, UpsertPositionDto input, CancellationToken ct = default)
    {
        var entity = await _db.Positions.FindAsync([id], ct) ?? throw new EntityNotFoundException(typeof(Position), id);
        await EnsureUniqueCodeAsync(_db.Positions, input.Code, id, ct);
        entity.Update(input.Code, input.Name, input.SignOrder, input.SortOrder, input.IsActive);
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public virtual Task DeletePositionAsync(Guid id, CancellationToken ct = default) => DeleteAsync(_db.Positions, id, ct);

    public virtual async Task<PagedResultDto<MasterDataItemDto>> GetMasterDataAsync(string? type, OrganizationListInput input, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(type)) EnsureMasterDataTypeAllowed(type);
        var query = Filter(_db.MasterDataItems.AsNoTracking(), input);
        if (!string.IsNullOrWhiteSpace(type)) query = query.Where(x => x.Type == type.Trim());
        return await PageAsync(query, input, x => new MasterDataItemDto(x.Id, x.Type, x.Code, x.Name, x.SortOrder, x.IsActive), ct);
    }

    public virtual async Task<MasterDataItemDto> CreateMasterDataAsync(UpsertMasterDataItemDto input, CancellationToken ct = default)
    {
        EnsureMasterDataTypeAllowed(input.Type);
        await EnsureMasterDataUniqueAsync(input.Type, input.Code, null, ct);
        var entity = new MasterDataItem(_guidGenerator.Create(), input.Type, input.Code, input.Name, input.SortOrder, input.IsActive);
        _db.MasterDataItems.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public virtual async Task<MasterDataItemDto> UpdateMasterDataAsync(Guid id, UpsertMasterDataItemDto input, CancellationToken ct = default)
    {
        var entity = await _db.MasterDataItems.FindAsync([id], ct) ?? throw new EntityNotFoundException(typeof(MasterDataItem), id);
        EnsureMasterDataTypeAllowed(input.Type);
        await EnsureMasterDataUniqueAsync(input.Type, input.Code, id, ct);
        entity.Update(input.Type, input.Code, input.Name, input.SortOrder, input.IsActive);
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public virtual Task DeleteMasterDataAsync(Guid id, CancellationToken ct = default) => DeleteAsync(_db.MasterDataItems, id, ct);

    public virtual async Task<PagedResultDto<Icd10Dto>> GetIcd10Async(OrganizationListInput input, CancellationToken ct = default)
    {
        var query = _db.Icd10s.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var filter = input.Filter.Trim().ToLowerInvariant();
            query = query.Where(x => x.Code.ToLower().Contains(filter)
                || x.Name.ToLower().Contains(filter)
                || x.DiseaseGroup.ToLower().Contains(filter));
        }

        return await PageReferenceAsync(query.OrderBy(x => x.SortOrder).ThenBy(x => x.Code), input,
            x => new Icd10Dto(x.Id, x.Code, x.Name, x.DiseaseGroup, x.IsChronic, x.SortOrder), ct);
    }

    public virtual async Task<Icd10Dto> CreateIcd10Async(UpsertIcd10Dto input, CancellationToken ct = default)
    {
        await EnsureReferenceUniqueCodeAsync(_db.Icd10s, input.Code, null, ct);
        var entity = new Icd10(_guidGenerator.Create(), input.Code, input.Name, input.DiseaseGroup, input.IsChronic, input.SortOrder);
        _db.Icd10s.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public virtual async Task<Icd10Dto> UpdateIcd10Async(Guid id, UpsertIcd10Dto input, CancellationToken ct = default)
    {
        var entity = await _db.Icd10s.FindAsync([id], ct) ?? throw new EntityNotFoundException(typeof(Icd10), id);
        await EnsureReferenceUniqueCodeAsync(_db.Icd10s, input.Code, id, ct);
        entity.Update(input.Code, input.Name, input.DiseaseGroup, input.IsChronic, input.SortOrder);
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public virtual Task DeleteIcd10Async(Guid id, CancellationToken ct = default) => DeleteAsync(_db.Icd10s, id, ct);

    public virtual async Task<PagedResultDto<BloodPressureRangeDto>> GetBloodPressureRangesAsync(OrganizationListInput input, CancellationToken ct = default)
    {
        var query = _db.BloodPressureRanges.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var filter = input.Filter.Trim().ToLowerInvariant();
            query = query.Where(x => x.Title.ToLower().Contains(filter) || x.Description.ToLower().Contains(filter));
        }

        return await PageReferenceAsync(query.OrderBy(x => x.SortOrder).ThenBy(x => x.Title), input,
            x => new BloodPressureRangeDto(x.Id, x.HATTMin, x.HATTMax, x.HATTrMin, x.HATTrMax, x.Title, x.Description, x.SortOrder), ct);
    }

    public virtual async Task<BloodPressureRangeDto> CreateBloodPressureRangeAsync(UpsertBloodPressureRangeDto input, CancellationToken ct = default)
    {
        var entity = new BloodPressureRange(_guidGenerator.Create(), input.HATTMin, input.HATTMax,
            input.HATTrMin, input.HATTrMax, input.Title, input.Description, input.SortOrder);
        _db.BloodPressureRanges.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public virtual async Task<BloodPressureRangeDto> UpdateBloodPressureRangeAsync(Guid id, UpsertBloodPressureRangeDto input, CancellationToken ct = default)
    {
        var entity = await _db.BloodPressureRanges.FindAsync([id], ct)
            ?? throw new EntityNotFoundException(typeof(BloodPressureRange), id);
        entity.Update(input.HATTMin, input.HATTMax, input.HATTrMin, input.HATTrMax,
            input.Title, input.Description, input.SortOrder);
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public virtual Task DeleteBloodPressureRangeAsync(Guid id, CancellationToken ct = default) => DeleteAsync(_db.BloodPressureRanges, id, ct);

    public virtual async Task<PagedResultDto<BloodGlucoseRangeDto>> GetBloodGlucoseRangesAsync(OrganizationListInput input, CancellationToken ct = default)
    {
        var query = _db.BloodGlucoseRanges.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var filter = input.Filter.Trim().ToLowerInvariant();
            query = query.Where(x => x.Title.ToLower().Contains(filter) || x.Description.ToLower().Contains(filter));
        }

        return await PageReferenceAsync(query.OrderBy(x => x.SortOrder).ThenBy(x => x.Title), input,
            x => new BloodGlucoseRangeDto(x.Id, x.Title, x.MinValue, x.MaxValue, x.Description, x.BeforeMeal, x.SortOrder), ct);
    }

    public virtual async Task<BloodGlucoseRangeDto> CreateBloodGlucoseRangeAsync(UpsertBloodGlucoseRangeDto input, CancellationToken ct = default)
    {
        var entity = new BloodGlucoseRange(_guidGenerator.Create(), input.Title, input.MinValue, input.MaxValue,
            input.Description, input.BeforeMeal, input.SortOrder);
        _db.BloodGlucoseRanges.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public virtual async Task<BloodGlucoseRangeDto> UpdateBloodGlucoseRangeAsync(Guid id, UpsertBloodGlucoseRangeDto input, CancellationToken ct = default)
    {
        var entity = await _db.BloodGlucoseRanges.FindAsync([id], ct)
            ?? throw new EntityNotFoundException(typeof(BloodGlucoseRange), id);
        entity.Update(input.Title, input.MinValue, input.MaxValue, input.Description, input.BeforeMeal, input.SortOrder);
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public virtual Task DeleteBloodGlucoseRangeAsync(Guid id, CancellationToken ct = default) => DeleteAsync(_db.BloodGlucoseRanges, id, ct);

    public virtual async Task<PagedResultDto<BmiRangeDto>> GetBmiRangesAsync(OrganizationListInput input, CancellationToken ct = default)
    {
        var query = _db.BmiRanges.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var filter = input.Filter.Trim().ToLowerInvariant();
            query = query.Where(x => x.Title.ToLower().Contains(filter)
                || x.Gender.ToLower().Contains(filter)
                || x.Description.ToLower().Contains(filter));
        }

        return await PageReferenceAsync(query.OrderBy(x => x.SortOrder).ThenBy(x => x.Title), input,
            x => new BmiRangeDto(x.Id, x.Title, x.Gender, x.MinValue, x.MaxValue, x.Description, x.SortOrder), ct);
    }

    public virtual async Task<BmiRangeDto> CreateBmiRangeAsync(UpsertBmiRangeDto input, CancellationToken ct = default)
    {
        var entity = new BmiRange(_guidGenerator.Create(), input.Title, input.Gender, input.MinValue, input.MaxValue,
            input.Description, input.SortOrder);
        _db.BmiRanges.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public virtual async Task<BmiRangeDto> UpdateBmiRangeAsync(Guid id, UpsertBmiRangeDto input, CancellationToken ct = default)
    {
        var entity = await _db.BmiRanges.FindAsync([id], ct) ?? throw new EntityNotFoundException(typeof(BmiRange), id);
        entity.Update(input.Title, input.Gender, input.MinValue, input.MaxValue, input.Description, input.SortOrder);
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public virtual Task DeleteBmiRangeAsync(Guid id, CancellationToken ct = default) => DeleteAsync(_db.BmiRanges, id, ct);

    public virtual async Task<PagedResultDto<CountryDto>> GetCountriesAsync(OrganizationListInput input, CancellationToken ct = default)
    {
        var query = _db.Countries.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var filter = input.Filter.Trim().ToLowerInvariant();
            query = query.Where(x => x.Code.ToLower().Contains(filter)
                || x.Name.ToLower().Contains(filter)
                || x.CountryCode.ToLower().Contains(filter));
        }

        return await PageReferenceAsync(query.OrderBy(x => x.SortOrder).ThenBy(x => x.Name), input,
            x => new CountryDto(x.Id, x.Code, x.Name, x.CountryCode, x.SortOrder), ct);
    }

    public virtual async Task<CountryDto> CreateCountryAsync(UpsertCountryDto input, CancellationToken ct = default)
    {
        await EnsureReferenceUniqueCodeAsync(_db.Countries, input.Code, null, ct);
        var entity = new Country(_guidGenerator.Create(), input.Code, input.Name, input.CountryCode, input.SortOrder);
        _db.Countries.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public virtual async Task<CountryDto> UpdateCountryAsync(Guid id, UpsertCountryDto input, CancellationToken ct = default)
    {
        var entity = await _db.Countries.FindAsync([id], ct) ?? throw new EntityNotFoundException(typeof(Country), id);
        await EnsureReferenceUniqueCodeAsync(_db.Countries, input.Code, id, ct);
        entity.Update(input.Code, input.Name, input.CountryCode, input.SortOrder);
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public virtual async Task DeleteCountryAsync(Guid id, CancellationToken ct = default)
    {
        if (await _db.Provinces.AnyAsync(x => x.CountryId == id, ct))
            throw new BusinessException(OrganizationErrorCodes.ReferenceInUse);
        await DeleteAsync(_db.Countries, id, ct);
    }

    public virtual async Task<PagedResultDto<ProvinceDto>> GetProvincesAsync(OrganizationListInput input, CancellationToken ct = default)
    {
        var query = from province in _db.Provinces.AsNoTracking()
                    join country in _db.Countries.AsNoTracking() on province.CountryId equals country.Id
                    select new { Item = province, CountryCode = country.Code };
        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var filter = input.Filter.Trim().ToLowerInvariant();
            query = query.Where(x => x.Item.Code.ToLower().Contains(filter)
                || x.Item.Name.ToLower().Contains(filter)
                || x.CountryCode.ToLower().Contains(filter));
        }

        return await PageReferenceAsync(query.OrderBy(x => x.Item.SortOrder).ThenBy(x => x.Item.Name), input,
            x => new ProvinceDto(x.Item.Id, x.Item.Code, x.Item.Name, x.Item.CountryId, x.CountryCode, x.Item.SortOrder), ct);
    }

    public virtual async Task<ProvinceDto> CreateProvinceAsync(UpsertProvinceDto input, CancellationToken ct = default)
    {
        await EnsureCountryExistsAsync(input.CountryId, ct);
        await EnsureReferenceUniqueCodeAsync(_db.Provinces, input.Code, null, ct);
        var entity = new Province(_guidGenerator.Create(), input.Code, input.Name, input.CountryId, input.SortOrder);
        _db.Provinces.Add(entity);
        await _db.SaveChangesAsync(ct);
        return await GetProvinceAsync(entity.Id, ct);
    }

    public virtual async Task<ProvinceDto> UpdateProvinceAsync(Guid id, UpsertProvinceDto input, CancellationToken ct = default)
    {
        var entity = await _db.Provinces.FindAsync([id], ct) ?? throw new EntityNotFoundException(typeof(Province), id);
        await EnsureCountryExistsAsync(input.CountryId, ct);
        await EnsureReferenceUniqueCodeAsync(_db.Provinces, input.Code, id, ct);
        entity.Update(input.Code, input.Name, input.CountryId, input.SortOrder);
        await _db.SaveChangesAsync(ct);
        return await GetProvinceAsync(entity.Id, ct);
    }

    public virtual async Task DeleteProvinceAsync(Guid id, CancellationToken ct = default)
    {
        if (await _db.Communes.AnyAsync(x => x.ProvinceId == id, ct))
            throw new BusinessException(OrganizationErrorCodes.ReferenceInUse);
        await DeleteAsync(_db.Provinces, id, ct);
    }

    public virtual async Task<PagedResultDto<CommuneDto>> GetCommunesAsync(OrganizationListInput input, CancellationToken ct = default)
    {
        var query = from commune in _db.Communes.AsNoTracking()
                    join province in _db.Provinces.AsNoTracking() on commune.ProvinceId equals province.Id
                    select new { Item = commune, ProvinceCode = province.Code };
        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var filter = input.Filter.Trim().ToLowerInvariant();
            query = query.Where(x => x.Item.Code.ToLower().Contains(filter)
                || x.Item.Name.ToLower().Contains(filter)
                || x.ProvinceCode.ToLower().Contains(filter));
        }

        return await PageReferenceAsync(query.OrderBy(x => x.Item.SortOrder).ThenBy(x => x.Item.Name), input,
            x => new CommuneDto(x.Item.Id, x.Item.Code, x.Item.Name, x.Item.ProvinceId, x.ProvinceCode, x.Item.SortOrder), ct);
    }

    public virtual async Task<CommuneDto> CreateCommuneAsync(UpsertCommuneDto input, CancellationToken ct = default)
    {
        await EnsureProvinceExistsAsync(input.ProvinceId, ct);
        await EnsureReferenceUniqueCodeAsync(_db.Communes, input.Code, null, ct);
        var entity = new Commune(_guidGenerator.Create(), input.Code, input.Name, input.ProvinceId, input.SortOrder);
        _db.Communes.Add(entity);
        await _db.SaveChangesAsync(ct);
        return await GetCommuneAsync(entity.Id, ct);
    }

    public virtual async Task<CommuneDto> UpdateCommuneAsync(Guid id, UpsertCommuneDto input, CancellationToken ct = default)
    {
        var entity = await _db.Communes.FindAsync([id], ct) ?? throw new EntityNotFoundException(typeof(Commune), id);
        await EnsureProvinceExistsAsync(input.ProvinceId, ct);
        await EnsureReferenceUniqueCodeAsync(_db.Communes, input.Code, id, ct);
        entity.Update(input.Code, input.Name, input.ProvinceId, input.SortOrder);
        await _db.SaveChangesAsync(ct);
        return await GetCommuneAsync(entity.Id, ct);
    }

    public virtual async Task DeleteCommuneAsync(Guid id, CancellationToken ct = default) => await DeleteAsync(_db.Communes, id, ct);

    public virtual async Task<PagedResultDto<UserOrganizationMappingDto>> GetUserMappingsAsync(Guid? userId, int skipCount, int maxResultCount, CancellationToken ct = default)
    {
        var query = _db.UserOrganizationMappings.AsNoTracking();
        if (userId.HasValue) query = query.Where(x => x.UserId == userId.Value);
        var total = await query.LongCountAsync(ct);
        var items = await query.OrderByDescending(x => x.IsPrimary).ThenBy(x => x.CreationTime)
            .Skip(Math.Max(0, skipCount)).Take(Math.Clamp(maxResultCount, 1, 1000)).Select(x => Map(x)).ToListAsync(ct);
        return new PagedResultDto<UserOrganizationMappingDto>(total, items);
    }

    public virtual async Task<IReadOnlyList<UserDepartmentLookupDto>> GetUserDepartmentsAsync(
        IReadOnlyCollection<Guid> userIds, CancellationToken ct = default)
    {
        var ids = userIds.Where(x => x != Guid.Empty).Distinct().Take(200).ToHashSet();
        if (ids.Count == 0) return [];

        var mappings = await (from userMapping in _db.UserOrganizationMappings.AsNoTracking()
                              join department in _db.Departments.AsNoTracking()
                                  on userMapping.DepartmentId equals department.Id
                              join position in _db.Positions.AsNoTracking()
                                  on userMapping.PositionId equals (Guid?)position.Id into positionJoin
                              from position in positionJoin.DefaultIfEmpty()
                              where ids.Contains(userMapping.UserId)
                              orderby userMapping.UserId, userMapping.IsPrimary descending, userMapping.CreationTime
                              select new
                              {
                                  userMapping.UserId,
                                  userMapping.DepartmentId,
                                  DepartmentName = department.Name,
                                  userMapping.PositionId,
                                  PositionName = position == null ? null : position.Name
                              }).ToListAsync(ct);

        return ids.Select(userId => mappings.FirstOrDefault(x => x.UserId == userId) is { } mapping
                ? new UserDepartmentLookupDto(userId, mapping.DepartmentId, mapping.DepartmentName,
                    mapping.PositionId, mapping.PositionName)
                : new UserDepartmentLookupDto(userId, null))
            .ToArray();
    }

    public virtual async Task<UserOrganizationMappingDto> CreateUserMappingAsync(UpsertUserOrganizationMappingDto input, CancellationToken ct = default)
    {
        await ValidateMappingAsync(input, null, ct);
        var entity = new UserOrganizationMapping(_guidGenerator.Create(), input.UserId, input.DepartmentId, input.UnitId, input.PositionId, input.IsPrimary);
        _db.UserOrganizationMappings.Add(entity);
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public virtual async Task<UserOrganizationMappingDto> UpdateUserMappingAsync(Guid id, UpsertUserOrganizationMappingDto input, CancellationToken ct = default)
    {
        var entity = await _db.UserOrganizationMappings.FindAsync([id], ct) ?? throw new EntityNotFoundException(typeof(UserOrganizationMapping), id);
        await ValidateMappingAsync(input, id, ct);
        entity.Update(input.UserId, input.DepartmentId, input.UnitId, input.PositionId, input.IsPrimary);
        await _db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public virtual Task DeleteUserMappingAsync(Guid id, CancellationToken ct = default) => DeleteAsync(_db.UserOrganizationMappings, id, ct);

    private async Task ValidateMappingAsync(UpsertUserOrganizationMappingDto input, Guid? currentId, CancellationToken ct)
    {
        await EnsureDepartmentExistsAsync(input.DepartmentId, ct);
        if (input.UnitId.HasValue && !await _db.Units.AnyAsync(x => x.Id == input.UnitId && x.DepartmentId == input.DepartmentId, ct))
            throw new BusinessException(OrganizationErrorCodes.UnitDepartmentMismatch);
        if (input.PositionId.HasValue && !await _db.Positions.AnyAsync(x => x.Id == input.PositionId, ct))
            throw new EntityNotFoundException(typeof(Position), input.PositionId);
        if (input.IsPrimary && await _db.UserOrganizationMappings.AnyAsync(x => x.UserId == input.UserId && x.IsPrimary && x.Id != currentId, ct))
            throw new BusinessException(OrganizationErrorCodes.MultiplePrimaryMappings);
        if (await _db.UserOrganizationMappings.AnyAsync(x =>
                x.UserId == input.UserId && x.DepartmentId == input.DepartmentId &&
                x.UnitId == input.UnitId && x.PositionId == input.PositionId && x.Id != currentId, ct))
            throw new BusinessException(OrganizationErrorCodes.DuplicateUserMapping);
    }

    private async Task EnsureDepartmentParentAsync(Guid? departmentId, Guid? parentId, CancellationToken ct)
    {
        await EnsureDepartmentExistsAsync(parentId, ct);
        var cursor = parentId;
        while (cursor.HasValue)
        {
            if (cursor == departmentId)
                throw new BusinessException(OrganizationErrorCodes.DepartmentHierarchyCycle);
            cursor = await _db.Departments.Where(x => x.Id == cursor.Value).Select(x => x.ParentId).SingleAsync(ct);
        }
    }

    private async Task EnsureDepartmentExistsAsync(Guid? id, CancellationToken ct)
    {
        if (id.HasValue && !await _db.Departments.AnyAsync(x => x.Id == id.Value, ct))
            throw new BusinessException(OrganizationErrorCodes.InvalidDepartment).WithData("DepartmentId", id.Value);
    }

    private static async Task EnsureUniqueCodeAsync<TEntity>(DbSet<TEntity> set, string code, Guid? currentId, CancellationToken ct)
        where TEntity : CodedAggregate
    {
        var normalized = code.Trim();
        if (await set.AnyAsync(x => x.Code == normalized && x.Id != currentId, ct))
            throw new BusinessException(OrganizationErrorCodes.DuplicateCode).WithData("Code", normalized);
    }

    private async Task EnsureMasterDataUniqueAsync(string type, string code, Guid? currentId, CancellationToken ct)
    {
        var normalizedType = type.Trim();
        var normalizedCode = code.Trim();
        if (await _db.MasterDataItems.AnyAsync(x => x.Type == normalizedType && x.Code == normalizedCode && x.Id != currentId, ct))
            throw new BusinessException(OrganizationErrorCodes.DuplicateCode).WithData("Type", normalizedType).WithData("Code", normalizedCode);
    }

    private static void EnsureMasterDataTypeAllowed(string? type)
    {
        if (string.IsNullOrWhiteSpace(type) || !OrganizationConsts.AllowedMasterDataTypes.Contains(type.Trim()))
            throw new BusinessException(OrganizationErrorCodes.InvalidMasterDataType).WithData("Type", type ?? string.Empty);
    }

    private static async Task EnsureReferenceUniqueCodeAsync<TEntity>(DbSet<TEntity> set, string code, Guid? currentId, CancellationToken ct)
        where TEntity : CodedReferenceAggregate
    {
        var normalized = code.Trim();
        if (await set.AnyAsync(x => x.Code == normalized && x.Id != currentId, ct))
            throw new BusinessException(OrganizationErrorCodes.DuplicateCode).WithData("Code", normalized);
    }

    private async Task EnsureCountryExistsAsync(Guid countryId, CancellationToken ct)
    {
        if (countryId == Guid.Empty || !await _db.Countries.AnyAsync(x => x.Id == countryId, ct))
            throw new BusinessException(OrganizationErrorCodes.InvalidCountry).WithData("CountryId", countryId);
    }

    private async Task EnsureProvinceExistsAsync(Guid provinceId, CancellationToken ct)
    {
        if (provinceId == Guid.Empty || !await _db.Provinces.AnyAsync(x => x.Id == provinceId, ct))
            throw new BusinessException(OrganizationErrorCodes.InvalidProvince).WithData("ProvinceId", provinceId);
    }

    private async Task<ProvinceDto> GetProvinceAsync(Guid id, CancellationToken ct)
    {
        return await (from province in _db.Provinces.AsNoTracking()
                      join country in _db.Countries.AsNoTracking() on province.CountryId equals country.Id
                      where province.Id == id
                      select new ProvinceDto(province.Id, province.Code, province.Name,
                          province.CountryId, country.Code, province.SortOrder)).SingleAsync(ct);
    }

    private async Task<CommuneDto> GetCommuneAsync(Guid id, CancellationToken ct)
    {
        return await (from commune in _db.Communes.AsNoTracking()
                      join province in _db.Provinces.AsNoTracking() on commune.ProvinceId equals province.Id
                      where commune.Id == id
                      select new CommuneDto(commune.Id, commune.Code, commune.Name,
                          commune.ProvinceId, province.Code, commune.SortOrder)).SingleAsync(ct);
    }

    private static IQueryable<TEntity> Filter<TEntity>(IQueryable<TEntity> query, OrganizationListInput input) where TEntity : CodedAggregate
    {
        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var filter = input.Filter.Trim().ToLowerInvariant();
            query = query.Where(x => x.Code.ToLower().Contains(filter)
                                  || x.Name.ToLower().Contains(filter));
        }
        if (input.IsActive.HasValue) query = query.Where(x => x.IsActive == input.IsActive.Value);
        return query;
    }

    private static async Task<PagedResultDto<TDto>> PageAsync<TEntity, TDto>(IQueryable<TEntity> query, OrganizationListInput input, System.Linq.Expressions.Expression<Func<TEntity, TDto>> selector, CancellationToken ct)
        where TEntity : CodedAggregate
    {
        var total = await query.LongCountAsync(ct);
        var items = await query.OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
            .Skip(Math.Max(0, input.SkipCount)).Take(Math.Clamp(input.MaxResultCount, 1, 100)).Select(selector).ToListAsync(ct);
        return new PagedResultDto<TDto>(total, items);
    }

    private static async Task<PagedResultDto<TDto>> PageReferenceAsync<TEntity, TDto>(IQueryable<TEntity> query,
        OrganizationListInput input, System.Linq.Expressions.Expression<Func<TEntity, TDto>> selector, CancellationToken ct)
    {
        var total = await query.LongCountAsync(ct);
        var items = await query.Skip(Math.Max(0, input.SkipCount))
            .Take(Math.Clamp(input.MaxResultCount, 1, 100))
            .Select(selector).ToListAsync(ct);
        return new PagedResultDto<TDto>(total, items);
    }

    private async Task DeleteAsync<TEntity>(DbSet<TEntity> set, Guid id, CancellationToken ct) where TEntity : class
    {
        var entity = await set.FindAsync([id], ct) ?? throw new EntityNotFoundException(typeof(TEntity), id);
        set.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    private static DepartmentDto Map(Department x) => new(x.Id, x.Code, x.Name, x.ParentId, x.SortOrder, x.IsActive);
    private static UnitDto Map(Unit x) => new(x.Id, x.DepartmentId, x.Code, x.Name, x.SortOrder, x.IsActive);
    private static PositionDto Map(Position x) => new(x.Id, x.Code, x.Name, x.SignOrder, x.SortOrder, x.IsActive);
    private static MasterDataItemDto Map(MasterDataItem x) => new(x.Id, x.Type, x.Code, x.Name, x.SortOrder, x.IsActive);
    private static Icd10Dto Map(Icd10 x) => new(x.Id, x.Code, x.Name, x.DiseaseGroup, x.IsChronic, x.SortOrder);
    private static BloodPressureRangeDto Map(BloodPressureRange x) => new(x.Id, x.HATTMin, x.HATTMax,
        x.HATTrMin, x.HATTrMax, x.Title, x.Description, x.SortOrder);
    private static BloodGlucoseRangeDto Map(BloodGlucoseRange x) => new(x.Id, x.Title, x.MinValue, x.MaxValue,
        x.Description, x.BeforeMeal, x.SortOrder);
    private static BmiRangeDto Map(BmiRange x) => new(x.Id, x.Title, x.Gender, x.MinValue, x.MaxValue,
        x.Description, x.SortOrder);
    private static CountryDto Map(Country x) => new(x.Id, x.Code, x.Name, x.CountryCode, x.SortOrder);
    private static UserOrganizationMappingDto Map(UserOrganizationMapping x) => new(x.Id, x.UserId, x.DepartmentId, x.UnitId, x.PositionId, x.IsPrimary);
}
