using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using TheBuilder.DeliveryApiOidc.Configuration;
using TheBuilder.DeliveryApiOidc.Tokens;

namespace TheBuilder.DeliveryApiOidc.Tests;

internal sealed class TestOptionsMonitor<T>(T value) : IOptionsMonitor<T>
{
    public T CurrentValue { get; set; } = value;

    public T Get(string? name) => CurrentValue;

    public IDisposable? OnChange(Action<T, string?> listener) => null;
}

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

/// <summary>
///     Stands in for the issuers: one signing key, published at every metadata address, and tokens shaped like
///     the ones Vercel and GitHub Actions issue.
/// </summary>
internal sealed class FakeIssuers : IIssuerConfigurationProvider
{
    public static readonly DateTimeOffset Now = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);

    private readonly RsaSecurityKey _key = new(RSA.Create(2048)) { KeyId = "test-key" };

    public List<string> RequestedAddresses { get; } = [];

    public BaseConfigurationManager Get(string metadataAddress)
    {
        RequestedAddresses.Add(metadataAddress);
        var configuration = new OpenIdConnectConfiguration();
        configuration.SigningKeys.Add(_key);
        return new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
    }

    public OidcTokenValidator Validator(DeliveryApiOidcOptions options) =>
        new(new TestOptionsMonitor<DeliveryApiOidcOptions>(options), this, new FixedTimeProvider(Now));

    public static TrustedIssuerOptions Vercel() => new()
    {
        Preset = IssuerPreset.Vercel,
        Team = "acme",
        Projects = ["acme-web"],
    };

    public static TrustedIssuerOptions GitHub() => new()
    {
        Preset = IssuerPreset.GitHubActions,
        Audience = "https://cms.example.com",
        Repositories = ["acme/site"],
    };

    public static DeliveryApiOidcOptions Options(params TrustedIssuerOptions[] issuers) => new() { Issuers = issuers.ToList() };

    public string VercelToken(
        string issuer = "https://oidc.vercel.com/acme",
        string audience = "https://vercel.com/acme",
        string owner = "acme",
        string project = "acme-web",
        string projectId = "prj_123",
        string? environment = "production",
        DateTimeOffset? expires = null,
        SecurityKey? signingKey = null)
    {
        var claims = new Dictionary<string, object>
        {
            ["sub"] = $"owner:{owner}:project:{project}:environment:{environment}",
            ["owner"] = owner,
            ["owner_id"] = "team_123",
            ["project"] = project,
            ["project_id"] = projectId,
        };
        if (environment is not null)
            claims["environment"] = environment;
        return Issue(issuer, audience, claims, expires, signingKey);
    }

    public string GitHubToken(
        string audience = "https://cms.example.com",
        string repository = "acme/site",
        string reference = "refs/heads/main") =>
        Issue("https://token.actions.githubusercontent.com", audience, new Dictionary<string, object>
        {
            ["sub"] = $"repo:{repository}:ref:{reference}",
            ["repository"] = repository,
            ["ref"] = reference,
        });

    public string Issue(
        string issuer,
        string audience,
        Dictionary<string, object> claims,
        DateTimeOffset? expires = null,
        SecurityKey? signingKey = null) =>
        new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
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
