using hanhchinhso.AIManagementService.Tests;
using Microsoft.AspNetCore.Builder;
using Volo.Abp.AspNetCore.TestBase;

var builder = WebApplication.CreateBuilder();
builder.Environment.ContentRootPath = GetWebProjectContentRootPathHelper.Get("hanhchinhso.AIManagementService.csproj"); 
await builder.RunAbpModuleAsync<AIManagementServiceTestsModule>(applicationName: "hanhchinhso.AIManagementService");

public partial class TestProgram
{
}
