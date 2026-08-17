using hanhchinhso.LanguageService.Tests;
using Microsoft.AspNetCore.Builder;
using Volo.Abp.AspNetCore.TestBase;

var builder = WebApplication.CreateBuilder();
builder.Environment.ContentRootPath = GetWebProjectContentRootPathHelper.Get("hanhchinhso.LanguageService.csproj"); 
await builder.RunAbpModuleAsync<LanguageServiceTestsModule>(applicationName: "hanhchinhso.LanguageService");

public partial class TestProgram
{
}