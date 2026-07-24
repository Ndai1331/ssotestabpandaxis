using hanhchinhso.WorkflowService.Tests;
using Microsoft.AspNetCore.Builder;
using Volo.Abp.AspNetCore.TestBase;

var builder = WebApplication.CreateBuilder();
builder.Environment.ContentRootPath = GetWebProjectContentRootPathHelper.Get("hanhchinhso.WorkflowService.csproj"); 
await builder.RunAbpModuleAsync<WorkflowServiceTestsModule>(applicationName: "hanhchinhso.WorkflowService");

public partial class TestProgram
{
}