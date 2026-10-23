using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using TheBuilder.ApiOidc.Configuration;
using TheBuilder.ApiOidc.Tokens;

namespace TheBuilder.ApiOidc.Tests;

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
///     the ones Vercel, GitHub Actions, GitLab, Google and Entra ID issue.
/// </summary>
internal sealed class FakeIssuers : IIssuerConfigurationProvider
{
    public static readonly DateTimeOffset Now = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);

    private readonly RsaSecurityKey _key = new(RSA.Create(2048)) { KeyId = "test-key" };

    public List<string> RequestedAddresses { get; } = [];

    /// <summary>Keys published beside the real one, to test what the validator refuses to use.</summary>
    public List<SecurityKey> ExtraKeys { get; } = [];

    public BaseConfigurationManager Get(string metadataAddress)
    {
        RequestedAddresses.Add(metadataAddress);
        var configuration = new OpenIdConnectConfiguration();
        configuration.SigningKeys.Add(_key);
        foreach (var key in ExtraKeys)
            configuration.SigningKeys.Add(key);
        return new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
    }

    public OidcTokenValidator Validator(ApiOidcOptions options) =>
        new(new TestOptionsMonitor<ApiOidcOptions>(options), this, new FixedTimeProvider(Now));

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
        Owner = "acme",
        Repositories = ["acme/site"],
    };

    public static TrustedIssuerOptions GitLab() => new()
    {
        Preset = IssuerPreset.GitLab,
        Audience = "https://cms.example.com",
        Namespace = "acme",
        Projects = ["acme/site"],
    };

    public const string GoogleServiceAccountId = "104263578915240387162";

    public static TrustedIssuerOptions GoogleCloud() => new()
    {
        Preset = IssuerPreset.GoogleCloud,
        Audience = "https://cms.example.com",
        ServiceAccounts = ["renderer@acme.iam.gserviceaccount.com"],
    };

    public const string AzureTenant = "3f2b8c1d-6e4a-4b7f-9d2c-8a1e5f3b7c90";
    public const string AzureApp = "6e74172b-be56-4843-9ff4-e66a39bb12e3";
    public const string AzurePrincipal = "e9b1f3a0-4c2d-4f7e-9a51-3b8c6d2e1f04";

    public static TrustedIssuerOptions Azure() => new()
    {
        Preset = IssuerPreset.Azure,
        TenantId = AzureTenant,
        Audience = AzureApp,
        PrincipalIds = [AzurePrincipal],
    };

    public static ApiOidcOptions Options(params TrustedIssuerOptions[] issuers) => new() { Issuers = issuers.ToList() };

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
            ["repository_owner"] = repository.Split('/')[0],
            ["ref"] = reference,
        });

    public string GitLabToken(
        string project = "acme/site",
        string refPath = "refs/heads/main",
        string issuer = "https://gitlab.com",
        string audience = "https://cms.example.com")
    {
        var claims = new Dictionary<string, object>
        {
            ["sub"] = $"project_path:{project}:ref_type:branch:ref:main",
            ["namespace_path"] = project[..project.LastIndexOf('/')],
            ["project_path"] = project,
            ["ref"] = refPath.Split('/')[^1],
            ["ref_path"] = refPath,
        };
        return Issue(issuer, audience, claims);
    }

    /// <summary>A metadata server ID token, with the email only when it was requested in full format.</summary>
    public string GoogleToken(
        string? email = "renderer@acme.iam.gserviceaccount.com",
        string subject = GoogleServiceAccountId,
        string audience = "https://cms.example.com")
    {
        var claims = new Dictionary<string, object> { ["sub"] = subject, ["azp"] = subject };
        if (email is not null)
        {
            claims["email"] = email;
            claims["email_verified"] = true;
        }

        return Issue("https://accounts.google.com", audience, claims);
    }

    /// <summary>An app-only access token for a managed identity, in the v2.0 format or, with v1, the v1.0 one.</summary>
    public string AzureToken(
        bool v1 = false,
        string tenant = AzureTenant,
        string audience = AzureApp,
        string principal = AzurePrincipal) =>
        Issue(
            v1 ? $"https://sts.windows.net/{tenant}/" : $"https://login.microsoftonline.com/{tenant}/v2.0",
            audience,
            new Dictionary<string, object>
            {
                ["sub"] = principal,
                ["oid"] = principal,
                ["tid"] = tenant,
                [v1 ? "appid" : "azp"] = "0d4e2f9b-7a31-4c58-b6e2-91f3a8d5c720",
                ["ver"] = v1 ? "1.0" : "2.0",
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
            SigningCredentials = new SigningCredentials(
                signingKey ?? _key,
                signingKey is SymmetricSecurityKey ? SecurityAlgorithms.HmacSha256 : SecurityAlgorithms.RsaSha256),
        });
}
