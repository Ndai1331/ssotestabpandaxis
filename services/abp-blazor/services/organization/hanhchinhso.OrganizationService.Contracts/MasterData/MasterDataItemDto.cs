using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Entities;

namespace hanhchinhso.OrganizationService.MasterData;

public class MasterDataItemDto : FullAuditedEntityDto<Guid>, IHasConcurrencyStamp
{
    public string Type { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public string ConcurrencyStamp { get; set; } = string.Empty;
}
