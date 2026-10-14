using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.DeliveryApi;

namespace TheBuilder.ApiOidc.Authentication;

/// <summary>
///     Met when the Delivery API would serve the request: it is public, or the request carries a valid API key or
///     a valid OIDC token.
/// </summary>
internal sealed class DeliveryApiAccessRequirement : IAuthorizationRequirement;

// ASP.NET Core builds every handler for every authorization check, so the access service is fetched only when this
// requirement is checked, keeping endpoints that never ask for it free of its dependencies.
internal sealed class DeliveryApiAccessHandler(IServiceProvider services) : AuthorizationHandler<DeliveryApiAccessRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, DeliveryApiAccessRequirement requirement)
    {
        var apiAccessService = services.GetRequiredService<IApiAccessService>();
        var deliveryApiSettings = services.GetRequiredService<IOptionsMonitor<DeliveryApiSettings>>();

        // The token is checked through the OIDC scheme, because the Delivery API's access service only sees tokens
        // sent to the Delivery API's own path. Like the access service, it only counts while the API is enabled.
        if (apiAccessService.HasPublicAccess() ||
            (deliveryApiSettings.CurrentValue.Enabled && OidcTokenRequirement.IsSignedIn(context.User, [])))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
