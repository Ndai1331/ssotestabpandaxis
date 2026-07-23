using abptestwithsso.GdprService.Tests;
using Microsoft.AspNetCore.Builder;
using Volo.Abp.AspNetCore.TestBase;

var builder = WebApplication.CreateBuilder();
builder.Environment.ContentRootPath = GetWebProjectContentRootPathHelper.Get("abptestwithsso.GdprService.csproj"); 
await builder.RunAbpModuleAsync<GdprServiceTestsModule>(applicationName: "abptestwithsso.GdprService");

public partial class TestProgram
{
}
