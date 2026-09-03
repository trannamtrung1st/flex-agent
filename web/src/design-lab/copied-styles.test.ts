import { createHash } from "node:crypto";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const stylesRoot = join(dirname(fileURLToPath(import.meta.url)), "../styles");

const copiedSheets: Record<string, string> = {
  "tokens.css": "4267b819151b889d8afd4bd116f48802cff148b9127420a5de380aa490076551",
  "base.css": "4ea448cc1756c745cfd2ef60f1fe64020edd63413f5e1893de059f10996930f8",
  "components/chrome.css": "c578da626664f396705c57b7bfd823cf29b2e27507538529a1cda626d2ae275b",
  "components/datatable.css": "a4cc8e8008a79e9ae2079e996e640173be4fd3e90ae3ac77b2d37112336c17f1",
  "components/demo.css": "e07cfc90cd8663a593ee085ceac7d61dc121b37e0ab6341d879ecbefe2f752dd",
  "components/fields.css": "57b5b727c3a343e97e85e6b2b5bbb2612395d1d367c6d5263a456eb1251a1835",
  "components/keys.css": "935a1796b184e537fc88ec240f13a8dc392579dd06edeadf04c28529f2f35a18",
  "components/lists.css": "61e9684d612ee161964d055cd71b03c3f4c7f642c73c3b1f93ca75e45817d256",
  "components/menus.css": "d902e9462bd8b137b29b96022e92ce10fbf53bedf8c7c948e960576b8d4d7543",
  "components/navigation.css": "c7235cbf460c2a09694660299c4eba45d6f0e68a79c1907c8596df0a4d43d20d",
  "components/overlays.css": "8ab8697ed4192d3d3cc3165395f003ad24aabf5451d947ffebbfc2cb733b70cc",
  "components/plates.css": "feec4bb29ae49a5a70e1c87c185c74801636811e8da595ba9c0486547abd4b0f",
  "components/readouts.css": "c655ac058c9d92ab52df979aba4731869b080d246ded7af15a5024b442247298",
  "components/searchable.css": "4d8511174ace344a89c2b5f1d6c97fc906f495fb243f4c82589596ebc2a45c8f",
  "components/state.css": "8247d1086466e1269f99b2137e1ee39978b740e299de873b6a06fc698d5923f9",
  "components/temporal.css": "19004b626cd92cd77867bc5850bacd5e436b7f01c27d50f137bf488ee947baa4",
  "surfaces/admin-console.css": "576ae551d1dc65f9d09494bc4ff7d898925bd9a83fa202e720fb9ff6684b4654",
  "surfaces/gallery.css": "10c9b7ccfc43feafbb438ed166b345944c600394715021d0cd1cb021e241edf8",
  "surfaces/not-found.css": "c5d5e16b52e6e9511d6030af209b88407024c66e9fb8e73ecdd25360c0b79a0b",
  "surfaces/participant-home.css": "51625ce1f75f9be4a58f448328a212665d8f0323a8b279df1836ebb5328ad03a",
  "surfaces/participant-journey.css": "ef7228efee72784b591f4ec840d67537991eee85140f734bd1ceb3ada8815155",
  "surfaces/participant-session.css": "a52166e5204c64128f46fa7d5174e4f906e5add84c3fd3c4378b707efe1e09ff",
  "surfaces/reviewer-console.css": "19f5d6de01c1f063512d8514431dc225c1f046d5ccb6b5fefe95dae23b365b1d",
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
    expect(sharedCss).toContain('./components/work-plates.css');
    expect(sharedCss).toContain('./components/lists.css');
    expect(sharedCss).toContain('./components/file-field.css');
    expect(sharedCss.indexOf("./components/file-field.css")).toBeGreaterThan(
      sharedCss.indexOf("./components/fields.css"),
    );
    expect(sharedCss.indexOf("./components/layout-primitives.css")).toBeGreaterThan(
      sharedCss.indexOf("./components/layouts.css"),
    );
    expect(sharedCss).toContain('./components/live-session.css');
    expect(sharedCss.indexOf("./components/live-session.css")).toBeGreaterThan(
      sharedCss.indexOf("./components/layouts.css"),
    );
  });

  it("keeps production live-session chrome aligned with the lab participant-session donor", () => {
    const stripLeadingComment = (source: string) => source.replace(/^\s*\/\*[\s\S]*?\*\/\s*/, "");
    const normalize = (source: string) =>
      stripLeadingComment(source)
        .replace(/html\[data-surface="participant-session"\]/g, 'html:has([data-layout="live-session"])')
        .replace(/\s+\}\s+\n\}/, "\n}\n}")
        .trim();

    const lab = readFileSync(join(stylesRoot, "surfaces/participant-session.css"), "utf8");
    const production = readFileSync(join(stylesRoot, "components/live-session.css"), "utf8");
    expect(normalize(production)).toBe(normalize(lab));
  });

  it("does not open a horizontal scrollport on gallery layout-spec plates", () => {
    const galleryCss = readFileSync(join(stylesRoot, "surfaces/gallery.css"), "utf8");
    const layoutWorkspace = galleryCss.match(
      /\.layout-spec \.workspace-area \{[^}]+\}/,
    )?.[0] ?? "";

    expect(layoutWorkspace).toMatch(/overflow-x:\s*(?:hidden|clip)/);
    expect(layoutWorkspace).toMatch(/overflow-y:\s*clip/);
    expect(layoutWorkspace).not.toMatch(/overflow:\s*auto/);
    expect(layoutWorkspace).not.toMatch(/overflow-y:\s*auto/);
    expect(layoutWorkspace).not.toMatch(/overflow-y:\s*visible/);
  });

  it("makes Component Deck layout specimens hug so the catalog page owns the wheel", () => {
    const galleryCss = readFileSync(join(stylesRoot, "surfaces/gallery.css"), "utf8");
    const layoutSpec = galleryCss.match(/\.layout-spec \{\s*display: flex;[^}]+\}/)?.[0]
      ?? galleryCss.match(/\.layout-spec \{[^}]+height:[^}]+\}/)?.[0]
      ?? "";
    const nestedLayout = galleryCss.match(/\.layout-spec \[data-layout\] \{[^}]+\}/)?.[0] ?? "";
    const nestedHull = galleryCss.match(
      /\.layout-spec \.layout-management,\s*\.layout-spec \.layout-guided,\s*\.layout-spec \.layout-session,\s*\.layout-spec \.layout-reference \{[^}]+\}/,
    )?.[0] ?? "";
    const datatableScroll = galleryCss.match(/\.datatable-demo \.datatable-scroll \{[^}]+\}/)?.[0] ?? "";

    expect(galleryCss).not.toMatch(/\.layout-spec \{[^}]*72dvh/);
    expect(galleryCss).not.toMatch(/height:\s*min\(38rem,\s*72dvh\)/);
    expect(galleryCss).not.toMatch(/\.spec-row--layout-contain \.layout-spec \{[^}]*64dvh/);
    expect(galleryCss).not.toMatch(/--datatable-max-height/);
    expect(nestedLayout).toMatch(/height:\s*auto/);
    expect(nestedLayout).toMatch(/max-height:\s*none/);
    expect(nestedHull).toMatch(/height:\s*auto/);
    expect(nestedHull).toMatch(/min-height:\s*28rem/);
    expect(datatableScroll).toMatch(/overflow-x:\s*auto/);
    expect(datatableScroll).toMatch(/overflow-y:\s*clip/);
    expect(datatableScroll).not.toMatch(/overflow-y:\s*visible/);
    expect(layoutSpec).toMatch(/height:\s*auto/);
  });

  it("makes seated catalog dialog plates hug so the catalog page owns the wheel", () => {
    const galleryCss = readFileSync(join(stylesRoot, "surfaces/gallery.css"), "utf8");
    const dialogPlate = galleryCss.match(/#form-recipes \.form-recipe-dialog \.dialog-plate \{[^}]+\}/)?.[0] ?? "";
    const dialogBody = galleryCss.match(
      /#form-recipes \.form-recipe-dialog \.dialog-body,\s*#form-recipes \.form-recipe-dialog \.ceremony-body \{[^}]+\}/,
    )?.[0] ?? "";

    expect(dialogPlate).toMatch(/max-height:\s*none/);
    expect(dialogBody).toMatch(/overflow-x:\s*clip/);
    expect(dialogBody).toMatch(/overflow-y:\s*clip/);
    expect(dialogBody).toMatch(/overscroll-behavior:\s*auto/);
    expect(dialogBody).toMatch(/scrollbar-gutter:\s*auto/);
    expect(dialogBody).toMatch(/flex:\s*none/);
    expect(dialogBody).not.toMatch(/overflow-y:\s*auto/);

    const operateScroll = galleryCss.match(/#form-recipes \.form-recipe > \.operate-scroll \{[^}]+\}/)?.[0] ?? "";
    expect(operateScroll).toMatch(/overflow-x:\s*clip/);
    expect(operateScroll).toMatch(/overflow-y:\s*clip/);
    expect(operateScroll).toMatch(/overscroll-behavior:\s*auto/);
    expect(operateScroll).not.toMatch(/overflow:\s*visible/);
  });

  it("does not rewrite shared hull overflow for production or lab Operate routes", () => {
    const layouts = readFileSync(join(stylesRoot, "components/layouts.css"), "utf8");
    const appShell = readFileSync(join(stylesRoot, "app-shell.css"), "utf8");
    const operateScroll = appShell.match(/\.workspace-area > \.operate-scroll \{[^}]+\}/)?.[0] ?? "";

    expect(layouts).toMatch(/\.layout-management \{[^}]*height:\s*100dvh/);
    expect(layouts).toMatch(/\.layout-guided \{[^}]*height:\s*100dvh/);
    expect(layouts).toMatch(/\.layout-session \{[^}]*height:\s*100dvh/);
    expect(layouts).toMatch(/\.layout-guided \.phase-rail-scroll \{[^}]*overflow-y:\s*auto/);
    expect(operateScroll).toMatch(/overflow-y:\s*auto/);
  });

  it("does not open a horizontal scrollport on the gallery datatable frame", () => {
    const galleryCss = readFileSync(join(stylesRoot, "surfaces/gallery.css"), "utf8");
    const demo = galleryCss.match(/\.datatable-demo \{[^}]+\}/)?.[0] ?? "";

    expect(demo).not.toMatch(/overflow-x:\s*auto/);
    expect(demo).not.toMatch(/overflow:\s*auto/);
  });

  it("keeps the Component Deck header above portaled overlays", () => {
    const galleryCss = readFileSync(join(stylesRoot, "surfaces/gallery.css"), "utf8");
    const header = galleryCss.match(/header\.page-strip \{[^}]+\}/)?.[0] ?? "";
    expect(header).toMatch(/z-index:\s*70/);
  });

  it("does not let catalog command-strip specimens share hull stacking", () => {
    const galleryCss = readFileSync(join(stylesRoot, "surfaces/gallery.css"), "utf8");
    const specimen = galleryCss.match(/\.deck \.command-strip\s*\{[^}]+\}/)?.[0] ?? "";
    expect(specimen).toMatch(/z-index:\s*auto/);
  });

  it("optically centers the current-item tick on uppercase cap height", () => {
    const navigationCss = readFileSync(join(stylesRoot, "components/navigation.css"), "utf8");
    expect(navigationCss).not.toContain("translateY(calc(0.5cap - 0.5em))");
    expect(navigationCss).toMatch(/\.gangway-link-text,\s*\.gangway-label,\s*\.gangway-abbr \{\s*line-height: 1;/);
  });
});
