using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using TheBuilder.VercelOidc.Configuration;

namespace TheBuilder.VercelOidc.Tests;

internal sealed class TestOptionsMonitor<T>(T value) : IOptionsMonitor<T>
{
    public T CurrentValue { get; set; } = value;

    public T Get(string? name) => CurrentValue;

    public IDisposable? OnChange(Action<T, string?> listener) => null;
}

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}

/// <summary>
///     Stands in for Vercel: a signing key, the configuration that publishes it, and tokens shaped like the ones
///     Vercel issues to a function.
/// </summary>
internal sealed class FakeVercelIssuer
{
    public static readonly DateTimeOffset Now = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);

    private readonly RsaSecurityKey _key = new(RSA.Create(2048)) { KeyId = "vercel-test-key" };

    public StaticConfigurationManager<OpenIdConnectConfiguration> ConfigurationManager
    {
        get
        {
            var configuration = new OpenIdConnectConfiguration { Issuer = "https://oidc.vercel.com/acme" };
            configuration.SigningKeys.Add(_key);
            return new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
        }
    }

    public static VercelOidcOptions Options() => new()
    {
        TeamSlug = "acme",
        Projects = ["acme-web"],
    };

    public string Issue(
        string issuer = "https://oidc.vercel.com/acme",
        string audience = "https://vercel.com/acme",
        string owner = "acme",
        string project = "acme-web",
        string projectId = "prj_123",
        string environment = "production",
        DateTimeOffset? expires = null,
        SecurityKey? signingKey = null,
        IDictionary<string, object>? omit = null)
    {
        var claims = new Dictionary<string, object>
        {
            ["sub"] = $"owner:{owner}:project:{project}:environment:{environment}",
            [Constants.Claims.Owner] = owner,
            [Constants.Claims.OwnerId] = "team_123",
            [Constants.Claims.Project] = project,
            [Constants.Claims.ProjectId] = projectId,
            [Constants.Claims.Environment] = environment,
        };
        foreach (var name in omit?.Keys ?? [])
            claims.Remove(name);

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Claims = claims,
            IssuedAt = Now.UtcDateTime,
            NotBefore = Now.UtcDateTime,
            Expires = (expires ?? Now.AddHours(1)).UtcDateTime,
            SigningCredentials = new SigningCredentials(signingKey ?? _key, SecurityAlgorithms.RsaSha256),
        });
    }
}
