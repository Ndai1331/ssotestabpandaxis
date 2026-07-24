using hanhchinhso.AdministrationService.Tests;
using Microsoft.AspNetCore.Builder;
using Volo.Abp.AspNetCore.TestBase;

var builder = WebApplication.CreateBuilder();
builder.Environment.ContentRootPath = GetWebProjectContentRootPathHelper.Get("hanhchinhso.AdministrationService.csproj"); 
await builder.RunAbpModuleAsync<AdministrationServiceTestsModule>(applicationName: "hanhchinhso.AdministrationService");

public partial class TestProgram
{
}
