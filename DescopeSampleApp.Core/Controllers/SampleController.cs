using System.Net.Http.Headers;
using DescopeSampleApp.Core;
using Microsoft.AspNetCore.Mvc;

namespace DescopeSampleApp.Core.Controllers;

// No [ApiController]: the legacy Web API action did not use automatic model validation.
[Route("api/sample")]
[Route("api/sample/{id?}")]
public sealed class SampleController(TokenValidator validator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        if (!AuthenticationHeaderValue.TryParse(Request.Headers.Authorization.ToString(), out var authorization) ||
            !authorization.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrEmpty(authorization.Parameter))
            return Unauthorized();

        try
        {
            await validator.ValidateSession(authorization.Parameter, cancellationToken);
            // Core's Ok(string) prefers text/plain; Web API 2's Ok(string) used JSON.
            return new JsonResult("This is a sample API endpoint.");
        }
        catch (TokenValidationException)
        {
            return Unauthorized();
        }
        catch (JwksException)
        {
            // The key service is unavailable; don't disclose its URL, response, or exception.
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }
}
