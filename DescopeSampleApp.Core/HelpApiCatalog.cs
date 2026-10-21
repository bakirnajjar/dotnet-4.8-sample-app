using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace DescopeSampleApp.Core;

// Builds the help pages from the running host's API Explorer, not legacy HttpConfiguration.
public sealed class HelpApiCatalog(IApiDescriptionGroupCollectionProvider explorer)
{
    public IReadOnlyList<HelpApi> Apis => explorer.ApiDescriptionGroups.Items
        .SelectMany(group => group.Items)
        .Where(api => api.HttpMethod != null &&
                      api.RelativePath?.StartsWith("api/", StringComparison.OrdinalIgnoreCase) == true)
        .Select(api => new HelpApi(
            api.HttpMethod!,
            api.RelativePath!,
            api.ActionDescriptor.RouteValues.TryGetValue("controller", out var controller) ? controller ?? "API" : "API",
            api.ParameterDescriptions.Select(parameter => new HelpParameter(
                parameter.Name, parameter.Type?.Name ?? "Object", parameter.Source?.DisplayName ?? "Unknown")).ToArray(),
            api.SupportedResponseTypes.Select(response => new HelpResponse(
                response.StatusCode, response.Type?.Name ?? "None")).ToArray()))
        .OrderBy(api => api.Path, StringComparer.Ordinal)
        .ThenBy(api => api.Method, StringComparer.Ordinal)
        .ToArray();

    public HelpApi? FindApi(string? id) =>
        string.IsNullOrEmpty(id) ? null : Apis.FirstOrDefault(api =>
            string.Equals(api.Id, id, StringComparison.OrdinalIgnoreCase));

    public HelpResource? FindResource(string? name)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        var types = explorer.ApiDescriptionGroups.Items.SelectMany(group => group.Items)
            .Where(api => api.RelativePath?.StartsWith("api/", StringComparison.OrdinalIgnoreCase) == true)
            .SelectMany(api => api.ParameterDescriptions.Select(parameter => parameter.Type)
                .Concat(api.SupportedResponseTypes.Select(response => response.Type)));
        var type = types.Where(type => type != null && type.IsClass && type != typeof(string) &&
                                      type != typeof(object) && !typeof(Microsoft.AspNetCore.Mvc.IActionResult).IsAssignableFrom(type))
            .FirstOrDefault(type => string.Equals(type!.Name, name, StringComparison.OrdinalIgnoreCase));
        return type == null ? null : new HelpResource(type.Name,
            type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .Select(property => new HelpProperty(property.Name, property.PropertyType.Name)).ToArray());
    }
}

public sealed record HelpApi(string Method, string Path, string Controller,
    IReadOnlyList<HelpParameter> Parameters, IReadOnlyList<HelpResponse> Responses)
{
    // Same method/path normalization as the generated Web API 2 HelpPage friendly ID.
    public string Id => $"{Method}-{Path.Split('?')[0].Replace("/", "-").Replace("{", "").Replace("}", "").Replace("?", "")}";
}

public sealed record HelpParameter(string Name, string Type, string Source);
public sealed record HelpResponse(int StatusCode, string Type);
public sealed record HelpResource(string Name, IReadOnlyList<HelpProperty> Properties);
public sealed record HelpProperty(string Name, string Type);
