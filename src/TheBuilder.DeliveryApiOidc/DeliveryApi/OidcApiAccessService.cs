using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.DeliveryApi;
using DeliveryApiConstants = Umbraco.Cms.Core.Constants.DeliveryApi;

namespace TheBuilder.DeliveryApiOidc.DeliveryApi;

/// <summary>
///     Umbraco's Delivery API access rules, with a valid OIDC token accepted wherever the API key is.
/// </summary>
/// <remarks>
///     Umbraco's own implementation is internal, so this restates its rules rather than wrapping it. Public access,
///     the media settings and the API key all behave as before, which lets a site move its frontends over one at
///     a time and remove <c>Umbraco:CMS:DeliveryApi:ApiKey</c> once none of them send it.
/// </remarks>
internal sealed class OidcApiAccessService(
    IHttpContextAccessor httpContextAccessor,
    IOptionsMonitor<DeliveryApiSettings> deliveryApiSettings) : IApiAccessService
{
    public bool HasPublicAccess()
    {
        var settings = deliveryApiSettings.CurrentValue;
        return settings.Enabled && (settings.PublicAccess || HasValidApiKey(settings) || Token() is not null);
    }

    public bool HasPreviewAccess()
    {
        var settings = deliveryApiSettings.CurrentValue;
        return settings.Enabled &&
               (HasValidApiKey(settings) || Token()?.Identity.AllowPreview == true);
    }

    public bool HasMediaAccess()
    {
        var settings = deliveryApiSettings.CurrentValue;
        return settings is { Enabled: true, Media.Enabled: true } &&
               (settings is { PublicAccess: true, Media.PublicAccess: true } || HasValidApiKey(settings) || Token() is not null);
    }

    private OidcAccessFeature? Token() => httpContextAccessor.HttpContext?.Features.Get<OidcAccessFeature>();

    private bool HasValidApiKey(DeliveryApiSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            return false;

        string? supplied = httpContextAccessor.HttpContext?.Request.Headers[DeliveryApiConstants.HeaderNames.ApiKey];
        if (supplied is null)
            return false;

        // Umbraco compares with string equality; hashing first makes the comparison fixed-time whatever the lengths.
        return CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(settings.ApiKey)),
            SHA256.HashData(Encoding.UTF8.GetBytes(supplied)));
    }
}
