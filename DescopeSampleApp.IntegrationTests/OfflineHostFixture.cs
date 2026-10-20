using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace DescopeSampleApp.IntegrationTests;

/// <summary>All application HTTP dependencies in tests use this local handler, never a live endpoint.</summary>
public sealed class OfflineHostFixture : WebApplicationFactory<Program>
{
    public const string JwksUrl = "https://jwks.example.test/.well-known/jwks.json";
    public const string JwksClientName = "Jwks";
    public const string EmptyJwks = """{"keys":[]}""";

    private readonly string _jwksJson;
    private readonly bool _failFetch;

    public OfflineHostFixture(string jwksJson = EmptyJwks, bool failFetch = false)
    {
        _jwksJson = jwksJson;
        _failFetch = failFetch;
    }

    public RejectingJwksHandler Outbound => new(_jwksJson);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Descope:JwksUrl"] = JwksUrl,
            }));
        builder.ConfigureTestServices(services =>
        {
            // Supply both common injection paths until the token validator chooses its HTTP abstraction.
            services.AddSingleton(new HttpClient(new RejectingJwksHandler(_jwksJson, _failFetch)));
            services.AddHttpClient(string.Empty)
                .ConfigurePrimaryHttpMessageHandler(() => new RejectingJwksHandler(_jwksJson, _failFetch));
            services.AddHttpClient(JwksClientName)
                .ConfigurePrimaryHttpMessageHandler(() => new RejectingJwksHandler(_jwksJson, _failFetch));
        });
    }
}

/// <summary>Only the explicitly declared JWKS URL is accepted; unexpected requests fail immediately.</summary>
public sealed class RejectingJwksHandler : HttpMessageHandler
{
    private readonly string _jwksJson;
    private readonly bool _failFetch;

    public RejectingJwksHandler(string jwksJson = OfflineHostFixture.EmptyJwks, bool failFetch = false)
    {
        _jwksJson = jwksJson;
        _failFetch = failFetch;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Method != HttpMethod.Get || request.RequestUri?.AbsoluteUri != OfflineHostFixture.JwksUrl)
        {
            throw new InvalidOperationException($"Unexpected outbound HTTP request: {request.Method} {request.RequestUri}");
        }
        if (_failFetch)
            throw new HttpRequestException("Simulated offline JWKS transport failure.");

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(_jwksJson, Encoding.UTF8, "application/json"),
        });
    }
}
