using Microsoft.Extensions.Options;

namespace TheBuilder.DeliveryApiOidc.Configuration;

internal sealed class DeliveryApiOidcOptionsValidator : IValidateOptions<DeliveryApiOidcOptions>
{
    public ValidateOptionsResult Validate(string? name, DeliveryApiOidcOptions options)
    {
        var failures = TrustedIssuerResolver.Validate(options).ToList();
        if (string.IsNullOrWhiteSpace(options.HeaderName))
            failures.Add($"{DeliveryApiOidcOptions.SectionName}:HeaderName must not be empty.");
        if (options.ClockSkew < TimeSpan.Zero)
            failures.Add($"{DeliveryApiOidcOptions.SectionName}:ClockSkew must not be negative.");

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
