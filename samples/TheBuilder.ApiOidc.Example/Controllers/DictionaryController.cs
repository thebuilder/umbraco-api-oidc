using Microsoft.AspNetCore.Mvc;

namespace TheBuilder.ApiOidc.Example.Controllers;

/// <summary>
///     A custom endpoint under the Delivery API's path. It has no access check of its own: the package applies the
///     Delivery API's, so it needs the same API key or OIDC token as Umbraco's content endpoints.
/// </summary>
[ApiController]
[Route("umbraco/delivery/api/v1/dictionary")]
public sealed class DictionaryController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new Dictionary<string, string> { ["greeting"] = "Hello" });
}
