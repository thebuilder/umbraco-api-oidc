namespace TheBuilder.ApiOidc.Tokens;

/// <summary>
///     The outcome of validating an OIDC token.
/// </summary>
public sealed record OidcValidationResult
{
    private OidcValidationResult(OidcIdentity? identity, string? failure)
    {
        Identity = identity;
        Failure = failure;
    }

    /// <summary>The workload the token was issued to, when the token is valid.</summary>
    public OidcIdentity? Identity { get; }

    /// <summary>Why the token was refused. For logs only: it is never sent back to the caller.</summary>
    public string? Failure { get; }

    /// <summary>Whether the token is valid and allowed.</summary>
    public bool IsValid => Identity is not null;

    internal static OidcValidationResult Valid(OidcIdentity identity) => new(identity, null);

    internal static OidcValidationResult Invalid(string failure) => new(null, failure);
}
