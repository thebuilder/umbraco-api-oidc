namespace TheBuilder.DeliveryApiOidc.Configuration;

/// <summary>
///     Which workloads may read the Delivery API with an OIDC token, bound from <c>TheBuilder:DeliveryApiOidc</c>.
/// </summary>
public sealed class DeliveryApiOidcOptions
{
    /// <summary>The configuration section the options are read from.</summary>
    public const string SectionName = "TheBuilder:DeliveryApiOidc";

    /// <summary>The issuers whose tokens are accepted. Leaving it empty turns the package off.</summary>
    public IList<TrustedIssuerOptions> Issuers { get; set; } = [];

    /// <summary>
    ///     The request header that carries the token. A <c>Bearer </c> prefix is accepted, so this can be set to
    ///     <c>Authorization</c> when the site does not also use Delivery API member authorization.
    /// </summary>
    public string HeaderName { get; set; } = "X-Oidc-Token";

    /// <summary>How far a token's lifetime may disagree with this server's clock.</summary>
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(1);
}
