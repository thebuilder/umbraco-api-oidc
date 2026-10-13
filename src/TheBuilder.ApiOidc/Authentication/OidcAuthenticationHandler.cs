using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TheBuilder.ApiOidc.Configuration;
using TheBuilder.ApiOidc.DeliveryApi;
using TheBuilder.ApiOidc.Tokens;

namespace TheBuilder.ApiOidc.Authentication;

/// <summary>
///     Signs in the workload behind a valid OIDC token, for endpoints that opt in with
///     <see cref="RequireOidcTokenAttribute" /> or <see cref="OidcAuthenticationDefaults.Policy" />.
/// </summary>
internal sealed class OidcAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> schemeOptions,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    IOptionsMonitor<ApiOidcOptions> options,
    IOidcTokenValidator validator) : AuthenticationHandler<AuthenticationSchemeOptions>(schemeOptions, loggerFactory, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var settings = options.CurrentValue;
        if (settings.Issuers.Count == 0)
            return AuthenticateResult.NoResult();

        // The Delivery API middleware may already have checked this request's token.
        var identity = Context.Features.Get<OidcAccessFeature>()?.Identity;
        if (identity is null)
        {
            if (OidcTokenReader.Read(Request, settings.HeaderName) is not { } token)
                return AuthenticateResult.NoResult();

            var result = await validator.ValidateAsync(token, Context.RequestAborted);
            if (result.Identity is null)
            {
                // The reason is for the log only. The caller gets a 401 and nothing more.
                Logger.LogWarning("Refused an OIDC token on {Path}: {Reason}", Request.Path, result.Failure);
                return AuthenticateResult.Fail(result.Failure ?? "The token is not valid.");
            }
            identity = result.Identity;
        }

        // A token that carries its own issuer name claim must not choose which issuer it counts as.
        var claims = identity.Claims
            .Where(claim => claim.Type != OidcAuthenticationDefaults.IssuerNameClaimType)
            .Append(new Claim(OidcAuthenticationDefaults.IssuerNameClaimType, identity.IssuerName))
            .ToList();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name, "sub", null));
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }
}
