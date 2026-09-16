using System.Security.Claims;
using HCS.WorkManagementService.Contracts;
using HCS.WorkManagementService.Domain;
using HCS.WorkManagementService.Controllers;
using Microsoft.AspNetCore.Authorization;

namespace HCS.WorkManagementService.Tests;

public sealed class SecurityContractTests
{
    [Fact]
    public void Survey_submission_ignores_a_caller_supplied_user_id()
    {
        var authenticated = Guid.NewGuid();
        Assert.Equal(authenticated, HCS.WorkManagementService.Application.SurveySubmissionIdentity.Resolve(
            authenticated, Guid.NewGuid()));
    }

    [Fact]
    public void Public_work_asset_contract_never_exposes_internal_blob_name()
    {
        Assert.Null(typeof(SurveyFileReferenceDto).GetProperty("BlobName"));
    }

    [Fact]
    public void Owned_records_reject_an_empty_caller_identity()
    {
        Assert.Throws<Volo.Abp.BusinessException>(() => new Project(Guid.NewGuid(), "P", "Project",
            DateTime.UtcNow, DateTime.UtcNow, "Active", null, Guid.Empty));
        Assert.Throws<Volo.Abp.BusinessException>(() => new SurveySession(Guid.NewGuid(), "S", "Survey",
            DateTime.UtcNow, DateTime.UtcNow, null, Guid.Empty));
    }

    [Fact]
    public void Survey_submission_always_requires_the_active_window()
    {
        var owner = Guid.NewGuid(); var audience = Guid.NewGuid(); var now = DateTime.UtcNow;
        Assert.False(HCS.WorkManagementService.Application.SurveyAccessRules.CanSubmit(false, owner, owner,
            "Draft", now.AddDays(1), now.AddDays(2), now));
        Assert.True(HCS.WorkManagementService.Application.SurveyAccessRules.CanSubmit(false, owner, audience,
            "Active", now.AddMinutes(-1), now.AddMinutes(1), now));
        Assert.False(HCS.WorkManagementService.Application.SurveyAccessRules.CanSubmit(false, owner, audience,
            "Active", now.AddMinutes(1), now.AddMinutes(2), now));
    }

    [Theory]
    [InlineData(nameof(SurveysController.CreateCriteria))]
    [InlineData(nameof(SurveysController.CreateLocation))]
    [InlineData(nameof(SurveysController.CreateSession))]
    [InlineData(nameof(SurveysController.UpdateCriteria))]
    [InlineData(nameof(SurveysController.DeleteCriteria))]
    [InlineData(nameof(SurveysController.UpdateLocation))]
    [InlineData(nameof(SurveysController.DeleteLocation))]
    [InlineData(nameof(SurveysController.UpdateSession))]
    [InlineData(nameof(SurveysController.ChangeStatus))]
    [InlineData(nameof(SurveysController.DeleteSession))]
    public void Global_survey_management_endpoints_require_the_management_policy(string action)
    {
        var method = typeof(SurveysController).GetMethod(action)!;
        Assert.Contains(method.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>(),
            attribute => attribute.Policy == WorkPermissions.SurveyManagement);
    }

    [Theory]
    [InlineData(nameof(SurveysController.GetResults))]
    [InlineData(nameof(SurveysController.Submit))]
    [InlineData(nameof(SurveysController.GetFiles))]
    public void Survey_participant_endpoints_inherit_the_survey_policy(string action)
    {
        Assert.NotNull(typeof(SurveysController).GetMethod(action));
        Assert.Contains(typeof(SurveysController).GetCustomAttributes(typeof(AuthorizeAttribute), true)
                .Cast<AuthorizeAttribute>(),
            attribute => attribute.Policy == WorkPermissions.Surveys);
    }

