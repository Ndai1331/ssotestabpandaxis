using abptestwithsso.AdministrationService.Tests;
using Microsoft.AspNetCore.Builder;
using Volo.Abp.AspNetCore.TestBase;

var builder = WebApplication.CreateBuilder();
builder.Environment.ContentRootPath = GetWebProjectContentRootPathHelper.Get("abptestwithsso.AdministrationService.csproj"); 
await builder.RunAbpModuleAsync<AdministrationServiceTestsModule>(applicationName: "abptestwithsso.AdministrationService");

public partial class TestProgram
{
}
