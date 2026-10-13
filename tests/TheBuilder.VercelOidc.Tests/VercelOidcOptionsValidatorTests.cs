using TheBuilder.VercelOidc.Configuration;

namespace TheBuilder.VercelOidc.Tests;

public sealed class VercelOidcOptionsValidatorTests
{
    private static bool IsValid(VercelOidcOptions options) =>
        new VercelOidcOptionsValidator().Validate(null, options).Succeeded;

    [Fact]
    public void Accepts_the_package_being_off() => Assert.True(IsValid(new VercelOidcOptions()));

    [Fact]
    public void Accepts_a_team_and_a_project() => Assert.True(IsValid(FakeVercelIssuer.Options()));

    [Fact]
    public void Requires_a_project_once_a_team_is_set() =>
        Assert.False(IsValid(new VercelOidcOptions { TeamSlug = "acme" }));

    [Fact]
    public void Refuses_a_team_url_in_place_of_the_slug() =>
        Assert.False(IsValid(new VercelOidcOptions { TeamSlug = "vercel.com/acme", Projects = ["acme-web"] }));

    [Fact]
    public void Requires_an_environment()
    {
        var options = FakeVercelIssuer.Options();
        options.Environments = [];

        Assert.False(IsValid(options));
    }
}
