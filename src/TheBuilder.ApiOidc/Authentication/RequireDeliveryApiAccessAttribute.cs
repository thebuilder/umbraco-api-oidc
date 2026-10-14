using Microsoft.AspNetCore.Authorization;

namespace TheBuilder.ApiOidc.Authentication;

/// <summary>
///     Requires the same access as the Delivery API's content endpoints on a controller or action: public access,
///     a valid API key or a valid OIDC token.
/// </summary>
/// <remarks>
///     Umbraco only checks access on its own Delivery API controllers, so an endpoint you add, such as a dictionary
///     endpoint, is open unless it asks for this. Requests without access get a 401. Use
///     <see cref="RequireOidcTokenAttribute" /> instead to accept only a token.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequireDeliveryApiAccessAttribute : AuthorizeAttribute
{
    /// <summary>
    ///     Requires Delivery API access.
    /// </summary>
    public RequireDeliveryApiAccessAttribute()
        : base(OidcAuthenticationDefaults.DeliveryApiAccessPolicy)
    {
        // A named policy rather than requirements on the attribute: ASP.NET Core adds its default policy, which
        // demands a signed-in user, to an attribute without one, and that would refuse the API key and public access.
    }
}
