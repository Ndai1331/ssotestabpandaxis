using System.Net.Http.Headers;
using System.Net.Http.Json;
using IdentityModel.Client;
using Microsoft.Extensions.Configuration;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;

namespace hanhchinhso.DocumentService.Workflows;

public class WorkflowIdentityClient : ITransientDependency
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ICurrentTenant _currentTenant;

    public WorkflowIdentityClient(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ICurrentTenant currentTenant)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _currentTenant = currentTenant;
    }

    public async Task<TResponse> PostAsync<TRequest, TResponse>(
        string route,
        TRequest input,
        CancellationToken cancellationToken = default)
    {
        var authority = Required("AuthServer:Authority").TrimEnd('/');
        var identityBaseUrl = Required(
            "RemoteServices:AbpIdentity:BaseUrl").TrimEnd('/');
        var clientId = _configuration["IdentityValidation:ClientId"]
            ?? "DocumentService.Internal";
        var clientSecret = Required("IdentityValidation:ClientSecret");
        var client = _httpClientFactory.CreateClient(
            "DocumentService.IdentityValidation");

        TokenResponse token;
        try
        {
            token = await client.RequestClientCredentialsTokenAsync(
                new ClientCredentialsTokenRequest
                {
                    Address = $"{authority}/connect/token",
                    ClientId = clientId,
                    ClientSecret = clientSecret,
                    Scope = "IdentityService"
                },
                cancellationToken);
        }
        catch (HttpRequestException)
        {
            throw Unavailable();
        }
        if (token.IsError || token.AccessToken.IsNullOrWhiteSpace())
        {
            throw Unavailable();
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{identityBaseUrl}/{route.TrimStart('/')}");
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token.AccessToken);
        if (_currentTenant.Id.HasValue)
        {
            request.Headers.TryAddWithoutValidation(
                "__tenant",
                _currentTenant.Id.Value.ToString("D"));
        }
        request.Content = JsonContent.Create(input);

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw Unavailable();
            }
            return await response.Content.ReadFromJsonAsync<TResponse>(
                       cancellationToken: cancellationToken)
                   ?? throw Unavailable();
        }
        catch (HttpRequestException)
        {
            throw Unavailable();
        }
    }

    private string Required(string key) =>
        _configuration[key].IsNullOrWhiteSpace()
            ? throw Unavailable()
            : _configuration[key]!;

    private static UserFriendlyException Unavailable() =>
        new("Identity workflow resolution is unavailable. No workflow changes were saved.");
}
