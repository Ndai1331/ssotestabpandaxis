using System.Net.Http.Json;
using HCS.Coding;
using HCS.Settings;
using Volo.Abp.DependencyInjection;

namespace HCS.OrganizationService.Host.Integration;

public sealed class PlatformAutoCodeSettings(
    IHttpClientFactory httpClientFactory,
    IHttpContextAccessor httpContext) : IAutoCodeSettings, ISingletonDependency
{
    private readonly object sync = new();
    private Snapshot? snapshot;
    private DateTime nextRefreshUtc = DateTime.MinValue;

    public async Task<string> GetPrefixAsync(AutoCodeKind kind, CancellationToken cancellationToken = default)
    {
        var current = await CurrentAsync(cancellationToken);
        return kind switch
        {
            AutoCodeKind.Project => HCSSettings.ParseAutoCodePrefix(current.ProjectCodePrefix, kind),
            AutoCodeKind.Task => HCSSettings.ParseAutoCodePrefix(current.TaskCodePrefix, kind),
            AutoCodeKind.Document => HCSSettings.ParseAutoCodePrefix(current.DocumentCodePrefix, kind),
            AutoCodeKind.PersonalDocument => HCSSettings.ParseAutoCodePrefix(current.PersonalDocumentCodePrefix, kind),
            AutoCodeKind.Archive => HCSSettings.ParseAutoCodePrefix(current.ArchiveNumberPrefix, kind),
            AutoCodeKind.PersonalArchive => HCSSettings.ParseAutoCodePrefix(current.PersonalArchiveNumberPrefix, kind),
            AutoCodeKind.Workflow => HCSSettings.ParseAutoCodePrefix(current.WorkflowCodePrefix, kind),
            AutoCodeKind.Catalog => HCSSettings.ParseAutoCodePrefix(current.CatalogCodePrefix, kind),
            _ => AutoCode.DefaultPrefix(kind)
        };
    }

    private async Task<Snapshot> CurrentAsync(CancellationToken cancellationToken)
    {
        lock (sync)
        {
            if (snapshot is not null && DateTime.UtcNow < nextRefreshUtc)
            {
                return snapshot;
            }
        }

        var loaded = await LoadAsync(cancellationToken) ?? new Snapshot();
        lock (sync)
        {
            snapshot = loaded;
            nextRefreshUtc = DateTime.UtcNow.AddSeconds(20);
        }

        return loaded;
    }

    private async Task<Snapshot?> LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            var client = httpClientFactory.CreateClient("HCS.Platform");
            if (client.BaseAddress is null)
            {
                return null;
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, "api/hcs/system-features");
            var authorization = httpContext.HttpContext?.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrWhiteSpace(authorization))
            {
                request.Headers.TryAddWithoutValidation("Authorization", authorization);
            }

            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<Snapshot>(cancellationToken: cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    private sealed class Snapshot
    {
        public string? ProjectCodePrefix { get; set; }
        public string? TaskCodePrefix { get; set; }
        public string? DocumentCodePrefix { get; set; }
        public string? PersonalDocumentCodePrefix { get; set; }
        public string? ArchiveNumberPrefix { get; set; }
        public string? PersonalArchiveNumberPrefix { get; set; }
        public string? WorkflowCodePrefix { get; set; }
        public string? CatalogCodePrefix { get; set; }
    }
}
