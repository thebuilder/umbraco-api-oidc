using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TheBuilder.ApiOidc.Authentication;
using TheBuilder.ApiOidc.Composition;
using TheBuilder.ApiOidc.Tokens;

namespace TheBuilder.ApiOidc.Tests;

public sealed class EndpointProtectionTests : IAsyncLifetime
{
    private readonly FakeIssuers _issuers = new();
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ApiOidc:Issuers:0:Preset"] = "Vercel",
            ["ApiOidc:Issuers:0:Team"] = "acme",
            ["ApiOidc:Issuers:0:Projects:0"] = "acme-web",
            ["ApiOidc:Issuers:1:Name"] = "deploy",
            ["ApiOidc:Issuers:1:Preset"] = "GitHubActions",
            ["ApiOidc:Issuers:1:Audience"] = "https://cms.example.com",
            ["ApiOidc:Issuers:1:Repositories:0"] = "acme/site",
        });
        builder.Services.AddSingleton<TimeProvider>(new FixedTimeProvider(FakeIssuers.Now));
        ApiOidcComposer.AddServices(builder.Services, builder.Configuration);
        builder.Services.AddSingleton<IIssuerConfigurationProvider>(_issuers);
        builder.Services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, AlwaysSignedInHandler>(AlwaysSignedInHandler.SchemeName, null);
        builder.Services.AddControllers().AddApplicationPart(typeof(EndpointProtectionTests).Assembly);

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapGet("/open", () => "open");
        _app.MapGet("/search", (HttpContext context) => context.User.FindFirst("project")?.Value ?? context.User.Identity?.Name)
            .RequireOidcToken();
        _app.MapGet("/deploy-only", () => "deploy").RequireOidcToken("deploy");
        _app.MapGet("/policy", () => "policy").RequireAuthorization(OidcAuthenticationDefaults.Policy);
        _app.MapGet("/stacked", () => "stacked")
            .RequireAuthorization(new AuthorizeAttribute { AuthenticationSchemes = AlwaysSignedInHandler.SchemeName })
            .RequireOidcToken("deploy");
        _app.MapControllers();
        await _app.StartAsync(TestContext.Current.CancellationToken);
        _client = _app.GetTestClient();
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    private Task<HttpResponseMessage> Get(string path, string? token = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (token is not null)
            request.Headers.Add("X-Oidc-Token", token);
        return _client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Refuses_a_protected_endpoint_without_a_token()
    {
        var response = await Get("/search");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refuses_a_protected_endpoint_with_an_invalid_token()
    {
        var response = await Get("/search", _issuers.VercelToken(project: "other"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Serves_a_protected_endpoint_and_exposes_the_token_claims()
    {
        var response = await Get("/search", _issuers.VercelToken());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("acme-web", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Refuses_an_issuer_the_endpoint_does_not_accept()
    {
        var response = await Get("/deploy-only", _issuers.VercelToken());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Serves_an_issuer_the_endpoint_accepts()
    {
        var response = await Get("/deploy-only", _issuers.GitHubToken());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Supports_the_named_policy()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await Get("/policy")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Get("/policy", _issuers.GitHubToken())).StatusCode);
    }

    [Fact]
    public async Task Supports_the_attribute_on_a_controller()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await Get("/controller/search")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Get("/controller/search", _issuers.GitHubToken())).StatusCode);

        var response = await Get("/controller/search", _issuers.VercelToken());
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Vercel", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Ignores_an_issuer_name_claim_inside_the_token()
    {
        var token = _issuers.Issue("https://oidc.vercel.com/acme", "https://vercel.com/acme", new Dictionary<string, object>
        {
            ["sub"] = "owner:acme:project:acme-web:environment:production",
            ["owner"] = "acme",
            ["project"] = "acme-web",
            ["environment"] = "production",
            [OidcAuthenticationDefaults.IssuerNameClaimType] = "deploy",
        });

        var response = await Get("/deploy-only", token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Is_not_satisfied_by_another_scheme_on_the_same_endpoint()
    {
        Assert.Equal(HttpStatusCode.Forbidden, (await Get("/stacked")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Get("/stacked", _issuers.GitHubToken())).StatusCode);
    }

    [Fact]
    public async Task Leaves_endpoints_that_do_not_opt_in_alone()
    {
        var response = await Get("/open", "not.a.token");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

[ApiController]
[Route("controller")]
public sealed class ProtectedTestController : ControllerBase
{
    [HttpGet("search")]
    [RequireOidcToken("Vercel")]
    public string Search() => User.FindFirst(OidcAuthenticationDefaults.IssuerNameClaimType)!.Value;
}

/// <summary>Stands in for a cookie or other scheme that signs everyone in, with a claim that looks like ours.</summary>
internal sealed class AlwaysSignedInHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "AlwaysSignedIn";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var identity = new ClaimsIdentity([new Claim(OidcAuthenticationDefaults.IssuerNameClaimType, "deploy")], SchemeName);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
    }
}
