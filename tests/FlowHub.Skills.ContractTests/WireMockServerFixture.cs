using WireMock.Server;

namespace FlowHub.Skills.ContractTests;

/// <summary>
/// xUnit per-class fixture that boots a WireMock.Net HTTP server on a random
/// loopback port, then tears it down after the class runs. Tests get a real
/// HttpClient against a real socket, so wire-level concerns (headers, JSON
/// shape, status codes, retries) are exercised end-to-end without secrets
/// or external network.
/// </summary>
public sealed class WireMockServerFixture : IDisposable
{
    public WireMockServer Server { get; }

    public string BaseUrl => Server.Url!;

    public WireMockServerFixture()
    {
        Server = WireMockServer.Start();
    }

    public void Reset() => Server.Reset();

    public void Dispose()
    {
        Server.Stop();
        Server.Dispose();
    }
}
