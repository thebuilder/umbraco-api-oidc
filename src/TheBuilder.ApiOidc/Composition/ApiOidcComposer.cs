using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using TheBuilder.ApiOidc.Authentication;
using TheBuilder.ApiOidc.Configuration;
using TheBuilder.ApiOidc.DeliveryApi;
using TheBuilder.ApiOidc.Tokens;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DeliveryApi;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Web.Common.ApplicationBuilder;
using Umbraco.Extensions;

namespace TheBuilder.ApiOidc.Composition;

/// <summary>
///     Registers the OIDC token check for the Delivery API, and the authentication scheme for other endpoints.
/// </summary>
public sealed class ApiOidcComposer : IComposer
{
    /// <inheritdoc />
    public void Compose(IUmbracoBuilder builder)
    {
        AddServices(builder.Services, builder.Config);
        builder.AddNotificationHandler<UmbracoApplicationStartedNotification, AccessServiceRegistrationCheck>();
        builder.AddNotificationHandler<UmbracoApplicationStartedNotification, ProtectedEndpointsReport>();
    }

    internal static void AddServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ApiOidcOptions>()
            .Bind(configuration.GetSection(ApiOidcOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<ApiOidcOptions>, ApiOidcOptionsValidator>();
        services.AddHttpClient(HttpIssuerConfigurationProvider.HttpClientName);
        // TryAdd, so a site that registers its own TimeProvider keeps it.
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IIssuerConfigurationProvider, HttpIssuerConfigurationProvider>();
        services.AddSingleton<IOidcTokenValidator, OidcTokenValidator>();

        // Replaces the registration AddDeliveryApi() made, which is why the site must call AddDeliveryApi() before
        // AddComposers(), as Umbraco's own template does.
        services.AddUnique<IApiAccessService, OidcApiAccessService>();

        services.Configure<UmbracoPipelineOptions>(pipeline => pipeline.AddFilter(
            new UmbracoPipelineFilter(Constants.PackageName)
            {
                PrePipeline = app => app.UseMiddleware<OidcMiddleware>(),
                PostRouting = app => app.UseMiddleware<DeliveryApiEndpointGate>(),
            }));

        // Added beside Umbraco's own schemes, never as the default, so the backoffice and members are untouched
        // and only endpoints that ask for the scheme run it.
        services.AddAuthentication()
            .AddScheme<AuthenticationSchemeOptions, OidcAuthenticationHandler>(OidcAuthenticationDefaults.AuthenticationScheme, null);
        services.AddAuthorization(authorization => authorization.AddPolicy(
            OidcAuthenticationDefaults.Policy,
            policy => policy.AddAuthenticationSchemes(OidcAuthenticationDefaults.AuthenticationScheme)
                .AddRequirements(new OidcTokenRequirement([]))));
    }
}
