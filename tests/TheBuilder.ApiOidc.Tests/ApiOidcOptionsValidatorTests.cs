using TheBuilder.ApiOidc.Configuration;

namespace TheBuilder.ApiOidc.Tests;

public sealed class ApiOidcOptionsValidatorTests
{
    private static bool IsValid(params TrustedIssuerOptions[] issuers) =>
        new ApiOidcOptionsValidator().Validate(null, FakeIssuers.Options(issuers)).Succeeded;

    [Fact]
    public void Accepts_the_package_being_off() => Assert.True(IsValid());

    [Fact]
    public void Accepts_the_presets() => Assert.True(IsValid(FakeIssuers.Vercel(), FakeIssuers.GitHub(), FakeIssuers.Azure()));

    [Fact]
    public void Requires_a_vercel_team() =>
        Assert.False(IsValid(new TrustedIssuerOptions { Preset = IssuerPreset.Vercel, Projects = ["acme-web"] }));

    [Fact]
    public void Refuses_a_vercel_team_url_in_place_of_the_slug() =>
        Assert.False(IsValid(new TrustedIssuerOptions { Preset = IssuerPreset.Vercel, Team = "vercel.com/acme", Projects = ["acme-web"] }));

    [Fact]
    public void Requires_a_github_owner()
    {
        var github = FakeIssuers.GitHub();
        github.Owner = null;

        Assert.False(IsValid(github));
    }

    [Fact]
    public void Refuses_a_github_repository_of_another_owner()
    {
        var github = FakeIssuers.GitHub();
        github.Repositories = ["acme/site", "other/site"];

        Assert.False(IsValid(github));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("contoso.onmicrosoft.com")]
    public void Requires_an_azure_tenant_id(string? tenantId)
    {
        var azure = FakeIssuers.Azure();
        azure.TenantId = tenantId;

        Assert.False(IsValid(azure));
    }

    [Fact]
    public void Requires_an_azure_audience()
    {
        var azure = FakeIssuers.Azure();
        azure.Audience = null;

        Assert.False(IsValid(azure));
    }

    [Fact]
    public void Requires_an_azure_principal()
    {
        var azure = FakeIssuers.Azure();
        azure.PrincipalIds = [];

        Assert.False(IsValid(azure));
    }

    [Fact]
    public void Refuses_an_issuer_on_the_azure_preset()
    {
        var azure = FakeIssuers.Azure();
        azure.Issuer = $"https://login.microsoftonline.us/{FakeIssuers.AzureTenant}/v2.0";

        Assert.False(IsValid(azure));
    }

    [Fact]
    public void Refuses_an_azure_principal_that_is_not_an_object_id()
    {
        var azure = FakeIssuers.Azure();
        azure.PrincipalIds = ["/subscriptions/123/resourceGroups/web/providers/Microsoft.ManagedIdentity/userAssignedIdentities/renderer"];

        Assert.False(IsValid(azure));
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
