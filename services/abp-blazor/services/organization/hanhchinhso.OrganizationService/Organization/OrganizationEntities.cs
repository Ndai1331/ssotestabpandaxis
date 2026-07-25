using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace hanhchinhso.OrganizationService.Organization;

public abstract class CodedOrganizationAggregate : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; protected set; }
    public string Code { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public bool IsActive { get; protected set; }

    protected CodedOrganizationAggregate() { }

    protected void SetCommon(string code, string name, bool isActive)
    {
        Code = Check.NotNullOrWhiteSpace(code, nameof(code), 50);
        Name = Check.NotNullOrWhiteSpace(name, nameof(name), 256);
        IsActive = isActive;
    }
}

public class Unit : CodedOrganizationAggregate
{
    public int SortOrder { get; private set; }

    protected Unit() { }

    public Unit(Guid id, Guid? tenantId, string code, string name, int sortOrder, bool isActive) : base()
    {
        Id = id;
        TenantId = tenantId;
        Update(code, name, sortOrder, isActive);
    }

    public void Update(string code, string name, int sortOrder, bool isActive)
    {
        SetCommon(code, name, isActive);
        SortOrder = sortOrder;
    }
}

public class Position : CodedOrganizationAggregate
{
    public int SignOrder { get; private set; }

    protected Position() { }

    public Position(Guid id, Guid? tenantId, string code, string name, int signOrder, bool isActive) : base()
    {
        Id = id;
        TenantId = tenantId;
        Update(code, name, signOrder, isActive);
    }

    public void Update(string code, string name, int signOrder, bool isActive)
    {
        if (signOrder is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(signOrder), signOrder, "Sign order must be between 0 and 100.");
        }

        SetCommon(code, name, isActive);
        SignOrder = signOrder;
    }
}
