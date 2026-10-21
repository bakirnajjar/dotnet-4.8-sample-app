using Microsoft.AspNetCore.Mvc;

namespace DescopeSampleApp.Core.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public sealed class HelpController(HelpApiCatalog catalog) : Controller
{
    public IActionResult Index() => View(catalog.Apis);

    public IActionResult Api(string? apiId)
    {
        var api = catalog.FindApi(apiId);
        return api == null ? View("Error") : View(api);
    }

    public IActionResult ResourceModel(string? modelName)
    {
        var resource = catalog.FindResource(modelName);
        return resource == null ? View("Error") : View(resource);
    }
}
