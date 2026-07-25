using System.Security.Claims;
using hanhchinhso.DocumentService.Documents;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Volo.Abp.BlobStoring;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Security.Claims;
using Xunit;

namespace hanhchinhso.DocumentService.Tests.Document;

public class DocumentFileManagerTests : DocumentServiceIntegrationTestBase
{
    [Fact]
    public async Task Should_Persist_Metadata_After_Blob_Write()
    {
        var documentId = await CreateDocumentAsync();
        var entity = NewFile(documentId, "host/document/one.pdf");

        await ServiceProvider.GetRequiredService<DocumentFileManager>()
            .SaveAsync(entity, PdfStream());

        await WithUnitOfWorkAsync(async () =>
        {
            var stored = await ServiceProvider
                .GetRequiredService<IRepository<DocumentFile, Guid>>()
                .GetAsync(entity.Id);
            stored.BlobName.ShouldBe(entity.BlobName);
        });
    }

    [Fact]
    public async Task Should_Not_Create_Metadata_When_Blob_Write_Fails()
    {
        var documentId = await CreateDocumentAsync();
        var entity = NewFile(documentId, "host/document/fail.pdf");
        var blobs = ServiceProvider
            .GetRequiredService<IBlobContainer<DocumentBlobContainer>>();
        blobs.SaveAsync(entity.BlobName, Arg.Any<Stream>(), false, Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new IOException("MinIO unavailable"));

        await Should.ThrowAsync<IOException>(() =>
            ServiceProvider.GetRequiredService<DocumentFileManager>()
                .SaveAsync(entity, PdfStream()));

        await WithUnitOfWorkAsync(async () =>
            (await ServiceProvider
                .GetRequiredService<IRepository<DocumentFile, Guid>>()
                .FindAsync(entity.Id)).ShouldBeNull());
    }

