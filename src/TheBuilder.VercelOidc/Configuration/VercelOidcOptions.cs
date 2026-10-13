namespace TheBuilder.VercelOidc.Configuration;

/// <summary>
///     Which Vercel deployments may read the Delivery API, bound from <c>TheBuilder:VercelOidc</c>.
/// </summary>
public sealed class VercelOidcOptions
{
    /// <summary>The configuration section the options are read from.</summary>
    public const string SectionName = "TheBuilder:VercelOidc";

    /// <summary>
    ///     The Vercel team slug, as it appears in the team's URL. Leaving it empty turns the package off, and the
    ///     Delivery API behaves exactly as Umbraco ships it.
    /// </summary>
    public string? TeamSlug { get; set; }

    /// <summary>The issuer mode set on the Vercel team. Defaults to <see cref="VercelIssuerMode.Team" />.</summary>
    public VercelIssuerMode IssuerMode { get; set; } = VercelIssuerMode.Team;

    /// <summary>
    ///     The audience the token must carry. Defaults to <c>https://vercel.com/{team}</c>, which is what
    ///     <c>getVercelOidcToken()</c> returns unless it is asked for a custom audience.
    /// </summary>
    public string? Audience { get; set; }

    /// <summary>
    ///     The Vercel projects allowed to read the Delivery API, by name or by id (<c>prj_...</c>). Required:
    ///     without it, every project on the team would get in.
    /// </summary>
    public IList<string> Projects { get; set; } = [];

    /// <summary>
    ///     The Vercel environments allowed to read the Delivery API. Development tokens are left out by default,
    ///     because <c>vercel env pull</c> hands one to anyone on the team for local use.
    /// </summary>
    public IList<string> Environments { get; set; } = ["production", "preview"];

    /// <summary>
    ///     Whether a valid token also unlocks preview (draft) content, as the API key does.
    /// </summary>
    public bool AllowPreview { get; set; } = true;

    /// <summary>
    ///     The request header that carries the token. A <c>Bearer </c> prefix is accepted, so this can be set to
    ///     <c>Authorization</c> when the site does not also use Delivery API member authorization.
    /// </summary>
    public string HeaderName { get; set; } = "X-Vercel-Oidc-Token";

    /// <summary>How far the token's lifetime may disagree with this server's clock.</summary>
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(1);

    internal bool IsEnabled => !string.IsNullOrWhiteSpace(TeamSlug);

    internal string Issuer => IssuerMode == VercelIssuerMode.Team
        ? $"{Constants.VercelOidcHost}/{TeamSlug}"
        : Constants.VercelOidcHost;

    internal string EffectiveAudience => string.IsNullOrWhiteSpace(Audience)
        ? $"https://vercel.com/{TeamSlug}"
        : Audience;
}
