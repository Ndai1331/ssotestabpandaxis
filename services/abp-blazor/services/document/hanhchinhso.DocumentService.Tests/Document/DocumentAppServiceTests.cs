using hanhchinhso.DocumentService.Documents;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace hanhchinhso.DocumentService.Tests.Document;

public class DocumentAppServiceTests : DocumentServiceIntegrationTestBase
{
    [Fact]
    public async Task Should_CRUD_And_Filter_Document_Aggregates()
    {
        DocumentDto created = null!;
        await WithUnitOfWorkAsync(async () =>
        {
            var service = ServiceProvider.GetRequiredService<IDocumentAppService>();
            created = await service.CreateAsync(new CreateUpdateDocumentDto
            {
                Number = "CV-001",
                Title = "Công văn thử nghiệm",
                StorageNumber = "HS-001",
                IncomingDate = DateTime.UtcNow,
                SourceType = DocumentSourceType.Archive
            });

            created.Title.ShouldBe("Công văn thử nghiệm");
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var service = ServiceProvider.GetRequiredService<IDocumentAppService>();
            var filtered = await service.GetListAsync(new DocumentListInput
            {
                FilterText = "thử nghiệm"
            });
            filtered.TotalCount.ShouldBe(1);

            var updated = await service.UpdateAsync(created.Id, new CreateUpdateDocumentDto
            {
                Number = created.Number,
                Title = "Công văn đã cập nhật",
                StorageNumber = created.StorageNumber,
                IncomingDate = created.IncomingDate,
                SourceType = created.SourceType,
                ConcurrencyStamp = created.ConcurrencyStamp
            });
            updated.Title.ShouldBe("Công văn đã cập nhật");
            created = updated;
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var service = ServiceProvider.GetRequiredService<IDocumentAppService>();
            await service.DeleteAsync(created.Id);
        });

        await WithUnitOfWorkAsync(async () =>
        {
            var service = ServiceProvider.GetRequiredService<IDocumentAppService>();
            (await service.GetListAsync(new DocumentListInput())).TotalCount.ShouldBe(0);
        });
    }

    [Fact]
    public async Task Should_Isolate_Documents_By_Tenant()
    {
        var currentTenant = ServiceProvider.GetRequiredService<ICurrentTenant>();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        using (currentTenant.Change(tenantA))
        {
            await WithUnitOfWorkAsync(async () =>
                await ServiceProvider.GetRequiredService<IDocumentAppService>().CreateAsync(
                    NewDocument("CV-001", "Văn bản tenant A")));
        }

        using (currentTenant.Change(tenantB))
        {
            await WithUnitOfWorkAsync(async () =>
            {
                var service = ServiceProvider.GetRequiredService<IDocumentAppService>();
                await service.CreateAsync(NewDocument("CV-001", "Văn bản tenant B"));
                var result = await service.GetListAsync(new DocumentListInput());
                result.TotalCount.ShouldBe(1);
                result.Items.Single().Title.ShouldBe("Văn bản tenant B");
            });
        }
    }

    [Fact]
    public async Task Should_Reject_Invalid_Document_Invariants()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var service = ServiceProvider.GetRequiredService<IDocumentAppService>();
            var invalidSource = NewDocument("BAD-SOURCE", "Invalid source");
            invalidSource.SourceType = (DocumentSourceType)999;
            await Should.ThrowAsync<Volo.Abp.UserFriendlyException>(
                () => service.CreateAsync(invalidSource));

            var missingDate = NewDocument("BAD-DATE", "Missing date");
            missingDate.IncomingDate = default;
            await Should.ThrowAsync<Volo.Abp.UserFriendlyException>(
                () => service.CreateAsync(missingDate));
        });
    }

    [Fact]
    public async Task Should_Reject_Self_Parent_On_Update()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var service = ServiceProvider.GetRequiredService<IDocumentAppService>();
            var created = await service.CreateAsync(NewDocument("PARENT", "Parent invariant"));
            var update = NewDocument(created.Number!, created.Title);
            update.ParentDocumentId = created.Id;
            update.ConcurrencyStamp = created.ConcurrencyStamp;

            await Should.ThrowAsync<Volo.Abp.UserFriendlyException>(
                () => service.UpdateAsync(created.Id, update));
        });
    }

    [Fact]
    public async Task Should_Reject_Indirect_Parent_Cycle_On_Update()
    {
        await WithUnitOfWorkAsync(async () =>
        {
            var service = ServiceProvider.GetRequiredService<IDocumentAppService>();
            var parent = await service.CreateAsync(NewDocument("PARENT", "Parent"));
            var childInput = NewDocument("CHILD", "Child");
            childInput.ParentDocumentId = parent.Id;
            var child = await service.CreateAsync(childInput);

            var parentUpdate = NewDocument(parent.Number!, parent.Title);
            parentUpdate.ParentDocumentId = child.Id;
            parentUpdate.ConcurrencyStamp = parent.ConcurrencyStamp;

            await Should.ThrowAsync<Volo.Abp.UserFriendlyException>(
                () => service.UpdateAsync(parent.Id, parentUpdate));
        });
    }

    private static CreateUpdateDocumentDto NewDocument(string number, string title) => new()
    {
        Number = number,
        Title = title,
        StorageNumber = $"HS-{number}",
        IncomingDate = DateTime.UtcNow,
        SourceType = DocumentSourceType.Archive
    };
}
