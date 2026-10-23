namespace TheBuilder.ApiOidc.Configuration;

/// <summary>
///     One source of tokens the package accepts. Pick a <see cref="Preset" /> for Vercel, GitHub Actions, GitLab, Google Cloud or Azure,
///     or describe any other issuer with <see cref="Issuer" />, <see cref="Audience" /> and <see cref="RequiredClaims" />.
/// </summary>
public sealed class TrustedIssuerOptions
{
    /// <summary>
    ///     A name for logs and for <c>[RequireOidcToken(...)]</c>, such as <c>marketing-site</c>. Defaults to the
    ///     preset or the issuer.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>The kind of issuer. Defaults to <see cref="IssuerPreset.Custom" />.</summary>
    public IssuerPreset Preset { get; set; } = IssuerPreset.Custom;

    /// <summary>The token's <c>iss</c>. Set by the presets.</summary>
    public string? Issuer { get; set; }

    /// <summary>
    ///     The token's <c>aud</c>: which service the token was made for. Required for a custom issuer. Vercel defaults
    ///     to <c>https://vercel.com/{team}</c> and GitHub Actions to <c>https://github.com/{owner}</c>. Azure requires
    ///     it: the application (client) ID of the app registration that stands for the site. GitLab and Google Cloud
    ///     require it too, and the workload asks for the same value when it requests the token.
    /// </summary>
    public string? Audience { get; set; }

    /// <summary>
    ///     Where the issuer publishes its OpenID configuration. Defaults to <c>{Issuer}/.well-known/openid-configuration</c>.
    /// </summary>
    public string? MetadataAddress { get; set; }

    /// <summary>
    ///     Claims the token must carry, each with the values allowed for it. Required for a custom issuer, because
    ///     issuer and audience alone rarely say which workload sent the token. Added to the preset's own rules.
    /// </summary>
    public Dictionary<string, string[]> RequiredClaims { get; set; } = [];

    /// <summary>Whether a token from this issuer also unlocks preview (draft) content, as the API key does.</summary>
    public bool AllowPreview { get; set; } = true;

    /// <summary>Vercel: the team slug.</summary>
    public string? Team { get; set; }

    /// <summary>Vercel: the issuer mode set on the team.</summary>
    public VercelIssuerMode IssuerMode { get; set; } = VercelIssuerMode.Team;

    /// <summary>
    ///     Vercel: project names or ids (<c>prj_...</c>) allowed in. Empty allows every project in the team.
    ///     GitLab: projects allowed in, as their full path, such as <c>acme/web/site</c>. Empty allows every project
    ///     under the <see cref="Namespace" />.
    /// </summary>
    public IList<string> Projects { get; set; } = [];

    /// <summary>
    ///     GitHub Actions: the user or organization that owns the repositories, such as <c>acme</c>. Write it as GitHub
    ///     does, because the default audience is built from it.
    /// </summary>
    public string? Owner { get; set; }

    /// <summary>
    ///     GitHub Actions: repositories allowed in, as <c>owner/repo</c>. Empty allows every repository the
    ///     <see cref="Owner" /> has.
    /// </summary>
    public IList<string> Repositories { get; set; } = [];

    /// <summary>
    ///     GitLab: the group or user that owns the projects, as its full path, such as <c>acme</c> or <c>acme/web</c>.
    ///     Its subgroups are allowed in too.
    /// </summary>
    public string? Namespace { get; set; }

    /// <summary>
    ///     GitLab: the address of a self-managed instance, such as <c>https://gitlab.example.com</c>. Defaults to
    ///     <c>https://gitlab.com</c>.
    /// </summary>
    public string? Host { get; set; }

    /// <summary>GitHub Actions and GitLab: refs allowed in, such as <c>refs/heads/main</c>. Empty allows any.</summary>
    public IList<string> Refs { get; set; } = [];

    /// <summary>
    ///     Google Cloud: the service accounts allowed in, by email, such as
    ///     <c>renderer@acme.iam.gserviceaccount.com</c>, or by unique ID. At least one is required.
    /// </summary>
    public IList<string> ServiceAccounts { get; set; } = [];

    /// <summary>Azure: the Entra ID directory (tenant) ID the identities belong to.</summary>
    public string? TenantId { get; set; }

    /// <summary>
    ///     Azure: the object (principal) IDs of the managed identities, or the service principals of app registrations,
    ///     allowed in. At least one is required.
    /// </summary>
    public IList<string> PrincipalIds { get; set; } = [];

    /// <summary>
    ///     Vercel: environments allowed in, defaulting to <c>production</c> and <c>preview</c>. Add
    ///     <c>development</c> to let local machines in.
    ///     GitHub Actions and GitLab: deployment environments allowed in. Empty allows any.
    /// </summary>
    public IList<string> Environments { get; set; } = [];
}
