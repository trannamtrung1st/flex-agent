import { createHash } from "node:crypto";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const stylesRoot = join(dirname(fileURLToPath(import.meta.url)), "../styles");

const copiedSheets: Record<string, string> = {
  "tokens.css": "52913d967a87f5e8a69c60acbd9d9974a044678444b75d9ed18483676ddae6a1",
  "base.css": "4ea448cc1756c745cfd2ef60f1fe64020edd63413f5e1893de059f10996930f8",
  "components/chrome.css": "da7bad7e6d7cb172315034106018e41d79e5ff43b7ecd2ea7d6ed7203146ffb8",
  "components/datatable.css": "b794e8f5f85f702f28cfc9a79cda57b2f18e28b60a678e460a61e7c1e80d452c",
  "components/demo.css": "37eaff20814ff857c4ffa557eb3cb2ef68ae8600516578562277e057e20a9d85",
  "components/fields.css": "dfd508f630c180ac8393f113697873cbaf5ea487fb5f153dc2bd39c86655f477",
  "components/keys.css": "8025616ecb0649445b3baf9f12419a2de3fd624fdf4a04515a2e20f1939680ab",
  "components/lists.css": "61e9684d612ee161964d055cd71b03c3f4c7f642c73c3b1f93ca75e45817d256",
  "components/menus.css": "7dd1e8687b32be6b870c99ad86db302e4623023ee72fde3c47357f908f2c61a2",
  "components/navigation.css": "32cc51cc2146b478155432ea6c127a87400932468dd6d284090ef41d3e1bdb49",
  "components/overlays.css": "5b5ef7a6b24016daaa1cb9a0a198056133201c5dffb2185aa37604ae7ab7542c",
  "components/plates.css": "51599d0d7c2b2f5f3b183580e00fbc7a168a59470f8b4ac23785826cf8437f37",
  "components/readouts.css": "157acd6290f131395702ab6630c9dc0d35bf124aa9d48d30c22d156170c84066",
  "components/searchable.css": "58ca543890210a6e1a8fdbd61906abd378ae67fef0e9a0406d6b33d30c235988",
  "components/state.css": "8247d1086466e1269f99b2137e1ee39978b740e299de873b6a06fc698d5923f9",
  "components/temporal.css": "47f5ccb09af671979a9cb4cd7175e8d14bde4e02216a4d78245a53f0fe2c4f07",
  "surfaces/admin-console.css": "03ba597d5d036dcf8f7531389927bc071ecf7794bc205cd962aa0b6808e914ac",
  "surfaces/gallery.css": "beedaa7318eabf5f7098cb03cd30020af043c6938d8b8e3015013416aa8c1a9b",
  "surfaces/not-found.css": "6644baad6e6b596ea01aef7e5e9f8078beb7c56df8e1ac5c14dea1fa5e76aabc",
  "surfaces/participant-home.css": "23324b249b67e7e28f35467e050c3279cecbf6685f099733b0b1b65987cb7a24",
  "surfaces/participant-journey.css": "76f098a56cf63e1efb1a078f6b5f3f41e3cc32bae1524729fae1dabefa2addf2",
  "surfaces/participant-session.css": "5ef657cc75719ff4c5cc24777ba6f62fd86f9fa80b7c6525db0e361b1ddc72ce",
  "surfaces/reviewer-console.css": "258f27ed9aa67a6bcd3ba855203c60d04c773115c6a8db40bd23a0da1c2e8c57",
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
    expect(sharedCss).toContain('./components/lists.css');
    expect(sharedCss).toContain('./components/file-field.css');
    expect(sharedCss.indexOf("./components/file-field.css")).toBeGreaterThan(
      sharedCss.indexOf("./components/fields.css"),
    );
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

  it("optically centers the current-item tick on uppercase cap height", () => {
    const navigationCss = readFileSync(join(stylesRoot, "components/navigation.css"), "utf8");
    expect(navigationCss).not.toContain("translateY(calc(0.5cap - 0.5em))");
    expect(navigationCss).toMatch(/\.gangway-link-text,\s*\.gangway-label,\s*\.gangway-abbr \{\s*line-height: 1;/);
  });
});