    [Theory]
    [InlineData(nameof(SurveysController.GetPublicLocation))]
    [InlineData(nameof(SurveysController.GetPublicCriteria))]
    [InlineData(nameof(SurveysController.CreatePublicSession))]
    [InlineData(nameof(SurveysController.SubmitPublicResults))]
    [InlineData(nameof(SurveysController.UploadPublic))]
    public void Public_survey_endpoints_are_explicitly_anonymous(string action)
    {
        var method = typeof(SurveysController).GetMethod(action)!;
        Assert.NotNull(method.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).SingleOrDefault());
    }

    [Theory]
    [InlineData(nameof(EventsController.Public))]
    [InlineData(nameof(EventsController.PublicAttachment))]
    [InlineData(nameof(EventsController.PublicCheckIn))]
    public void Public_event_endpoints_are_explicitly_anonymous(string action)
    {
        var method = typeof(EventsController).GetMethod(action)!;
        Assert.NotNull(method.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).SingleOrDefault());
    }

    [Theory]
    [InlineData(typeof(CalendarController), nameof(CalendarController.GetList), WorkPermissions.CalendarRead, null)]
    [InlineData(typeof(CalendarController), nameof(CalendarController.Get), WorkPermissions.CalendarRead, null)]
    [InlineData(typeof(CalendarController), nameof(CalendarController.Create), WorkPermissions.CalendarRead, WorkPermissions.Calendar)]
    [InlineData(typeof(ProjectsController), nameof(ProjectsController.GetList), WorkPermissions.ProjectsRead, null)]
    [InlineData(typeof(ProjectsController), nameof(ProjectsController.Get), WorkPermissions.ProjectsRead, null)]
    [InlineData(typeof(ProjectsController), nameof(ProjectsController.Create), WorkPermissions.ProjectsRead, WorkPermissions.Projects)]
    [InlineData(typeof(ProjectTasksController), nameof(ProjectTasksController.GetList), WorkPermissions.TasksRead, null)]
    [InlineData(typeof(ProjectTasksController), nameof(ProjectTasksController.Get), WorkPermissions.TasksRead, null)]
    [InlineData(typeof(ProjectTasksController), nameof(ProjectTasksController.Create), WorkPermissions.TasksRead, WorkPermissions.Tasks)]
    public void Workspace_read_apis_accept_dashboard_and_keep_writes_on_the_feature_permission(
        Type controller, string action, string classPolicy, string? methodPolicy)
    {
        Assert.Contains(controller.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>(),
            attribute => attribute.Policy == classPolicy);
        var method = controller.GetMethod(action)!;
        var methodPolicies = method.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>()
            .Select(attribute => attribute.Policy).ToArray();
        if (methodPolicy is null)
            Assert.Empty(methodPolicies);
        else
            Assert.Contains(methodPolicy, methodPolicies);
    }

    [Fact]
    public void Workspace_dashboard_permission_can_read_calendar_projects_and_tasks()
    {
        var workspace = Principal(new Claim("permission", WorkPermissions.Dashboard));
        Assert.True(WorkPermissionAccess.CanReadWorkspaceResource(workspace, WorkPermissions.Calendar));
        Assert.True(WorkPermissionAccess.CanReadWorkspaceResource(workspace, WorkPermissions.Projects));
        Assert.True(WorkPermissionAccess.CanReadWorkspaceResource(workspace, WorkPermissions.Tasks));
        Assert.False(WorkPermissionAccess.HasPermission(workspace, WorkPermissions.Calendar));
        Assert.False(WorkPermissionAccess.HasPermission(workspace, WorkPermissions.Projects));
        Assert.False(WorkPermissionAccess.HasPermission(workspace, WorkPermissions.Tasks));
    }

    [Fact]
    public void Bearer_apis_do_not_auto_validate_antiforgery_cookies()
    {
        var options = new Volo.Abp.AspNetCore.Mvc.AntiForgery.AbpAntiForgeryOptions { AutoValidate = true };
        BearerApiAntiforgery.DisableCookieValidation(options);
        Assert.False(options.AutoValidate);
        Assert.True(typeof(Microsoft.AspNetCore.Mvc.ControllerBase).IsAssignableFrom(typeof(ProjectsController)));
    }

    [Fact]
    public void Employee_rating_api_does_not_expose_voter_identity()
    {
        Assert.Null(typeof(EmployeeRatingDto).GetProperty("VoterUserId"));
        Assert.Null(typeof(EmployeeRatingSummaryDto).GetProperty("VoterUserId"));
        Assert.Null(typeof(EmployeeRatingDetailDto).GetProperty("VoterUserId"));
    }

    [Fact]
    public void Employee_rating_endpoints_require_the_expected_permissions()
    {
        var controller = typeof(EmployeeRatingsController);
        Assert.Contains(controller.GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .Cast<AuthorizeAttribute>(), x => x.Policy == WorkPermissions.EmployeeRatingsRead);
        Assert.Contains(controller.GetMethod(nameof(EmployeeRatingsController.Submit))!
            .GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>(),
            x => x.Policy == WorkPermissions.EmployeeRatings);
        Assert.Contains(controller.GetMethod(nameof(EmployeeRatingsController.Detail))!
            .GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>(),
            x => x.Policy == WorkPermissions.EmployeeRatingsManagement);
        Assert.Contains(controller.GetMethod(nameof(EmployeeRatingsController.Dashboard))!
            .GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>(),
            x => x.Policy == WorkPermissions.EmployeeRatingsDashboard);
    }

    private static ClaimsPrincipal Principal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, authenticationType: "test"));
}
