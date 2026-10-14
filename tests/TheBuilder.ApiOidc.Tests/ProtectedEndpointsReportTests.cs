using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TheBuilder.ApiOidc.Composition;
using TheBuilder.ApiOidc.Configuration;
using Umbraco.Cms.Core.Notifications;

namespace TheBuilder.ApiOidc.Tests;

public sealed class ProtectedEndpointsReportTests
{
    private sealed class RecordingLogger : ILogger<ProtectedEndpointsReport>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Messages.Add(formatter(state, exception));
    }

    private readonly RecordingLogger _logger = new();
    private readonly ApiOidcOptions _options = FakeIssuers.Options(FakeIssuers.Vercel());

    private static RouteEndpoint Route(string pattern, params object[] metadata) =>
        new(_ => Task.CompletedTask, RoutePatternFactory.Parse(pattern), 0, new EndpointMetadataCollection(metadata), pattern);

    private void Report(params Endpoint[] endpoints) =>
        new ProtectedEndpointsReport(new DefaultEndpointDataSource(endpoints), Options.Create(_options), _logger)
            .Handle(new UmbracoApplicationStartedNotification(false));

    [Fact]
    public void Lists_the_custom_endpoints_under_the_delivery_api()
    {
        Report(
            Route("umbraco/delivery/api/v1/dictionary", new HttpMethodMetadata(["GET"])),
            Route("umbraco/delivery/api/v1/open", new AllowAnonymousAttribute()),
            Route("api/search"));

        var message = Assert.Single(_logger.Messages);
        Assert.Contains("GET /umbraco/delivery/api/v1/dictionary", message);
        Assert.DoesNotContain("open", message);
        Assert.DoesNotContain("search", message);
    }

    [Fact]
    public void Says_nothing_when_turned_off()
    {
        _options.ProtectCustomDeliveryApiEndpoints = false;

        Report(Route("umbraco/delivery/api/v1/dictionary"));

        Assert.Empty(_logger.Messages);
    }
}
