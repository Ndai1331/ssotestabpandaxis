using HCS.DocumentService.Signing;
using HCS.DocumentService.Workflows;
using Microsoft.EntityFrameworkCore;

namespace HCS.DocumentService.Tests;

public sealed class SoftDeletedCatalogVisibilityTests
{
    private static readonly DateTime Now = new(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Inactive_workflows_and_signing_records_are_excluded_but_retained()
    {
        await using var db = new DocumentServiceDbContext(new DbContextOptionsBuilder<DocumentServiceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        var activeKind = new WorkflowKind(Guid.NewGuid(), "active-kind", "Active kind", null, true, Now);
        var inactiveKind = new WorkflowKind(Guid.NewGuid(), "inactive-kind", "Inactive kind", null, false, Now);
        var activeDefinition = new WorkflowDefinition(Guid.NewGuid(), "active-workflow", "Active workflow",
            [new WorkflowStepInput("sign", "Sign", 1, "Documents.Approve", "SIGN")], Now, activeKind.Id);
        var inactiveDefinition = new WorkflowDefinition(Guid.NewGuid(), "inactive-workflow", "Inactive workflow",
            [new WorkflowStepInput("sign", "Sign", 1, "Documents.Approve", "SIGN")], Now, inactiveKind.Id, isActive: false);
        var activeTemplate = new WorkflowTemplate(Guid.NewGuid(), "active-template", "Active template",
            activeDefinition.Id, 1, "{}", Now);
        var inactiveTemplate = new WorkflowTemplate(Guid.NewGuid(), "inactive-template", "Inactive template",
            inactiveDefinition.Id, 1, "{}", Now);
        inactiveTemplate.SetActive(false);
        var activeCredential = new SigningCredential(Guid.NewGuid(), null, SigningKind.Electronic,
            "https://sign.local", "", Now);
        var deletedCredential = new SigningCredential(Guid.NewGuid(), null, SigningKind.Electronic,
            "https://deleted-sign.local", "", Now);
        deletedCredential.SetCatalogMetadata(null, null, null, null, null, null, isDeleted: true, legacyLayoutImagePath: null);
        var activeSignature = new UserSignature(Guid.NewGuid(), Guid.NewGuid(), "active.png", "image/png",
            "signatures/active", 10, Now);
        var inactiveSignature = new UserSignature(Guid.NewGuid(), activeSignature.UserId, "inactive.png", "image/png",
            "signatures/inactive", 10, Now, isActive: false);

        db.AddRange(activeKind, inactiveKind, activeDefinition, inactiveDefinition,
            activeTemplate, inactiveTemplate, activeCredential, deletedCredential,
            activeSignature, inactiveSignature);
        await db.SaveChangesAsync();

        var visibleDefinitions = await db.WorkflowDefinitions
            .WhereActiveWorkflowDefinitions().Include(x => x.Steps).ToListAsync();
        var visibleTemplates = await db.WorkflowTemplates.WhereActiveWorkflowTemplates().ToListAsync();
        var visibleCredentials = await db.SigningCredentials.WhereVisibleSigningCredentials().ToListAsync();
        var visibleSignatures = await db.UserSignatures.WhereActiveUserSignatures().ToListAsync();

        Assert.Equal(activeDefinition.Id, Assert.Single(visibleDefinitions).Id);
        Assert.Single(visibleDefinitions[0].Steps);
        Assert.Equal(activeTemplate.Id, Assert.Single(visibleTemplates).Id);
        Assert.Equal(activeCredential.Id, Assert.Single(visibleCredentials).Id);
        Assert.Equal(activeSignature.Id, Assert.Single(visibleSignatures).Id);
        Assert.Equal(2, await db.WorkflowDefinitions.CountAsync());
        Assert.Equal(2, await db.WorkflowSteps.CountAsync());
        Assert.Equal(2, await db.WorkflowTemplates.CountAsync());
        Assert.Equal(2, await db.SigningCredentials.CountAsync());
        Assert.Equal(2, await db.UserSignatures.CountAsync());
    }
}
