using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Options;
using TheBuilder.ApiOidc.Configuration;
using Umbraco.Cms.Core.DeliveryApi;

namespace TheBuilder.ApiOidc.DeliveryApi;

/// <summary>
///     Applies the Delivery API's access check to endpoints that other packages and the site itself add under
///     <c>/umbraco/delivery/api</c>, such as a dictionary endpoint.
/// </summary>
/// <remarks>
///     Umbraco's own Delivery API controllers check access themselves, including preview and member
///     authorization, so they are left to do that. Every other endpoint under the prefix gets the same rule as
///     Umbraco's content endpoints: public access, a valid API key or a trusted OIDC token. An endpoint marked
///     <c>[AllowAnonymous]</c> opts out. It runs after routing, so it knows which endpoint was matched.
/// </remarks>
internal sealed class DeliveryApiEndpointGate(
    RequestDelegate next,
    IOptionsMonitor<ApiOidcOptions> options,
    IApiAccessService apiAccessService)
{
    public Task InvokeAsync(HttpContext context)
    {
        if (options.CurrentValue.Issuers.Count > 0 &&
            context.Request.Path.StartsWithSegments(Constants.DeliveryApiPathPrefix, StringComparison.OrdinalIgnoreCase) &&
            context.GetEndpoint() is { } endpoint &&
            !IsUmbracos(endpoint) &&
            endpoint.Metadata.GetMetadata<IAllowAnonymous>() is null &&
            !apiAccessService.HasPublicAccess())
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        return next(context);
    }

    private static bool IsUmbracos(Endpoint endpoint) =>
        endpoint.Metadata.GetMetadata<ControllerActionDescriptor>()?.ControllerTypeInfo.Assembly.GetName().Name
            ?.StartsWith("Umbraco.", StringComparison.Ordinal) == true;
}
