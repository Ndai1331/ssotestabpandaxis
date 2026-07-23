using abptestwithsso.AuditLoggingService.Tests;
using Microsoft.AspNetCore.Builder;
using Volo.Abp.AspNetCore.TestBase;

var builder = WebApplication.CreateBuilder();
builder.Environment.ContentRootPath = GetWebProjectContentRootPathHelper.Get("abptestwithsso.AuditLoggingService.csproj"); 
await builder.RunAbpModuleAsync<AuditLoggingServiceTestsModule>(applicationName: "abptestwithsso.AuditLoggingService");

public partial class TestProgram
{
}
