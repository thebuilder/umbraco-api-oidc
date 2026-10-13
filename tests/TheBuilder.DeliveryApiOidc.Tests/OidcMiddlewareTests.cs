using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using TheBuilder.DeliveryApiOidc.Configuration;
using TheBuilder.DeliveryApiOidc.DeliveryApi;

namespace TheBuilder.DeliveryApiOidc.Tests;

public sealed class OidcMiddlewareTests
{
    private readonly FakeIssuers _issuers = new();
    private readonly DeliveryApiOidcOptions _options = FakeIssuers.Options(FakeIssuers.Vercel());
    private bool _nextCalled;

    private async Task<HttpContext> Send(string path, string? headerName = null, string? headerValue = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        if (headerName is not null)
            context.Request.Headers[headerName] = headerValue;

        var middleware = new OidcMiddleware(
            _ =>
            {
                _nextCalled = true;
                return Task.CompletedTask;
            },
            new TestOptionsMonitor<DeliveryApiOidcOptions>(_options),
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

    [Fact]
    public async Task Passes_requests_without_a_token_straight_through()
    {
        var context = await Send("/umbraco/delivery/api/v2/content");

        Assert.Null(context.Features.Get<OidcAccessFeature>());
        Assert.True(_nextCalled);
    }
}
