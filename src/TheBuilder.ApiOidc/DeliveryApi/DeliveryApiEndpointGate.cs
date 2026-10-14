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
///     <c>[AllowAnonymous]</c> opts out, and <see cref="ApiOidcOptions.ProtectCustomDeliveryApiEndpoints" /> turns it
///     off. It runs after routing, so it knows which endpoint was matched.
/// </remarks>
internal sealed class DeliveryApiEndpointGate(
    RequestDelegate next,
    IOptionsMonitor<ApiOidcOptions> options,
    IApiAccessService apiAccessService)
{
    public Task InvokeAsync(HttpContext context)
    {
        var settings = options.CurrentValue;
        if (settings.Issuers.Count > 0 &&
            settings.ProtectCustomDeliveryApiEndpoints &&
            context.Request.Path.StartsWithSegments(Constants.DeliveryApiPathPrefix, StringComparison.OrdinalIgnoreCase) &&
            !IsCorsPreflight(context.Request) &&
            context.GetEndpoint() is { } endpoint &&
            Protects(endpoint) &&
            !apiAccessService.HasPublicAccess())
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        return next(context);
    }

    /// <summary>Whether an endpoint under the prefix gets the check: it is not Umbraco's and not anonymous.</summary>
    internal static bool Protects(Endpoint endpoint) =>
        !IsUmbracos(endpoint) && endpoint.Metadata.GetMetadata<IAllowAnonymous>() is null;

    // Only Umbraco's own Delivery API assembly, which holds the content, media and member endpoints. A wider match,
    // such as every assembly starting with "Umbraco.", would also let community packages through.
    // FullName is cached by the runtime, where GetName() builds a new object on every request.
    private static bool IsUmbracos(Endpoint endpoint) =>
        endpoint.Metadata.GetMetadata<ControllerActionDescriptor>()?.ControllerTypeInfo.Assembly.FullName
            ?.StartsWith(UmbracoDeliveryApiAssembly + ",", StringComparison.Ordinal) == true;

    // A browser sends a CORS preflight without credentials, so refusing it would break every cross-origin call,
    // even ones that then send a valid token. The real request that follows is still checked.
    private static bool IsCorsPreflight(HttpRequest request) =>
        HttpMethods.IsOptions(request.Method) && request.Headers.ContainsKey("Access-Control-Request-Method");

    private const string UmbracoDeliveryApiAssembly = "Umbraco.Cms.Api.Delivery";
}
