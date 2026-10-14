---
title: Quickstart
description: Install API OIDC, trust your platform's tokens, and send the first one to the Delivery API.
seo:
  image: /og/quickstart.png
---

1. Install the package in your Umbraco site:

   ```sh
   dotnet add package TheBuilder.ApiOidc
   ```

2. Make sure your `Program.cs` calls `AddDeliveryApi()` before `AddComposers()`, as Umbraco's template does. The package replaces the Delivery API's access check, so it has to come after it.

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

## Next

- [Issuers](/issuers) covers GitHub Actions, Google Cloud, AWS, GitLab and the rest of the Vercel settings.
- [Custom endpoints](/custom-endpoints) protects your own APIs with the same tokens.
