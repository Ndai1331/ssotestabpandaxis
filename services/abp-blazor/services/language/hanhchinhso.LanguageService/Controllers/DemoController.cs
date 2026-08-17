using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace hanhchinhso.LanguageService.Controllers;

[Route("api/language-management/demo")]
[Area("language-management")]
[RemoteService(Name = "LanguageService")]
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