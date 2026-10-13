using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TheBuilder.ApiOidc.Composition;
using TheBuilder.ApiOidc.Configuration;
using TheBuilder.ApiOidc.DeliveryApi;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.DeliveryApi;
using Umbraco.Cms.Core.Notifications;

namespace TheBuilder.ApiOidc.Tests;

public sealed class AccessServiceRegistrationCheckTests
{
    private sealed class RecordingLogger : ILogger<AccessServiceRegistrationCheck>
    {
        public List<LogLevel> Levels { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            Levels.Add(logLevel);
    }

    private sealed class UmbracosOwnAccessService : IApiAccessService
    {
        public bool HasPublicAccess() => false;

        public bool HasPreviewAccess() => false;
    }

    private static List<LogLevel> Check(IApiAccessService service, ApiOidcOptions options)
    {
        var logger = new RecordingLogger();
        new AccessServiceRegistrationCheck(service, Options.Create(options), logger)
            .Handle(new UmbracoApplicationStartedNotification(false));
        return logger.Levels;
    }

    [Fact]
    public void Logs_an_error_when_another_access_service_won() =>
        Assert.Equal([LogLevel.Error], Check(new UmbracosOwnAccessService(), FakeIssuers.Options(FakeIssuers.Vercel())));

    [Fact]
    public void Stays_quiet_when_the_package_is_not_configured() =>
        Assert.Empty(Check(new UmbracosOwnAccessService(), new ApiOidcOptions()));

    [Fact]
    public void Stays_quiet_when_its_own_access_service_is_in_use()
    {
        var own = new OidcApiAccessService(new HttpContextAccessor(), new TestOptionsMonitor<DeliveryApiSettings>(new DeliveryApiSettings()));

        Assert.Empty(Check(own, FakeIssuers.Options(FakeIssuers.Vercel())));
    }
}
