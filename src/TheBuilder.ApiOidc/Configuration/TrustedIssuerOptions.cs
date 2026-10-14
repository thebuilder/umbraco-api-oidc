namespace TheBuilder.ApiOidc.Configuration;

/// <summary>
///     One source of tokens the package accepts. Pick a <see cref="Preset" /> for Vercel or GitHub Actions,
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

    /// <summary>The token's <c>iss</c>. Set by the Vercel and GitHub Actions presets.</summary>
    public string? Issuer { get; set; }

    /// <summary>The token's <c>aud</c>. Required, except for Vercel, which defaults to <c>https://vercel.com/{team}</c>.</summary>
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

    /// <summary>Vercel: project names or ids (<c>prj_...</c>) allowed in. Empty allows every project in the team.</summary>
    public IList<string> Projects { get; set; } = [];

    /// <summary>GitHub Actions: repositories allowed in, as <c>owner/repo</c>.</summary>
    public IList<string> Repositories { get; set; } = [];

    /// <summary>GitHub Actions: refs allowed in, such as <c>refs/heads/main</c>. Empty allows any.</summary>
    public IList<string> Refs { get; set; } = [];

    /// <summary>
    ///     Vercel: environments allowed in, defaulting to <c>production</c>, <c>preview</c> and
    ///     <c>development</c>.
    ///     GitHub Actions: deployment environments allowed in. Empty allows any.
    /// </summary>
    public IList<string> Environments { get; set; } = [];
}
