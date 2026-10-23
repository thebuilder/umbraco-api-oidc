namespace TheBuilder.ApiOidc.Configuration;

/// <summary>
///     Expands presets into issuer, audience and claim rules, and says what is wrong with a configuration.
/// </summary>
internal static class TrustedIssuerResolver
{
    public static IReadOnlyList<TrustedIssuer> Resolve(ApiOidcOptions options) =>
        options.Issuers.SelectMany((issuer, index) => Resolve(issuer, index, out _)).ToList();

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

    private static IReadOnlyList<TrustedIssuer> Resolve(TrustedIssuerOptions options, int index, out List<string> failures)
    {
        failures = [];
        var path = $"{ApiOidcOptions.SectionName}:Issuers:{index}";
        var rules = new List<ClaimRule>();
        string? issuer;
        string? audience = options.Audience;
        // An issuer that signs a second token format under another iss, with the metadata that describes it.
        (string Issuer, string MetadataAddress)? alternate = null;

        switch (options.Preset)
        {
            case IssuerPreset.Vercel:
                if (string.IsNullOrWhiteSpace(options.Team) || options.Team.Any(c => char.IsWhiteSpace(c) || c == '/'))
                    failures.Add($"{path}:Team must be the Vercel team slug, such as \"acme\".");

                issuer = options.IssuerMode == VercelIssuerMode.Team
                    ? $"{Constants.Vercel.Host}/{options.Team}"
                    : Constants.Vercel.Host;
                if (string.IsNullOrWhiteSpace(audience))
                    audience = $"https://vercel.com/{options.Team}";

                // In global issuer mode every team shares the issuer, so the owner is what keeps another team's
                // deployments out. In team mode it repeats the issuer, which costs nothing.
                rules.Add(Rule([Constants.Vercel.Owner], [options.Team ?? ""], StringComparison.Ordinal));
                // Without a project list, every project in the team gets in. The team is what keeps everyone else out.
                if (HasValues(options.Projects))
                    rules.Add(Rule([Constants.Vercel.Project, Constants.Vercel.ProjectId], options.Projects, StringComparison.Ordinal));
                // Development is left out by default: its token is the one `vercel env pull` hands to anyone on
                // the team for local work. A site that wants local machines in lists it in Environments.
                rules.Add(Rule(
                    [Constants.Vercel.Environment],
                    HasValues(options.Environments) ? options.Environments : ["production", "preview"],
                    StringComparison.OrdinalIgnoreCase));
                break;

            case IssuerPreset.GitHubActions:
                issuer = Constants.GitHubActions.Issuer;
                // GitHub's own default, which core.getIDToken() asks for when it is given no audience.
                if (string.IsNullOrWhiteSpace(audience))
                    audience = $"https://github.com/{options.Owner}";
                if (string.IsNullOrWhiteSpace(options.Owner) || options.Owner.Any(c => char.IsWhiteSpace(c) || c == '/'))
                    failures.Add($"{path}:Owner must be the GitHub user or organization, such as \"acme\".");
                else if (options.Repositories.Any(repository => !string.IsNullOrWhiteSpace(repository) &&
                             !repository.StartsWith($"{options.Owner}/", StringComparison.OrdinalIgnoreCase)))
                    failures.Add($"{path}:Repositories must belong to {options.Owner}, as {options.Owner}/repo.");

                // Every repository on GitHub shares the issuer, so the owner is what keeps everyone else out.
                // GitHub treats owner and repository names case-insensitively, so the allow-lists do too.
                rules.Add(Rule([Constants.GitHubActions.Owner], [options.Owner ?? ""], StringComparison.OrdinalIgnoreCase));
                if (HasValues(options.Repositories))
                    rules.Add(Rule([Constants.GitHubActions.Repository], options.Repositories, StringComparison.OrdinalIgnoreCase));
                if (HasValues(options.Refs))
                    rules.Add(Rule([Constants.GitHubActions.Ref], options.Refs, StringComparison.Ordinal));
                if (HasValues(options.Environments))
                    rules.Add(Rule([Constants.GitHubActions.Environment], options.Environments, StringComparison.Ordinal));
                break;

            case IssuerPreset.GitLab:
                issuer = string.IsNullOrWhiteSpace(options.Host) ? Constants.GitLab.Host : options.Host.TrimEnd('/');
                // GitLab has no default audience: the job names one in id_tokens, and the site must expect it.
                if (string.IsNullOrWhiteSpace(audience))
                    failures.Add($"{path}:Audience is required, and must match the aud the job asks for in id_tokens.");
                var group = options.Namespace?.Trim('/');
                if (string.IsNullOrWhiteSpace(group) || group.Any(char.IsWhiteSpace))
                    failures.Add($"{path}:Namespace must be the full path of the GitLab group or user, such as \"acme\".");
                else if (options.Projects.Any(project => !string.IsNullOrWhiteSpace(project) &&
                             !project.StartsWith($"{group}/", StringComparison.OrdinalIgnoreCase)))
                    failures.Add($"{path}:Projects must belong to {group}, as {group}/project.");

                // Every project on the instance shares the issuer, so the namespace is what keeps everyone else out.
                // A group's subgroups are part of it, so a project in acme/web passes a rule for acme. GitLab treats
                // paths case-insensitively, so the allow-lists do too.
                rules.Add(Rule([Constants.GitLab.Namespace], [group ?? ""], StringComparison.OrdinalIgnoreCase, subpaths: true));
                if (HasValues(options.Projects))
                    rules.Add(Rule([Constants.GitLab.Project], options.Projects, StringComparison.OrdinalIgnoreCase));
                if (HasValues(options.Refs))
                    rules.Add(Rule([Constants.GitLab.Ref], options.Refs, StringComparison.Ordinal));
                if (HasValues(options.Environments))
                    rules.Add(Rule([Constants.GitLab.Environment], options.Environments, StringComparison.Ordinal));
                break;

            case IssuerPreset.Azure:
                if (!Guid.TryParse(options.TenantId, out var tenantId))
                    failures.Add($"{path}:TenantId must be the Entra ID directory (tenant) ID, a GUID.");
                if (string.IsNullOrWhiteSpace(audience))
                    failures.Add($"{path}:Audience is required: the application (client) ID of the app registration that stands for the site.");
                // Entra ID writes a client ID in aud as a lower-case GUID, and aud is compared exactly.
                else if (Guid.TryParse(audience, out var clientId))
                    audience = clientId.ToString("D");
                if (!string.IsNullOrWhiteSpace(options.Issuer))
                    failures.Add($"{path}:Issuer is set by the Azure preset, which covers the public cloud. Set up a national cloud as a custom issuer.");
                if (!HasValues(options.PrincipalIds))
                    failures.Add($"{path}:PrincipalIds must name at least one identity, or any app in the tenant could get in.");
                else if (options.PrincipalIds.Any(id => !string.IsNullOrWhiteSpace(id) && !Guid.TryParse(id, out _)))
                    failures.Add($"{path}:PrincipalIds must be object (principal) IDs, which are GUIDs.");

                // The app registration's requestedAccessTokenVersion decides which format the tenant issues, and
                // the workload has no say in it, so both are accepted. They differ in iss, not in what they prove.
                issuer = $"{Constants.Azure.Host}/{tenantId:D}/v2.0";
                alternate = ($"{Constants.Azure.V1Host}/{tenantId:D}/", $"{Constants.Azure.Host}/{tenantId:D}/.well-known/openid-configuration");

                // By default, any app or managed identity in the tenant can get a token for any app registration,
                // so the identity is what keeps the rest of the tenant out. oid is the same in both formats and,
                // unlike a client ID, can only ever name the workload itself.
                // Written the way Entra ID writes oid, so an ID pasted without hyphens or in braces still matches.
                rules.Add(Rule(
                    [Constants.Azure.ObjectId],
                    options.PrincipalIds.Select(id => Guid.TryParse(id, out var principalId) ? principalId.ToString("D") : id),
                    StringComparison.OrdinalIgnoreCase));
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
            return [];

        var name = string.IsNullOrWhiteSpace(options.Name)
            ? options.Preset == IssuerPreset.Custom ? issuer! : options.Preset.ToString()
            : options.Name;
        var trusted = new TrustedIssuer(name, issuer!, audience!, metadataAddress, rules, options.AllowPreview);
        if (alternate is not { } other)
            return [trusted];

        return
        [
            trusted,
            trusted with
            {
                Issuer = other.Issuer,
                MetadataAddress = string.IsNullOrWhiteSpace(options.MetadataAddress) ? other.MetadataAddress : options.MetadataAddress,
            },
        ];
    }

    private static ClaimRule Rule(
        IReadOnlyList<string> claimNames,
        IEnumerable<string> values,
        StringComparison comparison,
        bool subpaths = false) =>
        new(claimNames, values.Where(value => !string.IsNullOrWhiteSpace(value)).ToList(), comparison, subpaths);

    private static bool HasValues(IEnumerable<string>? values) => values?.Any(value => !string.IsNullOrWhiteSpace(value)) == true;
}
