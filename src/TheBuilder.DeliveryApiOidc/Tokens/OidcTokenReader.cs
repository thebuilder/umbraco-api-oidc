using Microsoft.AspNetCore.Http;

namespace TheBuilder.DeliveryApiOidc.Tokens;

internal static class OidcTokenReader
{
    private const string BearerPrefix = "Bearer ";

    /// <summary>
    ///     Reads the token from the configured header, with or without a <c>Bearer</c> prefix.
    /// </summary>
    public static string? Read(HttpRequest request, string headerName)
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
