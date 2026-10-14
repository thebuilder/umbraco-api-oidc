import { defineUmbracoPackage } from "@thebuilder/umbraco-docs";

export const apiOidcPackage = defineUmbracoPackage({
  id: "thebuilder.apioidc",
  name: "API OIDC",
  summary:
    "Let frontends call the Umbraco Delivery API and your own API endpoints with the OIDC token their platform already issues, instead of a shared API key.",
  links: {
    docs: "https://api-oidc.thebuilder.dk",
    github: "https://github.com/thebuilder/umbraco-api-oidc",
  },
  logo: "/logo-mark.svg",
  compatibility: { umbraco: ">=17.1 <19", dotnet: ">=10" },
  status: "preview",
  categories: ["Headless", "Developer Tools"],
});
