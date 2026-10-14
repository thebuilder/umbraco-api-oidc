using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TheBuilder.ApiOidc.Configuration;
using TheBuilder.ApiOidc.DeliveryApi;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace TheBuilder.ApiOidc.Composition;

/// <summary>
///     Logs at startup which custom endpoints under the Delivery API's path get its access check, so a site can see
///     what the package protects without reading its routes.
/// </summary>
internal sealed class ProtectedEndpointsReport(
    EndpointDataSource endpoints,
    IOptions<ApiOidcOptions> options,
    ILogger<ProtectedEndpointsReport> logger) : INotificationHandler<UmbracoApplicationStartedNotification>
{
    private static readonly string Prefix = Constants.DeliveryApiPathPrefix.TrimStart('/');

    public void Handle(UmbracoApplicationStartedNotification notification)
    {
        if (options.Value.Issuers.Count == 0 || !options.Value.ProtectCustomDeliveryApiEndpoints)
            return;

        var protectedRoutes = endpoints.Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?.TrimStart('/').StartsWith(Prefix, StringComparison.OrdinalIgnoreCase) == true)
            .Where(DeliveryApiEndpointGate.Protects)
            .Select(Describe)
            .Distinct()
            .Order(StringComparer.Ordinal)
            .ToList();

        if (protectedRoutes.Count == 0)
            return;

        logger.LogInformation(
            "{Package} applies the Delivery API access check to {Count} custom endpoint(s): {Endpoints}",
            Constants.PackageName,
            protectedRoutes.Count,
            string.Join(", ", protectedRoutes));
    }

    private static string Describe(RouteEndpoint endpoint)
    {
        var methods = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods;
        var route = "/" + endpoint.RoutePattern.RawText!.TrimStart('/');
        return methods is { Count: > 0 } ? $"{string.Join("|", methods)} {route}" : route;
    }
}
