using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DescopeSampleApp.IntegrationTests;

public sealed class HelpMvcTests
{
    [Theory]
    [InlineData("/Help")]
    [InlineData("/Help/Index")]
    public async Task IndexListsApiExplorerEndpointsWithNavigableDetailLinks(string path)
    {
        using var factory = new OfflineHostFixture();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("ASP.NET Web API Help Page", html);
        Assert.Contains("GET api/sample", html);
        Assert.Contains("href=\"/Areas/HelpPage/HelpPage.css\"", html);
        var detailLink = Regex.Match(html, "href=\"([^\"]*/Help/Api/[^\"]+)\"", RegexOptions.IgnoreCase);
        Assert.True(detailLink.Success, html);

        using var detail = await client.GetAsync(WebUtility.HtmlDecode(detailLink.Groups[1].Value));
        var detailHtml = await detail.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        Assert.Contains("Request Information", detailHtml);
        Assert.Contains("Response Information", detailHtml);
        Assert.Contains("GET api/sample", detailHtml);
    }

    [Theory]
    [InlineData("/Help/Api")]
    [InlineData("/Help/Api/")]
    [InlineData("/Help/Api/unknown-api-id")]
    [InlineData("/Help/ResourceModel")]
    [InlineData("/Help/ResourceModel?modelName=")]
    [InlineData("/Help/ResourceModel?modelName=unknown-model")]
    public async Task MissingAndUnknownInputsRenderErrorView(string path)
    {
        using var factory = new OfflineHostFixture();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("<title>Error</title>", html);
        Assert.Contains("The requested help page was not found.", html);
    }

    [Fact]
    public async Task ResourceModelRendersDiscoveredResponseTypeAndProperties()
    {
        using var factory = new HelpModelHost();
        using var client = factory.CreateClient();

        using var index = await client.GetAsync("/Help");
        var indexHtml = await index.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, index.StatusCode);
        Assert.Contains("GET api/help-widget", indexHtml);

        using var detail = await client.GetAsync("/Help/Api/GET-api-help-widget");
        var detailHtml = await detail.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        Assert.Contains("Resource Description", detailHtml);
        Assert.Contains("/Help/ResourceModel?modelName=HelpWidget", WebUtility.HtmlDecode(detailHtml));

        using var resource = await client.GetAsync("/Help/ResourceModel?modelName=HelpWidget");
        var html = await resource.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, resource.StatusCode);
        Assert.Contains("<h1>HelpWidget</h1>", html);
        Assert.Contains("Label", html);
        Assert.Contains("String", html);
    }

    [Fact]
    public async Task HelpRoutingDoesNotInterceptHomeOrSampleApi()
    {
        using var factory = new OfflineHostFixture();
        using var client = factory.CreateClient();
        using var home = await client.GetAsync("/");
        using var api = await client.GetAsync("/api/sample");
        Assert.Equal(HttpStatusCode.OK, home.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, api.StatusCode);
    }

    [Fact]
    public async Task HelpStylesheetIsServedAtLegacyUrl()
    {
        using var factory = new OfflineHostFixture();
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/Areas/HelpPage/HelpPage.css");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/css", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains(".help-page", await response.Content.ReadAsStringAsync());
    }
}

public sealed record HelpWidget(string Label);

[Route("api/help-widget")]
[ApiExplorerSettings(IgnoreApi = false)]
public sealed class HelpWidgetController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(HelpWidget), 200)]
    public IActionResult Get() => Ok(new HelpWidget("example"));
}

// Test-only API descriptor: keeps production endpoints untouched while exercising a real model page.
public sealed class HelpModelHost : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
            services.AddControllersWithViews().AddApplicationPart(typeof(HelpWidgetController).Assembly));
    }
}
