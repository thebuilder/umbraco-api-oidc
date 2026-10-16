using Microsoft.AspNetCore.Builder;

namespace TheBuilder.ApiOidc.Authentication;

/// <summary>
///     Protects minimal API endpoints with an OIDC token, or with the Delivery API's access rules.
/// </summary>
public static class OidcEndpointConventionBuilderExtensions
{
    /// <summary>
    ///     Requires a valid OIDC token from a trusted issuer, the same as <see cref="RequireOidcTokenAttribute" />.
    /// </summary>
    /// <param name="builder">The endpoint or group.</param>
    /// <param name="issuers">The <c>Name</c>s of the trusted issuers to accept. Leave it empty to accept any.</param>
    public static TBuilder RequireOidcToken<TBuilder>(this TBuilder builder, params string[] issuers)
        where TBuilder : IEndpointConventionBuilder =>
        builder.RequireAuthorization(policy => policy.RequireOidcToken(issuers));

    /// <summary>
    ///     Requires the same access as the Delivery API's content endpoints, the same as
    ///     <see cref="RequireDeliveryApiAccessAttribute" />.
    /// </summary>
    /// <param name="builder">The endpoint or group.</param>
    public static TBuilder RequireDeliveryApiAccess<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.RequireAuthorization(new RequireDeliveryApiAccessAttribute());
}
