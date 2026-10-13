using System.Security.Claims;

namespace TheBuilder.DeliveryApiOidc.Tokens;

/// <summary>
///     The workload a validated token was issued to.
/// </summary>
/// <param name="IssuerName">The name of the trusted issuer that accepted the token.</param>
/// <param name="Issuer">The token's <c>iss</c>.</param>
/// <param name="Subject">The token's <c>sub</c>.</param>
/// <param name="AllowPreview">Whether the trusted issuer grants preview access.</param>
public sealed record OidcIdentity(string IssuerName, string Issuer, string Subject, bool AllowPreview)
{
    /// <summary>The token's claims, such as <c>project</c> or <c>repository</c>, with their names as issued.</summary>
    public IReadOnlyList<Claim> Claims { get; init; } = [];
}
