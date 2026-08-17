using Volo.Abp.Application.Services;

namespace hanhchinhso.IdentityService.Internal;

public sealed class IdentityReferenceValidationRequest
{
    public List<Guid> UserIds { get; set; } = [];
    public List<Guid> OrganizationUnitIds { get; set; } = [];
    public List<Guid> RoleIds { get; set; } = [];
}

public sealed class IdentityReferenceValidationResult
{
    public List<Guid> MissingOrInactiveUserIds { get; set; } = [];
    public List<Guid> MissingOrganizationUnitIds { get; set; } = [];
    public List<Guid> MissingRoleIds { get; set; } = [];
}

public interface IIdentityReferenceValidationAppService : IApplicationService
{
    Task<IdentityReferenceValidationResult> ValidateAsync(
        IdentityReferenceValidationRequest input);

    Task<WorkflowAssigneeResolutionResult> ResolveWorkflowAssigneesAsync(
        WorkflowAssigneeResolutionRequest input);

    Task<IdentityUserOrganizationUnitMembershipResult>
        ResolveUserOrganizationUnitMembershipsAsync(
            IdentityUserOrganizationUnitMembershipRequest input);
}

public sealed class IdentityUserOrganizationUnitMembershipRequest
{
    public Guid UserId { get; set; }
    public List<Guid> OrganizationUnitIds { get; set; } = [];
    public bool IncludeAllUserMemberships { get; set; }
}

public sealed class IdentityUserOrganizationUnitMembershipResult
{
    public List<Guid> OrganizationUnitIds { get; set; } = [];
}

public sealed class WorkflowAssigneeResolutionRequest
{
    public Guid SubmitterUserId { get; set; }
    public List<WorkflowAssigneeConfigurationRequest> Configurations { get; set; } = [];
}

public sealed class WorkflowAssigneeConfigurationRequest
{
    public Guid ConfigurationId { get; set; }
    public int AssigneeType { get; set; }
    public Guid? RoleId { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime CreationTime { get; set; }
    public List<Guid> UserIds { get; set; } = [];
    public List<Guid> OrganizationUnitIds { get; set; } = [];
}

public sealed class WorkflowAssigneeResolutionResult
{
    public Guid? PrimarySubmitterOrganizationUnitId { get; set; }
    public List<Guid> SubmitterOrganizationUnitIds { get; set; } = [];
    public List<WorkflowResolvedCandidate> Candidates { get; set; } = [];
}

public sealed class WorkflowResolvedCandidate
{
    public Guid ConfigurationId { get; set; }
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public Guid? ProvenanceOrganizationUnitId { get; set; }
    public Guid? ProvenanceRoleId { get; set; }
    public int OrganizationUnitDepth { get; set; }
    public bool IsPrimaryConfiguration { get; set; }
    public DateTime ConfigurationCreationTime { get; set; }
}
