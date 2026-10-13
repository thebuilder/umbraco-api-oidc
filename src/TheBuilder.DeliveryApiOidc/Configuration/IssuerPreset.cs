namespace TheBuilder.DeliveryApiOidc.Configuration;

/// <summary>
///     Shortcuts for issuers whose tokens have a known shape.
/// </summary>
public enum IssuerPreset
{
    /// <summary>Any OIDC issuer, described by <c>Issuer</c>, <c>Audience</c> and <c>RequiredClaims</c>.</summary>
    Custom,

    /// <summary>Vercel functions and builds, scoped by team, project and environment.</summary>
    Vercel,

    /// <summary>GitHub Actions workflows, scoped by repository and optionally by ref and environment.</summary>
    GitHubActions,
}
