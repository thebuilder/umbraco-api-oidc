using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using TheBuilder.ApiOidc.Configuration;
using TheBuilder.ApiOidc.Tokens;

namespace TheBuilder.ApiOidc.Tests;

public sealed class OidcTokenValidatorTests
{
    private readonly FakeIssuers _issuers = new();

    private Task<OidcValidationResult> Validate(string token, params TrustedIssuerOptions[] issuers) =>
        _issuers.Validator(FakeIssuers.Options(issuers.Length == 0 ? [FakeIssuers.Vercel()] : issuers))
            .ValidateAsync(token, TestContext.Current.CancellationToken);

    [Fact]
    public async Task Accepts_a_vercel_token_from_an_allowed_project_and_environment()
    {
        var result = await Validate(_issuers.VercelToken());

        Assert.True(result.IsValid);
        Assert.Equal("Vercel", result.Identity!.IssuerName);
        Assert.Equal("owner:acme:project:acme-web:environment:production", result.Identity.Subject);
        Assert.Equal(["https://oidc.vercel.com/acme/.well-known/openid-configuration"], _issuers.RequestedAddresses);
    }

    [Fact]
    public async Task Accepts_a_vercel_project_listed_by_id()
    {
        var vercel = FakeIssuers.Vercel();
        vercel.Projects = ["prj_123"];

        Assert.True((await Validate(_issuers.VercelToken(), vercel)).IsValid);
    }

    [Fact]
    public async Task Refuses_a_vercel_project_that_is_not_listed()
    {
        var result = await Validate(_issuers.VercelToken(project: "other-site", projectId: "prj_999"));

        Assert.False(result.IsValid);
        Assert.Contains("acme-web", result.Failure);
    }

    [Fact]
    public async Task Refuses_vercel_development_tokens_by_default()
    {
        Assert.True((await Validate(_issuers.VercelToken(environment: "preview"))).IsValid);
        Assert.False((await Validate(_issuers.VercelToken(environment: "development"))).IsValid);
    }

    [Fact]
    public async Task Accepts_vercel_development_tokens_when_listed()
    {
        var vercel = FakeIssuers.Vercel();
        vercel.Environments = ["production", "development"];

        Assert.True((await Validate(_issuers.VercelToken(environment: "development"), vercel)).IsValid);
        Assert.False((await Validate(_issuers.VercelToken(environment: "preview"), vercel)).IsValid);
    }

    [Fact]
    public async Task Accepts_every_project_in_the_team_without_a_project_list()
    {
        var vercel = FakeIssuers.Vercel();
        vercel.Projects = [];

        Assert.True((await Validate(_issuers.VercelToken(project: "other-site", projectId: "prj_999"), vercel)).IsValid);
        Assert.False((await Validate(_issuers.VercelToken(issuer: "https://oidc.vercel.com/evil", owner: "evil"), vercel)).IsValid);
    }

    [Fact]
    public async Task Refuses_a_vercel_token_without_an_environment()
    {
        Assert.False((await Validate(_issuers.VercelToken(environment: null))).IsValid);
    }

    [Fact]
    public async Task Refuses_another_vercel_team_in_global_issuer_mode()
    {
        var vercel = FakeIssuers.Vercel();
        vercel.IssuerMode = VercelIssuerMode.Global;

        var result = await Validate(_issuers.VercelToken(issuer: "https://oidc.vercel.com", owner: "evil"), vercel);

        Assert.False(result.IsValid);
        Assert.Contains("owner", result.Failure);
    }

    [Fact]
    public async Task Accepts_the_shared_issuer_in_vercel_global_issuer_mode()
    {
        var vercel = FakeIssuers.Vercel();
        vercel.IssuerMode = VercelIssuerMode.Global;

        Assert.True((await Validate(_issuers.VercelToken(issuer: "https://oidc.vercel.com"), vercel)).IsValid);
    }

    [Fact]
    public async Task Refuses_an_issuer_that_is_not_trusted()
    {
        var result = await Validate(_issuers.VercelToken(issuer: "https://oidc.vercel.com/evil"));

        Assert.False(result.IsValid);
        Assert.Contains("not trusted", result.Failure);
        Assert.Empty(_issuers.RequestedAddresses);
    }

