using hanhchinhso.GdprService.Tests;
using Microsoft.AspNetCore.Builder;
using Volo.Abp.AspNetCore.TestBase;

var builder = WebApplication.CreateBuilder();
builder.Environment.ContentRootPath = GetWebProjectContentRootPathHelper.Get("hanhchinhso.GdprService.csproj"); 
await builder.RunAbpModuleAsync<GdprServiceTestsModule>(applicationName: "hanhchinhso.GdprService");

public partial class TestProgram
{
}
