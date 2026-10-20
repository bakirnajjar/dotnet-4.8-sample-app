using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Xunit;

namespace DescopeSampleApp.IntegrationTests;

public sealed class SampleApiTests
{
    private const string SuccessJson = "\"This is a sample API endpoint.\"";
    private const string ValidPayload = """{"sub":"test-user","exp":4102444800}""";

    [Theory]
    [InlineData("/api/sample")]
    [InlineData("/api/sample/")]
    [InlineData("/api/sample/any-id")]
    public async Task SignedTokenReturnsLegacyJsonStringOnSupportedRoutes(string path)
    {
        using var factory = new OfflineHostFixture(TokenValidatorTests.Jwks());
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TokenValidatorTests.Sign(ValidPayload));

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(SuccessJson, await response.Content.ReadAsStringAsync());
        Assert.Equal("This is a sample API endpoint.", JsonSerializer.Deserialize<string>(SuccessJson));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Basic credentials")]
    [InlineData("Bearer")]
    [InlineData("Bearer ")]
    [InlineData("not-a-bearer-scheme broken")]
    [InlineData("Bearer not.a.jwt")]
    [InlineData("Bearer malformed")]
    public async Task MissingOrMalformedBearerHasEmptyUnauthorizedResponse(string? authorization)
    {
        using var factory = new OfflineHostFixture();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/sample");
        if (authorization is not null)
            request.Headers.TryAddWithoutValidation("Authorization", authorization);

        using var response = await client.SendAsync(request);

        await AssertEmptyResponse(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UnrecognizedSigningKeyIsUnauthorized()
    {
        using var factory = new OfflineHostFixture(TokenValidatorTests.Jwks());
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/sample");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
            TokenValidatorTests.Sign(ValidPayload, "unknown"));

        using var response = await client.SendAsync(request);
        await AssertEmptyResponse(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task InvalidSignatureIsUnauthorized()
    {
        using var factory = new OfflineHostFixture(TokenValidatorTests.Jwks());
        using var client = factory.CreateClient();
        var parts = TokenValidatorTests.Sign(ValidPayload).Split('.');
        parts[2] = (parts[2][0] == 'A' ? "B" : "A") + parts[2][1..];
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/sample");
        request.Headers.Authorization = new AuthenticationHeaderValue("bearer", string.Join(".", parts));

        using var response = await client.SendAsync(request);
        await AssertEmptyResponse(response, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ExpiredTokenIsUnauthorized()
    {
        using var factory = new OfflineHostFixture(TokenValidatorTests.Jwks());
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/sample");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
            TokenValidatorTests.Sign("""{"exp":946684800}"""));

        using var response = await client.SendAsync(request);
        await AssertEmptyResponse(response, HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("""{"keys":{}}""")]
    [InlineData("""{"keys":[]}""")]
    public async Task KeyServiceFaultDoesNotLeakDetails(string jwks)
    {
        using var factory = new OfflineHostFixture(jwks);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/sample");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TokenValidatorTests.Sign(ValidPayload));

        using var response = await client.SendAsync(request);
        // An empty but well-formed key set means no matching key: bad token, not a JWKS parsing failure.
        await AssertEmptyResponse(response, jwks == OfflineHostFixture.EmptyJwks
            ? HttpStatusCode.Unauthorized : HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task TransportFaultDoesNotLeakDetails()
    {
        using var factory = new OfflineHostFixture(failFetch: true);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/sample");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", TokenValidatorTests.Sign(ValidPayload));

        using var response = await client.SendAsync(request);
        await AssertEmptyResponse(response, HttpStatusCode.ServiceUnavailable);
    }

    private static async Task AssertEmptyResponse(HttpResponseMessage response, HttpStatusCode status)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("", await response.Content.ReadAsStringAsync());
        Assert.Null(response.Content.Headers.ContentType);
    }
}
