# TheBuilder.ApiOidc

Let your frontends call the Umbraco Delivery API, and your own API endpoints, with the OIDC token their
platform already gives them, instead of a shared API key.

Umbraco protects a private Delivery API with one API key. Every frontend gets the same key, it lives in
environment variables forever, and rotating it means touching every project at once. Most hosting and CI
platforms now give each workload a short-lived, signed token that says who it is. This package teaches
Umbraco to accept those tokens, on the Delivery API and on any endpoint you
[protect with `[RequireOidcToken]`](#protecting-your-own-endpoints).

- **No shared secret.** Tokens are signed by the platform and expire on their own. There is nothing to rotate.
- **Scoped to exactly who you trust.** You list the Vercel teams or projects, GitHub repositories or other workloads
  that may read content. A token from anyone else gets a `401`.
- **Drop-in.** Public access, media access and the API key keep working exactly as before, so you can move
  frontends over one at a time and remove the API key when the last one is done.

The package requires Umbraco 17 or 18.

## Which platforms work

| Platform | How the frontend gets a token | Setup |
| --- | --- | --- |
| [Vercel](https://vercel.com/docs/oidc) functions and builds | `getVercelOidcToken()` from `@vercel/oidc` | [`Vercel` preset](#vercel) |
| [GitHub Actions](https://docs.github.com/en/actions/concepts/security/openid-connect) | `core.getIDToken(audience)` | [`GitHubActions` preset](#github-actions) |
| [Google Cloud](https://docs.cloud.google.com/docs/authentication/get-id-token) Run, Functions and GKE | The metadata server, with your audience | [Custom issuer](#google-cloud) |
| [AWS](https://docs.aws.amazon.com/IAM/latest/UserGuide/id_roles_providers_outbound.html) Lambda, ECS and EC2 | STS `GetWebIdentityToken` (IAM outbound identity federation) | [Custom issuer](#aws) |
| [GitLab CI](https://docs.gitlab.com/ci/secrets/id_token_authentication/) | `id_tokens` in `.gitlab-ci.yml` | [Custom issuer](#gitlab-ci) |
| [Azure](https://learn.microsoft.com/en-us/entra/identity/managed-identities-azure-resources/how-to-use-vm-token) | A managed identity token from Entra ID | [Custom issuer](#any-other-issuer) |

Cloudflare Workers and Netlify Functions have no built-in workload token, so frontends there keep using
the API key.

## Getting started

1. Install the package in your Umbraco site:

   ```sh
   dotnet add package TheBuilder.ApiOidc
   ```

2. Make sure your `Program.cs` calls `AddDeliveryApi()` before `AddComposers()`, as Umbraco's template
   does. The package replaces the Delivery API's access check, so it has to come after it.

3. List the issuers you trust:

   ```json
   {
     "Umbraco": {
       "CMS": {
         "DeliveryApi": {
           "Enabled": true,
           "PublicAccess": false
         }
       }
     },
     "ApiOidc": {
       "Issuers": [
         {
           "Preset": "Vercel",
           "Team": "acme"
         }
       ]
     }
   }
   ```

4. Send the token in the `X-Oidc-Token` header with each Delivery API request. On Vercel:

   ```ts
   import { getVercelOidcToken } from '@vercel/oidc';

   const response = await fetch(`${process.env.UMBRACO_URL}/umbraco/delivery/api/v2/content`, {
     headers: { 'X-Oidc-Token': await getVercelOidcToken() },
   });
   ```

5. Once no frontend sends the `Api-Key` header any more, remove `Umbraco:CMS:DeliveryApi:ApiKey`.

## Custom endpoints under the Delivery API

Umbraco only checks access on its own Delivery API controllers. An endpoint that a package or your site adds
under `/umbraco/delivery/api`, such as a dictionary endpoint, would otherwise be open even when the Delivery
API is private.

With this package installed, those endpoints get the same check as Umbraco's content endpoints: they are
served when `PublicAccess` is on, or the request carries a valid API key or a trusted OIDC token, and get a
`401` otherwise. You don't add anything to the controller.

- Umbraco's own Delivery API endpoints are left to Umbraco, so preview and member authorization work as
  before.
- An endpoint marked `[AllowAnonymous]` stays open.
- To turn this off, set `"ProtectCustomDeliveryApiEndpoints": false` in the `ApiOidc` section.
- At startup, the log lists every endpoint that gets the check, such as
  `GET /umbraco/delivery/api/v1/dictionary`.
- Endpoints outside `/umbraco/delivery/api` are not touched. Protect those with `[RequireOidcToken]`, below.

## Protecting your own endpoints

The same tokens can protect your own API controllers and minimal APIs, such as a search endpoint that only
your frontend should call. Add `[RequireOidcToken]` to a controller or action:

```csharp
using TheBuilder.ApiOidc.Authentication;

[ApiController]
[Route("api/search")]
[RequireOidcToken]
public sealed class SearchController : ControllerBase
{
    [HttpGet]
    public IActionResult Search(string query) => Ok(/* ... */);
}
```

Or, for a minimal API, call `RequireOidcToken()`:

```csharp
app.MapGet("/api/search", (string query) => /* ... */).RequireOidcToken();
```

A request without a valid token gets a `401`. The token goes in the same header and is checked against the
same `Issuers` as on the Delivery API. To accept only some of them, pass their `Name`s exactly as
configured, for example `[RequireOidcToken("Vercel")]`. A token from another trusted issuer then gets a `403`.
Only a token checked by this package satisfies the attribute, even when the endpoint also accepts a cookie
or another scheme.

Inside the endpoint, `User` carries the token's claims under their own names, such as `sub`, `project` or
`repository`, plus `oidc_issuer_name` with the `Name` of the issuer that accepted it.

If you prefer plain ASP.NET Core authorization, the package also registers the authentication scheme
`OidcAuthenticationDefaults.AuthenticationScheme` and the policy `OidcAuthenticationDefaults.Policy`, so
`[Authorize(Policy = OidcAuthenticationDefaults.Policy)]` works too. Neither is a default, so the backoffice,
members and every endpoint that does not ask for a token are unaffected. The API key is not accepted on
these endpoints.

## Issuers

Each entry in `Issuers` is one source of tokens. A token is accepted when its issuer, audience and claims
match an entry. Every entry accepts these settings:

| Setting          | What it does |
| ---------------- | ------------ |
| `Name`           | A name for log messages and for `[RequireOidcToken(...)]`. Defaults to the preset or the issuer URL. |
| `Preset`         | `Vercel`, `GitHubActions`, or `Custom` (the default). |
| `Audience`       | The `aud` the token must carry. |
| `RequiredClaims` | Claims the token must carry, each with its allowed values. Added to the preset's own rules. |
| `AllowPreview`   | Whether the token also unlocks draft content, as the API key does. Defaults to `true`. |
| `MetadataAddress`| Where the issuer publishes its keys. Defaults to `{issuer}/.well-known/openid-configuration`. |

### Vercel

See Vercel's [OIDC federation docs](https://vercel.com/docs/oidc) for how tokens are issued and what
they contain.

```json
{ "Preset": "Vercel", "Team": "acme" }
```

- `Team` is your team slug, and the only required setting. Tokens from any other team are refused.
- `Projects` limits access to some of the team's projects, by name or id, such as
  `["acme-web", "prj_123"]`. Leave it out to accept every project in the team.
- `Environments` defaults to `production`, `preview` and `development`. The `development` token is the one
  `vercel env pull` writes for local work, and anyone on the team can pull it. To keep local machines out,
  set `"Environments": ["production", "preview"]`.
- `IssuerMode` is `Team` (the default) or `Global`, matching your team's OIDC settings on Vercel.
- `Audience` defaults to `https://vercel.com/{team}`, which is what `getVercelOidcToken()` returns.
- Builds get a token too, through `VERCEL_OIDC_TOKEN`, so static generation works.

### GitHub Actions

See GitHub's [OpenID Connect docs](https://docs.github.com/en/actions/concepts/security/openid-connect)
for how workflows request tokens and what they contain.

```json
{
  "Preset": "GitHubActions",
  "Audience": "https://cms.example.com",
  "Repositories": ["acme/site"],
  "Refs": ["refs/heads/main"]
}
```

Request the token with the same audience, in a job with `permissions: id-token: write`:

```js
const token = await core.getIDToken('https://cms.example.com');
```

`Audience` and `Repositories` are required. `Refs` and `Environments` are optional filters.

### Any other issuer

`Issuer`, `Audience` and at least one required claim are needed. Without a claim, any workload the
issuer serves could get in. With Google, for example, that would be anyone with a Google account.
Required claims are compared with top-level string claims in the token, exactly.

Decode a real token from your workload, for example with [jwt.io](https://jwt.io), before you write the
entry: it shows the exact `iss`, `aud` and claims to match.

#### Google Cloud

Request an [ID token](https://docs.cloud.google.com/docs/authentication/get-id-token) from the metadata
server with your audience, and require the service account's email:

```json
{
  "Name": "renderer",
  "Issuer": "https://accounts.google.com",
  "Audience": "https://cms.example.com",
  "RequiredClaims": { "email": ["renderer@acme.iam.gserviceaccount.com"] }
}
```

#### AWS

Request a token with [`GetWebIdentityToken`](https://docs.aws.amazon.com/IAM/latest/UserGuide/id_roles_providers_outbound.html)
after enabling outbound identity federation on the account. The issuer is specific to your account, and
`sub` is the [ARN of the role](https://docs.aws.amazon.com/IAM/latest/UserGuide/id_roles_providers_outbound_token_claims.html)
that asked for the token:

```json
{
  "Name": "lambda-renderer",
  "Issuer": "https://abc123-def456-ghi789-jkl012.tokens.sts.global.api.aws",
  "Audience": "https://cms.example.com",
  "RequiredClaims": { "sub": ["arn:aws:iam::123456789012:role/Renderer"] }
}
```

#### GitLab CI

Declare an [ID token](https://docs.gitlab.com/ci/secrets/id_token_authentication/) in the job with
your audience, and require the project path:

```json
{
  "Name": "gitlab-build",
  "Issuer": "https://gitlab.com",
  "Audience": "https://cms.example.com",
  "RequiredClaims": { "project_path": ["acme/site"] }
}
```

### Other settings

| Setting      | Default        | What it does |
| ------------ | -------------- | ------------ |
| `HeaderName` | `X-Oidc-Token` | The header that carries the token. A `Bearer ` prefix is accepted. |
| `ProtectCustomDeliveryApiEndpoints` | `true` | Whether [custom endpoints under the Delivery API](#custom-endpoints-under-the-delivery-api) get its access check. |
| `ClockSkew`  | `00:01:00`     | How far token lifetimes may disagree with the server clock. |

The site refuses to start if an issuer is missing what it needs, so a typo cannot quietly let everyone in.

### Why not the `Authorization` header?

Many platforms' examples send the token as `Authorization: Bearer`. The Delivery API already uses that
header for member authorization, so this package defaults to its own header. If your site does not use
member authorization, you can set `HeaderName` to `Authorization`.

## How it works

1. A middleware runs at the start of Umbraco's pipeline. For requests under `/umbraco/delivery/api`
   that carry the header, it finds the trusted issuers matching the token's `iss`, and validates the
   signature against the keys that issuer publishes. Then it checks the audience, the lifetime and the
   claims the issuer entry requires.
2. The package replaces Umbraco's `IApiAccessService` with one that grants access when step 1 found a
   valid token, and otherwise applies Umbraco's own rules unchanged.
3. After routing, a second middleware applies that access check to every endpoint under
   `/umbraco/delivery/api` that does not come from Umbraco itself. See
   [Custom endpoints under the Delivery API](#custom-endpoints-under-the-delivery-api).

A refused token is logged as a warning with the reason, and the request carries on. Umbraco then
answers it as it would any request without credentials.

On your own endpoints, an ASP.NET Core authentication handler does the same validation, and only for
endpoints that require it.

## Development

```sh
dotnet build TheBuilder.ApiOidc.slnx
dotnet test TheBuilder.ApiOidc.slnx
```

`samples/TheBuilder.ApiOidc.Example` is an Umbraco site that references the package. It installs
itself unattended into SQLite on first run.

[RELEASE.md](RELEASE.md) describes how a GitHub Release publishes the package to nuget.org and the
Umbraco Marketplace.

## License

MIT
