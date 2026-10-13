using Microsoft.Extensions.Options;

namespace TheBuilder.VercelOidc.Configuration;

internal sealed class VercelOidcOptionsValidator : IValidateOptions<VercelOidcOptions>
{
    public ValidateOptionsResult Validate(string? name, VercelOidcOptions options)
    {
        if (!options.IsEnabled)
            return ValidateOptionsResult.Success;

        var failures = new List<string>();
        if (options.TeamSlug!.Any(character => char.IsWhiteSpace(character) || character == '/'))
            failures.Add($"{VercelOidcOptions.SectionName}:TeamSlug must be the team slug alone, such as \"acme\".");
        if (!options.Projects.Any(project => !string.IsNullOrWhiteSpace(project)))
            failures.Add($"{VercelOidcOptions.SectionName}:Projects must name at least one Vercel project.");
        if (!options.Environments.Any(environment => !string.IsNullOrWhiteSpace(environment)))
            failures.Add($"{VercelOidcOptions.SectionName}:Environments must name at least one Vercel environment.");
        if (string.IsNullOrWhiteSpace(options.HeaderName))
            failures.Add($"{VercelOidcOptions.SectionName}:HeaderName must not be empty.");
        if (options.ClockSkew < TimeSpan.Zero)
            failures.Add($"{VercelOidcOptions.SectionName}:ClockSkew must not be negative.");

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
