using System.Threading.Tasks;
using HCS.Logging;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;

namespace HCS.Controllers.Logging;

[Route("api/hcs/service-logs")]
[Route("api/service-logs")]
public class ServiceLogViewerController : HCSController, IServiceLogViewerAppService
{
    private readonly IServiceLogViewerAppService _service;

    public ServiceLogViewerController(IServiceLogViewerAppService service) => _service = service;

    [HttpGet]
    public Task<ListResultDto<ServiceLogDto>> GetListAsync([FromQuery] GetServiceLogsInput input) =>
        _service.GetListAsync(input);
}
