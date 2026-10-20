using System.Net;
using Xunit;

namespace DescopeSampleApp.IntegrationTests;

public sealed class AuthenticatedPageTests
{
    [Theory]
    [InlineData("/AuthenticatedPage.aspx")]
    [InlineData("/authenticatedpage.aspx")]
    public async Task ServesClientGuardedPageWithoutServerRedirect(string path)
    {
        using var factory = new OfflineHostFixture();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("<title>Login</title>", html);
        Assert.Contains("Welcome to the Authenticated Page!", html);
        Assert.Contains("src=\"https://unpkg.com/@descope/web-js-sdk@1.10.45/dist/index.umd.js\"", html);
        Assert.Contains("persistTokens: true, autoRefresh: true", html);
        Assert.Contains("const sessionToken = sdk.getSessionToken()", html);
        Assert.Contains("!sdk.isJwtExpired(sessionToken)", html);
        Assert.Contains("currentPath != '/login.aspx'", html);
        Assert.Contains("window.location.replace('/login.aspx')", html);
        Assert.DoesNotContain("runat=\"server\"", html);
        // The legacy page has no logout control or logout call; don't invent one here.
        Assert.DoesNotContain("logout", html, StringComparison.OrdinalIgnoreCase);
        Assert.Null(response.Headers.Location);
    }

    [Fact]
    public async Task ApiButtonWiresTokenHeaderAndResponseHandling()
    {
        using var factory = new OfflineHostFixture();
        using var client = factory.CreateClient();

        var html = await client.GetStringAsync("/AuthenticatedPage.aspx");

        Assert.Contains("onclick=\"testSampleAPI(event)\"", html);
        Assert.Contains("async function testSampleAPI(event)", html);
        Assert.Contains("event.preventDefault()", html);
        Assert.Contains("const sessionToken = sdk.getSessionToken();", html);
        Assert.Contains("if (sessionToken && !sdk.isJwtExpired(sessionToken))", html);
        Assert.Contains("fetch(\"/api/sample\"", html);
        Assert.Contains("\"Authorization\": `Bearer ${sessionToken}`", html);
        Assert.Contains("if (!response.ok)", html);
        Assert.Contains("await response.json()", html);
        Assert.Contains("Status Code: ${response.status}", html);
        Assert.Contains("Error: ${error.message}", html);
        Assert.Contains("id=\"apiResponse\"", html);
    }
}
