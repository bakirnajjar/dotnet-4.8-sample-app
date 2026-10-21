using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DescopeSampleApp.IntegrationTests;

public sealed class HostIntegrationTests
{
    [Fact]
    public async Task MinimalHostStartsAndRespondsWithoutMigratedRoutes()
    {
        using var factory = new OfflineHostFixture();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/__integration_host_probe__");

        // The current host is intentionally empty. This pins startup, not a future route contract.
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task JwksHandlerRespondsLocallyToDeclaredRequest()
    {
        using var handler = new RejectingJwksHandler();
        using var client = new HttpClient(handler);

        using var response = await client.GetAsync(OfflineHostFixture.JwksUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(OfflineHostFixture.EmptyJwks, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task JwksHandlerRejectsOtherOutboundRequests()
    {
        using var handler = new RejectingJwksHandler();
        using var client = new HttpClient(handler);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.GetAsync("https://other.example.test/.well-known/jwks.json"));
    }

    [Fact]
    public async Task FactoryInjectedHttpClientsUseOfflineJwksAndRejectUnknownRequests()
    {
        const string jwks = """{"keys":[],"fixture":"custom"}""";
        using var fixture = new OfflineHostFixture(jwks);
        using var hostClient = fixture.CreateClient();
        var services = fixture.Services;
        var httpClients = new[]
        {
            services.GetRequiredService<HttpClient>(),
            services.GetRequiredService<IHttpClientFactory>().CreateClient(),
            services.GetRequiredService<IHttpClientFactory>().CreateClient(OfflineHostFixture.JwksClientName),
        };

        foreach (var client in httpClients)
        {
            using (var response = await client.GetAsync(OfflineHostFixture.JwksUrl))
            {
                Assert.Equal(jwks, await response.Content.ReadAsStringAsync());
            }
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => client.GetAsync("https://unexpected.example.test/"));
            client.Dispose();
        }
    }
}
