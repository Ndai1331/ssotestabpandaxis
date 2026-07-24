using hanhchinhso.IdentityService.Tests;
using Microsoft.AspNetCore.Builder;
using Volo.Abp.AspNetCore.TestBase;

var builder = WebApplication.CreateBuilder();
builder.Environment.ContentRootPath = GetWebProjectContentRootPathHelper.Get("hanhchinhso.IdentityService.csproj"); 
await builder.RunAbpModuleAsync<IdentityServiceTestsModule>(applicationName: "hanhchinhso.IdentityService");

public partial class TestProgram
{
}
