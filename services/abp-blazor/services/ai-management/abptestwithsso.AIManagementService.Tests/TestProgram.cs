using abptestwithsso.AIManagementService.Tests;
using Microsoft.AspNetCore.Builder;
using Volo.Abp.AspNetCore.TestBase;

var builder = WebApplication.CreateBuilder();
builder.Environment.ContentRootPath = GetWebProjectContentRootPathHelper.Get("abptestwithsso.AIManagementService.csproj"); 
await builder.RunAbpModuleAsync<AIManagementServiceTestsModule>(applicationName: "abptestwithsso.AIManagementService");

public partial class TestProgram
{
}
