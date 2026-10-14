using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using TheBuilder.ApiOidc.Configuration;
using TheBuilder.ApiOidc.DeliveryApi;

namespace TheBuilder.ApiOidc.Tests;

public sealed class OidcMiddlewareTests
{
    private readonly FakeIssuers _issuers = new();
    private readonly ApiOidcOptions _options = FakeIssuers.Options(FakeIssuers.Vercel());
    private bool _nextCalled;

    private async Task<HttpContext> Send(string path, string headerName, string headerValue)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Request.Headers[headerName] = headerValue;

        var middleware = new OidcMiddleware(
            _ =>
            {
                _nextCalled = true;
                return Task.CompletedTask;
            },
            new TestOptionsMonitor<ApiOidcOptions>(_options),
            _issuers.Validator(_options),
            NullLogger<OidcMiddleware>.Instance);

        await middleware.InvokeAsync(context);
        return context;
    }

    [Fact]
    public async Task Marks_a_delivery_api_request_with_a_valid_token()
    {
        var context = await Send("/umbraco/delivery/api/v2/content", "X-Oidc-Token", _issuers.VercelToken());

        Assert.Equal("Vercel", context.Features.Get<OidcAccessFeature>()?.Identity.IssuerName);
        Assert.True(_nextCalled);
    }

    [Fact]
    public async Task Leaves_an_invalid_token_for_umbraco_to_refuse()
    {
        var context = await Send("/umbraco/delivery/api/v2/content", "X-Oidc-Token", _issuers.VercelToken(project: "other"));

        Assert.Null(context.Features.Get<OidcAccessFeature>());
        Assert.True(_nextCalled);
    }

    [Fact]
    public async Task Ignores_requests_outside_the_delivery_api()
    {
        var context = await Send("/umbraco/management/api/v1/document", "X-Oidc-Token", _issuers.VercelToken());

        Assert.Null(context.Features.Get<OidcAccessFeature>());
    }

    [Fact]
    public async Task Reads_a_bearer_token_from_the_authorization_header_when_configured()
    {
        _options.HeaderName = "Authorization";

        var context = await Send("/umbraco/delivery/api/v2/media", "Authorization", $"Bearer {_issuers.VercelToken()}");

        Assert.NotNull(context.Features.Get<OidcAccessFeature>());
    }
}
