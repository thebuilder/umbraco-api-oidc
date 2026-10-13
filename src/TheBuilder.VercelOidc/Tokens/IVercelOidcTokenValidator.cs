namespace TheBuilder.VercelOidc.Tokens;

/// <summary>
///     Validates a Vercel OIDC token against the configured team, projects and environments.
/// </summary>
public interface IVercelOidcTokenValidator
{
    /// <summary>Checks the token's signature, issuer, audience and lifetime, then the deployment it was issued to.</summary>
    Task<VercelOidcValidationResult> ValidateAsync(string token, CancellationToken cancellationToken);
}
