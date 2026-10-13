using TheBuilder.VercelOidc.Tokens;

namespace TheBuilder.VercelOidc.DeliveryApi;

/// <summary>
///     Set on the request when it carried a valid Vercel OIDC token. Its presence is what grants access.
/// </summary>
/// <param name="Identity">The deployment the token was issued to.</param>
public sealed record VercelOidcAccessFeature(VercelOidcIdentity Identity);
