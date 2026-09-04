using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace HCS.OrganizationService.Contracts;

public sealed class OrganizationListInput : PagedAndSortedResultRequestDto
{
    [StringLength(200)]
    public string? Filter { get; set; }
    public bool? IsActive { get; set; }
}

public sealed record DepartmentDto(Guid Id, string Code, string Name, Guid? ParentId, int SortOrder, bool IsActive);
public sealed record UnitDto(Guid Id, Guid DepartmentId, string Code, string Name, int SortOrder, bool IsActive);
public sealed record PositionDto(Guid Id, string Code, string Name, int SignOrder, int SortOrder, bool IsActive);
public sealed record MasterDataItemDto(Guid Id, string Type, string Code, string Name, int SortOrder, bool IsActive);
public sealed record Icd10Dto(Guid Id, string Code, string Name, string DiseaseGroup, bool IsChronic, int SortOrder);
public sealed record BloodPressureRangeDto(Guid Id, int HATTMin, int HATTMax, int HATTrMin, int HATTrMax,
    string Title, string Description, int SortOrder);
public sealed record BloodGlucoseRangeDto(Guid Id, string Title, decimal MinValue, decimal MaxValue,
    string Description, bool BeforeMeal, int SortOrder);
public sealed record BmiRangeDto(Guid Id, string Title, string Gender, decimal MinValue, decimal MaxValue,
    string Description, int SortOrder);
public sealed record CountryDto(Guid Id, string Code, string Name, string CountryCode, int SortOrder);
public sealed record ProvinceDto(Guid Id, string Code, string Name, Guid CountryId, string CountryCode, int SortOrder);
public sealed record CommuneDto(Guid Id, string Code, string Name, Guid ProvinceId, string ProvinceCode, int SortOrder);
public sealed record UserOrganizationMappingDto(
    Guid Id, Guid UserId, Guid DepartmentId, Guid? UnitId, Guid? PositionId, bool IsPrimary);

public sealed record UserDepartmentLookupDto(Guid UserId, Guid? DepartmentId, string? DepartmentName = null,
    Guid? PositionId = null, string? PositionName = null);

public sealed class UpsertDepartmentDto
{
    [Required, StringLength(50)] public string Code { get; set; } = string.Empty;
    [Required, StringLength(256)] public string Name { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    [Range(0, 10_000)] public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class UpsertUnitDto
{
    public Guid DepartmentId { get; set; }
    [Required, StringLength(50)] public string Code { get; set; } = string.Empty;
    [Required, StringLength(256)] public string Name { get; set; } = string.Empty;
    [Range(0, 10_000)] public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class UpsertPositionDto
{
    [Required, StringLength(50)] public string Code { get; set; } = string.Empty;
    [Required, StringLength(256)] public string Name { get; set; } = string.Empty;
    [Range(0, 100)] public int SignOrder { get; set; }
    [Range(0, 10_000)] public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class UpsertMasterDataItemDto
{
    [Required, StringLength(50)] public string Type { get; set; } = string.Empty;
    [Required, StringLength(50)] public string Code { get; set; } = string.Empty;
    [Required, StringLength(256)] public string Name { get; set; } = string.Empty;
    [Range(0, 10_000)] public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class UpsertIcd10Dto
{
    [Required, StringLength(50)] public string Code { get; set; } = string.Empty;
    [Required, StringLength(256)] public string Name { get; set; } = string.Empty;
    [Required, StringLength(256)] public string DiseaseGroup { get; set; } = string.Empty;
    public bool IsChronic { get; set; }
    [Range(0, 10_000)] public int SortOrder { get; set; }
}

public sealed class UpsertBloodPressureRangeDto
{
    [Range(0, 1000)] public int HATTMin { get; set; }
    [Range(0, 1000)] public int HATTMax { get; set; }
    [Range(0, 1000)] public int HATTrMin { get; set; }
    [Range(0, 1000)] public int HATTrMax { get; set; }
    [Required, StringLength(256)] public string Title { get; set; } = string.Empty;
    [StringLength(2000)] public string? Description { get; set; }
    [Range(0, 10_000)] public int SortOrder { get; set; }
}

public sealed class UpsertBloodGlucoseRangeDto
{
    [Required, StringLength(256)] public string Title { get; set; } = string.Empty;
    [Range(typeof(decimal), "0", "1000000")] public decimal MinValue { get; set; }
    [Range(typeof(decimal), "0", "1000000")] public decimal MaxValue { get; set; }
    [StringLength(2000)] public string? Description { get; set; }
    public bool BeforeMeal { get; set; }
    [Range(0, 10_000)] public int SortOrder { get; set; }
}

public sealed class UpsertBmiRangeDto
{
    [Required, StringLength(256)] public string Title { get; set; } = string.Empty;
    [Required, StringLength(50)] public string Gender { get; set; } = string.Empty;
    [Range(typeof(decimal), "0", "1000000")] public decimal MinValue { get; set; }
    [Range(typeof(decimal), "0", "1000000")] public decimal MaxValue { get; set; }
    [StringLength(2000)] public string? Description { get; set; }
    [Range(0, 10_000)] public int SortOrder { get; set; }
}

public sealed class UpsertCountryDto
{
    [Required, StringLength(50)] public string Code { get; set; } = string.Empty;
    [Required, StringLength(256)] public string Name { get; set; } = string.Empty;
    [Required, StringLength(10)] public string CountryCode { get; set; } = string.Empty;
    [Range(0, 10_000)] public int SortOrder { get; set; }
}

public sealed class UpsertProvinceDto
{
    [Required, StringLength(50)] public string Code { get; set; } = string.Empty;
    [Required, StringLength(256)] public string Name { get; set; } = string.Empty;
    public Guid CountryId { get; set; }
    [Range(0, 10_000)] public int SortOrder { get; set; }
}

public sealed class UpsertCommuneDto
{
    [Required, StringLength(50)] public string Code { get; set; } = string.Empty;
    [Required, StringLength(256)] public string Name { get; set; } = string.Empty;
    public Guid ProvinceId { get; set; }
    [Range(0, 10_000)] public int SortOrder { get; set; }
}

public sealed class UpsertUserOrganizationMappingDto
{
    public Guid UserId { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? PositionId { get; set; }
    public bool IsPrimary { get; set; }
}
