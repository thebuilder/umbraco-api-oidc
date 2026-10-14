import { readFile } from "node:fs/promises";
import { fileURLToPath } from "node:url";
import sharp from "sharp";

// The favicon and the package icon are drawn from the logo, so they cannot drift apart.
const docsRoot = fileURLToPath(new URL("../", import.meta.url));
const repoRoot = fileURLToPath(new URL("../../../", import.meta.url));
const logo = await readFile(`${docsRoot}public/logo-mark.svg`, "utf8");

await sharp(Buffer.from(logo)).resize(256, 256).png().toFile(`${docsRoot}favicon.png`);

// NuGet and the Umbraco Marketplace show the icon on light and dark pages, so it sits on its own dark tile.
const mark = logo.replace(/<svg[^>]*>/, '<svg x="66" y="66" width="380" height="380" viewBox="0 0 256 256">');
const icon = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512">
  <defs>
    <linearGradient id="tile" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0" stop-color="#232a5c"/>
      <stop offset="1" stop-color="#0f1226"/>
    </linearGradient>
  </defs>
  <rect width="512" height="512" rx="112" fill="url(#tile)"/>
  ${mark}
</svg>`;
await sharp(Buffer.from(icon)).png().toFile(`${repoRoot}icon.png`);

console.log("Generated favicon.png and the package icon.png from public/logo-mark.svg.");
