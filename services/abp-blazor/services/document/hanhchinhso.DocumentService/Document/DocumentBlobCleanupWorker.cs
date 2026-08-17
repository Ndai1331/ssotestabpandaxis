using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;
using Volo.Abp.DependencyInjection;

namespace hanhchinhso.DocumentService.Documents;

public class DocumentBlobCleanupWorker :
    AsyncPeriodicBackgroundWorkerBase,
    ISingletonDependency
{
    public DocumentBlobCleanupWorker(
        AbpAsyncTimer timer,
        IServiceScopeFactory serviceScopeFactory)
        : base(timer, serviceScopeFactory)
    {
        Timer.Period = 60_000;
    }

    protected override async Task DoWorkAsync(
        PeriodicBackgroundWorkerContext workerContext)
    {
        await workerContext.ServiceProvider
            .GetRequiredService<DocumentFileManager>()
            .ReconcilePendingAsync();
    }
}
