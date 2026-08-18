namespace HCS.IntegrationEvents.Work;

public sealed record ProjectTaskChangedEto(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    string? CorrelationId,
    Guid ProjectId,
    Guid TaskId,
    string ChangeType,
    string Status,
    Guid? AssigneeUserId)
    : IntegrationEvent(EventId, OccurredAtUtc, CorrelationId);
