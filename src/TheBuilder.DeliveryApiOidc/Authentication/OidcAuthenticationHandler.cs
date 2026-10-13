using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TheBuilder.DeliveryApiOidc.Configuration;
using TheBuilder.DeliveryApiOidc.DeliveryApi;
using TheBuilder.DeliveryApiOidc.Tokens;

namespace TheBuilder.DeliveryApiOidc.Authentication;

/// <summary>
///     Signs in the workload behind a valid OIDC token, for endpoints that opt in with
///     <see cref="RequireOidcTokenAttribute" /> or <see cref="OidcAuthenticationDefaults.Policy" />.
/// </summary>
internal sealed class OidcAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> schemeOptions,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    IOptionsMonitor<DeliveryApiOidcOptions> options,
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
                // The handler logs the reason. It never reaches the caller, who only gets a 401.
                return AuthenticateResult.Fail(result.Failure ?? "The token is not valid.");
            identity = result.Identity;
        }

        var claims = new List<Claim>(identity.Claims)
        {
            new(OidcAuthenticationDefaults.IssuerNameClaimType, identity.IssuerName),
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name, "sub", null));
        return AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name));
    }
}
