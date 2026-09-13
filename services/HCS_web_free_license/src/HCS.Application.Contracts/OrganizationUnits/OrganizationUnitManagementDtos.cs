using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace HCS.OrganizationUnits;

public sealed class OrganizationUnitDto
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ConcurrencyStamp { get; set; } = string.Empty;
}

public sealed class GetOrganizationUnitMembersInput : PagedAndSortedResultRequestDto
{
    [StringLength(256)]
    public string? Filter { get; set; }
}

public sealed class OrganizationUnitMemberDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class CreateOrganizationUnitInput
{
    [Required]
    [StringLength(128)]
    public string DisplayName { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
}

public sealed class UpdateOrganizationUnitInput
{
    [Required]
    [StringLength(128)]
    public string DisplayName { get; set; } = string.Empty;
}

public sealed class MoveOrganizationUnitInput
{
    public Guid? ParentId { get; set; }
}

public sealed class MoveAllOrganizationUnitMembersInput
{
    public Guid TargetOrganizationUnitId { get; set; }
}

public sealed class AddOrganizationUnitMemberInput
{
    public Guid UserId { get; set; }
}
