---
title: API OIDC for Umbraco
description: Let frontends call the Umbraco Delivery API and your own endpoints with their platform's OIDC token, instead of a shared API key.
sidebar:
  label: Overview
seo:
  image: /og/overview.png
---

A private Delivery API is protected by a single API key. Every frontend shares it, and rotating it means updating every project at the same time. Most hosting and CI platforms can instead give each workload a short-lived token, signed by the platform, that says which project or repository it came from. This package lets Umbraco accept those tokens on the Delivery API and on [your own endpoints](/custom-endpoints).

- Tokens expire on their own, so there is no secret to store or rotate.
- You choose which Vercel teams or projects, GitHub repositories, Azure identities or other workloads are allowed. Tokens from anywhere else are refused.
- Public access, media access and the API key work as before, so you can move frontends over one at a time and remove the key after the last one.

## Supported platforms

| Platform | How the frontend gets a token | Setup |
| --- | --- | --- |
| [Vercel](https://vercel.com/docs/oidc) functions and builds | `getVercelOidcToken()` from `@vercel/oidc` | [`Vercel` preset](/issuers#vercel) |
| [GitHub Actions](https://docs.github.com/en/actions/concepts/security/openid-connect) | `core.getIDToken(audience)` | [`GitHubActions` preset](/issuers#github-actions) |
| [Azure](https://learn.microsoft.com/en-us/entra/identity/managed-identities-azure-resources/overview) App Service, Functions, Container Apps, AKS and VMs | A managed identity token for your app registration | [`Azure` preset](/issuers#azure) |
| [Google Cloud](https://docs.cloud.google.com/docs/authentication/get-id-token) Run, Functions and GKE | The metadata server, with your audience | [Custom issuer](/issuers#google-cloud) |
| [AWS](https://docs.aws.amazon.com/IAM/latest/UserGuide/id_roles_providers_outbound.html) Lambda, ECS and EC2 | STS `GetWebIdentityToken` (IAM outbound identity federation) | [Custom issuer](/issuers#aws) |
| [GitLab CI](https://docs.gitlab.com/ci/secrets/id_token_authentication/) | `id_tokens` in `.gitlab-ci.yml` | [Custom issuer](/issuers#gitlab-ci) |

Cloudflare Workers and Netlify Functions have no built-in workload token, so frontends there keep using the API key.

## Requirements

- Umbraco CMS 17.1 or later, Umbraco 18 included, on .NET 10.
- A platform that gives your frontend an OIDC token. The table above lists the common ones.

## Where to go next

- [Quickstart](/quickstart) installs the package and sends the first token.
- [Issuers](/issuers) sets up Vercel, GitHub Actions and any other issuer.
- [Custom endpoints](/custom-endpoints) protects your own controllers and minimal APIs.
