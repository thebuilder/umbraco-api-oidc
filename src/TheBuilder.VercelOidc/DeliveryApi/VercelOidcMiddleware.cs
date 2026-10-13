using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TheBuilder.VercelOidc.Configuration;
using TheBuilder.VercelOidc.Tokens;

namespace TheBuilder.VercelOidc.DeliveryApi;

/// <summary>
///     Validates the Vercel OIDC token on Delivery API requests, ahead of Umbraco's own access checks.
/// </summary>
/// <remarks>
///     The check has to happen here rather than in <see cref="VercelOidcApiAccessService" />, because the access
///     service is synchronous and fetching Vercel's signing keys is not. It also has to happen before the output
///     cache, which asks the access service whether a request may be served from cache.
///     A missing or invalid token never ends the request: Umbraco still decides, so public access and the API key
///     keep working alongside the token.
/// </remarks>
internal sealed class VercelOidcMiddleware(
    RequestDelegate next,
    IOptionsMonitor<VercelOidcOptions> options,
    IVercelOidcTokenValidator validator,
    ILogger<VercelOidcMiddleware> logger)
{
    private const string BearerPrefix = "Bearer ";

    public async Task InvokeAsync(HttpContext context)
    {
        var settings = options.CurrentValue;
        if (settings.IsEnabled &&
            context.Request.Path.StartsWithSegments(Constants.DeliveryApiPathPrefix, StringComparison.OrdinalIgnoreCase) &&
            ReadToken(context.Request, settings.HeaderName) is { } token)
        {
            var result = await validator.ValidateAsync(token, context.RequestAborted);
            if (result.Identity is { } identity)
            {
                context.Features.Set(new VercelOidcAccessFeature(identity));
            }
            else
            {
                logger.LogWarning("Refused a Vercel OIDC token on {Path}: {Reason}", context.Request.Path, result.Failure);
            }
        }

        await next(context);
    }

    private static string? ReadToken(HttpRequest request, string headerName)
    {
        string? value = request.Headers[headerName];
        if (string.IsNullOrWhiteSpace(value))
            return null;

        value = value.Trim();
        if (value.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
            value = value[BearerPrefix.Length..].Trim();

        // Only something shaped like a JWT is worth validating. With HeaderName set to Authorization, this is
        // also what lets member access tokens, which are not JWTs, pass through without a logged refusal.
        return value.Count(character => character == '.') == 2 ? value : null;
    }
}
