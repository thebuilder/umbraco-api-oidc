# TheBuilder.VercelOidc

Let your Vercel deployments read the Umbraco Delivery API with their own OIDC token, instead of a
shared API key.

Umbraco protects a private Delivery API with one API key. Every frontend gets the same key, it lives in
environment variables forever, and rotating it means touching every project at once. Vercel already gives
each deployment a short-lived, signed token that says which team, project and environment it belongs to.
This package teaches Umbraco to accept that token.

- **No shared secret.** Tokens are signed by Vercel and expire on their own. There is nothing to rotate.
- **Scoped by project and environment.** You list the Vercel projects, and the environments, that may
  read content. A token from any other project gets a `401`.
- **Drop-in.** Public access, media access and the API key keep working exactly as before, so you can
  move frontends over one at a time and remove the API key when the last one is done.

The package requires Umbraco 17 or 18.

## Getting started

1. Install the package in your Umbraco site:

   ```sh
   dotnet add package TheBuilder.VercelOidc
   ```

2. Make sure your `Program.cs` calls `AddDeliveryApi()` before `AddComposers()`, as Umbraco's template
   does. The package replaces the Delivery API's access check, so it has to come after it.

3. Configure which Vercel deployments may read content:

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
     "TheBuilder": {
       "VercelOidc": {
         "TeamSlug": "acme",
         "Projects": ["acme-web"]
       }
     }
   }
   ```

4. In your Vercel project, send the token with each Delivery API request:

   ```ts
   import { getVercelOidcToken } from '@vercel/oidc';

   const response = await fetch(`${process.env.UMBRACO_URL}/umbraco/delivery/api/v2/content`, {
     headers: { 'X-Vercel-Oidc-Token': await getVercelOidcToken() },
   });
   ```

   `getVercelOidcToken()` reads the token from the incoming request in a Vercel function, and from
   `VERCEL_OIDC_TOKEN` during builds, so static generation works too. For local development, run
   `vercel env pull` to get a development token, and add `development` to `Environments`.

5. Once no frontend sends the `Api-Key` header any more, remove `Umbraco:CMS:DeliveryApi:ApiKey`.

## Configuration

All settings live under `TheBuilder:VercelOidc`.

| Setting        | Default                    | What it does |
| -------------- | -------------------------- | ------------ |
| `TeamSlug`     | *(empty)*                  | Your Vercel team slug. Leaving it empty turns the package off. |
| `IssuerMode`   | `Team`                     | `Team` or `Global`, matching the issuer mode in your Vercel team's OIDC settings. |
| `Audience`     | `https://vercel.com/{team}`| Set this if you request the token with a custom audience. |
| `Projects`     | *(required)*               | Project names or ids (`prj_...`) allowed to read content. |
| `Environments` | `production`, `preview`    | Vercel environments allowed to read content. |
| `AllowPreview` | `true`                     | Whether a valid token also unlocks draft content, as the API key does. |
| `HeaderName`   | `X-Vercel-Oidc-Token`      | The header that carries the token. A `Bearer ` prefix is accepted. |
| `ClockSkew`    | `00:01:00`                 | How far token lifetimes may disagree with the server clock. |

The site refuses to start if `TeamSlug` is set but `Projects` or `Environments` is empty, so a typo
cannot quietly open the API to every project on the team.

### Why not the `Authorization` header?

Vercel's examples send the token as `Authorization: Bearer`. The Delivery API already uses that header
for member authorization, so this package defaults to its own header. If your site does not use member
authorization, you can set `HeaderName` to `Authorization`.

## How it works

1. A middleware runs at the start of Umbraco's pipeline. For requests under `/umbraco/delivery/api`
   that carry the header, it validates the token: the signature against the keys Vercel publishes at
   `https://oidc.vercel.com/{team}/.well-known/jwks`, then the issuer, audience and lifetime, and
   finally the team, project and environment claims.
2. The package replaces Umbraco's `IApiAccessService` with one that grants access when step 1 found a
   valid token, and otherwise applies Umbraco's own rules unchanged.

A refused token is logged as a warning with the reason, and the request carries on. Umbraco then
answers it as it would any request without credentials.

## Development

```sh
dotnet build TheBuilder.VercelOidc.slnx
dotnet test TheBuilder.VercelOidc.slnx
```

`samples/TheBuilder.VercelOidc.Example` is an Umbraco site that references the package. It installs
itself unattended into SQLite on first run.

## License

MIT
