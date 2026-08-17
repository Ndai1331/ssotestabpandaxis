using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace hanhchinhso.DocumentService.Controllers;

[Route("api/document-management/demo")]
[Area("document-management")]
[RemoteService(Name = "DocumentService")]
public class DemoController : AbpController
{
    private readonly hanhchinhsoMetrics _hanhchinhsoMetrics;

    public DemoController(hanhchinhsoMetrics hanhchinhsoMetrics)
    {
        _hanhchinhsoMetrics = hanhchinhsoMetrics;
    }
    
    [HttpGet]
    [Route("hello")]
    public async Task<string> HelloWorld()
    {
        _hanhchinhsoMetrics.IncrementHelloCounter();
        return await Task.FromResult("Hello World!");
    }
}