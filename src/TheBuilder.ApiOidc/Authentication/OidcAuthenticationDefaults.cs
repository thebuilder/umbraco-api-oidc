namespace TheBuilder.ApiOidc.Authentication;

/// <summary>
///     Names for protecting your own endpoints with an OIDC token.
/// </summary>
public static class OidcAuthenticationDefaults
{
    /// <summary>
    ///     The authentication scheme that reads the token. It is never the default scheme, so it only runs on
    ///     endpoints that ask for it.
    /// </summary>
    public const string AuthenticationScheme = "TheBuilder.Oidc";

    /// <summary>
    ///     A policy that requires a valid token from any trusted issuer, for <c>[Authorize(Policy = ...)]</c>.
    /// </summary>
    public const string Policy = "TheBuilder.Oidc";

    /// <summary>
    ///     A policy that requires the same access as the Delivery API: public access, a valid API key or a valid
    ///     token, for <c>[Authorize(Policy = ...)]</c>.
    /// </summary>
    public const string DeliveryApiAccessPolicy = "TheBuilder.Oidc.DeliveryApiAccess";

    /// <summary>The claim that holds the <c>Name</c> of the trusted issuer that accepted the token.</summary>
    public const string IssuerNameClaimType = "oidc_issuer_name";
}
