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
        var accepted = context.User.Identities.Any(identity =>
            identity.IsAuthenticated &&
            identity.AuthenticationType == OidcAuthenticationDefaults.AuthenticationScheme &&
            (requirement.Issuers.Count == 0 ||
             requirement.Issuers.Contains(identity.FindFirst(OidcAuthenticationDefaults.IssuerNameClaimType)?.Value, StringComparer.Ordinal)));
        if (accepted)
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
