using abptestwithsso.LanguageService.Tests;
using Microsoft.AspNetCore.Builder;
using Volo.Abp.AspNetCore.TestBase;

var builder = WebApplication.CreateBuilder();
builder.Environment.ContentRootPath = GetWebProjectContentRootPathHelper.Get("abptestwithsso.LanguageService.csproj"); 
await builder.RunAbpModuleAsync<LanguageServiceTestsModule>(applicationName: "abptestwithsso.LanguageService");

public partial class TestProgram
{
}