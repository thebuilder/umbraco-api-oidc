using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using TheBuilder.VercelOidc.Configuration;
using TheBuilder.VercelOidc.Tokens;

namespace TheBuilder.VercelOidc.Tests;

public sealed class VercelOidcTokenValidatorTests
{
    private readonly FakeVercelIssuer _vercel = new();

    private VercelOidcTokenValidator Validator(VercelOidcOptions? options = null) => new(
        new TestOptionsMonitor<VercelOidcOptions>(options ?? FakeVercelIssuer.Options()),
        _vercel.ConfigurationManager,
        new FixedTimeProvider(FakeVercelIssuer.Now));

    private Task<VercelOidcValidationResult> Validate(string token, VercelOidcOptions? options = null) =>
        Validator(options).ValidateAsync(token, TestContext.Current.CancellationToken);

    [Fact]
    public async Task Accepts_a_token_from_an_allowed_project_and_environment()
    {
        var result = await Validate(_vercel.Issue());

        Assert.True(result.IsValid);
        Assert.Equal(new VercelOidcIdentity("acme", "team_123", "acme-web", "prj_123", "production"), result.Identity);
    }

    [Fact]
    public async Task Accepts_a_project_listed_by_id()
    {
        var options = FakeVercelIssuer.Options();
        options.Projects = ["prj_123"];

        Assert.True((await Validate(_vercel.Issue(), options)).IsValid);
    }

    [Fact]
    public async Task Refuses_a_project_that_is_not_listed()
    {
        var result = await Validate(_vercel.Issue(project: "other-site", projectId: "prj_999"));

        Assert.False(result.IsValid);
        Assert.Contains("other-site", result.Failure);
    }

    [Fact]
    public async Task Refuses_development_tokens_by_default()
    {
        Assert.False((await Validate(_vercel.Issue(environment: "development"))).IsValid);
    }

    [Fact]
    public async Task Accepts_development_tokens_when_listed()
    {
        var options = FakeVercelIssuer.Options();
        options.Environments = ["development"];

        Assert.True((await Validate(_vercel.Issue(environment: "development"), options)).IsValid);
    }

    [Fact]
    public async Task Refuses_another_teams_issuer()
    {
        Assert.False((await Validate(_vercel.Issue(issuer: "https://oidc.vercel.com/evil"))).IsValid);
    }

    [Fact]
    public async Task Refuses_another_teams_token_in_global_issuer_mode()
    {
        var options = FakeVercelIssuer.Options();
        options.IssuerMode = VercelIssuerMode.Global;

        var result = await Validate(
            _vercel.Issue(issuer: "https://oidc.vercel.com", owner: "evil"),
            options);

        Assert.False(result.IsValid);
        Assert.Contains("evil", result.Failure);
    }

    [Fact]
    public async Task Accepts_the_shared_issuer_in_global_issuer_mode()
    {
        var options = FakeVercelIssuer.Options();
        options.IssuerMode = VercelIssuerMode.Global;

        Assert.True((await Validate(_vercel.Issue(issuer: "https://oidc.vercel.com"), options)).IsValid);
    }

    [Fact]
    public async Task Refuses_the_wrong_audience()
    {
        Assert.False((await Validate(_vercel.Issue(audience: "https://api.example.com"))).IsValid);
    }

    [Fact]
    public async Task Accepts_a_configured_custom_audience()
    {
        var options = FakeVercelIssuer.Options();
        options.Audience = "https://cms.example.com";

        Assert.True((await Validate(_vercel.Issue(audience: "https://cms.example.com"), options)).IsValid);
    }

    [Fact]
    public async Task Refuses_an_expired_token()
    {
        Assert.False((await Validate(_vercel.Issue(expires: FakeVercelIssuer.Now.AddMinutes(-5)))).IsValid);
    }

    [Fact]
    public async Task Allows_clock_skew_on_expiry()
    {
        Assert.True((await Validate(_vercel.Issue(expires: FakeVercelIssuer.Now.AddSeconds(-30)))).IsValid);
    }

    [Fact]
    public async Task Refuses_a_token_signed_by_another_key()
    {
        var forged = new RsaSecurityKey(RSA.Create(2048)) { KeyId = "vercel-test-key" };

        Assert.False((await Validate(_vercel.Issue(signingKey: forged))).IsValid);
    }

    [Fact]
    public async Task Refuses_a_token_without_deployment_claims()
    {
        var token = _vercel.Issue(omit: new Dictionary<string, object> { [Constants.Claims.Environment] = true });

        Assert.False((await Validate(token)).IsValid);
    }

    [Fact]
    public async Task Refuses_everything_when_not_configured()
    {
        Assert.False((await Validate(_vercel.Issue(), new VercelOidcOptions())).IsValid);
    }

    [Fact]
    public async Task Refuses_garbage()
    {
        Assert.False((await Validate("not.a.jwt")).IsValid);
    }
}
