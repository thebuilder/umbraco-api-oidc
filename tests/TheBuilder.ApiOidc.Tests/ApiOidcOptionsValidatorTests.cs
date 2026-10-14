using TheBuilder.ApiOidc.Configuration;

namespace TheBuilder.ApiOidc.Tests;

public sealed class ApiOidcOptionsValidatorTests
{
    private static bool IsValid(params TrustedIssuerOptions[] issuers) =>
        new ApiOidcOptionsValidator().Validate(null, FakeIssuers.Options(issuers)).Succeeded;

    [Fact]
    public void Accepts_the_package_being_off() => Assert.True(IsValid());

    [Fact]
    public void Accepts_the_presets() => Assert.True(IsValid(FakeIssuers.Vercel(), FakeIssuers.GitHub()));

    [Fact]
    public void Requires_a_vercel_team() =>
        Assert.False(IsValid(new TrustedIssuerOptions { Preset = IssuerPreset.Vercel, Projects = ["acme-web"] }));

    [Fact]
    public void Refuses_a_vercel_team_url_in_place_of_the_slug() =>
        Assert.False(IsValid(new TrustedIssuerOptions { Preset = IssuerPreset.Vercel, Team = "vercel.com/acme", Projects = ["acme-web"] }));

    [Fact]
    public void Requires_a_github_audience()
    {
        var github = FakeIssuers.GitHub();
        github.Audience = null;

        Assert.False(IsValid(github));
    }

    [Fact]
    public void Requires_a_github_repository()
    {
        var github = FakeIssuers.GitHub();
        github.Repositories = [];

        Assert.False(IsValid(github));
    }

    [Fact]
    public void Requires_claims_on_a_custom_issuer() =>
        Assert.False(IsValid(new TrustedIssuerOptions { Issuer = "https://accounts.google.com", Audience = "https://cms.example.com" }));

    [Fact]
    public void Requires_an_https_issuer() =>
        Assert.False(IsValid(new TrustedIssuerOptions
        {
            Issuer = "http://issuer.example.com",
            Audience = "https://cms.example.com",
            RequiredClaims = new() { ["sub"] = ["x"] },
        }));
}
