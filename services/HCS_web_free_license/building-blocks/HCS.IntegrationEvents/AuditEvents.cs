using Volo.Abp.EventBus;

namespace HCS.IntegrationEvents.Auditing;

[EventName(AuditRecordCapturedEto.EventName)]
public sealed record AuditRecordCapturedEto(
    Guid Id,
    string SourceService,
    string? ApplicationName,
    Guid? UserId,
    string? UserName,
    DateTime ExecutionTime,
    int ExecutionDuration,
    string? ActionName,
    string? HttpMethod,
    string? Url,
    int? HttpStatusCode,
    string? CorrelationId,
    string? ClientIpAddress,
    string? BrowserInfo,
    string? Exceptions,
    string? Comments,
    IReadOnlyList<AuditActionCapturedEto> Actions,
    IReadOnlyList<AuditEntityChangeCapturedEto> EntityChanges)
{
    public const string EventName = "hcs.audit.record.v1";
}

public sealed record AuditActionCapturedEto(
    Guid Id,
    string? ServiceName,
    string? MethodName,
    string? Parameters,
    DateTime ExecutionTime,
    int ExecutionDuration);

public sealed record AuditEntityChangeCapturedEto(
    Guid Id,
    DateTime ChangeTime,
    string ChangeType,
    string? EntityId,
    string? EntityTypeFullName);

/// <summary>
/// Converts server exceptions into a stable, non-sensitive value that is safe to project
/// into the administrator-facing audit viewer. The original exception remains in server logs.
/// </summary>
public static class AuditExceptionSanitizer
{
    public const string RequestFailed = "Request failed. Inspect server logs using the correlation id.";

    public static string? ToAuditValue(Exception? exception) =>
        exception is null ? null : RequestFailed;

    public static string? SanitizeCapturedValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : RequestFailed;
}
