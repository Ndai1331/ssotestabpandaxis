using System.Text.Json;
using hanhchinhso.DocumentService.Workflows;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace hanhchinhso.DocumentService.Tests.Workflow;

public class WorkflowAssignmentConfigurationTests : DocumentServiceIntegrationTestBase
{
    [Fact]
    public async Task Should_Create_Normalize_Filter_And_Update_Specific_Users()
    {
        var step = await CreateStepAsync();
        var userId = Guid.NewGuid();
        ConfigureActiveUser(userId);
        var service = ServiceProvider
            .GetRequiredService<IWorkflowStepAssignmentConfigurationAppService>();

        var created = await service.CreateAsync(new()
        {
            WorkflowStepTemplateId = step.Id,
            AssigneeType = WorkflowAssigneeType.SpecificUser,
            UserIds = [Guid.Empty, userId, userId],
            IsPrimary = true,
            IsActive = true
        });

        created.UserIds.ShouldBe([userId]);
        created.OrganizationUnitIds.ShouldBeEmpty();
        created.RoleId.ShouldBeNull();
        var list = await service.GetListAsync(new()
        {
            WorkflowStepTemplateId = step.Id,
            AssigneeType = WorkflowAssigneeType.SpecificUser
        });
        list.TotalCount.ShouldBe(1);

        var nextUserId = Guid.NewGuid();
        ConfigureActiveUser(nextUserId);
        var updated = await service.UpdateAsync(created.Id, new()
        {
            WorkflowStepTemplateId = step.Id,
            AssigneeType = WorkflowAssigneeType.SpecificUser,
            UserIds = [nextUserId],
            IsPrimary = true,
            IsActive = true,
            ConcurrencyStamp = created.ConcurrencyStamp
        });
        updated.UserIds.ShouldBe([nextUserId]);

        await Should.ThrowAsync<AbpDbConcurrencyException>(() =>
            service.UpdateAsync(created.Id, new()
            {
                WorkflowStepTemplateId = step.Id,
                AssigneeType = WorkflowAssigneeType.SpecificUser,
                UserIds = [nextUserId],
                IsActive = true,
                ConcurrencyStamp = created.ConcurrencyStamp
            }));
    }

