using hanhchinhso.DocumentService.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.Text.Json;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Validation;
using Xunit;

namespace hanhchinhso.DocumentService.Tests.Workflow;

public class WorkflowCatalogTests : DocumentServiceIntegrationTestBase
{
    [Fact]
    public async Task Should_Create_Filter_And_Map_The_Definition_Chain()
    {
        var definitionService = ServiceProvider.GetRequiredService<IWorkflowDefinitionAppService>();
        var workflowService = ServiceProvider.GetRequiredService<IWorkflowAppService>();
        var templateService = ServiceProvider.GetRequiredService<IWorkflowTemplateAppService>();
        var stepService = ServiceProvider.GetRequiredService<IWorkflowStepTemplateAppService>();

        var definition = await definitionService.CreateAsync(new()
        {
            Code = " incoming ",
            Name = "Incoming documents",
            Description = "HCS parity",
            IsActive = true
        });
        definition.Code.ShouldBe("INCOMING");

        var workflow = await workflowService.CreateAsync(new()
        {
            Code = "standard",
            Name = "Standard approval",
            WorkflowDefinitionId = definition.Id,
            IsActive = true
        });
        var template = await templateService.CreateAsync(new()
        {
            Code = "default",
            Name = "Default template",
            WorkflowId = workflow.Id,
            ContentSchema = """{"type":"object"}""",
            OutputFormat = WorkflowOutputFormat.Pdf,
            SignMode = WorkflowSignMode.Sequential,
            IsActive = true
        });
        var step = await stepService.CreateAsync(new()
        {
            Order = 1,
            Name = "Approve",
            Type = WorkflowStepType.Sign,
            SlaDays = 2,
            AllowReturn = true,
            WorkflowTemplateId = template.Id,
            IsActive = true
        });

        var filtered = await stepService.GetListAsync(new()
        {
            ParentId = template.Id,
            FilterText = "Approve"
        });
        filtered.TotalCount.ShouldBe(1);
        filtered.Items.Single().Id.ShouldBe(step.Id);
        filtered.Items.Single().Type.ShouldBe(WorkflowStepType.Sign);
        await Should.ThrowAsync<Exception>(() =>
            stepService.CreateAsync(new()
            {
                Order = 1,
                Name = "Duplicate order",
                Type = WorkflowStepType.View,
                WorkflowTemplateId = template.Id,
                IsActive = true
            }));
    }

    [Fact]
    public async Task Should_Reject_Invalid_Template_And_Step_Invariants()
    {
        var (_, workflow) = await CreateDefinitionAndWorkflowAsync();
        var templateService = ServiceProvider.GetRequiredService<IWorkflowTemplateAppService>();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            templateService.CreateAsync(new()
            {
                Code = "INVALID-OUTPUT",
                Name = "Invalid output",
                WorkflowId = workflow.Id,
                OutputFormat = (WorkflowOutputFormat)999,
                IsActive = true
            }));
        await Should.ThrowAsync<UserFriendlyException>(() =>
            templateService.CreateAsync(new()
            {
                Code = "INVALID-SIGN",
                Name = "Invalid sign",
                WorkflowId = workflow.Id,
                SignMode = (WorkflowSignMode)999,
                IsActive = true
            }));
        Should.Throw<JsonException>(() =>
            JsonSerializer.Deserialize<WorkflowOutputFormat>("999"));
        Should.Throw<JsonException>(() =>
            JsonSerializer.Deserialize<WorkflowSignMode>("\"Unknown\""));
        JsonSerializer.Serialize(WorkflowStepType.Sign).ShouldBe("\"Sign\"");

        await Should.ThrowAsync<UserFriendlyException>(() =>
            templateService.CreateAsync(new()
            {
                Code = "INVALID-JSON",
                Name = "Invalid JSON",
                WorkflowId = workflow.Id,
                ContentSchema = "{not-json}",
                IsActive = true
            }));

