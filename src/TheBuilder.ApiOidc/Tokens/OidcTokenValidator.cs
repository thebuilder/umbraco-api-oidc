using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using TheBuilder.ApiOidc.Configuration;

namespace TheBuilder.ApiOidc.Tokens;

internal sealed class OidcTokenValidator(
    IOptionsMonitor<ApiOidcOptions> options,
    IIssuerConfigurationProvider configurationProvider,
    TimeProvider timeProvider) : IOidcTokenValidator
{
    private const int MaxLoggedIssuerLength = 200;

    private static readonly string[] AsymmetricAlgorithms =
    [
        SecurityAlgorithms.RsaSha256, SecurityAlgorithms.RsaSha384, SecurityAlgorithms.RsaSha512,
        SecurityAlgorithms.RsaSsaPssSha256, SecurityAlgorithms.RsaSsaPssSha384, SecurityAlgorithms.RsaSsaPssSha512,
        SecurityAlgorithms.EcdsaSha256, SecurityAlgorithms.EcdsaSha384, SecurityAlgorithms.EcdsaSha512,
    ];

    private readonly JsonWebTokenHandler _handler = new() { MapInboundClaims = false };

    public async Task<OidcValidationResult> ValidateAsync(string token, CancellationToken cancellationToken)
    {
        var settings = options.CurrentValue;

        // The issuer is read before the signature is checked, but only to choose which keys to check it with:
        // nothing is trusted until the validation below has passed against that issuer's own keys.
        string unverifiedIssuer;
        try
        {
            unverifiedIssuer = _handler.ReadJsonWebToken(token).Issuer;
        }
        catch (ArgumentException)
        {
            return OidcValidationResult.Invalid("The token is not a JWT.");
        }

        var candidates = TrustedIssuerResolver.Resolve(settings)
            .Where(issuer => string.Equals(issuer.Issuer, unverifiedIssuer, StringComparison.Ordinal))
            .ToList();
        if (candidates.Count == 0)
            // Anyone can put anything in an unverified token, so only a bounded piece of it reaches the log.
            return OidcValidationResult.Invalid(
                $"The issuer \"{unverifiedIssuer[..Math.Min(unverifiedIssuer.Length, MaxLoggedIssuerLength)]}\" is not trusted.");

        // Several entries can share an issuer, such as two Vercel teams in global issuer mode, or two GitHub
        // repositories. Their keys are the same, so the signature is checked once against every audience they
        // accept, and the entries are then tried in order.
        var failures = new List<string>();
        foreach (var group in candidates.GroupBy(issuer => issuer.MetadataAddress))
        {
            var parameters = new TokenValidationParameters
            {
                ValidIssuer = unverifiedIssuer,
                ValidAudiences = group.Select(issuer => issuer.Audience).ToList(),
                // Workload tokens are signed with the issuer's private key. Refusing every other algorithm
                // keeps a token from choosing how it is checked.
                ValidAlgorithms = AsymmetricAlgorithms,
                ValidateIssuerSigningKey = true,
                RequireSignedTokens = true,
                RequireExpirationTime = true,
                ClockSkew = settings.ClockSkew,
                // The handler fetches the signing keys from here, and refetches them once when a token names a key
                // it has not seen, which is how key rotation reaches a long-running site.
                ConfigurationManager = configurationProvider.Get(group.Key),
                LifetimeValidator = (notBefore, expires, _, validation) => IsWithinLifetime(notBefore, expires, validation.ClockSkew),
            };

            var result = await _handler.ValidateTokenAsync(token, parameters);
            if (!result.IsValid)
            {
                failures.Add(result.Exception?.Message ?? "The token is not valid.");
                continue;
            }

            var jwt = (JsonWebToken)result.SecurityToken;
            foreach (var issuer in group)
            {
                if (!jwt.Audiences.Contains(issuer.Audience, StringComparer.Ordinal))
                {
                    failures.Add($"{issuer.Name}: the audience is not \"{issuer.Audience}\".");
                    continue;
                }

                var unmet = issuer.Rules.FirstOrDefault(rule => !rule.IsSatisfiedBy(name => Read(result.Claims, name)));
                if (unmet is not null)
                {
                    failures.Add($"{issuer.Name}: the token does not have {unmet}.");
                    continue;
                }

                return OidcValidationResult.Valid(new OidcIdentity(issuer.Name, issuer.Issuer, jwt.Subject, issuer.AllowPreview)
                {
                    Claims = result.ClaimsIdentity.Claims.ToList(),
                });
            }
        }

        return OidcValidationResult.Invalid(string.Join(" ", failures));
    }

    private bool IsWithinLifetime(DateTime? notBefore, DateTime? expires, TimeSpan clockSkew)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        // The skew is applied to now rather than to the token's dates, because a token can carry a date so far
        // out that adding to it overflows.
        return expires is { } expiry && now - clockSkew <= expiry &&
               (notBefore is not { } start || now + clockSkew >= start);
    }

    private static string? Read(IDictionary<string, object> claims, string name) =>
        claims.TryGetValue(name, out var value) && value is string text && !string.IsNullOrWhiteSpace(text) ? text : null;
}
