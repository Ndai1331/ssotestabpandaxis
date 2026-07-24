using hanhchinhso.AuditLoggingService.Tests;
using Microsoft.AspNetCore.Builder;
using Volo.Abp.AspNetCore.TestBase;

var builder = WebApplication.CreateBuilder();
builder.Environment.ContentRootPath = GetWebProjectContentRootPathHelper.Get("hanhchinhso.AuditLoggingService.csproj"); 
await builder.RunAbpModuleAsync<AuditLoggingServiceTestsModule>(applicationName: "hanhchinhso.AuditLoggingService");

public partial class TestProgram
{
}
