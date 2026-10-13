namespace TheBuilder.DeliveryApiOidc.Tokens;

/// <summary>
///     Validates an OIDC token against the configured trusted issuers.
/// </summary>
public interface IOidcTokenValidator
{
    /// <summary>Checks the token's signature, issuer, audience and lifetime, then the claims its issuer requires.</summary>
    Task<OidcValidationResult> ValidateAsync(string token, CancellationToken cancellationToken);
}
