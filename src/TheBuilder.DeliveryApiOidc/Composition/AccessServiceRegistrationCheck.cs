using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TheBuilder.DeliveryApiOidc.Configuration;
using TheBuilder.DeliveryApiOidc.DeliveryApi;
using Umbraco.Cms.Core.DeliveryApi;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace TheBuilder.DeliveryApiOidc.Composition;

/// <summary>
///     Says so at startup when the package's access check is not the one in use, which happens when the site calls
///     <c>AddDeliveryApi()</c> after <c>AddComposers()</c>. Tokens would otherwise be refused without explanation.
/// </summary>
internal sealed class AccessServiceRegistrationCheck(
    IApiAccessService apiAccessService,
    IOptions<DeliveryApiOidcOptions> options,
    ILogger<AccessServiceRegistrationCheck> logger) : INotificationHandler<UmbracoApplicationStartedNotification>
{
    public void Handle(UmbracoApplicationStartedNotification notification)
    {
        if (options.Value.Issuers.Count == 0 || apiAccessService is OidcApiAccessService)
            return;

        logger.LogError(
            "{Package} is configured, but the Delivery API is using {AccessService} to check access, so OIDC tokens will be refused. Call AddDeliveryApi() before AddComposers() in Program.cs.",
            Constants.PackageName,
            apiAccessService.GetType().FullName);
    }
}
