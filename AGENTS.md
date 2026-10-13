# AGENTS

An Umbraco package that lets Vercel deployments authenticate to the Content Delivery API with their
Vercel OIDC token instead of the shared Delivery API key.

## Layout

- `src/TheBuilder.VercelOidc/` - the package. No backoffice UI; it is configured from appsettings.
  - `Configuration/` - `VercelOidcOptions`, bound from `TheBuilder:VercelOidc`, and its validator.
  - `Tokens/` - JWT validation against Vercel's JWKS, then team/project/environment checks.
  - `DeliveryApi/` - the middleware that validates the token, and the `IApiAccessService` replacement.
  - `Composition/` - the composer that wires it all up.
- `tests/TheBuilder.VercelOidc.Tests/` - xunit v3. Tokens are minted locally with a throwaway RSA key.
- `samples/TheBuilder.VercelOidc.Example/` - an Umbraco site for trying the package by hand.

## Commands

```sh
dotnet build TheBuilder.VercelOidc.slnx -c Release
dotnet test TheBuilder.VercelOidc.slnx -c Release
dotnet pack src/TheBuilder.VercelOidc/TheBuilder.VercelOidc.csproj -c Release -o artifacts
```

`TreatWarningsAsErrors` is on everywhere, NuGet audit warnings included, so a vulnerable transitive
dependency fails the build. Lift it with a floor in `Directory.Packages.props` plus a `PackageReference`
in the package csproj, as the existing MailKit and MessagePack floors do.

## Rules

- Never weaken Umbraco's own rules in `VercelOidcApiAccessService`. A token adds a way in; it must not
  change what public access or the API key do.
- A refused token must never end the request. Log it and let Umbraco answer.
- Never log the token itself.
