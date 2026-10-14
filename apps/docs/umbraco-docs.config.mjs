import { defineOgConfig } from "@thebuilder/umbraco-docs/og";
import { fileURLToPath } from "node:url";

export default defineOgConfig({
  contentDir: fileURLToPath(new URL("./content", import.meta.url)),
  publicDir: fileURLToPath(new URL("./public", import.meta.url)),
  prefix: "/og",
  brand: "TheBuilder · API OIDC",
  accent: "#5b7cf5",
  logo: "/logo-mark.svg",
  root: {
    title: "API OIDC for Umbraco",
    description: "Call the Umbraco Delivery API with your platform's OIDC token instead of a shared API key.",
  },
});
