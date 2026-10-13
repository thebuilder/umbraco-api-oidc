namespace TheBuilder.VercelOidc.Tokens;

/// <summary>
///     The outcome of validating a Vercel OIDC token.
/// </summary>
public sealed record VercelOidcValidationResult
{
    private VercelOidcValidationResult(VercelOidcIdentity? identity, string? failure)
    {
        Identity = identity;
        Failure = failure;
    }

    /// <summary>The deployment the token was issued to, when the token is valid.</summary>
    public VercelOidcIdentity? Identity { get; }

    /// <summary>Why the token was refused. For logs only: it is never sent back to the caller.</summary>
    public string? Failure { get; }

    /// <summary>Whether the token is valid and allowed.</summary>
    public bool IsValid => Identity is not null;

    internal static VercelOidcValidationResult Valid(VercelOidcIdentity identity) => new(identity, null);

    internal static VercelOidcValidationResult Invalid(string failure) => new(null, failure);
}
