using Microsoft.AspNetCore.Mvc;
using TheBuilder.ApiOidc.Authentication;

namespace TheBuilder.ApiOidc.Example.Controllers;

/// <summary>
///     A custom endpoint beside the Delivery API. Umbraco does not check access on endpoints you add, so it asks
///     for the Delivery API's rules: the same API key or OIDC token as Umbraco's content endpoints.
/// </summary>
[ApiController]
[Route("umbraco/delivery/api/v1/dictionary")]
[RequireDeliveryApiAccess]
public sealed class DictionaryController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new Dictionary<string, string> { ["greeting"] = "Hello" });
}
