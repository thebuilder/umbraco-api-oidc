namespace TheBuilder.VercelOidc.Configuration;

/// <summary>
///     The issuer mode set on the Vercel team, under Settings > Security > Secure backend access with OIDC federation.
/// </summary>
public enum VercelIssuerMode
{
    /// <summary>Tokens are issued by <c>https://oidc.vercel.com/{team}</c>. Vercel's default and recommended mode.</summary>
    Team,

    /// <summary>Tokens are issued by <c>https://oidc.vercel.com</c>, shared by every team.</summary>
    Global,
}
