using hanhchinhso.DocumentService.Tests;
using Microsoft.AspNetCore.Builder;
using Volo.Abp.AspNetCore.TestBase;

var builder = WebApplication.CreateBuilder();
builder.Environment.ContentRootPath = GetWebProjectContentRootPathHelper.Get("hanhchinhso.DocumentService.csproj"); 
await builder.RunAbpModuleAsync<DocumentServiceTestsModule>(applicationName: "hanhchinhso.DocumentService");

public partial class TestProgram
{
}