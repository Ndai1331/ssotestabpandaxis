using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Volo.Abp.BlobStoring.Database;
using Volo.Abp.BlobStoring.Database.EntityFrameworkCore;

namespace hanhchinhso.AdministrationService.Data;

/* This class is needed for EF Core console commands
 * To Add Add New Migration, execute the following command a command-line terminal in this project's root folder:
 * 
 *   dotnet ef migrations add "My_Migration_Name" -c BlobStoringDbContext -o Migrations/BlobStoring
 * 
 * */
public class BlobStoringDbContextFactory : IDesignTimeDbContextFactory<BlobStoringDbContext>
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // https://www.npgsql.org/efcore/release-notes/6.0.html#opting-out-of-the-new-timestamp-mapping-logic
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }
    
    public BlobStoringDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<BlobStoringDbContext>()
            .UseNpgsql(GetConnectionStringFromConfiguration(), b =>
            {
                b.MigrationsHistoryTable("__AbpBlobStoring_Migrations");
                b.MigrationsAssembly(typeof(hanhchinhsoAdministrationServiceModule).Assembly.GetName().Name);
            });

        return new BlobStoringDbContext(builder.Options);
    }

    private static string GetConnectionStringFromConfiguration()
    {
        return BuildConfiguration().GetConnectionString(AbpBlobStoringDatabaseDbProperties.ConnectionStringName) 
               ?? throw new ApplicationException($"Could not find a connection string named '{AbpBlobStoringDatabaseDbProperties.ConnectionStringName}'.");
    }

    private static IConfigurationRoot BuildConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables();

        return builder.Build();
    }
}