using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace TheBuilder.ApiOidc.Authentication;

/// <summary>
///     Met when the request was signed in by the OIDC scheme itself, from one of the given issuers.
/// </summary>
/// <remarks>
///     It checks the identity the scheme created rather than any claim on the user, so a cookie or another scheme
///     added by a second <c>[Authorize]</c> on the same endpoint cannot satisfy it.
/// </remarks>
internal sealed class OidcTokenRequirement(IReadOnlyList<string> issuers)
    : AuthorizationHandler<OidcTokenRequirement>, IAuthorizationRequirement
{
    public IReadOnlyList<string> Issuers { get; } = issuers;

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OidcTokenRequirement requirement)
    {
        if (IsSignedIn(context.User, requirement.Issuers))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }

    /// <summary>Whether the OIDC scheme itself signed the user in, from one of the issuers when any are given.</summary>
    public static bool IsSignedIn(ClaimsPrincipal user, IReadOnlyList<string> issuers) =>
        user.Identities.Any(identity =>
            identity.IsAuthenticated &&
            identity.AuthenticationType == OidcAuthenticationDefaults.AuthenticationScheme &&
            (issuers.Count == 0 ||
             issuers.Contains(identity.FindFirst(OidcAuthenticationDefaults.IssuerNameClaimType)?.Value, StringComparer.Ordinal)));
}
