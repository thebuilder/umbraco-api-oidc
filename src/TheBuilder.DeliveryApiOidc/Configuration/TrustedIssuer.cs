namespace TheBuilder.DeliveryApiOidc.Configuration;

/// <summary>
///     A trusted issuer with its preset expanded: what the validator actually checks.
/// </summary>
internal sealed record TrustedIssuer(
    string Name,
    string Issuer,
    string Audience,
    string MetadataAddress,
    IReadOnlyList<ClaimRule> Rules,
    bool AllowPreview);

/// <summary>
///     The token must carry one of <paramref name="ClaimNames" /> with one of <paramref name="Values" />. Several
///     names let Vercel projects be listed by name or by id.
/// </summary>
internal sealed record ClaimRule(IReadOnlyList<string> ClaimNames, IReadOnlyList<string> Values, StringComparison Comparison)
{
    public bool IsSatisfiedBy(Func<string, string?> readClaim) =>
        ClaimNames.Select(readClaim).Any(actual =>
            actual is not null && Values.Any(allowed => string.Equals(allowed, actual, Comparison)));

    public override string ToString() => $"{string.Join(" or ", ClaimNames)} in [{string.Join(", ", Values)}]";
}
