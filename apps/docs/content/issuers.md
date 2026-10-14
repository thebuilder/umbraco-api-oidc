---
title: Issuers
description: Trust tokens from Vercel, GitHub Actions, Google Cloud, AWS, GitLab and any other OIDC issuer.
seo:
  image: /og/issuers.png
---

Each entry in `Issuers` is one source of tokens. A token is accepted when its issuer, audience and claims match an entry. Every entry accepts these settings:

| Setting          | What it does |
| ---------------- | ------------ |
| `Name`           | A name for log messages and for `[RequireOidcToken(...)]`. Defaults to the preset name or the issuer URL. |
| `Preset`         | `Vercel`, `GitHubActions`, or `Custom` (the default). |
| `Audience`       | The `aud` the token must carry. |
| `RequiredClaims` | Claims the token must carry, each with its allowed values. Added to the preset's own rules. |
| `AllowPreview`   | Whether the token also unlocks draft content, as the API key does. Defaults to `true`. |
| `MetadataAddress`| Where the issuer publishes its keys. Defaults to `{issuer}/.well-known/openid-configuration`. |

## Vercel

See Vercel's [OIDC federation docs](https://vercel.com/docs/oidc) for how tokens are issued and what they contain.

```json
{ "Preset": "Vercel", "Team": "acme" }
```

- `Team` is your team slug, and the only required setting. Tokens from any other team are refused.
- `Projects` limits access to some of the team's projects, by name or id, such as `["acme-web", "prj_123"]`. Leave it out to accept every project in the team.
- `Environments` defaults to `production`, `preview` and `development`. The `development` token is the one `vercel env pull` writes for local work, and anyone on the team can pull it. To keep local machines out, set `"Environments": ["production", "preview"]`.
- `IssuerMode` is `Team` (the default) or `Global`, matching your team's OIDC settings on Vercel.
- `Audience` defaults to `https://vercel.com/{team}`, which is what `getVercelOidcToken()` returns.
- Builds get a token too, through `VERCEL_OIDC_TOKEN`, so static generation works.

## GitHub Actions

See GitHub's [OpenID Connect docs](https://docs.github.com/en/actions/concepts/security/openid-connect) for how workflows request tokens and what they contain.

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

## Any other issuer

`Issuer`, `Audience` and at least one required claim are needed. Without a claim, any workload the issuer serves could get in. With Google, for example, that would be anyone with a Google account. Each required claim must match a top-level string claim in the token, including its case.

Decode a real token from your workload, for example with [jwt.io](https://jwt.io), before you write the entry: it shows the exact `iss`, `aud` and claims to match.

### Google Cloud

Request an [ID token](https://docs.cloud.google.com/docs/authentication/get-id-token) from the metadata server with your audience, and require the service account's email:

```json
{
  "Name": "renderer",
  "Issuer": "https://accounts.google.com",
  "Audience": "https://cms.example.com",
  "RequiredClaims": { "email": ["renderer@acme.iam.gserviceaccount.com"] }
}
```

### AWS

Request a token with [`GetWebIdentityToken`](https://docs.aws.amazon.com/IAM/latest/UserGuide/id_roles_providers_outbound.html) after enabling outbound identity federation on the account. The issuer is specific to your account, and `sub` is the [ARN of the role](https://docs.aws.amazon.com/IAM/latest/UserGuide/id_roles_providers_outbound_token_claims.html) that asked for the token:

```json
{
  "Name": "lambda-renderer",
  "Issuer": "https://abc123-def456-ghi789-jkl012.tokens.sts.global.api.aws",
  "Audience": "https://cms.example.com",
  "RequiredClaims": { "sub": ["arn:aws:iam::123456789012:role/Renderer"] }
}
```

### GitLab CI

Declare an [ID token](https://docs.gitlab.com/ci/secrets/id_token_authentication/) in the job with your audience, and require the project path:

```json
{
  "Name": "gitlab-build",
  "Issuer": "https://gitlab.com",
  "Audience": "https://cms.example.com",
  "RequiredClaims": { "project_path": ["acme/site"] }
}
```
