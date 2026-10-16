using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheBuilder.ApiOidc.Authentication;
using TheBuilder.ApiOidc.Composition;
using TheBuilder.ApiOidc.Tokens;

namespace TheBuilder.ApiOidc.Tests;

/// <summary>
///     A plain <c>[Authorize]</c> beside the token check keeps the site's default policy, on controllers and minimal
///     APIs alike.
/// </summary>
public sealed class DefaultPolicyTests : IAsyncLifetime
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
            ["ApiOidc:Issuers:1:Preset"] = "GitHubActions",
            ["ApiOidc:Issuers:1:Audience"] = "https://cms.example.com",
            ["ApiOidc:Issuers:1:Owner"] = "acme",
            ["ApiOidc:Issuers:1:Repositories:0"] = "acme/site",
        });
        builder.Services.AddSingleton<TimeProvider>(new FixedTimeProvider(FakeIssuers.Now));
        ApiOidcComposer.AddServices(builder.Services, builder.Configuration);
        builder.Services.AddSingleton<IIssuerConfigurationProvider>(_issuers);
        // Only Vercel tokens carry a project claim.
        builder.Services.AddAuthorization(options =>
            options.DefaultPolicy = new AuthorizationPolicyBuilder().RequireClaim("project").Build());
        builder.Services.AddControllers().AddApplicationPart(typeof(DefaultPolicyTests).Assembly);

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapGet("/default-policy/minimal", () => "minimal").RequireAuthorization().RequireOidcToken();
        _app.MapControllers();
        await _app.StartAsync(TestContext.Current.CancellationToken);
        _client = _app.GetTestClient();
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    private Task<HttpResponseMessage> Get(string path, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("X-Oidc-Token", token);
        return _client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("/default-policy/minimal")]
    [InlineData("/default-policy/controller")]
    public async Task Keeps_the_default_policy_beside_the_token_check(string path)
    {
        Assert.Equal(HttpStatusCode.Forbidden, (await Get(path, _issuers.GitHubToken())).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Get(path, _issuers.VercelToken())).StatusCode);
    }
}

[ApiController]
[Route("default-policy/controller")]
public sealed class DefaultPolicyTestController : ControllerBase
{
    [HttpGet]
    [Authorize]
    [RequireOidcToken]
    public string Get() => "controller";
}
