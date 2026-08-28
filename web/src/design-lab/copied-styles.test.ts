import { createHash } from "node:crypto";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const stylesRoot = join(dirname(fileURLToPath(import.meta.url)), "../styles");

const copiedSheets: Record<string, string> = {
  "tokens.css": "f034b2184fa3e93c96555eb3c26e70b39f53e2cfae098df2bd747bc1bdf8b0d0",
  "base.css": "cead6c4a9b3e48201ea5c9239f778331df7d3002c904ef8eca695bd61acc0b88",
  "components/chrome.css": "99a8782089e6adbb4cb6226a0fb92d40c2c8e1ac12cb4db074907c57dc7edc20",
  "components/datatable.css": "04d65258fc08dd754fec848609bb7ee4ddad2b1d752ef0d64309794c20d7cbaa",
  "components/demo.css": "7ed517875331cf2bffb299a9e43e27508e013f66655bbcb5795b383eb7460ab7",
  "components/fields.css": "b29c1463c19f1a4924b04daa629b1c35d1f0aafcf13d253de4b9b01af7d4ae88",
  "components/keys.css": "6ff3cb97550a635ed665e259a525af9424bc1a423c53b29632e8ff5094adb9e4",
  "components/menus.css": "f6a97728b378cb34e5a6079e3f79bbdd2024f93bafb0219f6eb9139c342e6be4",
  "components/navigation.css": "af938d42dcb4e1cc62d14eba810bd73e5c0f220c95a9aa00b921e0f773c542e2",
  "components/overlays.css": "7f38a1a9952e4b2159016caa721c21449f929ef1b98386dd5b6b32b0b727a830",
  "components/plates.css": "f7f42c01e0530a7a7e9e502e4f3164faccb7fc2074ffb434521251b30667ad58",
  "components/readouts.css": "b6ff725bed82510db9316f011067893893f31dd41194781a3f80e3ec8e77a152",
  "components/searchable.css": "0af11cea385957f12e877a56a926f3f178b825a8ad303292af8c7103626795ff",
  "components/state.css": "e3291ab085cfb10ac18be55f8a21a2d64f4b69073d0717cf5330441c82e4017e",
  "components/temporal.css": "11b66f5c3efdcfd2602bf8eaf2427028326e144d7ab9c5b1f6d9193cc75fe1d5",
  "surfaces/admin-console.css": "306554397cd9b692558e86598e67de7d43603fbc2595d90b1b2ce7bcb7702863",
  "surfaces/gallery.css": "b49637642a93714f73f3b412aad9c1b7286d06af53926d28e822da5710f8b65f",
  "surfaces/not-found.css": "359a389fb1f0f60143fcd528dab71faf42b5426e1906d32a0295873b600518b4",
  "surfaces/participant-home.css": "b4c62c6ded9f874483663515b4a86a3e278bdad24f4bb45ee782fef8a492de71",
  "surfaces/participant-journey.css": "fb25fcdcca441e2b09f41db8e6a067cd9e97d22d6711669ab90078956801aff1",
  "surfaces/participant-session.css": "ecd1358871d2cd07115eaa0f057c4ed1bcf7ca7312fa85dcca82ed7c56c00936",
  "surfaces/reviewer-console.css": "4b505c00a9dec45b264913ad1af1de7ba9676ed9b593478542139f25517b6e76",
  "surfaces/surfaces-index.css": "88940c3016133d384eb2862005bbc4326cf900f405a148934d192fe0dac5af7a",
};

describe("shared Shipboard stylesheets", () => {
  it("keeps adopted shared sheets byte-identical except isolation-adjusted demo/temporal sheets", () => {
    for (const [relative, digest] of Object.entries(copiedSheets)) {
      const bytes = readFileSync(join(stylesRoot, relative));
      expect(createHash("sha256").update(bytes).digest("hex"), relative).toBe(digest);
    }
  });

  it("loads copied sheets after semantic aliases and forced-colors adaptations", () => {
    const sharedCss = readFileSync(join(stylesRoot, "shared.css"), "utf8");
    expect(sharedCss).toContain('@import "@fontsource/michroma"');
    expect(sharedCss).toContain('@import "@fontsource/sometype-mono"');
    expect(sharedCss.indexOf('./semantic-aliases.css')).toBeGreaterThan(sharedCss.indexOf("./tokens.css"));
    expect(sharedCss.indexOf("./adaptations.css")).toBeGreaterThan(sharedCss.indexOf("./semantic-aliases.css"));
    expect(sharedCss.indexOf("./base.css")).toBeGreaterThan(sharedCss.indexOf("./adaptations.css"));
    expect(sharedCss).not.toContain("demo.css");
    expect(sharedCss).not.toContain("./surfaces/");
    expect(sharedCss).toContain('./components/layout-primitives.css');
    expect(sharedCss.indexOf("./components/layout-primitives.css")).toBeGreaterThan(
      sharedCss.indexOf("./components/layouts.css"),
    );
  });

  it("does not open a horizontal scrollport on gallery layout-spec plates", () => {
    const galleryCss = readFileSync(join(stylesRoot, "surfaces/gallery.css"), "utf8");
    const layoutWorkspace = galleryCss.match(
      /\.layout-spec \.workspace-area \{[^}]+\}/,
    )?.[0] ?? "";

    expect(layoutWorkspace).toMatch(/overflow-x:\s*(?:hidden|clip)/);
    expect(layoutWorkspace).toMatch(/overflow-y:\s*auto/);
    expect(layoutWorkspace).not.toMatch(/overflow:\s*auto/);
  });

  it("does not open a horizontal scrollport on the gallery datatable frame", () => {
    const galleryCss = readFileSync(join(stylesRoot, "surfaces/gallery.css"), "utf8");
    const demo = galleryCss.match(/\.datatable-demo \{[^}]+\}/)?.[0] ?? "";

    expect(demo).not.toMatch(/overflow-x:\s*auto/);
    expect(demo).not.toMatch(/overflow:\s*auto/);
  });

  it("optically centers the current-item tick on uppercase cap height", () => {
    const navigationCss = readFileSync(join(stylesRoot, "components/navigation.css"), "utf8");
    expect(navigationCss).not.toContain("translateY(calc(0.5cap - 0.5em))");
    expect(navigationCss).toMatch(/\.gangway-link-text,\s*\.gangway-label,\s*\.gangway-abbr \{\s*line-height: 1;/);
  });
});
