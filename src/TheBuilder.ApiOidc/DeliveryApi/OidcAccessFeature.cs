using TheBuilder.ApiOidc.Tokens;

namespace TheBuilder.ApiOidc.DeliveryApi;

/// <summary>
///     Set on the request when it carried a valid OIDC token. Its presence is what grants access.
/// </summary>
/// <param name="Identity">The workload the token was issued to.</param>
public sealed record OidcAccessFeature(OidcIdentity Identity);
