namespace TheBuilder.ApiOidc.Configuration;

/// <summary>
///     Expands presets into issuer, audience and claim rules, and says what is wrong with a configuration.
/// </summary>
internal static class TrustedIssuerResolver
{
    public static IReadOnlyList<TrustedIssuer> Resolve(ApiOidcOptions options) =>
        options.Issuers.Select((issuer, index) => Resolve(issuer, index, out _)).OfType<TrustedIssuer>().ToList();

    public static IReadOnlyList<string> Validate(ApiOidcOptions options)
    {
        var failures = new List<string>();
        for (var index = 0; index < options.Issuers.Count; index++)
        {
            Resolve(options.Issuers[index], index, out var issuerFailures);
            failures.AddRange(issuerFailures);
        }

        return failures;
    }

    private static TrustedIssuer? Resolve(TrustedIssuerOptions options, int index, out List<string> failures)
    {
        failures = [];
        var path = $"{ApiOidcOptions.SectionName}:Issuers:{index}";
        var rules = new List<ClaimRule>();
        string? issuer;
        string? audience = options.Audience;

        switch (options.Preset)
        {
            case IssuerPreset.Vercel:
                if (string.IsNullOrWhiteSpace(options.Team) || options.Team.Any(c => char.IsWhiteSpace(c) || c == '/'))
                    failures.Add($"{path}:Team must be the Vercel team slug, such as \"acme\".");
                if (!HasValues(options.Projects))
                    failures.Add($"{path}:Projects must name at least one Vercel project.");

                issuer = options.IssuerMode == VercelIssuerMode.Team
                    ? $"{Constants.Vercel.Host}/{options.Team}"
                    : Constants.Vercel.Host;
                if (string.IsNullOrWhiteSpace(audience))
                    audience = $"https://vercel.com/{options.Team}";

                // In global issuer mode every team shares the issuer, so the owner is what keeps another team's
                // deployments out. In team mode it repeats the issuer, which costs nothing.
                rules.Add(Rule([Constants.Vercel.Owner], [options.Team ?? ""], StringComparison.Ordinal));
                rules.Add(Rule([Constants.Vercel.Project, Constants.Vercel.ProjectId], options.Projects, StringComparison.Ordinal));
                // Development tokens are left out by default, because `vercel env pull` hands one to anyone on the team.
                rules.Add(Rule(
                    [Constants.Vercel.Environment],
                    HasValues(options.Environments) ? options.Environments : ["production", "preview"],
                    StringComparison.OrdinalIgnoreCase));
                break;

            case IssuerPreset.GitHubActions:
                issuer = Constants.GitHubActions.Issuer;
                if (string.IsNullOrWhiteSpace(audience))
                    failures.Add($"{path}:Audience is required for GitHub Actions; pass the same value to core.getIDToken().");
                if (!HasValues(options.Repositories))
                    failures.Add($"{path}:Repositories must name at least one repository, as owner/repo.");

                // GitHub treats repository names case-insensitively, so the allow-list does too.
                rules.Add(Rule([Constants.GitHubActions.Repository], options.Repositories, StringComparison.OrdinalIgnoreCase));
                if (HasValues(options.Refs))
                    rules.Add(Rule([Constants.GitHubActions.Ref], options.Refs, StringComparison.Ordinal));
                if (HasValues(options.Environments))
                    rules.Add(Rule([Constants.GitHubActions.Environment], options.Environments, StringComparison.Ordinal));
                break;

            default:
                issuer = options.Issuer;
                if (string.IsNullOrWhiteSpace(audience))
                    failures.Add($"{path}:Audience is required.");
                if (!options.RequiredClaims.Any(claim => HasValues(claim.Value)))
                    failures.Add($"{path}:RequiredClaims must name at least one claim, or any workload the issuer serves could get in.");
                break;
        }

        if (!Uri.TryCreate(issuer, UriKind.Absolute, out var issuerUri) || issuerUri.Scheme != Uri.UriSchemeHttps)
            failures.Add($"{path}:Issuer must be an https URL.");

        var metadataAddress = string.IsNullOrWhiteSpace(options.MetadataAddress)
            ? $"{issuer?.TrimEnd('/')}/.well-known/openid-configuration"
            : options.MetadataAddress;
        if (!Uri.TryCreate(metadataAddress, UriKind.Absolute, out var metadataUri) || metadataUri.Scheme != Uri.UriSchemeHttps)
            failures.Add($"{path}:MetadataAddress must be an https URL.");

        rules.AddRange(options.RequiredClaims
            .Where(claim => HasValues(claim.Value))
            .Select(claim => Rule([claim.Key], claim.Value, StringComparison.Ordinal)));

        if (failures.Count > 0)
            return null;

        var name = string.IsNullOrWhiteSpace(options.Name)
            ? options.Preset == IssuerPreset.Custom ? issuer! : options.Preset.ToString()
            : options.Name;
        return new TrustedIssuer(name, issuer!, audience!, metadataAddress, rules, options.AllowPreview);
    }

    private static ClaimRule Rule(IReadOnlyList<string> claimNames, IEnumerable<string> values, StringComparison comparison) =>
        new(claimNames, values.Where(value => !string.IsNullOrWhiteSpace(value)).ToList(), comparison);

    private static bool HasValues(IEnumerable<string>? values) => values?.Any(value => !string.IsNullOrWhiteSpace(value)) == true;
}
