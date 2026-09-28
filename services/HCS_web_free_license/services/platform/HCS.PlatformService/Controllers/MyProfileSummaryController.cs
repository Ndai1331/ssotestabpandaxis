using System.Net.Http.Json;
using System.Text.Json;
using HCS.CollaborationService.Contracts;
using HCS.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Users;

namespace HCS.PlatformService.Controllers;

public sealed record MyProfileDepartmentDto(Guid Id, string? Name, bool IsPrimary);

public sealed record MyProfileSummaryDto(
    Guid Id,
    string UserName,
    string? Email,
    string? Name,
    string? Surname,
    string? PhoneNumber,
    string DisplayName,
    string? AvatarUrl,
    IReadOnlyList<MyProfileDepartmentDto> Departments,
    Guid? PositionId,
    string? PositionName);

/// <summary>Mobile profile header: identity fields plus the caller's own department/position mappings.</summary>
[ApiController, Authorize, Route("api/identity/my-profile-summary")]
public sealed class MyProfileSummaryController(
    ICurrentUser currentUser,
    IIdentityUserRepository identityUsers,
    HCSDbContext db,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<MyProfileSummaryController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<MyProfileSummaryDto>> Get(CancellationToken cancellationToken)
    {
        if (currentUser.Id is not { } userId) return Unauthorized();
        var user = await identityUsers.FindAsync(userId, includeDetails: false, cancellationToken);
        if (user is null) return NotFound();

        var hasAvatar = await db.UserAvatars.AsNoTracking().AnyAsync(x => x.UserId == userId, cancellationToken);
        var mappings = await GetOrganizationMappingsAsync(cancellationToken);
        var departments = mappings
            .Where(x => x.DepartmentId.HasValue)
            .GroupBy(x => x.DepartmentId!.Value)
            .Select((group, index) => new MyProfileDepartmentDto(group.Key, group.First().DepartmentName, index == 0))
            .ToArray();
        var position = mappings.FirstOrDefault(x => x.PositionId.HasValue);

        return Ok(new MyProfileSummaryDto(
            user.Id,
            user.UserName,
            user.Email,
            user.Name,
            user.Surname,
            user.PhoneNumber,
            UserDisplayNames.FromPerson(user.Surname, user.Name, user.UserName),
            hasAvatar ? $"/api/identity/users/{user.Id:D}/avatar" : null,
            departments,
            position?.PositionId,
            position?.PositionName));
    }

    private async Task<IReadOnlyList<OrganizationMappingDto>> GetOrganizationMappingsAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(configuration["Services:Organization:BaseUrl"])) return [];
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/organization/user-departments/mine");
        var token = Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(token)) request.Headers.TryAddWithoutValidation("Authorization", token);
        try
        {
            using var response = await httpClientFactory.CreateClient("HCS.Organization").SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Organization self lookup returned {StatusCode}", (int)response.StatusCode);
                return [];
            }
            return await response.Content.ReadFromJsonAsync<List<OrganizationMappingDto>>(cancellationToken) ?? [];
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException
                                              && !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Organization self lookup failed");
            return [];
        }
    }

    private sealed record OrganizationMappingDto(Guid UserId, Guid? DepartmentId, string? DepartmentName,
        Guid? PositionId, string? PositionName);
}
