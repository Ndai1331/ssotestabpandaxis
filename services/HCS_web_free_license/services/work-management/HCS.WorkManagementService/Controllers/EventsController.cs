using HCS.WorkManagementService.Application;
using HCS.WorkManagementService.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HCS.WorkManagementService.Controllers;

[ApiController, Authorize(Policy = WorkPermissions.Events), Route("api/events")]
public sealed class EventsController(EventAppService service, WorkAssetService assets) : ControllerBase
{
    [HttpGet]
    public Task<PagedWorkDto<EventListItemDto>> GetList(string? filter, string? group, string? status, int skip = 0, int take = 20, CancellationToken ct = default) =>
        service.GetListAsync(filter, group, status, skip, take, ct);

    [HttpGet("dashboard")]
    public Task<EventDashboardDto> Dashboard(DateTime? from, DateTime? to, CancellationToken ct) => service.GetDashboardAsync(from, to, ct);

    [HttpGet("{id:guid}")]
    public Task<EventDto> Get(Guid id, CancellationToken ct) => service.GetAsync(id, ct);

    [HttpPost]
    public Task<EventDto> Create(CreateManagedEventDto input, CancellationToken ct) => service.CreateAsync(input, ct);

    [HttpPut("{id:guid}")]
    public Task<EventDto> Update(Guid id, UpdateManagedEventDto input, CancellationToken ct) => service.UpdateAsync(id, input, ct);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) { await service.DeleteAsync(id, ct); return NoContent(); }

    [HttpGet("{eventId:guid}/attendees")]
    public Task<PagedWorkDto<EventAttendeeDto>> Attendees(Guid eventId, string? filter, string? registrationStatus,
        string? checkInStatus, int skip = 0, int take = 20, CancellationToken ct = default) =>
        service.GetAttendeesAsync(eventId, filter, registrationStatus, checkInStatus, skip, take, ct);

    [HttpPost("{eventId:guid}/attendees")]
    public Task<EventAttendeeDto> AddAttendee(Guid eventId, CreateEventAttendeeDto input, CancellationToken ct) => service.AddAttendeeAsync(eventId, input, ct);

    [HttpPut("attendees/{id:guid}")]
    public Task<EventAttendeeDto> UpdateAttendee(Guid id, UpdateEventAttendeeDto input, CancellationToken ct) => service.UpdateAttendeeAsync(id, input, ct);

    [HttpPost("attendees/{id:guid}/status")]
    public Task<EventAttendeeDto> ChangeStatus(Guid id, ChangeEventAttendeeStatusDto input, CancellationToken ct) => service.ChangeAttendeeStatusAsync(id, input, ct);

    [HttpDelete("attendees/{id:guid}")]
    public async Task<IActionResult> DeleteAttendee(Guid id, CancellationToken ct) { await service.DeleteAttendeeAsync(id, ct); return NoContent(); }

    [HttpPost("{eventId:guid}/attendees/delete-bulk")]
    public Task<int> DeleteAttendees(Guid eventId, IReadOnlyCollection<Guid> ids, CancellationToken ct) => service.DeleteAttendeesAsync(eventId, ids, ct);

    [HttpPost("{eventId:guid}/attendees/import")]
    [RequestSizeLimit(2 * 1024 * 1024)]
    public async Task<EventImportResultDto> Import(Guid eventId, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) throw new BadHttpRequestException("CSV file is required.");
        await using var stream = file.OpenReadStream();
        return await service.ImportAsync(eventId, stream, ct);
    }

    [HttpGet("{id:guid}/qr")]
    public async Task<IActionResult> Qr(Guid id, CancellationToken ct) =>
        File(await service.GetQrCodeAsync(id, $"{Request.Scheme}://{Request.Host}", ct), "image/png", "event-qr.png");

    [HttpPost("{eventId:guid}/attachments")]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<EventAttachmentDto> UploadAttachment(Guid eventId, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) throw new BadHttpRequestException("Attachment is required.");
        await using var stream = file.OpenReadStream();
        return await assets.SaveEventFileAsync(eventId, stream, file.FileName, file.ContentType, file.Length, ct);
    }

    [HttpGet("attachments/{fileId:guid}")]
    public async Task<IActionResult> Attachment(Guid fileId, CancellationToken ct)
    {
        var result = await assets.GetEventFileAsync(fileId, ct);
        return File(result.Stream, result.File.ContentType, result.File.FileName);
    }

    [HttpDelete("attachments/{fileId:guid}")]
    public async Task<IActionResult> DeleteAttachment(Guid fileId, CancellationToken ct)
    {
        await assets.DeleteEventFileAsync(fileId, ct); return NoContent();
    }

    [AllowAnonymous, HttpGet("public/{code}")]
    public Task<PublicEventDto> Public(string code, [FromQuery] string token, CancellationToken ct) => service.GetPublicAsync(code, token, ct);

    [AllowAnonymous, HttpPost("public/{code}/check-in")]
    public Task<PublicEventCheckInResultDto> PublicCheckIn(string code, [FromQuery] string token,
        PublicEventCheckInDto input, CancellationToken ct) => service.CheckInPublicAsync(code, token, input, ct);
}
