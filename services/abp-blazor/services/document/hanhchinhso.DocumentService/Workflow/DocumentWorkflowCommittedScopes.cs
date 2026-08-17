using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.MultiTenancy;

namespace hanhchinhso.DocumentService.Workflows;

public class DocumentWorkflowCommittedReceiver : Entity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public Guid CommittedStepId { get; private set; }
    public Guid UserId { get; private set; }
    public bool IsSelected { get; private set; }
    public bool IsPrimary { get; private set; }
    public Guid? ProvenanceOrganizationUnitId { get; private set; }
    public Guid? ProvenanceRoleId { get; private set; }

    protected DocumentWorkflowCommittedReceiver() { }

    public DocumentWorkflowCommittedReceiver(
        Guid id,
        Guid? tenantId,
        Guid committedStepId,
        Guid userId,
        bool isSelected,
        bool isPrimary,
        Guid? provenanceOrganizationUnitId,
        Guid? provenanceRoleId) : base(id)
    {
        TenantId = tenantId;
        CommittedStepId = committedStepId;
        UserId = Check.NotDefaultOrNull<Guid>(userId, nameof(userId));
        IsSelected = isSelected;
        IsPrimary = isPrimary;
        ProvenanceOrganizationUnitId = provenanceOrganizationUnitId;
        ProvenanceRoleId = provenanceRoleId;
    }
}

public class DocumentWorkflowCommittedViewScope : Entity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public Guid CommittedStepId { get; private set; }
    public Guid? OrganizationUnitId { get; private set; }
    public Guid? UserId { get; private set; }

    protected DocumentWorkflowCommittedViewScope() { }

    public DocumentWorkflowCommittedViewScope(
        Guid id,
        Guid? tenantId,
        Guid committedStepId,
        Guid? organizationUnitId,
        Guid? userId) : base(id)
    {
        if ((organizationUnitId.HasValue ? 1 : 0) + (userId.HasValue ? 1 : 0) != 1)
        {
            throw new UserFriendlyException(
                "A committed view scope must contain exactly one organization unit or user.");
        }
        TenantId = tenantId;
        CommittedStepId = committedStepId;
        OrganizationUnitId = organizationUnitId;
        UserId = userId;
    }
}
