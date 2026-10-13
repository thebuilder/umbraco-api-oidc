using Microsoft.IdentityModel.Tokens;

namespace TheBuilder.ApiOidc.Tokens;

/// <summary>
///     Hands out the configuration manager that fetches, caches and refreshes an issuer's signing keys.
/// </summary>
internal interface IIssuerConfigurationProvider
{
    BaseConfigurationManager Get(string metadataAddress);
}
