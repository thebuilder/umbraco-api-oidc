namespace TheBuilder.ApiOidc.Configuration;

/// <summary>
///     Shortcuts for issuers whose tokens have a known shape.
/// </summary>
public enum IssuerPreset
{
    /// <summary>Any OIDC issuer, described by <c>Issuer</c>, <c>Audience</c> and <c>RequiredClaims</c>.</summary>
    Custom,

    /// <summary>Vercel functions and builds, scoped by team, project and environment.</summary>
    Vercel,

    /// <summary>GitHub Actions workflows, scoped by owner and optionally by repository, ref and environment.</summary>
    GitHubActions,

    /// <summary>
    ///     Azure managed identities and workload identities, with Entra ID tokens for an app registration, scoped by
    ///     tenant and principal.
    /// </summary>
    Azure,

    /// <summary>GitLab CI/CD jobs, scoped by group or user and optionally by project, ref and environment.</summary>
    GitLab,
}
