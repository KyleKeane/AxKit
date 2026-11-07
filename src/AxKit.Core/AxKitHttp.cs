using System.Net.Http.Headers;

namespace AxKit.Core;

/// <summary>
/// The one HTTP client of axKit's own code (docs/axkit/SPEC.md AX-11.11, B-15): the updater's check and downloads
/// (AX-3) and the report sender (docs/axkit/reports.md RP-6), both to GitHub over HTTPS and nothing else. Ten
/// minutes for a large zip; each call sets its own shorter limit through its cancellation. The User-Agent is
/// "AxKit/&lt;version&gt;", and nothing else about the user travels in a header.
/// </summary>
public static class AxKitHttp
{
    public static HttpClient Client { get; } = Create();

    private static HttpClient Create()
    {
        // Outside call, network, GitHub: the one HTTP client of axKit, for the update check, the two downloads and the
        // reports; no other code makes a network request (AX-11.11).
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("AxKit", BundleInfo.Version));
        return client;
    }
}
