using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace hanhchinhso.OrganizationService.MasterData;

public class MasterDataItem : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }

    protected MasterDataItem()
    {
    }

    public MasterDataItem(
        Guid id,
        Guid? tenantId,
        string type,
        string code,
        string name,
        int sortOrder,
        bool isActive = true) : base(id)
    {
        TenantId = tenantId;
        SetType(type);
        SetCode(code);
        SetName(name);
        SetSortOrder(sortOrder);
        IsActive = isActive;
    }

    public void Update(string type, string code, string name, int sortOrder, bool isActive)
    {
        SetType(type);
        SetCode(code);
        SetName(name);
        SetSortOrder(sortOrder);
        IsActive = isActive;
    }

    private void SetType(string value) => Type = Check.NotNullOrWhiteSpace(value, nameof(value), 50);
    private void SetCode(string value) => Code = Check.NotNullOrWhiteSpace(value, nameof(value), 50);
    private void SetName(string value) => Name = Check.NotNullOrWhiteSpace(value, nameof(value), 256);
    private void SetSortOrder(int value)
    {
        if (value is < 0 or > 10000)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Sort order must be between 0 and 10000.");
        }

        SortOrder = value;
    }
}
