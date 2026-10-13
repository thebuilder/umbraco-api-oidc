using Microsoft.AspNetCore.Authorization;

namespace TheBuilder.ApiOidc.Authentication;

/// <summary>
///     Requires a valid OIDC token from a trusted issuer on a controller or action.
/// </summary>
/// <remarks>
///     Requests without a valid token get a 401. The token is read from the same header as on the Delivery API,
///     and the endpoint can read the token's claims from <c>User</c>.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequireOidcTokenAttribute : AuthorizeAttribute, IAuthorizationRequirementData
{
    /// <summary>
    ///     Requires a valid token.
    /// </summary>
    /// <param name="issuers">
    ///     The <c>Name</c>s of the trusted issuers to accept, exactly as configured. Leave it empty to accept any
    ///     of them.
    /// </param>
    public RequireOidcTokenAttribute(params string[] issuers)
    {
        Issuers = issuers;
        AuthenticationSchemes = OidcAuthenticationDefaults.AuthenticationScheme;
    }

    /// <summary>The trusted issuers this endpoint accepts. Empty means any.</summary>
    public IReadOnlyList<string> Issuers { get; }

    /// <inheritdoc />
    public IEnumerable<IAuthorizationRequirement> GetRequirements() => [new OidcTokenRequirement(Issuers)];
}