    [Fact]
    public async Task Refuses_the_wrong_audience()
    {
        Assert.False((await Validate(_issuers.VercelToken(audience: "https://api.example.com"))).IsValid);
    }

    [Fact]
    public async Task Accepts_a_configured_custom_audience()
    {
        var vercel = FakeIssuers.Vercel();
        vercel.Audience = "https://cms.example.com";

        Assert.True((await Validate(_issuers.VercelToken(audience: "https://cms.example.com"), vercel)).IsValid);
    }

    [Fact]
    public async Task Refuses_an_expired_token()
    {
        Assert.False((await Validate(_issuers.VercelToken(expires: FakeIssuers.Now.AddMinutes(-5)))).IsValid);
    }

    [Fact]
    public async Task Allows_clock_skew_on_expiry()
    {
        Assert.True((await Validate(_issuers.VercelToken(expires: FakeIssuers.Now.AddSeconds(-30)))).IsValid);
    }

    [Fact]
    public async Task Handles_an_expiry_at_the_end_of_time()
    {
        Assert.True((await Validate(_issuers.VercelToken(expires: DateTimeOffset.MaxValue.AddSeconds(-1)))).IsValid);
    }

    [Fact]
    public async Task Refuses_a_token_signed_by_another_key()
    {
        var forged = new RsaSecurityKey(RSA.Create(2048)) { KeyId = "test-key" };

        Assert.False((await Validate(_issuers.VercelToken(signingKey: forged))).IsValid);
    }

