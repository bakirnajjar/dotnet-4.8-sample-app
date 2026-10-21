using System.Net;
using Xunit;

namespace DescopeSampleApp.IntegrationTests;

public sealed class LoginPageTests
{
    [Theory]
    [InlineData("/login.aspx")]
    [InlineData("/Login.aspx")]
    public async Task LoginRouteRendersClientSideDescopeFlow(string path)
    {
        using var factory = new OfflineHostFixture();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(path);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("<title>Login</title>", html);
        Assert.Contains("src=\"https://unpkg.com/@descope/web-js-sdk@1.10.45/dist/index.umd.js\"", html);
        Assert.Contains("src=\"https://unpkg.com/@descope/web-component@3.11.14/dist/index.js\"", html);
        Assert.Contains("<form id=\"loginForm\">", html);
        Assert.Contains("<descope-wc project-id=\"P2dI0leWLEC45BDmfxeOCSSOWiCt\" flow-id=\"sign-up-or-in\"></descope-wc>", html);
        Assert.Contains("document.getElementsByTagName('descope-wc')[0]", html);
        Assert.Contains("wcElement.addEventListener('success', onSuccess)", html);
        Assert.Contains("wcElement.addEventListener('error', onError)", html);
        Assert.Contains("const onError = (err) => console.log(err)", html);
        Assert.Contains("window.location.replace('/AuthenticatedPage.aspx')", html);
        Assert.DoesNotContain("window.location.replace('/')", html);
        Assert.DoesNotContain("runat=\"server\"", html);
    }
}
