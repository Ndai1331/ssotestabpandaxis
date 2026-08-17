using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities;

namespace hanhchinhso.OrganizationService.MasterData;

public class CreateUpdateMasterDataItemDto : IHasConcurrencyStamp
{
    [Required]
    [StringLength(50)]
    public string Type { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string Name { get; set; } = string.Empty;

    [Range(0, 10000)]
    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
    public string ConcurrencyStamp { get; set; } = string.Empty;
}
