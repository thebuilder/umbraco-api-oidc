# AGENTS

An Umbraco package that lets workloads (Vercel, GitHub Actions, Google Cloud, AWS and other OIDC issuers)
authenticate to the Content Delivery API, and to custom endpoints that opt in, with their OIDC token instead
of the shared Delivery API key.

## Layout

- `src/TheBuilder.ApiOidc/` - the package. No backoffice UI; it is configured from appsettings.
  - `Configuration/` - `ApiOidcOptions`, bound from `ApiOidc`, and
    `TrustedIssuerResolver`, which expands the Vercel and GitHub Actions presets into issuer, audience and
    claim rules. Add a new preset there.
  - `Tokens/` - JWT validation against each issuer's published keys, then the issuer entry's claim rules.
  - `DeliveryApi/` - the middleware that validates the token, the `IApiAccessService` replacement, and
    `DeliveryApiEndpointGate`, which applies that access check to non-Umbraco endpoints under the prefix.
  - `Authentication/` - the authentication scheme, policy and `[RequireOidcToken]` for custom endpoints.
  - `Composition/` - the composer that wires it all up.
- `tests/TheBuilder.ApiOidc.Tests/` - xunit v3. Tokens are minted locally with a throwaway RSA key.
- `samples/TheBuilder.ApiOidc.Example/` - an Umbraco site for trying the package by hand.

## Commands

```sh
dotnet build TheBuilder.ApiOidc.slnx -c Release
dotnet test TheBuilder.ApiOidc.slnx -c Release
dotnet pack src/TheBuilder.ApiOidc/TheBuilder.ApiOidc.csproj -c Release -o artifacts
```

`TreatWarningsAsErrors` is on everywhere, NuGet audit warnings included, so a vulnerable transitive
dependency fails the build. Lift it with a floor in `Directory.Packages.props` plus a `PackageReference`
in the package csproj, as the existing MailKit and MessagePack floors do.

## Rules

- Never weaken Umbraco's own rules in `OidcApiAccessService`. A token adds a way in; it must not
  change what public access or the API key do.
- A refused token must never end the request. Log it and let Umbraco answer.
- Never log the token itself.
- A custom issuer must require at least one claim. Issuer and audience alone often admit anyone.

## Releasing

A GitHub Release publishes to nuget.org through `.github/workflows/publish-nuget.yml`; `RELEASE.md` has
the setup and steps. Never publish by hand. The Umbraco Marketplace listing comes from
`umbraco-marketplace.json` and `umbraco-marketplace-readme.md` at the root, plus the `umbraco-marketplace`
tag on the package.
