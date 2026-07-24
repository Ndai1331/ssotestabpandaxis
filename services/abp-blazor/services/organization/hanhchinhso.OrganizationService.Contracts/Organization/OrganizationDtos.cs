using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;

namespace hanhchinhso.OrganizationService.Organization;

public class OrganizationListInput : PagedAndSortedResultRequestDto
{
    public string? FilterText { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public int? SortOrderMin { get; set; }
    public int? SortOrderMax { get; set; }
    public bool? IsActive { get; set; }
}

public class UnitDto : FullAuditedEntityDto<Guid>, IHasConcurrencyStamp
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public string ConcurrencyStamp { get; set; } = string.Empty;
}

public class CreateUpdateUnitDto : IHasConcurrencyStamp
{
    [Required, StringLength(50)] public string Code { get; set; } = string.Empty;
    [Required, StringLength(256)] public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string ConcurrencyStamp { get; set; } = string.Empty;
}

public class PositionListInput : OrganizationListInput
{
    public int? SignOrderMin { get; set; }
    public int? SignOrderMax { get; set; }
}

public class PositionDto : FullAuditedEntityDto<Guid>, IHasConcurrencyStamp
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SignOrder { get; set; }
    public bool IsActive { get; set; }
    public string ConcurrencyStamp { get; set; } = string.Empty;
}

public class CreateUpdatePositionDto : IHasConcurrencyStamp
{
    [Required, StringLength(50)] public string Code { get; set; } = string.Empty;
    [Required, StringLength(256)] public string Name { get; set; } = string.Empty;
    [Range(0, 100)] public int SignOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string ConcurrencyStamp { get; set; } = string.Empty;
}

public class DepartmentListInput : OrganizationListInput
{
    public string? ParentId { get; set; }
    public Guid? LeaderUserId { get; set; }
    public int? LevelMin { get; set; }
    public int? LevelMax { get; set; }
}

public class DepartmentDto : FullAuditedEntityDto<Guid>, IHasConcurrencyStamp
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ParentId { get; set; }
    public int Level { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public Guid? LeaderUserId { get; set; }
    public string ConcurrencyStamp { get; set; } = string.Empty;
}

public class CreateUpdateDepartmentDto : IHasConcurrencyStamp
{
    [Required, StringLength(50)] public string Code { get; set; } = string.Empty;
    [Required, StringLength(256)] public string Name { get; set; } = string.Empty;
    [StringLength(64)] public string? ParentId { get; set; }
    public int Level { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid? LeaderUserId { get; set; }
    public string ConcurrencyStamp { get; set; } = string.Empty;
}

public class UserDepartmentListInput : PagedAndSortedResultRequestDto
{
    public Guid? DepartmentId { get; set; }
    public Guid? UserId { get; set; }
    public bool? IsPrimary { get; set; }
    public bool? IsActive { get; set; }
}

public class UserDepartmentDto : FullAuditedEntityDto<Guid>, IHasConcurrencyStamp
{
    public Guid DepartmentId { get; set; }
    public Guid UserId { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; }
    public string ConcurrencyStamp { get; set; } = string.Empty;
}

public class CreateUpdateUserDepartmentDto : IHasConcurrencyStamp
{
    public Guid DepartmentId { get; set; }
    public Guid UserId { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; } = true;
    public string ConcurrencyStamp { get; set; } = string.Empty;
}
