using Microsoft.AspNetCore.Authorization;

namespace TheBuilder.ApiOidc.Authentication;

/// <summary>
///     Requires an OIDC token in an authorization policy of your own.
/// </summary>
public static class OidcAuthorizationPolicyBuilderExtensions
{
    /// <summary>
    ///     Requires a valid OIDC token from a trusted issuer, the same as <see cref="RequireOidcTokenAttribute" />.
    /// </summary>
    /// <remarks>
    ///     Use it to register a policy under a name another package looks up, such as
    ///     <c>options.AddPolicy("SomePolicyName", policy => policy.RequireOidcToken("GitHubActions"))</c>. The
    ///     package's composer registers the authentication scheme the policy uses.
    /// </remarks>
    /// <param name="builder">The policy.</param>
    /// <param name="issuers">The <c>Name</c>s of the trusted issuers to accept. Leave it empty to accept any.</param>
    public static AuthorizationPolicyBuilder RequireOidcToken(this AuthorizationPolicyBuilder builder, params string[] issuers)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(issuers);

        // A copy, so changing the caller's array later cannot change which issuers the policy accepts.
        return builder.AddAuthenticationSchemes(OidcAuthenticationDefaults.AuthenticationScheme)
            .AddRequirements(new OidcTokenRequirement([.. issuers]));
    }
}
