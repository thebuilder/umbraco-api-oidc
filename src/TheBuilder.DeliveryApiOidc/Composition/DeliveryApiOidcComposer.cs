using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using TheBuilder.DeliveryApiOidc.Configuration;
using TheBuilder.DeliveryApiOidc.DeliveryApi;
using TheBuilder.DeliveryApiOidc.Tokens;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DeliveryApi;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Web.Common.ApplicationBuilder;
using Umbraco.Extensions;

namespace TheBuilder.DeliveryApiOidc.Composition;

/// <summary>
///     Registers the OIDC token check for the Delivery API.
/// </summary>
public sealed class DeliveryApiOidcComposer : IComposer
{
    /// <inheritdoc />
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddOptions<DeliveryApiOidcOptions>()
            .Bind(builder.Config.GetSection(DeliveryApiOidcOptions.SectionName))
            .ValidateOnStart();
        builder.Services.AddSingleton<IValidateOptions<DeliveryApiOidcOptions>, DeliveryApiOidcOptionsValidator>();
        builder.Services.AddHttpClient(HttpIssuerConfigurationProvider.HttpClientName);
        // TryAdd, so a site that registers its own TimeProvider keeps it.
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<IIssuerConfigurationProvider, HttpIssuerConfigurationProvider>();
        builder.Services.AddSingleton<IOidcTokenValidator, OidcTokenValidator>();

        // Replaces the registration AddDeliveryApi() made, which is why the site must call AddDeliveryApi() before
        // AddComposers(), as Umbraco's own template does.
        builder.Services.AddUnique<IApiAccessService, OidcApiAccessService>();
        builder.AddNotificationHandler<UmbracoApplicationStartedNotification, AccessServiceRegistrationCheck>();

        builder.Services.Configure<UmbracoPipelineOptions>(pipeline => pipeline.AddFilter(
            new UmbracoPipelineFilter(Constants.PackageName)
            {
                PrePipeline = app => app.UseMiddleware<OidcMiddleware>(),
            }));
    }
}