    [Fact]
    public async Task Should_Compensate_Blob_When_Metadata_Commit_Fails()
    {
        var documentId = await CreateDocumentAsync();
        var manager = ServiceProvider.GetRequiredService<DocumentFileManager>();
        var id = Guid.NewGuid();
        var first = NewFile(documentId, "host/document/first.pdf", id);
        await manager.SaveAsync(first, PdfStream());

        var duplicate = NewFile(documentId, "host/document/duplicate.pdf", id);
        await Should.ThrowAsync<Exception>(() =>
            manager.SaveAsync(duplicate, PdfStream()));

        var blobs = ServiceProvider
            .GetRequiredService<IBlobContainer<DocumentBlobContainer>>();
        await blobs.Received().DeleteAsync(
            duplicate.BlobName, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_Retry_Blob_Delete_Without_Exposing_Pending_File()
    {
        using var principal = ChangeUser();
        var currentTenant = ServiceProvider.GetRequiredService<ICurrentTenant>();
        var tenantId = Guid.NewGuid();
        var blobs = ServiceProvider
            .GetRequiredService<IBlobContainer<DocumentBlobContainer>>();
        Guid documentId;
        DocumentFile entity;

        using (currentTenant.Change(tenantId))
        {
            documentId = await CreateDocumentAsync();
            var manager = ServiceProvider.GetRequiredService<DocumentFileManager>();
            entity = NewFile(
                documentId,
                $"{tenantId:N}/{documentId:N}/delete.pdf",
                tenantId: tenantId);
            await manager.SaveAsync(entity, PdfStream());
            blobs.DeleteAsync(entity.BlobName, Arg.Any<CancellationToken>())
                .Returns<Task<bool>>(_ => throw new IOException("MinIO unavailable"));

            await manager.RequestDeleteAsync(entity.Id, entity.ConcurrencyStamp);
            await manager.RequestDeleteAsync(entity.Id, entity.ConcurrencyStamp);
            var fileService = ServiceProvider.GetRequiredService<IDocumentFileAppService>();
            (await fileService.GetListAsync(documentId)).ShouldBeEmpty();
        }

        blobs.DeleteAsync(entity.BlobName, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        (await ServiceProvider.GetRequiredService<DocumentFileManager>()
            .ReconcilePendingAsync()).ShouldBe(1);

        using (currentTenant.Change(tenantId))
        {
            await WithUnitOfWorkAsync(async () =>
                (await ServiceProvider
                    .GetRequiredService<IRepository<DocumentFile, Guid>>()
                    .FindAsync(entity.Id)).ShouldBeNull());
        }
    }

    [Fact]
    public async Task Should_Durably_Retry_Failed_Upload_Compensation()
    {
        var documentId = await CreateDocumentAsync();
        var manager = ServiceProvider.GetRequiredService<DocumentFileManager>();
        var id = Guid.NewGuid();
        await manager.SaveAsync(
            NewFile(documentId, "host/document/original.pdf", id),
            PdfStream());

        var orphan = NewFile(documentId, "host/document/orphan.pdf", id);
        var blobs = ServiceProvider
            .GetRequiredService<IBlobContainer<DocumentBlobContainer>>();
        blobs.DeleteAsync(orphan.BlobName, Arg.Any<CancellationToken>())
            .Returns<Task<bool>>(_ => throw new IOException("MinIO unavailable"));

        await Should.ThrowAsync<Exception>(() =>
            manager.SaveAsync(orphan, PdfStream()));

        await WithUnitOfWorkAsync(async () =>
            (await ServiceProvider
                .GetRequiredService<IRepository<DocumentBlobCleanup, Guid>>()
                .GetCountAsync()).ShouldBe(1));

        blobs.DeleteAsync(orphan.BlobName, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        (await manager.ReconcilePendingAsync()).ShouldBe(1);

        await WithUnitOfWorkAsync(async () =>
            (await ServiceProvider
                .GetRequiredService<IRepository<DocumentBlobCleanup, Guid>>()
                .GetCountAsync()).ShouldBe(0));
    }

    [Fact]
    public async Task Should_Not_Cleanup_Marker_While_Upload_Is_In_Progress()
    {
        var documentId = await CreateDocumentAsync();
        var entity = NewFile(documentId, "host/document/slow.pdf");
        var blobs = ServiceProvider
            .GetRequiredService<IBlobContainer<DocumentBlobContainer>>();
        var uploadEntered = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseUpload = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        blobs.SaveAsync(
                entity.BlobName,
                Arg.Any<Stream>(),
                false,
                Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                uploadEntered.SetResult();
                await releaseUpload.Task;
            });

        var manager = ServiceProvider.GetRequiredService<DocumentFileManager>();
        var saveTask = manager.SaveAsync(entity, PdfStream());
        await uploadEntered.Task;

        (await manager.ReconcilePendingAsync()).ShouldBe(0);
        await blobs.DidNotReceive().DeleteAsync(
            entity.BlobName, Arg.Any<CancellationToken>());

        releaseUpload.SetResult();
        await saveTask;
    }

    [Fact]
    public async Task Should_Reject_Delete_With_Stale_Concurrency_Stamp()
    {
        using var principal = ChangeUser();
        var documentId = await CreateDocumentAsync();
        var entity = NewFile(documentId, "host/document/concurrency.pdf");
        await ServiceProvider.GetRequiredService<DocumentFileManager>()
            .SaveAsync(entity, PdfStream());

        var fileService = ServiceProvider.GetRequiredService<IDocumentFileAppService>();
        var staleStamp = (await fileService.GetListAsync(documentId))
            .Single().ConcurrencyStamp;

        await WithUnitOfWorkAsync(async () =>
        {
            var repository = ServiceProvider
                .GetRequiredService<IRepository<DocumentFile, Guid>>();
            var current = await repository.GetAsync(entity.Id);
            current.SetProperty("ConcurrencyTest", Guid.NewGuid().ToString("N"));
            await repository.UpdateAsync(current, autoSave: true);
        });

        await Should.ThrowAsync<Volo.Abp.Data.AbpDbConcurrencyException>(() =>
            fileService.DeleteAsync(entity.Id, staleStamp));
    }

    [Fact]
    public async Task Should_Isolate_File_Metadata_By_Tenant()
    {
        using var principal = ChangeUser();
        var currentTenant = ServiceProvider.GetRequiredService<ICurrentTenant>();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        Guid documentId;

        using (currentTenant.Change(tenantA))
        {
            documentId = await CreateDocumentAsync();
            await ServiceProvider.GetRequiredService<DocumentFileManager>()
                .SaveAsync(
                    NewFile(documentId, $"{tenantA:N}/{documentId:N}/a.pdf", tenantId: tenantA),
                    PdfStream());
        }

        using (currentTenant.Change(tenantB))
        {
            await Should.ThrowAsync<
                Volo.Abp.Authorization.AbpAuthorizationException>(
                () => ServiceProvider
                    .GetRequiredService<IDocumentFileAppService>()
                    .GetListAsync(documentId));
        }
    }

    private async Task<Guid> CreateDocumentAsync()
    {
        DocumentDto? created = null;
        await WithUnitOfWorkAsync(async () =>
        {
            created = await ServiceProvider.GetRequiredService<IDocumentAppService>()
                .CreateAsync(new CreateUpdateDocumentDto
                {
                    Number = $"CV-{Guid.NewGuid():N}",
                    Title = "File manager test",
                    StorageNumber = "HS-TEST",
                    IncomingDate = DateTime.UtcNow,
                    SourceType = DocumentSourceType.Archive
                });
        });
        return created!.Id;
    }

    private DocumentFile NewFile(
        Guid documentId,
        string blobName,
        Guid? id = null,
        Guid? tenantId = null) =>
        new(
            id ?? Guid.NewGuid(),
            tenantId ?? ServiceProvider.GetRequiredService<ICurrentTenant>().Id,
            documentId,
            "test.pdf",
            blobName,
            "application/pdf",
            8,
            "hash");

    private static MemoryStream PdfStream() =>
        new("%PDF-1.7"u8.ToArray());

    private IDisposable ChangeUser() =>
        ServiceProvider.GetRequiredService<ICurrentPrincipalAccessor>()
            .Change(new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(AbpClaimTypes.UserId, Guid.NewGuid().ToString()),
                new Claim(AbpClaimTypes.UserName, "file-test-user")
            ], "Test")));
}
