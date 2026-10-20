using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Mvc;
using DescopeSampleApp.Areas.HelpPage.Controllers;
using DescopeSampleApp.Controllers;
using Xunit;

public class ControllerCharacterizationTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("Basic", "credentials")]
    [InlineData("Bearer", null)]
    [InlineData("Bearer", "")]
    [InlineData("not-a-bearer-scheme", "broken")]
    public async Task SampleGetWithoutUsableBearerReturnsUnauthorizedWithoutFetchingKeys(
        string scheme, string parameter)
    {
        // Nonempty bearer tokens enter ValidateSession and fetch live JWKS; exclude them here.
        using (var configuration = new HttpConfiguration())
        using (var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/api/sample"))
        using (var controller = new SampleController())
        {
            if (scheme != null)
                request.Headers.Authorization = new AuthenticationHeaderValue(scheme, parameter);
            controller.Configuration = configuration;
            controller.Request = request;

            var result = await controller.Get();
            using (var response = await result.ExecuteAsync(CancellationToken.None))
            {
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.Null(response.Content);
            }
        }
    }

    [Fact]
    public void HomeIndexSetsTitleAndReturnsDefaultView()
    {
        var controller = new HomeController();
        var result = Assert.IsType<ViewResult>(controller.Index());
        Assert.Equal("Home Page", (string)controller.ViewBag.Title);
        Assert.Equal("", result.ViewName);
        Assert.Null(result.Model);
    }

    [Fact]
    public async Task DefaultWebApiJsonFormatterSerializesSampleSuccessStringAsJsonString()
    {
        // SampleController's successful Ok(string) is gated by live JWKS; this
        // checks only the independent legacy formatter, not a successful request.
        using (var config = new HttpConfiguration())
        using (var content = new ObjectContent<string>(
            "This is a sample API endpoint.", config.Formatters.JsonFormatter))
        {
            Assert.Equal("application/json", content.Headers.ContentType.MediaType);
            Assert.Equal("\"This is a sample API endpoint.\"",
                await content.ReadAsStringAsync());
        }
    }

    [Fact]
    public void HelpIndexUsesInjectedConfigurationAndReturnsApiDescriptions()
    {
        using (var config = new HttpConfiguration())
        {
            DescopeSampleApp.WebApiConfig.Register(config);
            config.EnsureInitialized();
            var controller = new HelpController(config);
            var result = Assert.IsType<ViewResult>(controller.Index());
            Assert.Same(config, controller.Configuration);
            Assert.Equal("", result.ViewName);
            Assert.NotNull(result.Model);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown-api-id")]
    public void HelpApiMissingOrUnknownIdReturnsErrorView(string apiId)
    {
        using (var config = new HttpConfiguration())
        {
            var result = Assert.IsType<ViewResult>(new HelpController(config).Api(apiId));
            Assert.Equal("Error", result.ViewName);
            Assert.Null(result.Model);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown-model")]
    public void HelpResourceModelMissingOrUnknownNameReturnsErrorView(string name)
    {
        using (var config = new HttpConfiguration())
        {
            var result = Assert.IsType<ViewResult>(new HelpController(config).ResourceModel(name));
            Assert.Equal("Error", result.ViewName);
            Assert.Null(result.Model);
        }
    }
}
