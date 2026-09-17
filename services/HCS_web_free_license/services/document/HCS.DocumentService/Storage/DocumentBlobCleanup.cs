using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Volo.Abp.BlobStoring;

namespace HCS.DocumentService.Storage;

public readonly record struct DocumentBlobCleanupWork(
    IReadOnlyList<string> DocumentBlobNames,
    IReadOnlyList<string> SigningBlobNames);

public interface IDocumentBlobCleanup
{
    void Enqueue(IEnumerable<string> documentBlobNames, IEnumerable<string> signingBlobNames);
}

public sealed class DocumentBlobCleanupQueue : IDocumentBlobCleanup
{
    private readonly Channel<DocumentBlobCleanupWork> channel = Channel.CreateUnbounded<DocumentBlobCleanupWork>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    public ChannelReader<DocumentBlobCleanupWork> Reader => channel.Reader;

    public void Enqueue(IEnumerable<string> documentBlobNames, IEnumerable<string> signingBlobNames)
    {
        var documents = DistinctNames(documentBlobNames);
        var signing = DistinctNames(signingBlobNames);
        if (documents.Count == 0 && signing.Count == 0) return;
        channel.Writer.TryWrite(new DocumentBlobCleanupWork(documents, signing));
    }

    private static IReadOnlyList<string> DistinctNames(IEnumerable<string> names) =>
        names.Where(name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.Ordinal).ToArray();
}

public sealed class DocumentBlobCleanupWorker(
    DocumentBlobCleanupQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<DocumentBlobCleanupWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var work in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var documentBlobs = scope.ServiceProvider.GetRequiredService<IBlobContainer<DocumentBlobContainer>>();
                var signingBlobs = scope.ServiceProvider.GetRequiredService<IBlobContainer<SigningBlobContainer>>();
                await DeleteAsync(documentBlobs, work.DocumentBlobNames, stoppingToken);
                await DeleteAsync(signingBlobs, work.SigningBlobNames, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Background document blob cleanup failed");
            }
        }
    }

    private async Task DeleteAsync<TContainer>(IBlobContainer<TContainer> container, IReadOnlyList<string> blobNames,
        CancellationToken cancellationToken)
        where TContainer : class
    {
        foreach (var blobName in blobNames)
        {
            try
            {
                await container.DeleteAsync(blobName, cancellationToken: cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Could not delete blob {BlobName} after document removal", blobName);
            }
        }
    }
}
