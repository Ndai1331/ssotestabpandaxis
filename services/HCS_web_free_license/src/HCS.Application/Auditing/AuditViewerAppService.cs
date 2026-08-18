using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using HCS.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;

namespace HCS.Auditing;

[Authorize(HCSPermissions.AuditViewer.Default)]
public class AuditViewerAppService(
    IAuditRecordProjectionRepository repository) : HCSAppService, IAuditViewerAppService
{
    public virtual async Task<PagedResultDto<AuditLogDto>> GetListAsync(GetAuditLogsInput input)
    {
        var query = await repository.GetQueryableAsync();
        query = query
            .WhereIf(input.UserId.HasValue, record => record.UserId == input.UserId)
            .WhereIf(!input.UserName.IsNullOrWhiteSpace(), record => record.UserName == input.UserName)
            .WhereIf(input.StartTime.HasValue, record => record.ExecutionTime >= input.StartTime)
            .WhereIf(input.EndTime.HasValue, record => record.ExecutionTime <= input.EndTime)
            .WhereIf(input.HttpStatusCode.HasValue, record => record.HttpStatusCode == input.HttpStatusCode)
            .WhereIf(!input.CorrelationId.IsNullOrWhiteSpace(), record => record.CorrelationId == input.CorrelationId)
            .WhereIf(
                !input.Action.IsNullOrWhiteSpace(),
                record => record.ActionName != null && record.ActionName.Contains(input.Action!));

        var count = await AsyncExecuter.CountAsync(query);
        var descending = !string.Equals(input.Sorting, "ExecutionTime ASC", StringComparison.OrdinalIgnoreCase);
        query = descending
            ? query.OrderByDescending(record => record.ExecutionTime)
            : query.OrderBy(record => record.ExecutionTime);
        var records = await AsyncExecuter.ToListAsync(query.PageBy(input));

        return new PagedResultDto<AuditLogDto>(count, records.Select(Map).ToList());
    }

    public virtual async Task<AuditLogDetailDto> GetAsync(Guid id)
    {
        var record = await repository.GetAsync(id);
        var dto = new AuditLogDetailDto
        {
            ApplicationName = record.ApplicationName,
            BrowserInfo = record.BrowserInfo,
            Exceptions = record.Exceptions,
            Comments = record.Comments,
            Actions = Deserialize<List<AuditLogActionDto>>(record.ActionsJson) ?? [],
            EntityChanges = Deserialize<List<AuditEntityChangeDto>>(record.EntityChangesJson) ?? []
        };
        Map(record, dto);
        return dto;
    }

    private static T? Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

    private static AuditLogDto Map(AuditRecordProjection record)
    {
        var dto = new AuditLogDto();
        Map(record, dto);
        return dto;
    }

    private static void Map(AuditRecordProjection record, AuditLogDto dto)
    {
        dto.Id = record.Id;
        dto.UserId = record.UserId;
        dto.UserName = record.UserName;
        dto.ExecutionTime = record.ExecutionTime;
        dto.ExecutionDuration = record.ExecutionDuration;
        dto.HttpMethod = record.HttpMethod;
        dto.Url = record.Url;
        dto.HttpStatusCode = record.HttpStatusCode;
        dto.CorrelationId = record.CorrelationId;
        dto.ClientIpAddress = record.ClientIpAddress;
        dto.HasException = !record.Exceptions.IsNullOrWhiteSpace();
        dto.SourceService = record.SourceService;
        dto.ActionName = record.ActionName;
    }
}
