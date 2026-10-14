# API OIDC

Let your frontends call a private Umbraco Content Delivery API, and your own API endpoints, with the OIDC
token their platform already gives them, instead of a shared API key.

A private Delivery API is protected by one API key. Every frontend gets the same key, it lives in
environment variables forever, and rotating it means touching every project at once. Vercel, GitHub
Actions, Google Cloud, AWS and other platforms give each workload a short-lived, signed token that says
who it is. This package lets Umbraco accept those tokens.

## Key features

- No shared secret: tokens are signed by the platform and expire on their own.
- Scoped to exactly who you trust: a Vercel team, or specific projects and environments, GitHub repositories and
  branches, or any OIDC issuer with the claims you require.
- Presets for Vercel and GitHub Actions, and a generic setup for Google Cloud, AWS, Azure and GitLab.
- Drop-in: public access, media access and the existing API key keep working, so frontends can move over
  one at a time.
- Grants preview access to draft content, as the API key does, unless you turn it off per issuer.
- Protects your own endpoints too, such as a search API, with a `[RequireOidcToken]` attribute.
- Gives custom endpoints, such as a dictionary endpoint, the Delivery API's own access rules with `[RequireDeliveryApiAccess]`.

## Install

```sh
dotnet add package TheBuilder.ApiOidc
```

The package supports Umbraco CMS 17.1 through 18.x on .NET 10. It has no backoffice UI; you configure the
trusted issuers in `appsettings.json`.

[Read the documentation](https://github.com/thebuilder/umbraco-api-oidc#readme) for setup on each
platform.
