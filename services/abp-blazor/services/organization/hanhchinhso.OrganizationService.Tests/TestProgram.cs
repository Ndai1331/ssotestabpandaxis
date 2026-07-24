using hanhchinhso.OrganizationService.Tests;
using Microsoft.AspNetCore.Builder;
using Volo.Abp.AspNetCore.TestBase;

var builder = WebApplication.CreateBuilder();
builder.Environment.ContentRootPath = GetWebProjectContentRootPathHelper.Get("hanhchinhso.OrganizationService.csproj"); 
await builder.RunAbpModuleAsync<OrganizationServiceTestsModule>(applicationName: "hanhchinhso.OrganizationService");

public partial class TestProgram
{
}