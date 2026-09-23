using System.Net.Http.Json;
using HCS.OrganizationService.Application;

namespace HCS.OrganizationService.Host.Integration;

public sealed class HttpUnitDepartmentLookup(
    HttpClient client, IHttpContextAccessor context) : IUnitDepartmentLookup
{
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty) return false;
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/identity/organization-unit-lookup");
        var authorization = context.HttpContext?.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authorization))
            throw new UnauthorizedAccessException();
        request.Headers.TryAddWithoutValidation("Authorization", authorization);
        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var units = await response.Content.ReadFromJsonAsync<List<LookupItem>>(cancellationToken: cancellationToken);
        return units?.Any(unit => unit.Id == id) == true;
    }

    private sealed record LookupItem(Guid Id);
}
