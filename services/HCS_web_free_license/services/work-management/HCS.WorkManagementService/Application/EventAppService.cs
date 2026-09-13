using System.Globalization;
using System.Text;
using HCS.WorkManagementService.Contracts;
using HCS.WorkManagementService.Data;
using HCS.WorkManagementService.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QRCoder;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Users;

namespace HCS.WorkManagementService.Application;

public sealed class EventAppService(WorkManagementDbContext db, WorkRecordAuthorization access,
    ILogger<EventAppService> logger, ICurrentUser currentUser) : ITransientDependency
{
    public async Task<PagedWorkDto<EventListItemDto>> GetListAsync(string? filter, string? group, string? status,
        int skip, int take, CancellationToken ct)
    {
        take = Math.Clamp(take, 1, 100); skip = Math.Max(0, skip);
        var query = db.ManagedEvents.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(filter))
        {
            var term = filter.Trim().ToLowerInvariant();
            query = query.Where(x => EF.Functions.ILike(x.Code, $"%{term}%") || EF.Functions.ILike(x.Name, $"%{term}%"));
        }
        if (!string.IsNullOrWhiteSpace(group)) query = query.Where(x => x.Group == group);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        var total = await query.LongCountAsync(ct);
        var page = await query.OrderByDescending(x => x.StartTime).ThenBy(x => x.Name).Skip(skip).Take(take).ToListAsync(ct);
        var ids = page.Select(x => x.Id).ToArray();
        var counts = await db.EventAttendees.AsNoTracking().Where(x => ids.Contains(x.EventId))
            .GroupBy(x => x.EventId).Select(x => new { x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        return new(total, page.Select(x => MapList(x, counts.GetValueOrDefault(x.Id))).ToList());
    }

    public async Task<EventDashboardDto> GetDashboardAsync(DateTime? from, DateTime? to, CancellationToken ct)
    {
        var start = from?.ToUniversalTime() ?? DateTime.UtcNow.Date.AddMonths(-1);
        var end = to?.ToUniversalTime() ?? DateTime.UtcNow.Date.AddMonths(3).AddDays(1).AddTicks(-1);
        var events = await db.ManagedEvents.AsNoTracking().Where(x => x.EndTime >= start && x.StartTime <= end)
            .OrderBy(x => x.StartTime).ToListAsync(ct);
        var ids = events.Select(x => x.Id).ToArray();
        var attendees = await db.EventAttendees.AsNoTracking().CountAsync(x => ids.Contains(x.EventId), ct);
        var now = DateTime.UtcNow;
        var upcoming = events.Where(x => x.StartTime >= now).Take(8).ToList();
        return new(events.Count, events.Count(x => x.Status == ManagedEventStatuses.Completed),
            events.Count(x => x.Status == ManagedEventStatuses.Ongoing || (x.StartTime <= now && x.EndTime >= now)),
            events.Count(x => x.StartTime > now && x.Status != ManagedEventStatuses.Cancelled), attendees,
            upcoming.Select(x => MapList(x, 0)).ToList());
    }

    public async Task<EventDto> GetAsync(Guid id, CancellationToken ct)
    {
        var item = await db.ManagedEvents.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new EntityNotFoundException(typeof(ManagedEvent), id);
        return await MapDetailAsync(item, ct);
    }

    public async Task<EventDto> CreateAsync(CreateManagedEventDto input, CancellationToken ct)
    {
        var code = await NewCodeAsync(ct);
        var item = new ManagedEvent(Guid.NewGuid(), code, input.Group, input.Name, input.Content, input.Description,
            input.Location, input.StartTime, input.EndTime, input.Status, Convert.ToHexString(Guid.NewGuid().ToByteArray()), access.UserId);
        db.ManagedEvents.Add(item);
        await db.SaveChangesAsync(ct);
        return await MapDetailAsync(item, ct);
    }

    public async Task<EventDto> UpdateAsync(Guid id, UpdateManagedEventDto input, CancellationToken ct)
    {
        var item = await db.ManagedEvents.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new EntityNotFoundException(typeof(ManagedEvent), id);
        item.Change(input.Group, input.Name, input.Content, input.Description, input.Location,
            input.StartTime, input.EndTime, input.Status);
        await db.SaveChangesAsync(ct);
        return await MapDetailAsync(item, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var item = await db.ManagedEvents.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new EntityNotFoundException(typeof(ManagedEvent), id);
        db.ManagedEvents.Remove(item);
        await db.SaveChangesAsync(ct);
    }

    public async Task<PagedWorkDto<EventAttendeeDto>> GetAttendeesAsync(Guid eventId, string? filter,
        string? registrationStatus, string? checkInStatus, int skip, int take, CancellationToken ct)
    {
        await EnsureEventAsync(eventId, ct);
        take = Math.Clamp(take, 1, 100); skip = Math.Max(0, skip);
        var query = db.EventAttendees.AsNoTracking().Where(x => x.EventId == eventId);
        if (!string.IsNullOrWhiteSpace(filter))
        {
            var term = filter.Trim().ToLowerInvariant();
            query = query.Where(x => EF.Functions.ILike(x.FullName, $"%{term}%")
                || (x.PhoneNumber != null && EF.Functions.ILike(x.PhoneNumber, $"%{term}%"))
                || (x.Email != null && EF.Functions.ILike(x.Email, $"%{term}%"))
                || (x.Cccd != null && EF.Functions.ILike(x.Cccd, $"%{term}%")));
        }
        if (!string.IsNullOrWhiteSpace(registrationStatus)) query = query.Where(x => x.RegistrationStatus == registrationStatus);
        if (!string.IsNullOrWhiteSpace(checkInStatus)) query = query.Where(x => x.CheckInStatus == checkInStatus);
        var total = await query.LongCountAsync(ct);
        var page = await query.OrderBy(x => x.FullName).Skip(skip).Take(take).ToListAsync(ct);
        return new(total, page.Select(MapAttendee).ToList());
    }

    public async Task<EventAttendeeDto> AddAttendeeAsync(Guid eventId, CreateEventAttendeeDto input, CancellationToken ct)
    {
        await EnsureEventAsync(eventId, ct);
        EnsureManualAttendeeFields(input.FullName, input.PhoneNumber, input.Email, input.UserId);
        await EnsureNotDuplicateAsync(eventId, input.UserId, input.PhoneNumber, input.Email, input.Cccd, null, ct);
        var attendee = new EventAttendee(Guid.NewGuid(), eventId, input.UserId, input.Username, input.Surname, input.Name,
            input.FullName, input.Cccd, input.PhoneNumber, input.Email, input.Address, input.RegistrationStatus,
            input.CheckInStatus, input.Note);
        db.EventAttendees.Add(attendee);
        await db.SaveChangesAsync(ct);
        return MapAttendee(attendee);
    }

    public async Task<EventAttendeeDto> UpdateAttendeeAsync(Guid id, UpdateEventAttendeeDto input, CancellationToken ct)
    {
        var attendee = await db.EventAttendees.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new EntityNotFoundException(typeof(EventAttendee), id);
        EnsureManualAttendeeFields(input.FullName, input.PhoneNumber, input.Email, attendee.UserId);
        await EnsureNotDuplicateAsync(attendee.EventId, attendee.UserId, input.PhoneNumber, input.Email, input.Cccd, id, ct);
        attendee.Change(input.Username, input.Surname, input.Name, input.FullName, input.Cccd, input.PhoneNumber,
            input.Email, input.Address, input.RegistrationStatus, input.CheckInStatus, input.Note);
        await db.SaveChangesAsync(ct);
        return MapAttendee(attendee);
    }

    public async Task<EventAttendeeDto> ChangeAttendeeStatusAsync(Guid id, ChangeEventAttendeeStatusDto input, CancellationToken ct)
    {
        var attendee = await db.EventAttendees.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new EntityNotFoundException(typeof(EventAttendee), id);
        if (!string.IsNullOrWhiteSpace(input.RegistrationStatus)) attendee.SetRegistrationStatus(input.RegistrationStatus);
        if (!string.IsNullOrWhiteSpace(input.CheckInStatus)) attendee.SetCheckInStatus(input.CheckInStatus);
        await db.SaveChangesAsync(ct);
        return MapAttendee(attendee);
    }

    public async Task DeleteAttendeeAsync(Guid id, CancellationToken ct)
    {
        var attendee = await db.EventAttendees.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new EntityNotFoundException(typeof(EventAttendee), id);
        db.EventAttendees.Remove(attendee); await db.SaveChangesAsync(ct);
    }

    public async Task<int> DeleteAttendeesAsync(Guid eventId, IReadOnlyCollection<Guid> ids, CancellationToken ct)
    {
        await EnsureEventAsync(eventId, ct);
        var items = await db.EventAttendees.Where(x => x.EventId == eventId && ids.Contains(x.Id)).ToListAsync(ct);
        db.EventAttendees.RemoveRange(items); await db.SaveChangesAsync(ct); return items.Count;
    }

    public async Task<EventImportResultDto> ImportAsync(Guid eventId, Stream stream, CancellationToken ct)
    {
        await EnsureEventAsync(eventId, ct);
        using var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, leaveOpen: true);
        var header = await reader.ReadLineAsync(ct);
        if (string.IsNullOrWhiteSpace(header)) return new(0, 0);
        var columns = ParseCsv(header).Select(NormalizeHeader).ToList();
        var imported = 0; var skipped = 0;
        while (imported + skipped < 500 && await reader.ReadLineAsync(ct) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var cells = ParseCsv(line);
            string Get(params string[] names) => names.Select(x => columns.IndexOf(NormalizeHeader(x))).Where(x => x >= 0 && x < cells.Count)
                .Select(x => cells[x]).FirstOrDefault() ?? string.Empty;
            var phone = Get("phone", "sdt", "so dien thoai"); var email = Get("email"); var name = Get("fullname", "ho ten", "name");
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(email)
                || await db.EventAttendees.AnyAsync(x => x.EventId == eventId && (x.PhoneNumber == phone || x.Email == email), ct)) { skipped++; continue; }
            var attendee = new EventAttendee(Guid.NewGuid(), eventId, null, Get("username"), Get("surname", "ho"), Get("given name", "ten"),
                name, Get("cccd", "can cuoc"), phone, email, Get("address", "dia chi"),
                NormalizeStatus(Get("registration status", "trang thai dang ky"), EventRegistrationStatuses.Unconfirmed, EventRegistrationStatuses.All),
                NormalizeStatus(Get("checkin status", "trang thai checkin"), EventCheckInStatuses.NotCheckedIn, EventCheckInStatuses.All), Get("note", "ghi chu"));
            db.EventAttendees.Add(attendee); imported++;
        }
        await db.SaveChangesAsync(ct); return new(imported, skipped);
    }

    public async Task<PublicEventDto> GetPublicAsync(string code, string token, CancellationToken ct)
    {
        var item = await db.ManagedEvents.AsNoTracking().SingleOrDefaultAsync(x => x.Code == code && x.QrToken == token, ct)
            ?? throw new EntityNotFoundException(typeof(ManagedEvent), code);
        var attachments = await db.EventAttachments.AsNoTracking()
            .Where(x => x.EventId == item.Id)
            .OrderBy(x => x.FileName)
            .Select(x => new EventAttachmentDto(x.Id, x.FileName, x.ContentType, x.Size))
            .ToListAsync(ct);
        EventAttendee? attendee = null;
        if (AuthenticatedUserId() is { } userId)
        {
            var attendees = await db.EventAttendees.AsNoTracking().Where(x => x.EventId == item.Id).ToListAsync(ct);
            attendee = MatchPublicAttendee(attendees, userId);
        }

        return new(item.Code, item.Name, item.StartTime, item.EndTime, item.Location, attachments, item.Status,
            item.Content, item.Description, attendee?.RegistrationStatus, attendee?.CheckInStatus, attendee?.CheckedInAt);
    }

    public async Task<PublicEventConfirmResultDto> ConfirmPublicAsync(string code, string token,
        PublicEventCheckInDto input, CancellationToken ct)
    {
        var result = await RecordPublicAttendanceAsync(code, token, checkIn: false, EventRegistrationStatuses.Confirmed, ct);
        return new(result.FullName, result.RegistrationStatus);
    }

    public async Task<PublicEventConfirmResultDto> DeclinePublicAsync(string code, string token,
        PublicEventCheckInDto input, CancellationToken ct)
    {
        var result = await RecordPublicAttendanceAsync(code, token, checkIn: false, EventRegistrationStatuses.Declined, ct);
        return new(result.FullName, result.RegistrationStatus);
    }

    public async Task<PublicEventCheckInResultDto> CheckInPublicAsync(string code, string token,
        PublicEventCheckInDto input, CancellationToken ct)
    {
        var result = await RecordPublicAttendanceAsync(code, token, checkIn: true, EventRegistrationStatuses.Confirmed, ct);
        return new(result.FullName, result.CheckedInAt ?? DateTime.UtcNow);
    }

    private async Task<(string FullName, string RegistrationStatus, DateTime? CheckedInAt)> RecordPublicAttendanceAsync(
        string code, string token, bool checkIn, string registrationStatus, CancellationToken ct)
    {
        var item = await db.ManagedEvents.SingleOrDefaultAsync(x => x.Code == code && x.QrToken == token, ct)
            ?? throw new EntityNotFoundException(typeof(ManagedEvent), code);
        EnsurePublicAttendanceAllowed(item, checkIn);

        var authenticatedUserId = AuthenticatedUserId();
        if (authenticatedUserId is null)
        {
            logger.LogWarning("Public event {Action} requires login. EventId={EventId}, EventCode={EventCode}",
                checkIn ? "check-in" : registrationStatus, item.Id, item.Code);
            throw new BusinessException("Work:EventCheckInLoginRequired");
        }

        var rawUsername = currentUser.UserName;
        var rawPhone = currentUser.PhoneNumber;
        var rawEmail = currentUser.Email;
        var phone = NormalizePhone(rawPhone);
        var email = NormalizeEmail(rawEmail);
        var attendees = await db.EventAttendees.Where(x => x.EventId == item.Id).ToListAsync(ct);
        var attendee = MatchPublicAttendee(attendees, authenticatedUserId.Value);
        var status = checkIn ? EventRegistrationStatuses.Confirmed : registrationStatus;

        if (attendee is null)
        {
            var fullName = FirstNonEmpty(
                BuildFullName(currentUser.SurName, currentUser.Name),
                rawUsername,
                rawEmail,
                rawPhone,
                "Guest")!;
            attendee = new EventAttendee(Guid.NewGuid(), item.Id, authenticatedUserId, rawUsername,
                currentUser.SurName, currentUser.Name, fullName, null,
                phone.Length == 0 ? null : rawPhone,
                email.Length == 0 ? null : rawEmail,
                null, status,
                checkIn ? EventCheckInStatuses.CheckedIn : EventCheckInStatuses.NotCheckedIn, null);
            db.EventAttendees.Add(attendee);
            logger.LogInformation(
                "Public event attendee created. EventId={EventId}, EventCode={EventCode}, Status={Status}, CheckIn={CheckIn}, AuthenticatedUser={AuthenticatedUser}",
                item.Id, item.Code, status, checkIn, authenticatedUserId);
        }
        else
        {
            if (attendee.UserId is null) attendee.LinkUser(authenticatedUserId.Value, rawUsername);
            attendee.SetRegistrationStatus(status);
            if (checkIn) attendee.SetCheckInStatus(EventCheckInStatuses.CheckedIn);
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Public event {Action} succeeded. EventId={EventId}, EventCode={EventCode}",
            checkIn ? "check-in" : status, item.Id, item.Code);
        return (attendee.FullName, attendee.RegistrationStatus, attendee.CheckedInAt);
    }

    private Guid? AuthenticatedUserId() =>
        currentUser.IsAuthenticated && currentUser.Id is { } currentId && currentId != Guid.Empty ? currentId : null;

    private EventAttendee? MatchPublicAttendee(IEnumerable<EventAttendee> attendees, Guid userId)
    {
        var username = NormalizeUsername(currentUser.UserName);
        var phone = NormalizePhone(currentUser.PhoneNumber);
        var email = NormalizeEmail(currentUser.Email);
        return attendees.FirstOrDefault(x => x.UserId == userId)
            ?? attendees.FirstOrDefault(x => x.UserId is null &&
                ((phone.Length > 0 && NormalizePhone(x.PhoneNumber) == phone)
                || (email.Length > 0 && NormalizeEmail(x.Email) == email)
                || (username.Length > 0 && NormalizeUsername(x.Username) == username)));
    }

    private static void EnsurePublicAttendanceAllowed(ManagedEvent item, bool checkIn)
    {
        if (checkIn)
        {
            if (!string.Equals(item.Status, ManagedEventStatuses.Ongoing, StringComparison.OrdinalIgnoreCase))
                throw new BusinessException("Work:EventCheckInNotOpen");
            return;
        }

        if (!string.Equals(item.Status, ManagedEventStatuses.Preparing, StringComparison.OrdinalIgnoreCase))
            throw new BusinessException("Work:EventConfirmNotOpen");
    }

    public async Task<byte[]> GetQrCodeAsync(Guid id, string publicUrl, CancellationToken ct)
    {
        var item = await db.ManagedEvents.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (item is null)
        {
            logger.LogWarning("Event QR request references a missing event. EventId={EventId}", id);
            throw new EntityNotFoundException(typeof(ManagedEvent), id);
        }

        try
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode($"{publicUrl.TrimEnd('/')}/event-check-in/{item.Code}?token={item.QrToken}", QRCodeGenerator.ECCLevel.Q);
            var bytes = new PngByteQRCode(data).GetGraphic(8);
            logger.LogInformation("Generated event QR code. EventId={EventId}, EventCode={EventCode}, PublicOrigin={PublicOrigin}, Bytes={Bytes}",
                id, item.Code, publicUrl.TrimEnd('/'), bytes.Length);
            return bytes;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to generate event QR code. EventId={EventId}, EventCode={EventCode}", id, item.Code);
            throw;
        }
    }

    private async Task<EventDto> MapDetailAsync(ManagedEvent item, CancellationToken ct)
    {
        var attendees = await db.EventAttendees.AsNoTracking().Where(x => x.EventId == item.Id).ToListAsync(ct);
        var files = await db.EventAttachments.AsNoTracking().Where(x => x.EventId == item.Id).OrderBy(x => x.FileName).ToListAsync(ct);
        return new(item.Id, item.Code, item.Group, item.Name, item.Content, item.Description, item.Location, item.StartTime,
            item.EndTime, item.Status, item.QrToken, files.Select(x => new EventAttachmentDto(x.Id, x.FileName, x.ContentType, x.Size)).ToList(),
            new(attendees.Count, attendees.Count(x => x.RegistrationStatus == EventRegistrationStatuses.Confirmed),
                attendees.Count(x => x.RegistrationStatus == EventRegistrationStatuses.Unconfirmed), attendees.Count(x => x.RegistrationStatus == EventRegistrationStatuses.Declined),
                attendees.Count(x => x.CheckInStatus == EventCheckInStatuses.CheckedIn), attendees.Count(x => x.CheckInStatus == EventCheckInStatuses.NotCheckedIn)));
    }

    private async Task EnsureEventAsync(Guid id, CancellationToken ct)
    {
        if (!await db.ManagedEvents.AsNoTracking().AnyAsync(x => x.Id == id, ct))
            throw new EntityNotFoundException(typeof(ManagedEvent), id);
    }

    private async Task EnsureNotDuplicateAsync(Guid eventId, Guid? userId, string? phone, string? email, string? cccd,
        Guid? exceptId, CancellationToken ct)
    {
        var normalizedPhone = NormalizePhone(phone);
        var normalizedEmail = NormalizeEmail(email);
        var normalizedCccd = NormalizeIdentity(cccd);
        var hasPhone = normalizedPhone.Length > 0;
        var hasEmail = normalizedEmail.Length > 0;
        var hasCccd = normalizedCccd.Length > 0;
        if (!userId.HasValue && !hasPhone && !hasEmail && !hasCccd) return;

        var attendees = await db.EventAttendees.AsNoTracking()
            .Where(x => x.EventId == eventId && x.Id != exceptId)
            .ToListAsync(ct);
        if ((userId.HasValue && userId != Guid.Empty && attendees.Any(x => x.UserId == userId))
            || attendees.Any(x =>
                (hasPhone && NormalizePhone(x.PhoneNumber) == normalizedPhone)
                || (hasEmail && NormalizeEmail(x.Email) == normalizedEmail)
                || (hasCccd && NormalizeIdentity(x.Cccd) == normalizedCccd)))
            throw new BusinessException("Work:DuplicateEventAttendee");
    }

    private static void EnsureManualAttendeeFields(string fullName, string? phone, string? email, Guid? userId = null)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new BusinessException("Work:EventAttendeeRequiredFields");
        if (userId is { } id && id != Guid.Empty)
            return;
        if (string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(email))
            throw new BusinessException("Work:EventAttendeeRequiredFields");
    }

    private async Task<string> NewCodeAsync(CancellationToken ct)
    {
        for (var i = 0; i < 5; i++)
        {
            var code = $"EVT-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..26].ToUpperInvariant();
            if (!await db.ManagedEvents.AnyAsync(x => x.Code == code, ct)) return code;
        }
        throw new BusinessException("Work:EventCodeGenerationFailed");
    }

    private static EventListItemDto MapList(ManagedEvent x, int count) => new(x.Id, x.Code, x.Group, x.Name, x.StartTime, x.EndTime, x.Location, x.Status, count);
    private static EventAttendeeDto MapAttendee(EventAttendee x) => new(x.Id, x.EventId, x.UserId, x.Username, x.Surname, x.Name, x.FullName,
        x.Cccd, x.PhoneNumber, x.Email, x.Address, x.RegistrationStatus, x.CheckInStatus, x.Note, x.CheckedInAt);
    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim();
    private static string? BuildFullName(string? surname, string? name)
    {
        var value = $"{surname} {name}".Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
    private static string NormalizePhone(string? value)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.StartsWith("0084", StringComparison.Ordinal) && digits.Length == 13)
            return "0" + digits[4..];
        if (digits.StartsWith("84", StringComparison.Ordinal) && digits.Length == 11)
            return "0" + digits[2..];
        return digits;
    }
    private static string NormalizeEmail(string? value) => (value ?? string.Empty).Trim().ToUpperInvariant();
    private static string NormalizeUsername(string? value) => (value ?? string.Empty).Trim().ToUpperInvariant();
    private static string NormalizeIdentity(string? value) =>
        new string((value ?? string.Empty).Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
    private static string NormalizeStatus(string value, string fallback, IReadOnlyCollection<string> allowed) =>
        allowed.FirstOrDefault(x => string.Equals(x, value?.Trim(), StringComparison.OrdinalIgnoreCase)) ?? fallback;
    private static string NormalizeHeader(string value)
    {
        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        return new string(decomposed.Where(x => CharUnicodeInfo.GetUnicodeCategory(x) != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(x)).ToArray()).ToLowerInvariant();
    }
    private static List<string> ParseCsv(string line)
    {
        var result = new List<string>(); var cell = new StringBuilder(); var quoted = false;
        foreach (var ch in line)
        {
            if (ch == '"') { quoted = !quoted; continue; }
            if (ch == ',' && !quoted) { result.Add(cell.ToString().Trim()); cell.Clear(); } else cell.Append(ch);
        }
        result.Add(cell.ToString().Trim()); return result;
    }
}
