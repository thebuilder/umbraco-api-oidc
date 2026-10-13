using Microsoft.Extensions.Options;

namespace TheBuilder.ApiOidc.Configuration;

internal sealed class ApiOidcOptionsValidator : IValidateOptions<ApiOidcOptions>
{
    public ValidateOptionsResult Validate(string? name, ApiOidcOptions options)
    {
        var failures = TrustedIssuerResolver.Validate(options).ToList();
        if (string.IsNullOrWhiteSpace(options.HeaderName))
            failures.Add($"{ApiOidcOptions.SectionName}:HeaderName must not be empty.");
        if (options.ClockSkew < TimeSpan.Zero)
            failures.Add($"{ApiOidcOptions.SectionName}:ClockSkew must not be negative.");

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
