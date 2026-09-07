using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace HCS.DocumentService.Workflows;

public sealed class HttpWorkflowAssigneeResolver(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContext,
    IConfiguration configuration) : IWorkflowAssigneeResolver
{
    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<WorkflowAssigneeCandidateDto>>> ResolveByRolesAsync(
        IReadOnlyCollection<Guid> roleIds, Guid submitterUserId, CancellationToken cancellationToken = default)
    {
        var ids = roleIds.Where(x => x != Guid.Empty).Distinct().Take(100).ToArray();
        if (ids.Length == 0 || string.IsNullOrWhiteSpace(configuration["Services:Platform:BaseUrl"]) || submitterUserId == Guid.Empty)
            return new Dictionary<Guid, IReadOnlyList<WorkflowAssigneeCandidateDto>>();

        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"api/identity/workflow-assignees/roles?{string.Join('&', ids.Select(x => $"roleIds={x:D}"))}");
        AddAuthorization(request);
        try
        {
            using var response = await httpClientFactory.CreateClient("HCS.Platform").SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode) return new Dictionary<Guid, IReadOnlyList<WorkflowAssigneeCandidateDto>>();
            var payload = await response.Content.ReadFromJsonAsync<Dictionary<Guid, List<WorkflowAssigneeCandidateDto>>>(cancellationToken: cancellationToken);
            return payload?.ToDictionary(x => x.Key, x => (IReadOnlyList<WorkflowAssigneeCandidateDto>)x.Value)
                ?? new Dictionary<Guid, IReadOnlyList<WorkflowAssigneeCandidateDto>>();
        }
        catch (HttpRequestException) { return new Dictionary<Guid, IReadOnlyList<WorkflowAssigneeCandidateDto>>(); }
        catch (System.Text.Json.JsonException) { return new Dictionary<Guid, IReadOnlyList<WorkflowAssigneeCandidateDto>>(); }
    }

    public async Task<IReadOnlyDictionary<Guid, WorkflowAssigneeCandidateDto?>> ResolveByUsersAsync(
        IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default)
    {
        var ids = userIds.Where(x => x != Guid.Empty).Distinct().Take(200).ToArray();
        if (ids.Length == 0 || string.IsNullOrWhiteSpace(configuration["Services:Platform:BaseUrl"]))
            return new Dictionary<Guid, WorkflowAssigneeCandidateDto?>();

        using var request = new HttpRequestMessage(HttpMethod.Get,
            $"api/identity/workflow-assignees/lookup?{string.Join('&', ids.Select(x => $"userIds={x:D}"))}");
        AddAuthorization(request);
        try
        {
            using var response = await httpClientFactory.CreateClient("HCS.Platform").SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode) return new Dictionary<Guid, WorkflowAssigneeCandidateDto?>();
            var payload = await response.Content.ReadFromJsonAsync<List<WorkflowAssigneeCandidateDto>>(cancellationToken: cancellationToken) ?? [];
            return ids.ToDictionary(id => id, id => payload.FirstOrDefault(x => x.UserId == id));
        }
        catch (HttpRequestException) { return new Dictionary<Guid, WorkflowAssigneeCandidateDto?>(); }
        catch (System.Text.Json.JsonException) { return new Dictionary<Guid, WorkflowAssigneeCandidateDto?>(); }
    }

    public async Task<IReadOnlyList<WorkflowAssigneeCandidateDto>> ResolveByRoleAsync(
        Guid roleId, Guid submitterUserId, CancellationToken cancellationToken = default)
    {
        var result = await ResolveByRolesAsync([roleId], submitterUserId, cancellationToken);
        return result.GetValueOrDefault(roleId) ?? [];
    }

    public async Task<WorkflowAssigneeCandidateDto?> ResolveByUserAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var result = await ResolveByUsersAsync([userId], cancellationToken);
        return result.GetValueOrDefault(userId);
    }

    private void AddAuthorization(HttpRequestMessage request)
    {
        var token = httpContext.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.TryAddWithoutValidation("Authorization", token);
    }
}
