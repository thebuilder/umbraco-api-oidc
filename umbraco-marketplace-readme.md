# API OIDC

Let your frontends call a private Umbraco Content Delivery API, and your own API endpoints, with the OIDC
token their platform already gives them, instead of a shared API key.

A private Delivery API is protected by a single API key. Every frontend shares it, and rotating it means
updating every project at the same time. Vercel, GitHub Actions, Google Cloud, AWS and other platforms can
instead give each workload a short-lived token, signed by the platform. This package lets Umbraco accept
those tokens.

## What it does

- Accepts tokens from the Vercel teams or projects, GitHub repositories and branches, or other OIDC issuers
  you list, and refuses the rest.
- Has presets for Vercel and GitHub Actions. Google Cloud, AWS, Azure and GitLab need an issuer URL, an
  audience and one required claim.
- Leaves public access, media access and the API key as they are, so frontends can move over one at a time.
- Grants preview access to draft content, as the API key does, unless you turn it off per issuer.
- Protects your own endpoints with `[RequireOidcToken]`, or with the Delivery API's own rules using
  `[RequireDeliveryApiAccess]`.

## Install

```sh
dotnet add package TheBuilder.ApiOidc
```

The package supports Umbraco CMS 17.1 through 18.x on .NET 10. It has no backoffice UI; you configure the
trusted issuers in `appsettings.json`.

[Read the documentation](https://github.com/thebuilder/umbraco-api-oidc#readme) for setup on each
platform.
