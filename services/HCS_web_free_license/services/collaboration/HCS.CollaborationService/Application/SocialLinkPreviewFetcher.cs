using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace HCS.CollaborationService.Application;

public sealed record SocialLinkPreviewData(string Url, string? Title, string? Description,
    string? SiteName, string? ImageUrl);

public sealed class SocialLinkPreviewFetcher(HttpClient httpClient, ILogger<SocialLinkPreviewFetcher> logger)
{
    private const int MaxHtmlBytes = 512 * 1024;
    private static readonly Regex MetaTagRegex = new(@"<meta\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex AttributeRegex = new(@"(?<name>[\w:-]+)\s*=\s*(?<quote>[""'])(?<value>.*?)\k<quote>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline);
    private static readonly Regex TitleRegex = new(@"<title\b[^>]*>(?<value>.*?)</title\s*>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline);
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.CultureInvariant);

    public async Task<SocialLinkPreviewData?> FetchAsync(string url, CancellationToken ct = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !await IsSafeRemoteUriAsync(uri, ct))
            return null;

        var currentUri = uri;
        try
        {
            for (var redirect = 0; redirect <= 3; redirect++)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, currentUri);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
                request.Headers.UserAgent.ParseAdd("HCS-SocialPreview/1.0");
                using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
                if (response.StatusCode is >= HttpStatusCode.MovedPermanently and <= HttpStatusCode.PermanentRedirect)
                {
                    if (redirect == 3 || response.Headers.Location is not { } location ||
                        !Uri.TryCreate(currentUri, location, out var redirectedUri) || redirectedUri is null ||
                        !await IsSafeRemoteUriAsync(redirectedUri, ct))
                        break;
                    currentUri = redirectedUri;
                    continue;
                }

                if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength is > MaxHtmlBytes)
                    return new SocialLinkPreviewData(currentUri.AbsoluteUri, null, null, currentUri.Host, null);

                var mediaType = response.Content.Headers.ContentType?.MediaType;
                if (mediaType is not null && !mediaType.Equals("text/html", StringComparison.OrdinalIgnoreCase) &&
                    !mediaType.Equals("application/xhtml+xml", StringComparison.OrdinalIgnoreCase))
                    return new SocialLinkPreviewData(currentUri.AbsoluteUri, null, null, currentUri.Host, null);

                await using var stream = await response.Content.ReadAsStreamAsync(ct);
                var html = await ReadAtMostAsync(stream, MaxHtmlBytes, ct);
                var title = ReadMeta(html, "og:title") ?? ReadTitle(html);
                var description = ReadMeta(html, "og:description") ?? ReadMeta(html, "description");
                var siteName = ReadMeta(html, "og:site_name") ?? currentUri.Host;
                var image = ReadMeta(html, "og:image") ?? ReadMeta(html, "twitter:image");
                var imageUrl = await ResolveImageUriAsync(currentUri, image, ct);

                return new SocialLinkPreviewData(currentUri.AbsoluteUri, Clean(title, 512), Clean(description, 2000),
                    Clean(siteName, 256), imageUrl);
            }

            return new SocialLinkPreviewData(currentUri.AbsoluteUri, null, null, currentUri.Host, null);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return new SocialLinkPreviewData(uri.AbsoluteUri, null, null, uri.Host, null);
        }
        catch (Exception exception)
        {
            logger.LogDebug(exception, "Social link preview could not be fetched for host {Host}.", uri.Host);
            return new SocialLinkPreviewData(uri.AbsoluteUri, null, null, uri.Host, null);
        }
    }

    private static async Task<string> ReadAtMostAsync(Stream stream, int maxBytes, CancellationToken ct)
    {
        var buffer = new byte[8192];
        await using var content = new MemoryStream();
        var total = 0;
        while (total < maxBytes)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(0, Math.Min(buffer.Length, maxBytes - total)), ct);
            if (read == 0)
                break;
            await content.WriteAsync(buffer.AsMemory(0, read), ct);
            total += read;
        }

        return Encoding.UTF8.GetString(content.ToArray());
    }

    private static string? ReadMeta(string html, string key)
    {
        foreach (Match tag in MetaTagRegex.Matches(html))
        {
            var name = ReadAttribute(tag.Value, "property") ?? ReadAttribute(tag.Value, "name");
            if (!string.Equals(name, key, StringComparison.OrdinalIgnoreCase))
                continue;
            return WebUtility.HtmlDecode(ReadAttribute(tag.Value, "content") ?? string.Empty);
        }

        return null;
    }

    private static string? ReadTitle(string html)
    {
        var match = TitleRegex.Match(html);
        return match.Success ? WebUtility.HtmlDecode(match.Groups["value"].Value) : null;
    }

    private static string? ReadAttribute(string tag, string attribute)
    {
        foreach (Match match in AttributeRegex.Matches(tag))
        {
            if (match.Groups["name"].Value.Equals(attribute, StringComparison.OrdinalIgnoreCase))
                return match.Groups["value"].Value;
        }

        return null;
    }

    private static string? Clean(string? value, int maxLength)
    {
        var cleaned = WhitespaceRegex.Replace(value ?? string.Empty, " ").Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned[..Math.Min(cleaned.Length, maxLength)];
    }

    private static async Task<string?> ResolveImageUriAsync(Uri baseUri, string? image, CancellationToken ct)
    {
        if (!Uri.TryCreate(baseUri, image?.Trim(), out var imageUri) ||
            (imageUri.Scheme != Uri.UriSchemeHttp && imageUri.Scheme != Uri.UriSchemeHttps))
            return null;

        try
        {
            return await IsSafeRemoteUriAsync(imageUri, ct) ? imageUri.AbsoluteUri : null;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return null;
        }
        catch (SocketException)
        {
            return null;
        }
    }

    private static async Task<bool> IsSafeRemoteUriAsync(Uri uri, CancellationToken ct)
    {
        if ((uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            !string.IsNullOrEmpty(uri.UserInfo) || (uri.Port is not (-1 or 80 or 443)))
            return false;
        if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            uri.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase) ||
            uri.Host.EndsWith(".local", StringComparison.OrdinalIgnoreCase) ||
            uri.Host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase))
            return false;

        if (IPAddress.TryParse(uri.DnsSafeHost, out var address))
            return IsPublicAddress(address);

        try
        {
            var addresses = await Dns.GetHostAddressesAsync(uri.DnsSafeHost, ct);
            return addresses.Length > 0 && addresses.All(IsPublicAddress);
        }
        catch (SocketException)
        {
            return false;
        }
    }

    private static bool IsPublicAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any) ||
            address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast)
            return false;

        var bytes = address.IsIPv4MappedToIPv6 ? address.MapToIPv4().GetAddressBytes() : address.GetAddressBytes();
        if (bytes.Length == 4)
        {
            var first = bytes[0];
            var second = bytes[1];
            return first != 0 && first != 10 && first != 127 &&
                !(first == 100 && second is >= 64 and <= 127) &&
                !(first == 169 && second == 254) &&
                !(first == 172 && second is >= 16 and <= 31) &&
                !(first == 192 && second == 168) &&
                !(first == 192 && second == 0 && bytes[2] == 0) &&
                !(first == 198 && second is 18 or 19) &&
                !(first == 198 && second == 51 && bytes[2] == 100) &&
                !(first == 203 && second == 0 && bytes[2] == 113) &&
                first < 224;
        }

        return (bytes[0] & 0xfe) != 0xfc && !(bytes[0] == 0xfe && (bytes[1] & 0xc0) == 0x80);
    }
}
