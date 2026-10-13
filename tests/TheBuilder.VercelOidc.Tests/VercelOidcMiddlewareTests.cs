using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using TheBuilder.VercelOidc.Configuration;
using TheBuilder.VercelOidc.DeliveryApi;
using TheBuilder.VercelOidc.Tokens;

namespace TheBuilder.VercelOidc.Tests;

public sealed class VercelOidcMiddlewareTests
{
    private readonly FakeVercelIssuer _vercel = new();
    private readonly VercelOidcOptions _options = FakeVercelIssuer.Options();
    private bool _nextCalled;

    private async Task<HttpContext> Send(string path, string? headerName = null, string? headerValue = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        if (headerName is not null)
            context.Request.Headers[headerName] = headerValue;

        var monitor = new TestOptionsMonitor<VercelOidcOptions>(_options);
        var middleware = new VercelOidcMiddleware(
            _ =>
            {
                _nextCalled = true;
                return Task.CompletedTask;
            },
            monitor,
            new VercelOidcTokenValidator(monitor, _vercel.ConfigurationManager, new FixedTimeProvider(FakeVercelIssuer.Now)),
            NullLogger<VercelOidcMiddleware>.Instance);

        await middleware.InvokeAsync(context);
        return context;
    }

    [Fact]
    public async Task Marks_a_delivery_api_request_with_a_valid_token()
    {
        var context = await Send("/umbraco/delivery/api/v2/content", "X-Vercel-Oidc-Token", _vercel.Issue());

        Assert.Equal("acme-web", context.Features.Get<VercelOidcAccessFeature>()?.Identity.Project);
        Assert.True(_nextCalled);
    }

    [Fact]
    public async Task Leaves_an_invalid_token_for_umbraco_to_refuse()
    {
        var context = await Send("/umbraco/delivery/api/v2/content", "X-Vercel-Oidc-Token", _vercel.Issue(project: "other"));

        Assert.Null(context.Features.Get<VercelOidcAccessFeature>());
        Assert.True(_nextCalled);
    }

    [Fact]
    public async Task Ignores_requests_outside_the_delivery_api()
    {
        var context = await Send("/umbraco/management/api/v1/document", "X-Vercel-Oidc-Token", _vercel.Issue());

        Assert.Null(context.Features.Get<VercelOidcAccessFeature>());
    }

    [Fact]
    public async Task Reads_a_bearer_token_from_the_authorization_header_when_configured()
    {
        _options.HeaderName = "Authorization";

        var context = await Send("/umbraco/delivery/api/v2/media", "Authorization", $"Bearer {_vercel.Issue()}");

        Assert.NotNull(context.Features.Get<VercelOidcAccessFeature>());
    }

    [Fact]
    public async Task Passes_requests_without_a_token_straight_through()
    {
        var context = await Send("/umbraco/delivery/api/v2/content");

        Assert.Null(context.Features.Get<VercelOidcAccessFeature>());
        Assert.True(_nextCalled);
    }
}
