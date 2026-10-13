using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using TheBuilder.VercelOidc.Configuration;
using TheBuilder.VercelOidc.DeliveryApi;
using TheBuilder.VercelOidc.Tokens;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DeliveryApi;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.Common.ApplicationBuilder;
using Umbraco.Extensions;

namespace TheBuilder.VercelOidc.Composition;

/// <summary>
///     Registers the Vercel OIDC token check for the Delivery API.
/// </summary>
public sealed class VercelOidcComposer : IComposer
{
    internal const string HttpClientName = Constants.PackageName;

    /// <inheritdoc />
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddOptions<VercelOidcOptions>()
            .Bind(builder.Config.GetSection(VercelOidcOptions.SectionName))
            .ValidateOnStart();
        builder.Services.AddSingleton<IValidateOptions<VercelOidcOptions>, VercelOidcOptionsValidator>();
        builder.Services.AddHttpClient(HttpClientName);
        builder.Services.AddSingleton(TimeProvider.System);

        // Keyed on the issuer at startup. Changing TeamSlug or IssuerMode takes a restart, which a change of
        // trust should have anyway.
        builder.Services.AddSingleton<BaseConfigurationManager>(services =>
        {
            var settings = services.GetRequiredService<IOptions<VercelOidcOptions>>().Value;
            var httpClient = services.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName);
            return new ConfigurationManager<OpenIdConnectConfiguration>(
                $"{settings.Issuer}/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever(httpClient) { RequireHttps = true });
        });
        builder.Services.AddSingleton<IVercelOidcTokenValidator, VercelOidcTokenValidator>();

        // Replaces the registration AddDeliveryApi() made, which is why the site must call AddDeliveryApi() before
        // AddComposers(), as Umbraco's own template does.
        builder.Services.AddUnique<IApiAccessService, VercelOidcApiAccessService>();

        builder.Services.Configure<UmbracoPipelineOptions>(pipeline => pipeline.AddFilter(
            new UmbracoPipelineFilter(Constants.PackageName)
            {
                PrePipeline = app => app.UseMiddleware<VercelOidcMiddleware>(),
            }));
    }
}
