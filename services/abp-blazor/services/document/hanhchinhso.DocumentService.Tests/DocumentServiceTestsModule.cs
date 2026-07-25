using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using hanhchinhso.DocumentService.Data;
using Volo.Abp;
using Volo.Abp.AspNetCore.TestBase;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Caching;
using Volo.Abp.DistributedLocking;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;
using Volo.Abp.Uow;
using Volo.Abp.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Volo.Abp.EntityFrameworkCore.Sqlite;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Volo.Abp.BlobStoring;
using hanhchinhso.DocumentService.Documents;
using hanhchinhso.DocumentService.Workflows;
using Volo.Abp.Identity;
using Microsoft.AspNetCore.DataProtection;
using hanhchinhso.DocumentService.Signing;

namespace hanhchinhso.DocumentService.Tests;

/* This project has a dotnet project reference to the hanhchinhso.DocumentService project,
 * but it does not have a module dependency to the hanhchinhsoDocumentServiceModule module class.
 * Because, hanhchinhsoDocumentServiceModule has configurations proper for development and production
 * environments, but not proper or necessary for tests.
 *
 * In this test project, we are carefully depending on the modules that we need in tests.
 *
 * For example, hanhchinhsoDocumentServiceModule depends on AbpEventBusRabbitMqModule,
 * but this module depends on AbpEventBusModule since we don't want to use RabbitMQ in tests.
 * AbpEventBusModule has an in-process event bus instead of a real distributed event bus, and it is fine for tests.
 *
 * WARNING: If you change hanhchinhsoDocumentServiceModule class, you may need to properly change this class to keep
 *          test code compatible with the application code.
 */
[DependsOn(
    typeof(AbpAspNetCoreTestBaseModule),
    typeof(AbpEntityFrameworkCoreSqliteModule),
    typeof(hanhchinhsoDocumentServiceContractsModule),
    typeof(AbpEventBusModule),
    typeof(AbpCachingModule),
    typeof(AbpDistributedLockingAbstractionsModule)
)]
[AdditionalAssembly(typeof(hanhchinhsoDocumentServiceModule))]
public class DocumentServiceTestsModule : AbpModule
{
    private SqliteConnection? _sqliteConnection;
    
    public override void PreConfigureServices(ServiceConfigurationContext context)
    {
        PreConfigure<AbpSqliteOptions>(x => x.BusyTimeout = null);
    }

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        
        ConfigureAuthorization(context);
        ConfigureExternalDependencies(context);
        context.Services.AddDataProtection();
        ConfigureDatabase(context);
        ConfigureDatabaseTransactions(context);
        ConfigureBackgroundJobs();
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();

        app.UseCorrelationId();
        app.UseAbpRequestLocalization();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseUnitOfWork();
        app.UseConfiguredEndpoints();
    }

    public override async Task OnPreApplicationInitializationAsync(ApplicationInitializationContext context)
    {
        using var scope = context.ServiceProvider.CreateScope();
        await scope.ServiceProvider
            .GetRequiredService<DocumentServiceDataSeeder>()
            .SeedAsync();
    }
    
    public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        _sqliteConnection?.Dispose();
    }

    private static void ConfigureAuthorization(ServiceConfigurationContext context)
    {
        /* We don't need to authorization in tests */
        context.Services.AddAlwaysAllowAuthorization();
    }

    private static void ConfigureExternalDependencies(ServiceConfigurationContext context)
    {
        context.Services.Replace(
            ServiceDescriptor.Singleton(
                Substitute.For<IWorkflowIdentityReferenceValidator>()));
        context.Services.Replace(
            ServiceDescriptor.Singleton(
                Substitute.For<IWorkflowAssigneeResolver>()));
        context.Services.Replace(
            ServiceDescriptor.Singleton(
                Substitute.For<IWorkflowIdentityMembershipResolver>()));
        context.Services.Replace(
            ServiceDescriptor.Singleton(Substitute.For<IOrganizationUnitAppService>()));
        context.Services.Replace(
            ServiceDescriptor.Singleton(Substitute.For<IBlobContainer<DocumentBlobContainer>>()));
        context.Services.Replace(
            ServiceDescriptor.Singleton(Substitute.For<ISigningEndpointPolicy>()));
        context.Services.Replace(
            ServiceDescriptor.Singleton(
                Substitute.For<IBlobContainer<SigningBlobContainer>>()));
        context.Services.Replace(
            ServiceDescriptor.Singleton(
                Substitute.For<IRemoteCaSigningProvider>()));
        context.Services.Replace(
            ServiceDescriptor.Singleton(
                Substitute.For<IBnnSigningProvider>()));
    }

    private void ConfigureDatabase(ServiceConfigurationContext context)
    {
        _sqliteConnection = CreateDatabaseAndGetConnection();
        
        context.Services.AddAbpDbContext<DocumentServiceDbContext>(options =>
        {
            options.AddDefaultRepositories();
        });
        
        Configure<AbpDbContextOptions>(options =>
        {
            options.Configure(opts =>
            {
                /* Use SQLite for all EF Core DbContexts in tests */
                opts.UseSqlite(_sqliteConnection);
            });
        });
    }
    
    private void ConfigureDatabaseTransactions(ServiceConfigurationContext context)
    {
        context.Services.AddAlwaysDisableUnitOfWorkTransaction();
        
        Configure<AbpUnitOfWorkDefaultOptions>(options =>
        {
            options.TransactionBehavior = UnitOfWorkTransactionBehavior.Disabled;
        });
    }
    
    private void ConfigureBackgroundJobs()
    {
        Configure<AbpBackgroundWorkerOptions>(options =>
        {
            options.IsEnabled = false;
        });
        
        Configure<AbpBackgroundJobOptions>(options =>
        {
            options.IsJobExecutionEnabled = false;
        });
    }

    private static SqliteConnection CreateDatabaseAndGetConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        // DocumentServiceDbContext ()
        new DocumentServiceDbContext(
            new DbContextOptionsBuilder<DocumentServiceDbContext>().UseSqlite(connection).Options
        ).GetService<IRelationalDatabaseCreator>().CreateTables();
        
        return connection;
    }
}
