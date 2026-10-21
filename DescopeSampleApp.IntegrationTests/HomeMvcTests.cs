using System.Net;
using Xunit;

namespace DescopeSampleApp.IntegrationTests;

public sealed class HomeMvcTests
{
    [Theory]
    [InlineData("/")]
    [InlineData("/Home/Index")]
    public async Task HomeRoutesRenderLegacyTitleAndNavigationIntent(string path)
    {
        using var factory = new OfflineHostFixture();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("<title>Home Page - My ASP.NET Application</title>", html);
        Assert.Contains("<h1 id=\"aspnetTitle\">ASP.NET</h1>", html);
        Assert.Contains("Click on the Authenticated Page in the NavBar to login with Descope and test the Sample API.", html);
        Assert.Contains("href=\"/\" class=\"navbar-brand\">Application name</a>", html);
        Assert.Contains("href=\"/\" class=\"nav-link\">Home</a>", html);
        Assert.Contains("href=\"/AuthenticatedPage.aspx\" class=\"nav-link\">Authenticated Page</a>", html);
        Assert.Contains("href=\"/Content/bootstrap.min.css\"", html);
        Assert.Contains("href=\"/Content/Site.css\"", html);
        Assert.Contains("src=\"/Scripts/bootstrap.bundle.min.js\"", html);
    }

    [Theory]
    [InlineData("/Content/bootstrap.min.css", "text/css")]
    [InlineData("/Content/Site.css", "text/css")]
    [InlineData("/Scripts/bootstrap.bundle.min.js", "text/javascript")]
    public async Task LayoutAssetsAreServed(string path, string mediaType)
    {
        using var factory = new OfflineHostFixture();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(mediaType, response.Content.Headers.ContentType?.MediaType);
        Assert.NotEmpty(await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task HomeMappingDoesNotInterceptSampleApi()
    {
        using var factory = new OfflineHostFixture();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/sample");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync());
    }
}
