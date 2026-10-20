using System.Net;
using System.Text;
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

    public OfflineHostFixture(string jwksJson = EmptyJwks)
    {
        _jwksJson = jwksJson;
    }

    public RejectingJwksHandler Outbound => new(_jwksJson);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Supply both common injection paths until the token validator chooses its HTTP abstraction.
            services.AddSingleton(new HttpClient(new RejectingJwksHandler(_jwksJson)));
            services.AddHttpClient(string.Empty)
                .ConfigurePrimaryHttpMessageHandler(() => new RejectingJwksHandler(_jwksJson));
            services.AddHttpClient(JwksClientName)
                .ConfigurePrimaryHttpMessageHandler(() => new RejectingJwksHandler(_jwksJson));
        });
    }
}

/// <summary>Only the explicitly declared JWKS URL is accepted; unexpected requests fail immediately.</summary>
public sealed class RejectingJwksHandler : HttpMessageHandler
{
    private readonly string _jwksJson;

    public RejectingJwksHandler(string jwksJson = OfflineHostFixture.EmptyJwks)
    {
        _jwksJson = jwksJson;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Method != HttpMethod.Get || request.RequestUri?.AbsoluteUri != OfflineHostFixture.JwksUrl)
        {
            throw new InvalidOperationException($"Unexpected outbound HTTP request: {request.Method} {request.RequestUri}");
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(_jwksJson, Encoding.UTF8, "application/json"),
        });
    }
}