    [Fact]
    public async Task Should_Enforce_Mode_Invariants_And_Strict_Enum_Wire()
    {
        var step = await CreateStepAsync();
        var service = ServiceProvider
            .GetRequiredService<IWorkflowStepAssignmentConfigurationAppService>();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateAsync(new()
            {
                WorkflowStepTemplateId = step.Id,
                AssigneeType = WorkflowAssigneeType.SpecificUser,
                RoleId = Guid.NewGuid(),
                UserIds = [Guid.NewGuid()]
            }));
        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateAsync(new()
            {
                WorkflowStepTemplateId = step.Id,
                AssigneeType = WorkflowAssigneeType.RoleInSubmitterOrganizationUnit
            }));
        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateAsync(new()
            {
                WorkflowStepTemplateId = step.Id,
                AssigneeType = WorkflowAssigneeType.ScopedAssignee
            }));

        var userId = Guid.NewGuid();
        var normalized = await service.CreateAsync(new()
        {
            WorkflowStepTemplateId = step.Id,
            AssigneeType = WorkflowAssigneeType.ScopedAssignee,
            RoleId = Guid.Empty,
            UserIds = [userId]
        });
        normalized.RoleId.ShouldBeNull();

        JsonSerializer.Serialize(WorkflowAssigneeType.ScopedAssignee)
            .ShouldBe("\"ScopedAssignee\"");
        Should.Throw<JsonException>(() =>
            JsonSerializer.Deserialize<WorkflowAssigneeType>("2"));
        Should.Throw<JsonException>(() =>
            JsonSerializer.Deserialize<DocumentWorkflowStatus>("\"UNKNOWN\""));
    }

    [Fact]
    public async Task Should_Validate_ABP_Organization_Users_And_Role()
    {
        var step = await CreateStepAsync();
        var userId = Guid.NewGuid();
        var organizationUnitId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        var service = ServiceProvider
            .GetRequiredService<IWorkflowStepAssignmentConfigurationAppService>();
        var created = await service.CreateAsync(new()
        {
            WorkflowStepTemplateId = step.Id,
            AssigneeType = WorkflowAssigneeType.ScopedAssignee,
            RoleId = roleId,
            UserIds = [userId],
            OrganizationUnitIds = [organizationUnitId],
            IsActive = true
        });

        created.RoleId.ShouldBe(roleId);
        created.UserIds.ShouldBe([userId]);
        created.OrganizationUnitIds.ShouldBe([organizationUnitId]);
        await ServiceProvider
            .GetRequiredService<IWorkflowIdentityReferenceValidator>()
            .Received(1)
            .ValidateAsync(
                Arg.Is<IEnumerable<Guid>>(x => x.SequenceEqual(new[] { userId })),
                Arg.Is<IEnumerable<Guid>>(x => x.SequenceEqual(new[] { organizationUnitId })),
                roleId,
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Fail_Closed_When_Identity_Validation_Is_Unavailable()
    {
        var step = await CreateStepAsync();
        var validator = ServiceProvider
            .GetRequiredService<IWorkflowIdentityReferenceValidator>();
        validator.ValidateAsync(
                Arg.Any<IEnumerable<Guid>>(),
                Arg.Any<IEnumerable<Guid>>(),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new UserFriendlyException(
                "Identity reference validation is unavailable."));

        var service = ServiceProvider
            .GetRequiredService<IWorkflowStepAssignmentConfigurationAppService>();
        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateAsync(new()
            {
                WorkflowStepTemplateId = step.Id,
                AssigneeType = WorkflowAssigneeType.SpecificUser,
                UserIds = [Guid.NewGuid()],
                IsActive = true
            }));

        var list = await service.GetListAsync(new()
        {
            WorkflowStepTemplateId = step.Id
        });
        list.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Guard_Step_Delete_And_Active_Primary_Uniqueness()
    {
        var step = await CreateStepAsync();
        var firstUser = Guid.NewGuid();
        var secondUser = Guid.NewGuid();
        ConfigureActiveUser(firstUser);
        ConfigureActiveUser(secondUser);
        var service = ServiceProvider
            .GetRequiredService<IWorkflowStepAssignmentConfigurationAppService>();
        await service.CreateAsync(new()
        {
            WorkflowStepTemplateId = step.Id,
            AssigneeType = WorkflowAssigneeType.SpecificUser,
            UserIds = [firstUser],
            IsPrimary = true,
            IsActive = true
        });
        await Should.ThrowAsync<Exception>(() =>
            service.CreateAsync(new()
            {
                WorkflowStepTemplateId = step.Id,
                AssigneeType = WorkflowAssigneeType.SpecificUser,
                UserIds = [secondUser],
                IsPrimary = true,
                IsActive = true
            }));

        await Should.ThrowAsync<UserFriendlyException>(() =>
            ServiceProvider.GetRequiredService<IWorkflowStepTemplateAppService>()
                .DeleteAsync(step.Id, step.ConcurrencyStamp));
    }

    [Fact]
    public async Task Should_Reject_Cross_Tenant_Step()
    {
        var currentTenant = ServiceProvider.GetRequiredService<ICurrentTenant>();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        WorkflowStepTemplateDto step;
        using (currentTenant.Change(tenantA))
        {
            step = await CreateStepAsync();
        }

        var userId = Guid.NewGuid();
        ConfigureActiveUser(userId);
        using (currentTenant.Change(tenantB))
        {
            await Should.ThrowAsync<Volo.Abp.Domain.Entities.EntityNotFoundException>(() =>
                ServiceProvider
                    .GetRequiredService<IWorkflowStepAssignmentConfigurationAppService>()
                    .CreateAsync(new()
                    {
                        WorkflowStepTemplateId = step.Id,
                        AssigneeType = WorkflowAssigneeType.SpecificUser,
                        UserIds = [userId],
                        IsActive = true
                    }));
        }
    }

    private void ConfigureActiveUser(Guid userId)
    {
        _ = userId;
    }

    private async Task<WorkflowStepTemplateDto> CreateStepAsync()
    {
        var definition = await ServiceProvider
            .GetRequiredService<IWorkflowDefinitionAppService>()
            .CreateAsync(new()
            {
                Code = $"DEF-{Guid.NewGuid():N}",
                Name = "Definition",
                IsActive = true
            });
        var workflow = await ServiceProvider
            .GetRequiredService<IWorkflowAppService>()
            .CreateAsync(new()
            {
                Code = $"WF-{Guid.NewGuid():N}",
                Name = "Workflow",
                WorkflowDefinitionId = definition.Id,
                IsActive = true
            });
        var template = await ServiceProvider
            .GetRequiredService<IWorkflowTemplateAppService>()
            .CreateAsync(new()
            {
                Code = $"TPL-{Guid.NewGuid():N}",
                Name = "Template",
                WorkflowId = workflow.Id,
                IsActive = true
            });
        return await ServiceProvider
            .GetRequiredService<IWorkflowStepTemplateAppService>()
            .CreateAsync(new()
            {
                Order = 1,
                Name = "Process",
                Type = WorkflowStepType.Process,
                WorkflowTemplateId = template.Id,
                IsActive = true
            });
    }
}
