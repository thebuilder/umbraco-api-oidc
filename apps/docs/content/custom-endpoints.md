---
title: Custom endpoints
description: Protect your own controllers and minimal APIs with an OIDC token, or with the Delivery API's own access rules.
seo:
  image: /og/custom-endpoints.png
---

The same tokens can protect your own API controllers and minimal APIs, such as a search endpoint that only your frontend should call. Add `[RequireOidcToken]` to a controller or action:

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

A request without a valid token gets a `401`. The token goes in the same header and is checked against the same `Issuers` as on the Delivery API. To accept only some of them, pass the `Name` of each exactly as configured, for example `[RequireOidcToken("Vercel")]`. A token from another trusted issuer then gets a `403`. Only a token checked by this package satisfies the attribute, even when the endpoint also accepts a cookie or another scheme.

Inside the endpoint, `User` carries the token's claims under their own names, such as `sub`, `project` or `repository`, plus `oidc_issuer_name` with the `Name` of the issuer that accepted it.

If you prefer plain ASP.NET Core authorization, the package also registers the authentication scheme `OidcAuthenticationDefaults.AuthenticationScheme` and the policy `OidcAuthenticationDefaults.Policy`, so `[Authorize(Policy = OidcAuthenticationDefaults.Policy)]` works too. Neither is a default, so the backoffice, members and every endpoint that does not ask for a token are unaffected. The API key is not accepted here; to accept it as well, use `[RequireDeliveryApiAccess]` below.

## A policy under your own name

Some packages leave access to their own endpoints to the site. Such a package looks up an authorization policy by a name it defines, and the site registers that name. To make that policy require a token, call `RequireOidcToken()` on the policy builder. For example, for a package whose deployment endpoint looks up `Acme.DeploymentSync`:

```csharp
using TheBuilder.ApiOidc.Authentication;

builder.Services.AddAuthorization(options => options.AddPolicy(
    "Acme.DeploymentSync",
    policy => policy.RequireOidcToken("GitHubActions")));
```

The policy then checks the same as `[RequireOidcToken("GitHubActions")]`: only a token from that issuer gets in, and a cookie or another scheme does not count. Leave out the issuers to accept any trusted issuer. The other package does not need to know about this one.

## Endpoints that belong with the Delivery API

Umbraco only checks access on its own Delivery API endpoints. A custom endpoint you add beside them, such as a dictionary endpoint, is open to anyone, even under `/umbraco/delivery/api/`. To give it the same rules as Umbraco's content endpoints, add `[RequireDeliveryApiAccess]` instead:

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

For a minimal API, call `RequireDeliveryApiAccess()`. The endpoint then serves everyone when `Umbraco:CMS:DeliveryApi:PublicAccess` is `true`, and otherwise needs the `Api-Key` header or a valid token, the same as Umbraco's content endpoints. Requests without access get a `401`, and nothing gets in while the Delivery API is disabled. The policy is also available as `OidcAuthenticationDefaults.DeliveryApiAccessPolicy`.
