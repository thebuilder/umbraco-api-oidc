using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TheBuilder.VercelOidc.Configuration;

namespace TheBuilder.VercelOidc.Tokens;

internal sealed class VercelOidcTokenValidator(
    IOptionsMonitor<VercelOidcOptions> options,
    BaseConfigurationManager configurationManager,
    TimeProvider timeProvider) : IVercelOidcTokenValidator
{
    private readonly JsonWebTokenHandler _handler = new() { MapInboundClaims = false };

    public async Task<VercelOidcValidationResult> ValidateAsync(string token, CancellationToken cancellationToken)
    {
        var settings = options.CurrentValue;
        if (!settings.IsEnabled)
            return VercelOidcValidationResult.Invalid("Vercel OIDC is not configured.");

        var parameters = new TokenValidationParameters
        {
            ValidIssuer = settings.Issuer,
            ValidAudience = settings.EffectiveAudience,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            ValidateIssuerSigningKey = true,
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            ClockSkew = settings.ClockSkew,
            // The handler fetches the signing keys from here, and refetches them once when a token names a key it
            // has not seen, which is how Vercel's key rotation reaches a long-running site.
            ConfigurationManager = configurationManager,
            LifetimeValidator = (notBefore, expires, _, validation) => IsWithinLifetime(notBefore, expires, validation.ClockSkew),
        };

        var result = await _handler.ValidateTokenAsync(token, parameters);
        if (!result.IsValid)
            return VercelOidcValidationResult.Invalid(result.Exception?.Message ?? "The token is not valid.");

        return Authorize(result.Claims, settings);
    }

    private bool IsWithinLifetime(DateTime? notBefore, DateTime? expires, TimeSpan clockSkew)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return expires is { } expiry && now <= expiry + clockSkew &&
               (notBefore is not { } start || now >= start - clockSkew);
    }

    private static VercelOidcValidationResult Authorize(IDictionary<string, object> claims, VercelOidcOptions settings)
    {
        var owner = Read(claims, Constants.Claims.Owner);
        var ownerId = Read(claims, Constants.Claims.OwnerId);
        var project = Read(claims, Constants.Claims.Project);
        var projectId = Read(claims, Constants.Claims.ProjectId);
        var environment = Read(claims, Constants.Claims.Environment);
        if (owner is null || ownerId is null || project is null || projectId is null || environment is null)
            return VercelOidcValidationResult.Invalid("The token is missing one of the Vercel deployment claims.");

        // In global issuer mode every team shares the issuer, so the owner is what keeps another team's
        // deployments out. In team mode it repeats the issuer, which costs nothing.
        if (!string.Equals(owner, settings.TeamSlug, StringComparison.Ordinal))
            return VercelOidcValidationResult.Invalid($"The token belongs to the Vercel team \"{owner}\".");

        if (!settings.Projects.Any(allowed => string.Equals(allowed, project, StringComparison.Ordinal) ||
                                              string.Equals(allowed, projectId, StringComparison.Ordinal)))
            return VercelOidcValidationResult.Invalid($"The Vercel project \"{project}\" is not allowed.");

        if (!settings.Environments.Any(allowed => string.Equals(allowed, environment, StringComparison.OrdinalIgnoreCase)))
            return VercelOidcValidationResult.Invalid($"The Vercel environment \"{environment}\" is not allowed.");

        return VercelOidcValidationResult.Valid(new VercelOidcIdentity(owner, ownerId, project, projectId, environment));
    }

    private static string? Read(IDictionary<string, object> claims, string name) =>
        claims.TryGetValue(name, out var value) && value is string text && !string.IsNullOrWhiteSpace(text) ? text : null;
}
