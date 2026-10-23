<p align="center">
  <img src="https://raw.githubusercontent.com/thebuilder/umbraco-api-oidc/refs/heads/main/apps/docs/public/logo-mark.svg" width="112" height="112" alt="API OIDC logo" />
</p>

<h1 align="center">API OIDC for Umbraco</h1>

<p align="center">
  Let your frontends call the Umbraco Delivery API, and your own API endpoints,<br />
  with the OIDC token their platform already gives them, instead of a shared API key.
</p>

[![NuGet version](https://img.shields.io/nuget/v/TheBuilder.ApiOidc)](https://www.nuget.org/packages/TheBuilder.ApiOidc)
[![NuGet downloads](https://img.shields.io/nuget/dt/TheBuilder.ApiOidc)](https://www.nuget.org/packages/TheBuilder.ApiOidc)
[![License](https://img.shields.io/github/license/thebuilder/umbraco-api-oidc)](https://github.com/thebuilder/umbraco-api-oidc/blob/main/LICENSE)

A private Delivery API is protected by a single API key. Every frontend shares it, and rotating it means
updating every project at the same time. Most hosting and CI platforms can instead give each workload a
short-lived token, signed by the platform, that says which project or repository it came from. This package
lets Umbraco accept those tokens on the Delivery API and on [your own endpoints](#protecting-your-own-endpoints).

- Tokens expire on their own, so there is no secret to store or rotate.
- You choose which Vercel teams or projects, GitHub repositories, Azure identities or other workloads are
  allowed. Tokens from anywhere else are refused.
- Public access, media access and the API key work as before, so you can move frontends over one at a time
  and remove the key after the last one.

The package requires Umbraco 17.1 or later, including 18.

## Supported platforms

| Platform | How the frontend gets a token | Setup |
| --- | --- | --- |
| [Vercel](https://vercel.com/docs/oidc) functions and builds | `getVercelOidcToken()` from `@vercel/oidc` | [`Vercel` preset](#vercel) |
| [GitHub Actions](https://docs.github.com/en/actions/concepts/security/openid-connect) | `core.getIDToken(audience)` | [`GitHubActions` preset](#github-actions) |
| [Azure](https://learn.microsoft.com/en-us/entra/identity/managed-identities-azure-resources/overview) App Service, Functions, Container Apps, AKS and VMs | A managed identity token for your app registration | [`Azure` preset](#azure) |
| [Google Cloud](https://docs.cloud.google.com/docs/authentication/get-id-token) Run, Functions and GKE | The metadata server, with your audience | [Custom issuer](#google-cloud) |
| [AWS](https://docs.aws.amazon.com/IAM/latest/UserGuide/id_roles_providers_outbound.html) Lambda, ECS and EC2 | STS `GetWebIdentityToken` (IAM outbound identity federation) | [Custom issuer](#aws) |
| [GitLab CI](https://docs.gitlab.com/ci/secrets/id_token_authentication/) | `id_tokens` in `.gitlab-ci.yml` | [Custom issuer](#gitlab-ci) |

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
same `Issuers` as on the Delivery API. To accept only some of them, pass the `Name` of each exactly as
configured, for example `[RequireOidcToken("Vercel")]`. A token from another trusted issuer then gets a `403`.
Only a token checked by this package satisfies the attribute, even when the endpoint also accepts a cookie
or another scheme.

Inside the endpoint, `User` carries the token's claims under their own names, such as `sub`, `project` or
`repository`, plus `oidc_issuer_name` with the `Name` of the issuer that accepted it.

If you prefer plain ASP.NET Core authorization, the package also registers the authentication scheme
`OidcAuthenticationDefaults.AuthenticationScheme` and the policy `OidcAuthenticationDefaults.Policy`, so
`[Authorize(Policy = OidcAuthenticationDefaults.Policy)]` works too. Neither is a default, so the backoffice,
members and every endpoint that does not ask for a token are unaffected. The API key is not accepted here;
to accept it as well, use `[RequireDeliveryApiAccess]` below.

### Endpoints that belong with the Delivery API

Umbraco only checks access on its own Delivery API endpoints. A custom endpoint you add beside them, such as
a dictionary endpoint, is open to anyone, even under `/umbraco/delivery/api/`. To give it the same rules as
Umbraco's content endpoints, add `[RequireDeliveryApiAccess]` instead:

```csharp
using TheBuilder.ApiOidc.Authentication;

[ApiController]
[Route("umbraco/delivery/api/v1/dictionary")]
[RequireDeliveryApiAccess]
public sealed class DictionaryController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(/* ... */);
}
```

For a minimal API, call `RequireDeliveryApiAccess()`. The endpoint then serves everyone when
`Umbraco:CMS:DeliveryApi:PublicAccess` is `true`, and otherwise needs the `Api-Key` header or a valid token,
the same as Umbraco's content endpoints. Requests without access get a `401`, and nothing gets in while the
Delivery API is disabled. The policy is also available as
`OidcAuthenticationDefaults.DeliveryApiAccessPolicy`.

### A policy under your own name

Some packages leave access to their own endpoints to the site. Such a package looks up an authorization
policy by a name it defines, and the site registers that name. To make that policy require a token, call
`RequireOidcToken()` on the policy builder. For example, for a package whose deployment endpoint looks up
`Acme.DeploymentSync`:

```csharp
using TheBuilder.ApiOidc.Authentication;

builder.Services.AddAuthorization(options => options.AddPolicy(
    "Acme.DeploymentSync",
    policy => policy.RequireOidcToken("GitHubActions")));
```

The policy then checks the same as `[RequireOidcToken("GitHubActions")]`: only a token from that issuer gets
in, and a cookie or another scheme does not count. Leave out the issuers to accept any trusted issuer. The
other package does not need to know about this one.

## Issuers

Each entry in `Issuers` is one source of tokens. A token is accepted when its issuer, audience and claims
match an entry. Every entry accepts these settings:

| Setting          | What it does |
| ---------------- | ------------ |
| `Name`           | A name for log messages and for `[RequireOidcToken(...)]`. Defaults to the preset name or the issuer URL. |
| `Preset`         | `Vercel`, `GitHubActions`, `Azure`, or `Custom` (the default). |
| `Audience`       | The `aud` the token must carry, which names the service the token was made for. |
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
- `Environments` defaults to `production` and `preview`. To let local machines in as well, add
  `development`: `"Environments": ["production", "preview", "development"]`. The `development` token is
  the one `vercel env pull` writes for local work, and anyone on the team can pull it.
- `IssuerMode` is `Team` (the default) or `Global`, matching your team's OIDC settings on Vercel.
- `Audience` defaults to `https://vercel.com/{team}`, which is what `getVercelOidcToken()` returns.
- Builds get a token too, through `VERCEL_OIDC_TOKEN`, so static generation works.

### GitHub Actions

See GitHub's [OpenID Connect docs](https://docs.github.com/en/actions/concepts/security/openid-connect)
for how workflows request tokens and what they contain.

```json
{ "Preset": "GitHubActions", "Owner": "acme" }
```

`Owner` is your GitHub organization, or a user, written as GitHub writes it. It is the only required
setting, and tokens from any other owner are refused. On its own it accepts every repository the owner has.
To narrow it down, add any of these filters:

- `Repositories`, as `owner/repo`, such as `["acme/site"]`.
- `Refs`, such as `["refs/heads/main"]`.
- `Environments`, the GitHub deployment environments allowed in.

Request the token in a job with `permissions: id-token: write`:

```js
const token = await core.getIDToken();
```

The token's audience says which service it was made for. Without an argument, GitHub uses
`https://github.com/{owner}`, which is also this preset's default. With that default, a token your
workflows send to another service that accepts GitHub's default audience would also work on your CMS. To
keep them apart, pick your own audience: set it as `Audience`, such as `"https://cms.example.com"`, and pass
the same value to `core.getIDToken('https://cms.example.com')`.

### Azure

See Microsoft's [managed identity docs](https://learn.microsoft.com/en-us/entra/identity/managed-identities-azure-resources/overview)
for which Azure services have one and how they get tokens.

```json
{
  "Preset": "Azure",
  "TenantId": "3f2b8c1d-6e4a-4b7f-9d2c-8a1e5f3b7c90",
  "Audience": "6e74172b-be56-4843-9ff4-e66a39bb12e3",
  "PrincipalIds": ["e9b1f3a0-4c2d-4f7e-9a51-3b8c6d2e1f04"]
}
```

Entra ID only issues tokens for apps it knows, so the site needs an app registration of its own:

1. Under App registrations in Entra ID, register an app for the site, such as `Umbraco CMS`. It needs no
   redirect URI, secret or API permissions. Its Directory (tenant) ID is the `TenantId`, and its Application
   (client) ID is the `Audience`.
2. Turn on a managed identity for each workload, such as an App Service, Function App, Container App or
   virtual machine, or set up workload identity on AKS. Its Object (principal) ID goes in `PrincipalIds`.

All three settings are required. By default, any app or managed identity in the tenant can get a token for
your app registration, so `PrincipalIds` is what keeps the rest of the tenant out.

Request the token with the app's client ID as the scope. With `@azure/identity`:

```ts
import { ManagedIdentityCredential } from '@azure/identity';

const credential = new ManagedIdentityCredential();
const { token } = await credential.getToken('6e74172b-be56-4843-9ff4-e66a39bb12e3/.default');
```

For a user-assigned identity, pass its client ID: `new ManagedIdentityCredential({ clientId })`.

Entra ID issues v1.0 or v2.0 tokens, depending on the app registration's `requestedAccessTokenVersion`, and
the preset accepts both. A v2.0 token always carries the client ID as its audience. A v1.0 token carries
whatever the workload asked for, so if your workloads ask for an Application ID URI such as
`api://umbraco-cms` instead, set that as `Audience`. It then works with v1.0 tokens only.

Azure Government and the other national clouds sign in on another host. Set them up as a
[custom issuer](#any-other-issuer).

### Any other issuer

`Issuer`, `Audience` and at least one required claim are needed. Without a claim, any workload the
issuer serves could get in. With Google, for example, that would be anyone with a Google account.
Each required claim must match a top-level string claim in the token, including its case.

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

A refused token is logged as a warning with the reason, and the request carries on. Umbraco then
answers it as it would any request without credentials.

On your own endpoints, an ASP.NET Core authentication handler does the same validation, and only for
endpoints that require it.

## Documentation

The full documentation is at [api-oidc.thebuilder.dk](https://api-oidc.thebuilder.dk).

- [Quickstart](https://api-oidc.thebuilder.dk/quickstart): install the package, trust your platform and send the first token.
- [Issuers](https://api-oidc.thebuilder.dk/issuers): set up Vercel, GitHub Actions, Azure, Google Cloud, AWS, GitLab or any other issuer.
- [Custom endpoints](https://api-oidc.thebuilder.dk/custom-endpoints): protect your own controllers and minimal APIs.
- [Configuration](https://api-oidc.thebuilder.dk/configuration): the settings that apply to every issuer.

## Development

```sh
dotnet build TheBuilder.ApiOidc.slnx
dotnet test TheBuilder.ApiOidc.slnx
```

`samples/TheBuilder.ApiOidc.Example` is an Umbraco site that references the package. It installs
itself unattended into SQLite on first run.

The documentation site at [api-oidc.thebuilder.dk](https://api-oidc.thebuilder.dk) lives in `apps/docs` and
is built with Blume. `pnpm docs:dev` serves it with hot reload, and `pnpm docs:build` builds it as CI does.

[RELEASE.md](RELEASE.md) describes how a GitHub Release publishes the package to nuget.org and the
Umbraco Marketplace.

## License

MIT. See [LICENSE](LICENSE).
