namespace TheBuilder.VercelOidc.Tokens;

/// <summary>
///     The Vercel deployment a validated token was issued to.
/// </summary>
/// <param name="Owner">The team slug.</param>
/// <param name="OwnerId">The team id.</param>
/// <param name="Project">The project name.</param>
/// <param name="ProjectId">The project id.</param>
/// <param name="Environment">The environment, such as <c>production</c>.</param>
public sealed record VercelOidcIdentity(
    string Owner,
    string OwnerId,
    string Project,
    string ProjectId,
    string Environment);
