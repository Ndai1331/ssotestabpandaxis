using Volo.Abp.Application.Dtos;

namespace hanhchinhso.OrganizationService.MasterData;

public class MasterDataListInput : PagedAndSortedResultRequestDto
{
    public MasterDataListInput() => Sorting = "SortOrder";

    public string? FilterText { get; set; }
    public string? Type { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public int? SortOrderMin { get; set; }
    public int? SortOrderMax { get; set; }
    public bool? IsActive { get; set; }
}
