using System;
using System.IO;
using System.Linq;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using DescopeSampleApp;
using Xunit;

public class RouteAndMarkupCharacterizationTests
{
    [Fact]
    public void MvcDefaultRouteMapsHomeIndexAndIgnoresAxd()
    {
        var routes = new RouteCollection();
        RouteConfig.RegisterRoutes(routes);

        var ignored = Assert.IsAssignableFrom<Route>(routes[0]);
        Assert.Equal("{resource}.axd/{*pathInfo}", ignored.Url);
        Assert.IsType<StopRoutingHandler>(ignored.RouteHandler);
        var route = Assert.IsType<Route>(routes["Default"]);
        Assert.Equal("{controller}/{action}/{id}", route.Url);
        Assert.Equal("Home", route.Defaults["controller"]);
        Assert.Equal("Index", route.Defaults["action"]);
        Assert.Same(UrlParameter.Optional, route.Defaults["id"]);
    }

    [Fact]
    public void WebApiDefaultRouteMapsSampleWithOptionalId()
    {
        using (var config = new HttpConfiguration())
        {
            WebApiConfig.Register(config);
            var route = config.Routes["DefaultApi"];
            Assert.NotNull(route);
            Assert.Equal("api/{controller}/{id}", route.RouteTemplate);
            Assert.Same(RouteParameter.Optional, route.Defaults["id"]);
        }
    }

    [Fact]
    public void HelpAreaDeclaresHelpActionApiIdRoute()
    {
        // AreaRegistrationContext requires an ASP.NET hosting context; this pins the
        // checked-in route declaration, not a successfully served Help URL.
        var source = LegacySource("Areas", "HelpPage", "HelpPageAreaRegistration.cs");
        Assert.Contains("return \"HelpPage\";", source);
        Assert.Contains("\"HelpPage_Default\"", source);
        Assert.Contains("\"Help/{action}/{apiId}\"", source);
        Assert.Contains("action = \"Index\", apiId = UrlParameter.Optional", source);
    }

    [Fact]
    public void LoginMarkupWiresComponentSuccessToRoot()
    {
        var page = LegacySource("Login.aspx");
        Assert.Contains("<title>Login</title>", page);
        Assert.Contains("@descope/web-js-sdk@1.10.45/dist/index.umd.js", page);
        Assert.Contains("@descope/web-component@3.11.14/dist/index.js", page);
        Assert.Contains("<descope-wc ", page);
        Assert.Contains("flow-id=\"sign-up-or-in\"", page);
        Assert.Contains("wcElement.addEventListener('success', onSuccess)", page);
        Assert.Contains("window.location.replace('/')", page);
        Assert.Contains("wcElement.addEventListener('error', onError)", page);
    }

    [Fact]
    public void AuthenticatedMarkupUsesClientSideSessionGuardAndCallsSampleApi()
    {
        var page = LegacySource("AuthenticatedPage.aspx");
        Assert.Contains("Welcome to the Authenticated Page!", page);
        Assert.Contains("persistTokens: true, autoRefresh: true", page);
        Assert.Contains("sdk.getSessionToken()", page);
        Assert.Contains("!sdk.isJwtExpired(sessionToken)", page);
        Assert.Contains("currentPath != '/login.aspx'", page);
        Assert.Contains("window.location.replace('/login.aspx')", page);
        Assert.Contains("fetch(\"/api/sample\"", page);
        Assert.Contains("response.json()", page);
        Assert.Contains("Status Code: ${response.status}", page);
        // This is source-level browser intent, not a server authorization test.
    }

    [Fact]
    public void HomeViewLinksToAuthenticatedPageInItsText()
    {
        var view = LegacySource("Views", "Home", "Index.cshtml");
        Assert.Contains("Authenticated Page in the NavBar", view);
        Assert.Contains("test the Sample API.", view);
    }

    private static string LegacySource(params string[] parts)
    {
        var root = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "DescopeSampleApp.sln")))
            root = root.Parent;
        Assert.NotNull(root);
        return File.ReadAllText(Path.Combine(
            new[] { root.FullName, "DescopeSampleApp" }.Concat(parts).ToArray()));
    }
}
