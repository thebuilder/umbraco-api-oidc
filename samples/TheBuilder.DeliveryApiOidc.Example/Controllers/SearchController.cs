using Microsoft.AspNetCore.Mvc;
using TheBuilder.DeliveryApiOidc.Authentication;

namespace TheBuilder.DeliveryApiOidc.Example.Controllers;

/// <summary>
///     A custom endpoint that only a trusted workload can call, with the same token it uses for the Delivery API.
/// </summary>
[ApiController]
[Route("api/search")]
[RequireOidcToken]
public sealed class SearchController : ControllerBase
{
    [HttpGet]
    public IActionResult Search(string? query) => Ok(new
    {
        query,
        caller = User.FindFirst(OidcAuthenticationDefaults.IssuerNameClaimType)?.Value,
        results = Array.Empty<string>(),
    });
}
