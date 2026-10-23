namespace TheBuilder.ApiOidc.Configuration;

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
///     names let Vercel projects be listed by name or by id. With <paramref name="Subpaths" />, a value also admits
///     every path below it, as a GitLab group admits its subgroups.
/// </summary>
internal sealed record ClaimRule(
    IReadOnlyList<string> ClaimNames,
    IReadOnlyList<string> Values,
    StringComparison Comparison,
    bool Subpaths = false)
{
    public bool IsSatisfiedBy(Func<string, string?> readClaim) =>
        ClaimNames.Select(readClaim).Any(actual => actual is not null && Values.Any(allowed => Matches(allowed, actual)));

    private bool Matches(string allowed, string actual) =>
        string.Equals(allowed, actual, Comparison) ||
        (Subpaths && actual.StartsWith($"{allowed}/", Comparison));

    public override string ToString() =>
        $"{string.Join(" or ", ClaimNames)} in [{string.Join(", ", Values)}]{(Subpaths ? " or below" : "")}";
}
