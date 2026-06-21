using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.RegularExpressions;
using FlowHub.Core.Captures;
using FlowHub.Core.Skills;
using Microsoft.Extensions.Logging;
// Disambiguate the domain Capture from System.Text.RegularExpressions.Capture.
using Capture = FlowHub.Core.Captures.Capture;

namespace FlowHub.Skills.Wallabag;

public sealed partial class WallabagSkillIntegration : ISkillIntegration
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly WallabagTokenProvider _tokenProvider;
    private readonly ILogger<WallabagSkillIntegration> _log;
    private readonly Func<string, CancellationToken, Task<IPAddress[]>> _resolveHost;

    public WallabagSkillIntegration(
        HttpClient http,
        WallabagTokenProvider tokenProvider,
        ILogger<WallabagSkillIntegration> log,
        // The host resolver is injectable so the SSRF guard's IP classification can be
        // exercised deterministically and offline; production uses real DNS resolution.
        Func<string, CancellationToken, Task<IPAddress[]>>? resolveHost = null)
    {
        _http = http;
        _tokenProvider = tokenProvider;
        _log = log;
        _resolveHost = resolveHost ?? ((host, ct) => Dns.GetHostAddressesAsync(host, ct));
    }

    public string Name => "Wallabag";

    public async Task<SkillResult> HandleAsync(Capture capture, CancellationToken cancellationToken)
    {
        if (!TryExtractUrl(capture.Content, out var uri))
        {
            throw new InvalidOperationException(
                $"Capture {capture.Id} content contains no http(s) url to save: '{capture.Content}'");
        }

        // SSRF guard: Wallabag fetches this URL server-side from inside the internal
        // network, so a capture pointing at a private/link-local address (e.g. cloud
        // metadata 169.254.169.254 or an internal service) would let a visitor reach
        // hosts FlowHub can see. Refuse non-publicly-routable targets before handing
        // the URL off. This is defence-in-depth — network egress isolation remains the
        // primary control, since Wallabag re-resolves DNS and may follow redirects.
        if (!await IsPubliclyRoutableAsync(uri, cancellationToken))
        {
            throw new InvalidOperationException(
                $"Capture {capture.Id} target '{uri}' resolves to a non-public address; refusing to fetch (SSRF guard).");
        }

        var token = await _tokenProvider.GetTokenAsync(cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/entries.json")
        {
            Content = JsonContent.Create(new { url = uri.ToString() }, options: JsonOptions),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<WallabagEntryResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Wallabag response body was empty.");

        if (payload.Id is null)
        {
            throw new InvalidOperationException("Wallabag response did not include an 'id' field.");
        }

        return new SkillResult(Success: true, ExternalRef: payload.Id.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Extracts the first absolute http(s) URL contained in the capture content.
    /// A read-later capture often arrives as free text (e.g. "save &lt;url&gt; to read
    /// later"), so the URL must be located within the content rather than assuming
    /// the whole content is a URL. Trailing sentence punctuation is trimmed.
    /// </summary>
    private static bool TryExtractUrl(string content, [NotNullWhen(true)] out Uri? uri)
    {
        uri = null;
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        foreach (Match match in UrlRegex().Matches(content))
        {
            var candidate = match.Value.TrimEnd('.', ',', ';', ':', '!', '?', ')', ']', '}', '"', '\'', '>');
            if (Uri.TryCreate(candidate, UriKind.Absolute, out var parsed)
                && (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps))
            {
                uri = parsed;
                return true;
            }
        }

        return false;
    }

    [GeneratedRegex(@"https?://\S+", RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegex();

    /// <summary>
    /// True only when the URL's host resolves exclusively to publicly-routable
    /// addresses. IP-literal hosts are classified directly; named hosts are resolved
    /// via the injected resolver. An unresolvable host is treated as non-routable
    /// (fail closed) rather than passed through to Wallabag.
    /// </summary>
    private async Task<bool> IsPubliclyRoutableAsync(Uri uri, CancellationToken ct)
    {
        IPAddress[] addresses;
        if (IPAddress.TryParse(uri.Host, out var literal))
        {
            addresses = [literal];
        }
        else
        {
            try
            {
                addresses = await _resolveHost(uri.Host, ct);
            }
            catch (SocketException)
            {
                return false;
            }
        }

        return addresses.Length > 0 && Array.TrueForAll(addresses, IsPublic);
    }

    /// <summary>
    /// Classifies a single address as publicly routable, rejecting loopback,
    /// link-local (incl. the 169.254.169.254 cloud-metadata IP), private (RFC 1918),
    /// CGNAT, unspecified, and the IPv6 equivalents (ULA, link/site-local).
    /// </summary>
    private static bool IsPublic(IPAddress address)
    {
        var ip = address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;

        if (IPAddress.IsLoopback(ip))
        {
            return false;
        }

        if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal || ip.IsIPv6UniqueLocal)
        {
            return false;
        }

        if (ip.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = ip.GetAddressBytes();
            return (b[0], b[1]) switch
            {
                (10, _) => false,                 // 10.0.0.0/8
                (127, _) => false,                // loopback (defensive)
                (169, 254) => false,              // link-local incl. 169.254.169.254 metadata
                (172, >= 16 and <= 31) => false,  // 172.16.0.0/12
                (192, 168) => false,              // 192.168.0.0/16
                (100, >= 64 and <= 127) => false, // 100.64.0.0/10 CGNAT
                (0, _) => false,                  // 0.0.0.0/8 unspecified
                _ => true,
            };
        }

        return true; // globally-routable IPv6
    }

    private sealed record WallabagEntryResponse(long? Id);
}