    [Fact]
    public async Task Refuses_a_symmetric_signature_even_with_a_published_key()
    {
        var shared = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32)) { KeyId = "shared" };
        _issuers.ExtraKeys.Add(shared);

        Assert.False((await Validate(_issuers.VercelToken(signingKey: shared))).IsValid);
    }

    [Fact]
    public async Task Keeps_an_untrusted_issuer_short_in_the_failure()
    {
        var result = await Validate(_issuers.VercelToken(issuer: "https://" + new string('a', 5000)));

        Assert.True(result.Failure!.Length < 300);
    }

    [Fact]
    public async Task Refuses_garbage()
    {
        Assert.False((await Validate("not.a.jwt")).IsValid);
    }

    [Fact]
    public async Task Accepts_a_github_actions_token_from_an_allowed_repository()
    {
        var result = await Validate(_issuers.GitHubToken(), FakeIssuers.GitHub());

        Assert.True(result.IsValid);
        Assert.Equal("GitHubActions", result.Identity!.IssuerName);
    }

    [Fact]
    public async Task Matches_github_repositories_ignoring_case()
    {
        Assert.True((await Validate(_issuers.GitHubToken(repository: "Acme/Site"), FakeIssuers.GitHub())).IsValid);
    }

    [Fact]
    public async Task Refuses_a_github_actions_token_from_another_repository()
    {
        Assert.False((await Validate(_issuers.GitHubToken(repository: "acme/other"), FakeIssuers.GitHub())).IsValid);
        Assert.False((await Validate(_issuers.GitHubToken(repository: "evil/site"), FakeIssuers.GitHub())).IsValid);
    }

    [Fact]
    public async Task Accepts_every_repository_of_the_owner_without_a_repository_list()
    {
        var github = FakeIssuers.GitHub();
        github.Repositories = [];

        Assert.True((await Validate(_issuers.GitHubToken(repository: "acme/other"), github)).IsValid);
        Assert.False((await Validate(_issuers.GitHubToken(repository: "evil/site"), github)).IsValid);
    }

    [Fact]
    public async Task Refuses_a_github_actions_token_from_another_ref_when_refs_are_listed()
    {
        var github = FakeIssuers.GitHub();
        github.Refs = ["refs/heads/main"];

        Assert.False((await Validate(_issuers.GitHubToken(reference: "refs/heads/feature"), github)).IsValid);
    }

    [Fact]
    public async Task Refuses_githubs_default_audience_when_an_audience_is_set()
    {
        Assert.False((await Validate(_issuers.GitHubToken(audience: "https://github.com/acme"), FakeIssuers.GitHub())).IsValid);
    }

    [Fact]
    public async Task Defaults_to_githubs_own_audience()
    {
        var github = FakeIssuers.GitHub();
        github.Audience = null;

        Assert.True((await Validate(_issuers.GitHubToken(audience: "https://github.com/acme"), github)).IsValid);
        Assert.False((await Validate(_issuers.GitHubToken(audience: "https://cms.example.com"), github)).IsValid);
    }

    [Theory]
    [InlineData("renderer@acme.iam.gserviceaccount.com", true)]
    [InlineData("someone@gmail.com", false)]
    public async Task Checks_a_custom_issuers_required_claims(string email, bool valid)
    {
        var google = new TrustedIssuerOptions
        {
            Issuer = "https://accounts.google.com",
            Audience = "https://cms.example.com",
            RequiredClaims = new() { ["email"] = ["renderer@acme.iam.gserviceaccount.com"] },
        };
        var token = _issuers.Issue("https://accounts.google.com", "https://cms.example.com",
            new() { ["sub"] = "1234", ["email"] = email });

        Assert.Equal(valid, (await Validate(token, google)).IsValid);
    }

    [Fact]
    public async Task Tries_every_entry_that_shares_an_issuer()
    {
        var marketing = FakeIssuers.GitHub();
        marketing.Name = "marketing";
        var shop = FakeIssuers.GitHub();
        shop.Name = "shop";
        shop.Audience = "https://shop-cms.example.com";
        shop.Repositories = ["acme/shop"];
        shop.AllowPreview = false;

        var result = await Validate(
            _issuers.GitHubToken(audience: "https://shop-cms.example.com", repository: "acme/shop"),
            marketing,
            shop);

        Assert.Equal("shop", result.Identity?.IssuerName);
        Assert.False(result.Identity!.AllowPreview);
    }

    [Fact]
    public async Task Accepts_an_azure_token_from_an_allowed_identity()
    {
        var result = await Validate(_issuers.AzureToken(), FakeIssuers.Azure());

        Assert.True(result.IsValid);
        Assert.Equal("Azure", result.Identity!.IssuerName);
        Assert.Equal(
            [$"https://login.microsoftonline.com/{FakeIssuers.AzureTenant}/v2.0/.well-known/openid-configuration"],
            _issuers.RequestedAddresses);
    }

    [Fact]
    public async Task Accepts_an_azure_v1_token_with_the_keys_entra_id_publishes_for_it()
    {
        var result = await Validate(_issuers.AzureToken(v1: true), FakeIssuers.Azure());

        Assert.True(result.IsValid);
        Assert.Equal("Azure", result.Identity!.IssuerName);
        Assert.Equal(
            [$"https://login.microsoftonline.com/{FakeIssuers.AzureTenant}/.well-known/openid-configuration"],
            _issuers.RequestedAddresses);
    }

    [Fact]
    public async Task Refuses_another_identity_in_the_azure_tenant()
    {
        var result = await Validate(_issuers.AzureToken(principal: "0b6f8c2e-1d4a-4e7b-8c3f-5a9d2e6b1c07"), FakeIssuers.Azure());

        Assert.False(result.IsValid);
        Assert.Contains(FakeIssuers.AzurePrincipal, result.Failure);
    }

    [Fact]
    public async Task Refuses_an_azure_token_from_another_tenant()
    {
        var token = _issuers.AzureToken(tenant: "f8cdef31-a31e-4b4a-93e4-5f571e91255a");

        Assert.False((await Validate(token, FakeIssuers.Azure())).IsValid);
    }

    [Fact]
    public async Task Refuses_an_azure_token_for_another_app()
    {
        var token = _issuers.AzureToken(audience: "https://management.azure.com");

        Assert.False((await Validate(token, FakeIssuers.Azure())).IsValid);
    }

    [Fact]
    public async Task Matches_azure_ids_ignoring_case()
    {
        var azure = FakeIssuers.Azure();
        azure.TenantId = FakeIssuers.AzureTenant.ToUpperInvariant();
        azure.PrincipalIds = [FakeIssuers.AzurePrincipal.ToUpperInvariant()];

        Assert.True((await Validate(_issuers.AzureToken(), azure)).IsValid);
        Assert.True((await Validate(_issuers.AzureToken(v1: true), azure)).IsValid);
    }
}
