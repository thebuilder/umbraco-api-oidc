using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TheBuilder.ApiOidc.Configuration;
using TheBuilder.ApiOidc.Tokens;

namespace TheBuilder.ApiOidc.DeliveryApi;

/// <summary>
///     Validates the OIDC token on Delivery API requests, ahead of Umbraco's own access checks.
/// </summary>
/// <remarks>
///     The check has to happen here rather than in <see cref="OidcApiAccessService" />, because the access service
///     is synchronous and fetching signing keys is not. It also has to happen before the output cache, which asks
///     the access service whether a request may be served from cache.
///     A missing or invalid token never ends the request: Umbraco still decides, so public access and the API key
///     keep working alongside the token.
/// </remarks>
internal sealed class OidcMiddleware(
    RequestDelegate next,
    IOptionsMonitor<ApiOidcOptions> options,
    IOidcTokenValidator validator,
    ILogger<OidcMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var settings = options.CurrentValue;
        if (settings.Issuers.Count > 0 &&
            context.Request.Path.StartsWithSegments(Constants.DeliveryApiPathPrefix, StringComparison.OrdinalIgnoreCase) &&
            OidcTokenReader.Read(context.Request, settings.HeaderName) is { } token)
        {
            var result = await validator.ValidateAsync(token, context.RequestAborted);
            if (result.Identity is { } identity)
            {
                context.Features.Set(new OidcAccessFeature(identity));
            }
            else
            {
                logger.LogWarning("Refused an OIDC token on {Path}: {Reason}", context.Request.Path, result.Failure);
            }
        }

        await next(context);
    }
}
