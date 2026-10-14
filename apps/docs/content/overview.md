---
title: API OIDC for Umbraco
description: Let frontends call the Umbraco Delivery API and your own endpoints with their platform's OIDC token, instead of a shared API key.
sidebar:
  label: Overview
seo:
  image: /og/overview.png
---

Let your frontends call the Umbraco Delivery API, and your own API endpoints, with the OIDC token their platform already gives them, instead of a shared API key.

Umbraco protects a private Delivery API with one API key. Every frontend gets the same key, it lives in environment variables forever, and rotating it means touching every project at once. Most hosting and CI platforms now give each workload a short-lived, signed token that says who it is. This package teaches Umbraco to accept those tokens, on the Delivery API and on [your own endpoints](/custom-endpoints).

- **No shared secret.** Tokens are signed by the platform and expire on their own. There is nothing to rotate.
- **Scoped to exactly who you trust.** You list the Vercel teams or projects, GitHub repositories or other workloads that may read content. A token from anyone else is refused.
- **Drop-in.** Public access, media access and the API key keep working exactly as before, so you can move frontends over one at a time and remove the API key when the last one is done.

## Which platforms work

| Platform | How the frontend gets a token | Setup |
| --- | --- | --- |
| [Vercel](https://vercel.com/docs/oidc) functions and builds | `getVercelOidcToken()` from `@vercel/oidc` | [`Vercel` preset](/issuers#vercel) |
| [GitHub Actions](https://docs.github.com/en/actions/concepts/security/openid-connect) | `core.getIDToken(audience)` | [`GitHubActions` preset](/issuers#github-actions) |
| [Google Cloud](https://docs.cloud.google.com/docs/authentication/get-id-token) Run, Functions and GKE | The metadata server, with your audience | [Custom issuer](/issuers#google-cloud) |
| [AWS](https://docs.aws.amazon.com/IAM/latest/UserGuide/id_roles_providers_outbound.html) Lambda, ECS and EC2 | STS `GetWebIdentityToken` (IAM outbound identity federation) | [Custom issuer](/issuers#aws) |
| [GitLab CI](https://docs.gitlab.com/ci/secrets/id_token_authentication/) | `id_tokens` in `.gitlab-ci.yml` | [Custom issuer](/issuers#gitlab-ci) |
| [Azure](https://learn.microsoft.com/en-us/entra/identity/managed-identities-azure-resources/how-to-use-vm-token) | A managed identity token from Entra ID | [Custom issuer](/issuers#any-other-issuer) |

Cloudflare Workers and Netlify Functions have no built-in workload token, so frontends there keep using the API key.

## Requirements

- Umbraco CMS 17.1 or later, Umbraco 18 included, on .NET 10.
- A platform that gives your frontend an OIDC token. The table above lists the common ones.

## Where to go next

- [Quickstart](/quickstart) installs the package and sends the first token.
- [Issuers](/issuers) sets up Vercel, GitHub Actions and any other issuer.
- [Custom endpoints](/custom-endpoints) protects your own controllers and minimal APIs.
