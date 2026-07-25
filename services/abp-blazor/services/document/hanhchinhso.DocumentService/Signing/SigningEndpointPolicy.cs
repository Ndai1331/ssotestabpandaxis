using System.Net;
using Microsoft.Extensions.Configuration;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace hanhchinhso.DocumentService.Signing;

public interface ISigningEndpointPolicy
{
    void Validate(string endpoint);
}

public sealed class SigningEndpointPolicy :
    ISigningEndpointPolicy,
    ITransientDependency
{
    private readonly HashSet<string> _allowedHosts;
    private readonly HashSet<string> _allowedHttpHosts;
    private readonly HashSet<string> _allowedPrivateHosts;

    public SigningEndpointPolicy(IConfiguration configuration)
    {
        _allowedHosts = ReadHosts(configuration, "Signing:AllowedHosts");
        _allowedHttpHosts = ReadHosts(
            configuration, "Signing:AllowedHttpHosts");
        _allowedPrivateHosts = ReadHosts(
            configuration, "Signing:AllowedPrivateHosts");
    }

    public void Validate(string endpoint)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttps &&
             uri.Scheme != Uri.UriSchemeHttp) ||
            !uri.UserInfo.IsNullOrWhiteSpace() ||
            !uri.Query.IsNullOrWhiteSpace() ||
            !uri.Fragment.IsNullOrWhiteSpace())
        {
            throw new BusinessException(
                "DocumentService:InvalidSigningEndpoint");
        }
        var host = uri.IdnHost.ToLowerInvariant();
        if (_allowedHosts.Count == 0)
        {
            throw new BusinessException(
                "DocumentService:SigningEndpointAllowlistNotConfigured");
        }
        if (!_allowedHosts.Contains(host))
        {
            throw new BusinessException(
                "DocumentService:SigningEndpointHostNotAllowed");
        }
        if (uri.Scheme == Uri.UriSchemeHttp &&
            !_allowedHttpHosts.Contains(host))
        {
            throw new BusinessException(
                "DocumentService:SigningEndpointHttpsRequired");
        }
        if (IPAddress.TryParse(host, out var address) &&
            SigningNetworkPolicy.IsPrivateOrLocal(address) &&
            !_allowedPrivateHosts.Contains(host))
        {
            throw new BusinessException(
                "DocumentService:SigningEndpointPrivateAddressForbidden");
        }
        if ((host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
             host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)) &&
            !_allowedPrivateHosts.Contains(host))
        {
            throw new BusinessException(
                "DocumentService:SigningEndpointPrivateAddressForbidden");
        }
    }

    private static HashSet<string> ReadHosts(
        IConfiguration configuration,
        string key) =>
        configuration.GetSection(key).Get<string[]>()?
            .Select(x => x.Trim().ToLowerInvariant())
            .Where(x => x.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase) ??
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

}

public static class SigningNetworkPolicy
{
    public static bool IsPrivateOrLocal(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            return IsPrivateOrLocal(address.MapToIPv4());
        }
        if (IPAddress.IsLoopback(address) ||
            address.Equals(IPAddress.Any) ||
            address.Equals(IPAddress.IPv6Any) ||
            address.IsIPv6LinkLocal ||
            address.IsIPv6SiteLocal ||
            address.IsIPv6Multicast)
        {
            return true;
        }
        if (address.AddressFamily ==
            System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            var bytes = address.GetAddressBytes();
            return (bytes[0] & 0xfe) == 0xfc;
        }
        var octets = address.GetAddressBytes();
        return octets[0] == 0 ||
               octets[0] == 10 ||
               (octets[0] == 100 &&
                octets[1] is >= 64 and <= 127) ||
               octets[0] == 127 ||
               (octets[0] == 169 && octets[1] == 254) ||
               (octets[0] == 172 && octets[1] is >= 16 and <= 31) ||
               (octets[0] == 192 && octets[1] == 0 &&
                octets[2] is 0 or 2) ||
               (octets[0] == 192 && octets[1] == 168) ||
               (octets[0] == 198 && octets[1] is 18 or 19) ||
               (octets[0] == 198 && octets[1] == 51 &&
                octets[2] == 100) ||
               (octets[0] == 203 && octets[1] == 0 &&
                octets[2] == 113) ||
               octets[0] >= 224;
    }

    public static void EnsureAddressAllowed(
        string host,
        IPAddress address,
        IReadOnlySet<string> allowedPrivateHosts)
    {
        if (IsPrivateOrLocal(address) &&
            !allowedPrivateHosts.Contains(host))
        {
            throw new HttpRequestException(
                "The signing endpoint resolved to a forbidden address.");
        }
    }
}
