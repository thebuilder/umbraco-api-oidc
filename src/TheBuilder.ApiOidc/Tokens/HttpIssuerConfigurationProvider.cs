using System.Collections.Concurrent;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace TheBuilder.ApiOidc.Tokens;

internal sealed class HttpIssuerConfigurationProvider(IHttpClientFactory httpClientFactory) : IIssuerConfigurationProvider
{
    internal const string HttpClientName = Constants.PackageName;

    // One per metadata address for the life of the site, so the keys are fetched once and refreshed on their own
    // schedule, however many requests arrive.
    private readonly ConcurrentDictionary<string, BaseConfigurationManager> _managers = new(StringComparer.Ordinal);

    public BaseConfigurationManager Get(string metadataAddress) =>
        _managers.GetOrAdd(metadataAddress, address => new ConfigurationManager<OpenIdConnectConfiguration>(
            address,
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever(httpClientFactory.CreateClient(HttpClientName)) { RequireHttps = true }));
}
