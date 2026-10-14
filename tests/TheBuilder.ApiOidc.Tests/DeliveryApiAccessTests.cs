using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TheBuilder.ApiOidc.Authentication;
using TheBuilder.ApiOidc.Composition;
using TheBuilder.ApiOidc.Tokens;
using Umbraco.Cms.Core.Configuration.Models;

namespace TheBuilder.ApiOidc.Tests;

public sealed class DeliveryApiAccessTests : IAsyncLifetime
{
    private readonly FakeIssuers _issuers = new();
    private readonly DeliveryApiSettings _deliveryApi = new() { Enabled = true, PublicAccess = false, ApiKey = "secret" };
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
        });
        builder.Services.AddSingleton<TimeProvider>(new FixedTimeProvider(FakeIssuers.Now));
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddSingleton<IOptionsMonitor<DeliveryApiSettings>>(new TestOptionsMonitor<DeliveryApiSettings>(_deliveryApi));
        ApiOidcComposer.AddServices(builder.Services, builder.Configuration);
        builder.Services.AddSingleton<IIssuerConfigurationProvider>(_issuers);
        builder.Services.AddControllers().AddApplicationPart(typeof(DeliveryApiAccessTests).Assembly);

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapGet("/dictionary", () => "dictionary").RequireDeliveryApiAccess();
        _app.MapControllers();
        await _app.StartAsync(TestContext.Current.CancellationToken);
        _client = _app.GetTestClient();
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    private Task<HttpResponseMessage> Get(string path, string? token = null, string? apiKey = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (token is not null)
            request.Headers.Add("X-Oidc-Token", token);
        if (apiKey is not null)
            request.Headers.Add("Api-Key", apiKey);
        return _client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Refuses_a_request_with_nothing()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await Get("/dictionary")).StatusCode);
    }

    [Fact]
    public async Task Serves_the_api_key()
    {
        Assert.Equal(HttpStatusCode.OK, (await Get("/dictionary", apiKey: "secret")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Get("/dictionary", apiKey: "guess")).StatusCode);
    }

    [Fact]
    public async Task Serves_a_valid_token()
    {
        Assert.Equal(HttpStatusCode.OK, (await Get("/dictionary", _issuers.VercelToken())).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Get("/dictionary", _issuers.VercelToken(owner: "other"))).StatusCode);
    }

    [Fact]
    public async Task Supports_the_attribute_on_a_controller()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await Get("/controller/dictionary")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Get("/controller/dictionary", apiKey: "secret")).StatusCode);
    }

    [Fact]
    public async Task Serves_everyone_when_the_delivery_api_is_public()
    {
        _deliveryApi.PublicAccess = true;

        Assert.Equal(HttpStatusCode.OK, (await Get("/dictionary")).StatusCode);
    }

    [Fact]
    public async Task Refuses_everything_when_the_delivery_api_is_off()
    {
        _deliveryApi.Enabled = false;

        Assert.Equal(HttpStatusCode.Unauthorized, (await Get("/dictionary", apiKey: "secret")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Get("/dictionary", _issuers.VercelToken())).StatusCode);
    }
}

[ApiController]
[Route("controller/dictionary")]
[RequireDeliveryApiAccess]
public sealed class DeliveryApiAccessTestController : ControllerBase
{
    [HttpGet]
    public string Get() => "dictionary";
}
