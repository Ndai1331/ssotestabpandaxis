using abptestwithsso.IdentityService.Tests;
using Microsoft.AspNetCore.Builder;
using Volo.Abp.AspNetCore.TestBase;

var builder = WebApplication.CreateBuilder();
builder.Environment.ContentRootPath = GetWebProjectContentRootPathHelper.Get("abptestwithsso.IdentityService.csproj"); 
await builder.RunAbpModuleAsync<IdentityServiceTestsModule>(applicationName: "abptestwithsso.IdentityService");

public partial class TestProgram
{
}
