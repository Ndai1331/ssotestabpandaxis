using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace HCS.Logging;

public interface IServiceLogViewerAppService : IApplicationService
{
    Task<ListResultDto<ServiceLogDto>> GetListAsync(GetServiceLogsInput input);
}

public class GetServiceLogsInput
{
    public List<string>? Applications { get; set; }
    public List<string>? Levels { get; set; }
    public string? Filter { get; set; }
    public string? AfterId { get; set; }
    public int MaxResultCount { get; set; } = 100;
}

public class ServiceLogDto
{
    public string Id { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = null!;
    public string? Application { get; set; }
    public string? SourceContext { get; set; }
    public string Message { get; set; } = null!;
    public string? Exception { get; set; }
    public string? CorrelationId { get; set; }
    public Dictionary<string, string> Properties { get; set; } = [];
}
