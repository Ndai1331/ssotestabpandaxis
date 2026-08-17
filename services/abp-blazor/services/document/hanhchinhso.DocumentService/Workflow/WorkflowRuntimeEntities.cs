using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace hanhchinhso.DocumentService.Workflows;

public class WorkflowStepAssignmentConfiguration :
    FullAuditedAggregateRoot<Guid>,
    IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public Guid WorkflowStepTemplateId { get; private set; }
    public WorkflowAssigneeType AssigneeType { get; private set; }
    public Guid? RoleId { get; private set; }
    public bool IsPrimary { get; private set; }
    public bool IsActive { get; private set; }
    public ICollection<WorkflowStepAssignmentUser> Users { get; private set; } = [];
    public ICollection<WorkflowStepAssignmentOrganizationUnit> OrganizationUnits { get; private set; } = [];

    protected WorkflowStepAssignmentConfiguration() { }

    public WorkflowStepAssignmentConfiguration(
        Guid id,
        Guid? tenantId,
        CreateUpdateWorkflowStepAssignmentConfigurationDto input,
        Func<Guid> newId) : base(id)
    {
        TenantId = tenantId;
        Update(input, newId);
    }

    public void Update(
        CreateUpdateWorkflowStepAssignmentConfigurationDto input,
        Func<Guid> newId)
    {
        if (!input.AssigneeType.HasValue ||
            !Enum.IsDefined(input.AssigneeType.Value))
        {
            throw new UserFriendlyException(
                "Workflow assignee type is required and must be valid.");
        }

        var userIds = Normalize(input.UserIds);
        var organizationUnitIds = Normalize(input.OrganizationUnitIds);
        var roleId = input.RoleId is null || input.RoleId == Guid.Empty
            ? null
            : input.RoleId;
        ValidateMode(input.AssigneeType.Value, roleId, userIds, organizationUnitIds);

        WorkflowStepTemplateId = Check.NotDefaultOrNull<Guid>(
            input.WorkflowStepTemplateId,
            nameof(input.WorkflowStepTemplateId));
        AssigneeType = input.AssigneeType.Value;
        RoleId = roleId;
        IsPrimary = input.IsPrimary;
        IsActive = input.IsActive;

        Users.Clear();
        foreach (var userId in userIds)
        {
            Users.Add(new WorkflowStepAssignmentUser(
                newId(), TenantId, Id, userId));
        }

        OrganizationUnits.Clear();
        foreach (var organizationUnitId in organizationUnitIds)
        {
            OrganizationUnits.Add(new WorkflowStepAssignmentOrganizationUnit(
                newId(), TenantId, Id, organizationUnitId));
        }
    }

    private static List<Guid> Normalize(IEnumerable<Guid>? ids) =>
        ids?.Where(x => x != Guid.Empty).Distinct().Order().ToList() ?? [];

    private static void ValidateMode(
        WorkflowAssigneeType type,
        Guid? roleId,
        IReadOnlyCollection<Guid> userIds,
        IReadOnlyCollection<Guid> organizationUnitIds)
    {
        switch (type)
        {
            case WorkflowAssigneeType.SpecificUser
                when userIds.Count == 0 || roleId.HasValue || organizationUnitIds.Count > 0:
                throw new UserFriendlyException(
                    "Specific-user assignment requires users and forbids role or organization units.");
            case WorkflowAssigneeType.RoleInSubmitterOrganizationUnit
                when !roleId.HasValue || userIds.Count > 0 || organizationUnitIds.Count > 0:
                throw new UserFriendlyException(
                    "Role-in-submitter-organization assignment requires one role only.");
            case WorkflowAssigneeType.ScopedAssignee
                when userIds.Count == 0 && organizationUnitIds.Count == 0:
                throw new UserFriendlyException(
                    "Scoped assignment requires at least one user or organization unit.");
        }
    }
}

public class WorkflowStepAssignmentUser : Entity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public Guid ConfigurationId { get; private set; }
    public Guid UserId { get; private set; }

    protected WorkflowStepAssignmentUser() { }

    internal WorkflowStepAssignmentUser(
        Guid id,
        Guid? tenantId,
        Guid configurationId,
        Guid userId) : base(id)
    {
        TenantId = tenantId;
        ConfigurationId = configurationId;
        UserId = Check.NotDefaultOrNull<Guid>(userId, nameof(userId));
    }
}

public class WorkflowStepAssignmentOrganizationUnit : Entity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; private set; }
    public Guid ConfigurationId { get; private set; }
    public Guid OrganizationUnitId { get; private set; }

    protected WorkflowStepAssignmentOrganizationUnit() { }

    internal WorkflowStepAssignmentOrganizationUnit(
        Guid id,
        Guid? tenantId,
        Guid configurationId,
        Guid organizationUnitId) : base(id)
    {
        TenantId = tenantId;
        ConfigurationId = configurationId;
        OrganizationUnitId = Check.NotDefaultOrNull<Guid>(
            organizationUnitId,
            nameof(organizationUnitId));
    }
}
