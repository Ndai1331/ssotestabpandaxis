using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using HCS.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Validation;

namespace HCS.Logging;

[Authorize(HCSPermissions.ServiceLogs.Default)]
public class ServiceLogViewerAppService(IServiceLogQuery query) : HCSAppService, IServiceLogViewerAppService
{
    public virtual async Task<ListResultDto<ServiceLogDto>> GetListAsync(GetServiceLogsInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        string filter;
        int count;
        string? afterId;
        try
        {
            filter = ServiceLogQueryBuilder.Build(input.Applications, input.Levels, input.Filter);
            count = ServiceLogQueryBuilder.NormalizeCount(input.MaxResultCount);
            afterId = ServiceLogQueryBuilder.NormalizeAfterId(input.AfterId);
        }
        catch (ArgumentException exception)
        {
            throw new AbpValidationException(
            [
                new ValidationResult(exception.Message, [exception.ParamName ?? nameof(input)])
            ]);
        }

        try
        {
            var items = await query.GetEventsAsync(filter, count, afterId);
            return new ListResultDto<ServiceLogDto>(items);
        }
        catch (UserFriendlyException)
        {
            throw new UserFriendlyException(L["ServiceLogs:SeqUnavailable"]);
        }
    }
}