        var template = await templateService.CreateAsync(new()
        {
            Code = "VALID",
            Name = "Valid",
            WorkflowId = workflow.Id,
            IsActive = true
        });
        var stepService = ServiceProvider.GetRequiredService<IWorkflowStepTemplateAppService>();
        await Should.ThrowAsync<AbpValidationException>(() =>
            stepService.CreateAsync(new()
            {
                Order = 1,
                Name = "Missing type",
                WorkflowTemplateId = template.Id
            }));
        await Should.ThrowAsync<AbpValidationException>(() =>
            stepService.CreateAsync(new()
            {
                Order = 1,
                Name = "Negative SLA",
                Type = WorkflowStepType.Process,
                SlaDays = -1,
                WorkflowTemplateId = template.Id
            }));
    }

    [Fact]
    public async Task Should_Guard_Soft_Delete_And_Reject_Stale_Update()
    {
        var (definition, workflow) = await CreateDefinitionAndWorkflowAsync();
        var definitionService = ServiceProvider.GetRequiredService<IWorkflowDefinitionAppService>();

        await Should.ThrowAsync<AbpDbConcurrencyException>(() =>
            definitionService.DeleteAsync(definition.Id, Guid.NewGuid().ToString("N")));
        await Should.ThrowAsync<UserFriendlyException>(() =>
            definitionService.DeleteAsync(definition.Id, definition.ConcurrencyStamp));

        var updated = await definitionService.UpdateAsync(definition.Id, new()
        {
            Code = definition.Code,
            Name = "Updated definition",
            Description = definition.Description,
            IsActive = true,
            ConcurrencyStamp = definition.ConcurrencyStamp
        });
        updated.Name.ShouldBe("Updated definition");

        await Should.ThrowAsync<AbpDbConcurrencyException>(() =>
            definitionService.UpdateAsync(definition.Id, new()
            {
                Code = definition.Code,
                Name = "Stale update",
                IsActive = true,
                ConcurrencyStamp = definition.ConcurrencyStamp
            }));

        workflow.WorkflowDefinitionId.ShouldBe(definition.Id);
    }

    [Fact]
    public async Task Should_Isolate_Normalized_Codes_Per_Tenant()
    {
        var currentTenant = ServiceProvider.GetRequiredService<ICurrentTenant>();
        var service = ServiceProvider.GetRequiredService<IWorkflowDefinitionAppService>();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await service.CreateAsync(new()
        {
            Code = "host-code",
            Name = "Host",
            IsActive = true
        });
        await Should.ThrowAsync<Exception>(() =>
            service.CreateAsync(new()
            {
                Code = " HOST-CODE ",
                Name = "Host duplicate",
                IsActive = true
            }));

        using (currentTenant.Change(tenantA))
        {
            await service.CreateAsync(new()
            {
                Code = "same",
                Name = "Tenant A",
                IsActive = true
            });
            await Should.ThrowAsync<Exception>(() =>
                service.CreateAsync(new()
                {
                    Code = "SAME",
                    Name = "Tenant A duplicate",
                    IsActive = true
                }));
        }

        using (currentTenant.Change(tenantB))
        {
            var created = await service.CreateAsync(new()
            {
                Code = "same",
                Name = "Tenant B",
                IsActive = true
            });
            created.Code.ShouldBe("SAME");
        }
    }

    [Fact]
    public async Task Should_Reject_Cross_Tenant_Parent_Ids()
    {
        var currentTenant = ServiceProvider.GetRequiredService<ICurrentTenant>();
        var definitionService =
            ServiceProvider.GetRequiredService<IWorkflowDefinitionAppService>();
        var workflowService = ServiceProvider.GetRequiredService<IWorkflowAppService>();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        WorkflowDefinitionDto definition;

        using (currentTenant.Change(tenantA))
        {
            definition = await definitionService.CreateAsync(new()
            {
                Code = "TENANT-A",
                Name = "Tenant A",
                IsActive = true
            });
        }

        using (currentTenant.Change(tenantB))
        {
            await Should.ThrowAsync<EntityNotFoundException>(() =>
                workflowService.CreateAsync(new()
                {
                    Code = "CROSS-TENANT",
                    Name = "Invalid parent",
                    WorkflowDefinitionId = definition.Id,
                    IsActive = true
                }));
        }
    }

    [Fact]
    public async Task Should_Serialize_Parent_Delete_With_Child_Create()
    {
        var definitionService =
            ServiceProvider.GetRequiredService<IWorkflowDefinitionAppService>();
        var workflowService = ServiceProvider.GetRequiredService<IWorkflowAppService>();
        var definition = await definitionService.CreateAsync(new()
        {
            Code = $"RACE-{Guid.NewGuid():N}",
            Name = "Race definition",
            IsActive = true
        });

        var deleteTask = CaptureExceptionAsync(() =>
            definitionService.DeleteAsync(definition.Id, definition.ConcurrencyStamp));
        var createTask = CaptureExceptionAsync(async () =>
        {
            _ = await workflowService.CreateAsync(new()
            {
                Code = $"RACE-WF-{Guid.NewGuid():N}",
                Name = "Race workflow",
                WorkflowDefinitionId = definition.Id,
                IsActive = true
            });
        });
        var outcomes = await Task.WhenAll(deleteTask, createTask);

        outcomes.Count(x => x is null).ShouldBe(1);
        var failure = outcomes.Single(x => x is not null);
        (failure is UserFriendlyException or EntityNotFoundException).ShouldBeTrue(
            "Create-first must trigger the child guard; delete-first must make parent validation fail.");
    }

    [Fact]
    public async Task Should_Serialize_Parent_Delete_With_Child_Reparent()
    {
        var definitionService =
            ServiceProvider.GetRequiredService<IWorkflowDefinitionAppService>();
        var workflowService = ServiceProvider.GetRequiredService<IWorkflowAppService>();
        var (source, workflow) = await CreateDefinitionAndWorkflowAsync();
        var target = await definitionService.CreateAsync(new()
        {
            Code = $"TARGET-{Guid.NewGuid():N}",
            Name = "Target definition",
            IsActive = true
        });

        var deleteTask = CaptureExceptionAsync(() =>
            definitionService.DeleteAsync(target.Id, target.ConcurrencyStamp));
        var reparentTask = CaptureExceptionAsync(async () =>
        {
            _ = await workflowService.UpdateAsync(workflow.Id, new()
            {
                Code = workflow.Code,
                Name = workflow.Name,
                Description = workflow.Description,
                WorkflowDefinitionId = target.Id,
                IsActive = workflow.IsActive,
                ConcurrencyStamp = workflow.ConcurrencyStamp
            });
        });
        var outcomes = await Task.WhenAll(deleteTask, reparentTask);

        outcomes.Count(x => x is null).ShouldBe(1);
        var failure = outcomes.Single(x => x is not null);
        (failure is UserFriendlyException or EntityNotFoundException).ShouldBeTrue(
            "Reparent-first must trigger the child guard; delete-first must make target validation fail.");
        source.Id.ShouldNotBe(target.Id);
    }

    private static async Task<Exception?> CaptureExceptionAsync(Func<Task> action)
    {
        try
        {
            await action();
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }

    private async Task<(WorkflowDefinitionDto Definition, WorkflowDto Workflow)>
        CreateDefinitionAndWorkflowAsync()
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
        return (definition, workflow);
    }
}
