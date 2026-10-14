---
title: Configuration
description: The settings that apply to every issuer, and why the token has its own header.
seo:
  image: /og/configuration.png
---

These settings sit directly under `ApiOidc`, beside `Issuers`, and apply to every issuer.

| Setting      | Default        | What it does |
| ------------ | -------------- | ------------ |
| `HeaderName` | `X-Oidc-Token` | The header that carries the token. A `Bearer ` prefix is accepted. |
| `ClockSkew`  | `00:01:00`     | How far token lifetimes may disagree with the server clock. |

The site refuses to start if an issuer is missing what it needs, so a typo cannot quietly let everyone in.

## Why not the `Authorization` header?

Many platforms' examples send the token as `Authorization: Bearer`. The Delivery API already uses that header for member authorization, so this package defaults to its own header. If your site does not use member authorization, you can set `HeaderName` to `Authorization`.
