import { existsSync, readdirSync, readFileSync, statSync } from "node:fs";
import { dirname, join, normalize, relative } from "node:path";
import { fileURLToPath } from "node:url";

const srcRoot = join(dirname(fileURLToPath(import.meta.url)), "..");
const designLabRoot = join(srcRoot, "design-lab");

const importSpecifierPattern =
  /(?:import|export)\s+(?:type\s+)?(?:[\s\S]*?\sfrom\s+)?["']([^"']+)["']|import\s*\(\s*["']([^"']+)["']\s*\)|@import\s+(?:url\(\s*)?["']([^"']+)["']/g;
const labOwnedStylesheetPattern = /\/styles\/(?:design-lab\.css|components\/demo\.css|surfaces\/)/;

function extractImportSpecifiers(content: string): string[] {
  const specifiers: string[] = [];
  for (const match of content.matchAll(importSpecifierPattern)) {
    const specifier = match[1] || match[2] || match[3];
    if (specifier) {
      specifiers.push(specifier);
    }
  }
  return specifiers;
}

function resolvesToLabOwnedStylesheet(fromFile: string, specifier: string): boolean {
  if (!specifier.startsWith(".")) {
    return false;
  }
  const resolved = normalize(join(dirname(fromFile), specifier)).replaceAll("\\", "/");
  return labOwnedStylesheetPattern.test(resolved);
}

function isLabOwnedStylesheetPath(file: string): boolean {
  const rel = relative(srcRoot, file).replaceAll("\\", "/");
  return (
    rel === "styles/design-lab.css"
    || rel === "styles/components/demo.css"
    || rel.startsWith("styles/surfaces/")
  );
}

function walkCandidateSources(directory: string): string[] {
  return readdirSync(directory).flatMap((entry) => {
    const fullPath = join(directory, entry);
    if (statSync(fullPath).isDirectory()) {
      if (fullPath === designLabRoot) {
        return [];
      }
      return walkCandidateSources(fullPath);
    }
    if (/\.(ts|tsx|js|jsx|css)$/.test(entry) && !isLabOwnedStylesheetPath(fullPath)) {
      return [fullPath];
    }
    return [];
  });
}

describe("candidate style entry graph", () => {
  it("loads shared Shipboard sheets without lab-only demo or surface CSS", () => {
    const main = readFileSync(join(srcRoot, "main.tsx"), "utf8");
    expect(main).toContain('import "./styles/shared.css"');
    expect(main).not.toContain("styles/index.css");

    const shared = readFileSync(join(srcRoot, "styles/shared.css"), "utf8");
    expect(shared).toContain('@import "@fontsource/michroma"');
    expect(shared).toContain('@import "./tokens.css"');
    expect(shared).not.toContain("demo.css");
    expect(shared).not.toContain("./surfaces/");
  });

  it("keeps demo-plate selectors out of shared component family sheets", () => {
    const componentsDir = join(srcRoot, "styles/components");
    for (const name of readdirSync(componentsDir)) {
      if (name === "demo.css" || !name.endsWith(".css")) continue;
      const css = readFileSync(join(componentsDir, name), "utf8");
      expect(css, name).not.toContain(".demo-plate");
    }
  });

  it("pins management operate chrome and scrolls one work body", () => {
    const layouts = readFileSync(join(srcRoot, "styles/components/layouts.css"), "utf8");
    const appShell = readFileSync(join(srcRoot, "styles/app-shell.css"), "utf8");
    const managementMain = layouts.match(/\.layout-management__main \{[^}]+\}/)?.[0] ?? "";
    const ceremonyMain = layouts.match(
      /\.layout-management__main:has\(\.work-plane--ceremony\) \{[^}]+\}/,
    )?.[0] ?? "";
    const workspaceBay = appShell.match(/\.workspace-area \{[^}]+\}/)?.[0] ?? "";
    const operateScroll = appShell.match(
      /\.workspace-area > \.operate-scroll \{[^}]+\}/,
    )?.[0] ?? "";
    const recordPlaneBay = appShell.match(
      /\.workspace-area\.record-plane:not\(\.record-plane--setup\) \{[^}]+\}/,
    )?.[0] ?? "";

    expect(managementMain).toMatch(/overflow:\s*hidden/);
    expect(managementMain).not.toMatch(/overflow-y:\s*auto/);
    expect(ceremonyMain).toMatch(/overflow-y:\s*auto/);
    expect(workspaceBay).toMatch(/min-height:\s*0/);
    expect(operateScroll).toMatch(/overflow-y:\s*auto/);
    expect(recordPlaneBay).not.toMatch(/overflow-y:\s*auto/);
    expect(layouts).toMatch(/\.layout-guided__main\.well-frame \{[^}]*overflow:\s*hidden/);
    expect(appShell).toMatch(
      /\.workspace-area\.record-view > \.operate-scroll,\s*\.workspace-area\.record-plane--setup > \.operate-scroll,\s*\.operate-scroll:has\(\.datatable-scroll\),\s*\.workspace-area:has\(\.bays\) > \.operate-scroll \{[^}]*overflow:\s*hidden/,
    );
    expect(appShell).toMatch(
      /\.workspace-area\.registry-wall--hug > \.operate-scroll \{[^}]*overflow-y:\s*auto/,
    );
    expect(appShell).toMatch(
      /@media \(max-width: 1080px\)[^{]*\{[^}]*\.workspace-area:has\(\.bays\) > \.operate-scroll \{[^}]*overflow:\s*visible/,
    );
    expect(layouts).not.toMatch(
      /\.layout-management__main:has\(\.record-view\),\s*\.layout-management__main:has\(\.record-plane--setup\),\s*\.layout-management__main:has\(\.bays\)/,
    );
  });

  it("does not open a horizontal scrollport on etched plates", () => {
    const plates = readFileSync(join(srcRoot, "styles/components/plates.css"), "utf8");
    const appShell = readFileSync(join(srcRoot, "styles/app-shell.css"), "utf8");
    const frameCut = plates.match(/\.frame-cut \{[^}]+\}/)?.[0] ?? "";
    const frameNode = plates.match(/\.frame-in > \.frame-node \{[^}]+\}/)?.[0] ?? "";
    const workspaceScroll = appShell.match(
      /\.workspace-area > \.frame-cut > \.frame-in > \.frame-scroll,\s*\.workspace-area > \.operate-scroll > \.frame-cut > \.frame-in > \.frame-scroll \{[^}]+\}/,
    )?.[0] ?? "";
    const operateScroll = appShell.match(
      /\.workspace-area > \.operate-scroll \{[^}]+\}/,
    )?.[0] ?? "";
    const recordOperateScroll = appShell.match(
      /\.workspace-area\.record-view > \.operate-scroll,\s*\.workspace-area\.record-plane--setup > \.operate-scroll,\s*\.operate-scroll:has\(\.datatable-scroll\),\s*\.workspace-area:has\(\.bays\) > \.operate-scroll \{[^}]+\}/,
    )?.[0] ?? "";
    const ceremonyFrame = appShell.match(/\.ceremony-frame\.frame-cut \{[^}]+\}/)?.[0] ?? "";

    expect(frameCut).toMatch(/--frame-node-size:\s*8px/);
    expect(frameCut).toMatch(/--frame-node-hang:\s*-4px/);
    expect(frameNode).toMatch(/right:\s*var\(--frame-node-hang\)/);
    expect(frameNode).toMatch(/width:\s*var\(--frame-node-size\)/);
    expect(plates).toMatch(/\.operate-column--hug \.frame-in > \.frame-node \{[^}]*display:\s*none/);
    expect(workspaceScroll).toMatch(/overflow-x:\s*clip/);
    expect(workspaceScroll).toMatch(/overflow-y:\s*visible/);
    expect(workspaceScroll).not.toMatch(/overflow-y:\s*auto/);
    expect(workspaceScroll).not.toMatch(/overflow-x:\s*hidden/);
    expect(operateScroll).toMatch(/overflow-x:\s*(?:hidden|clip)/);
    expect(operateScroll).toMatch(/overflow-y:\s*auto/);
    expect(operateScroll).toMatch(/display:\s*flex/);
    expect(recordOperateScroll).toMatch(/overflow:\s*hidden/);
    const recordPlaneBay = appShell.match(
      /\.workspace-area\.record-plane:not\(\.record-plane--setup\) \{[^}]+\}/,
    )?.[0] ?? "";
    const recordPlaneSetupScroll = appShell.match(
      /\.workspace-area\.record-plane--setup \.frame-scroll \{[^}]+\}/,
    )?.[0] ?? "";
    const ceremonyInnerScroll = appShell.match(/\.create-ceremony__scroll \{[^}]+\}/)?.[0] ?? "";
    const setupCeremony = appShell.match(/\.setup-ceremony \{[^}]+\}/)?.[0] ?? "";
    expect(recordPlaneBay).toMatch(/overflow:\s*hidden/);
    expect(recordPlaneBay).not.toMatch(/overflow-y:\s*auto/);
    expect(appShell).not.toMatch(
      /\.workspace-area\.record-plane:not\(\.record-plane--setup\) > \.record-frame/,
    );
    expect(recordPlaneSetupScroll).toMatch(/overflow:\s*hidden/);
    expect(ceremonyInnerScroll).toMatch(/overflow-y:\s*auto/);
    expect(ceremonyInnerScroll).toMatch(/scrollbar-gutter:\s*stable/);
    expect(appShell).toMatch(
      /\.workspace-form\.setup-ceremony > \.setup-ceremony__foot,\s*\.setup-ceremony > \.setup-ceremony__foot \{[^}]*margin-block-start:\s*var\(--plate-foot-pad-block\)/,
    );
    expect(setupCeremony).toMatch(/display:\s*flex/);
    expect(setupCeremony).toMatch(/flex-direction:\s*column/);
    expect(appShell).toMatch(
      /\.workspace-area > \.operate-scroll > \.composition-grid\[data-flow-fit="fill"\],\s*\.workspace-area > \.operate-scroll > \.assignment-bays \{[^}]*flex:\s*0 0 auto/,
    );
    expect(appShell).toMatch(
      /\.workspace-area\.record-plane--setup > \.operate-scroll,\s*\.workspace-area\.record-plane--setup > \.workspace-alert \{[^}]*width:\s*min\(100%,\s*52rem\)/,
    );
    expect(appShell).not.toMatch(
      /\.workspace-area\.record-plane > \.operate-scroll,\s*\.workspace-area\.record-plane > \.workspace-alert \{[^}]*width:\s*min\(100%,\s*52rem\)/,
    );
    expect(appShell).toMatch(
      /\.frame-scroll > \.assignment-instruments,\s*\.setup-ceremony > \.assignment-instruments \{[^}]*margin-block-end:\s*var\(--form-group-gap\)/,
    );
    expect(ceremonyFrame).toMatch(/flex:\s*0 1 auto/);
    expect(plates).toMatch(/\.operate-column--hug\s*>\s*\.operate-head\s*\{[^}]*padding-inline:\s*var\(--cut\)/);
    expect(plates).toMatch(/\.operate-column--hug\s*>\s*\.operate-head\s*\{[^}]*contain:\s*inline-size/);
    expect(plates).toMatch(/\.operate-column--hug\s*\{[^}]*width:\s*var\(--operate-hug-w\)/);
    expect(plates).toMatch(/\.operate-column--hug\[data-hug-measure="auto"\]\s*\{[^}]*--operate-hug-w:\s*max-content/);
    expect(plates).toMatch(/\.operate-column--hug\[data-hug-measure="md"\]\s*\{[^}]*--operate-column-max:\s*520px/);
    expect(plates).toMatch(
      /\.operate-column--hug\[data-hug-measure="auto"\]\s+\.empty-plate--inset\s*\{[^}]*grid-template-columns:\s*7px minmax\(0,\s*48ch\)/,
    );
    expect(plates).toMatch(
      /\.operate-column--hug\[data-hug-measure="auto"\]\s+\.wait-plate--inset\s*\{[^}]*grid-template-columns:\s*auto minmax\(0,\s*1fr\)/,
    );
    expect(plates).toMatch(
      /@media \(min-width: 721px\)[^{]*\{[^}]*\.operate-column--hug\[data-hug-measure="auto"\]\s+\.wait-plate--inset\s+\.wait-plate-label\s*\{[^}]*white-space:\s*nowrap/,
    );
    expect(plates).toMatch(
      /\.operate-column--hug\[data-hug-measure="auto"\]:has\(\.wait-plate--inset\)\s*\{[^}]*--operate-hug-w:\s*min\(100%,\s*var\(--operate-column-max\)\)/,
    );
    expect(plates).not.toMatch(
      /\.operate-column--hug\[data-hug-measure="auto"\]:is\(:has\(\.wait-plate--inset\), :has\(\.empty-plate--inset\)\)/,
    );
    expect(plates).toMatch(
      /\.operate-column--hug\[data-hug-measure="auto"\]:has\(\.wait-plate--inset\)\s*>\s*\.frame-cut\s*\{[^}]*width:\s*100%/,
    );
    expect(plates).not.toMatch(
      /\.operate-column--hug\[data-hug-measure="auto"\]:has\(\.empty-plate--inset\) \.ceremony-empty\s*\{[^}]*width:\s*100%/,
    );
    expect(plates).toMatch(
      /\.workspace-area \.operate-column--hug\s*>\s*\.operate-head \.operate-head-copy \.page-desc\s*\{[^}]*white-space:\s*normal/,
    );
    expect(appShell).not.toMatch(
      /\.workspace-area > \.frame-cut > \.frame-in \{[^}]*overflow:\s*auto/,
    );
    expect(appShell).toMatch(
      /\.workspace-area > \.operate-scroll > \.bays \{[^}]*flex:\s*1 1 auto/,
    );
  });

  it("scrolls stacked wells on the operate pane, not each WorkWell body", () => {
    const plates = readFileSync(join(srcRoot, "styles/components/plates.css"), "utf8");
    const workWellBody = plates.match(/(?:^|\n)\.work-well__body \{[^}]+\}/)?.[0] ?? "";
    const guidedWorkWellBody = plates.match(
      /\.layout-guided__main \.work-well \.work-well__body \{[^}]+\}/,
    )?.[0] ?? "";

    expect(workWellBody).toMatch(/overflow:\s*visible/);
    expect(workWellBody).not.toMatch(/overflow-y:\s*auto/);
    expect(guidedWorkWellBody).toMatch(/overflow-y:\s*auto/);
  });

  it("clips filling table operate panes and restores hug lists to operate-scroll", () => {
    const appShell = readFileSync(join(srcRoot, "styles/app-shell.css"), "utf8");
    const home = readFileSync(join(srcRoot, "styles/surfaces/participant-home.css"), "utf8");
    const fillingTableScroll = appShell.match(
      /\.operate-scroll:has\(\.datatable-scroll\) \{[^}]+\}/,
    )?.[0] ?? "";
    expect(fillingTableScroll).toMatch(/display:\s*flex/);
    expect(fillingTableScroll).toMatch(/flex:\s*1 1 auto/);
    expect(fillingTableScroll).toMatch(/min-height:\s*0/);
    expect(appShell).toMatch(
      /\.workspace-area\.record-view > \.operate-scroll,\s*\.workspace-area\.record-plane--setup > \.operate-scroll,\s*\.operate-scroll:has\(\.datatable-scroll\),\s*\.workspace-area:has\(\.bays\) > \.operate-scroll \{[^}]*overflow:\s*hidden/,
    );
    expect(appShell).toMatch(
      /\.workspace-area\.registry-wall--hug > \.operate-scroll \{[^}]*overflow-y:\s*auto/,
    );
    expect(home).toMatch(
      /@media \(max-width: 1080px\)[^{]*\{[^}]*html\[data-surface="participant-home"\] \.bay-plates \{[^}]*overflow-y:\s*visible/,
    );
  });

  it("keeps OperateHead copy-cluster gap independent of the BackKey box", () => {
    const tokens = readFileSync(join(srcRoot, "styles/tokens.css"), "utf8");
    const appShell = readFileSync(join(srcRoot, "styles/app-shell.css"), "utf8");

    expect(tokens).not.toMatch(/--operate-head-title-rail-min/);
    expect(appShell).not.toMatch(/operate-head-title-rail-min/);
    expect(appShell).not.toMatch(/operate-head-title-row/);
    expect(appShell).toMatch(
      /\.operate-head-mast\s*>\s*\.operate-head-copy\s*\{[^}]*flex:\s*1 1 12rem/,
    );
    expect(appShell).toMatch(
      /@media \(max-width: 720px\)[^{]*\{[^}]*\.operate-head-mast\s*\{[^}]*flex-direction:\s*column/,
    );
    expect(appShell).toMatch(
      /\.operate-head-mast\s*>\s*:not\(\.operate-head-copy\)\s*\{[^}]*order:\s*-1/,
    );
    expect(appShell).toContain(
      ".workspace-area .operate-head-copy .page-desc {\n    white-space: normal;",
    );
  });

  it("keeps datatable horizontal overflow on the table scrollport, not the etched frame", () => {
    const datatable = readFileSync(join(srcRoot, "styles/components/datatable.css"), "utf8");
    const frame = datatable.match(/\.datatable-frame \{[^}]+\}/)?.[0] ?? "";
    const scroll = datatable.match(/\.datatable-scroll \{[^}]+\}/)?.[0] ?? "";

    expect(frame).toMatch(/overflow-x:\s*visible/);
    expect(frame).not.toMatch(/overflow-x:\s*auto/);
    expect(scroll).toMatch(/overflow-x:\s*auto/);
  });

  it("lets overlay dialog-body own vertical scroll when it hosts a table", () => {
    const overlays = readFileSync(join(srcRoot, "styles/components/overlays.css"), "utf8");
    const host = String.raw`:is\(\.dialog-body, \.ceremony-body\):has\(\.datatable-scroll\)`;
    const clip = overlays.match(new RegExp(`${host} \\{[^}]+\\}`))?.[0] ?? "";
    const fill = overlays.match(new RegExp(`${host} > \\.datatable \\{[^}]+\\}`))?.[0] ?? "";
    const rows = overlays.match(new RegExp(`${host} \\.datatable-scroll \\{[^}]+\\}`))?.[0] ?? "";
    const toolbar = overlays.match(new RegExp(`${host} \\.datatable-toolbar \\{[^}]+\\}`))?.[0] ?? "";

    expect(clip).toMatch(/overflow-y:\s*auto/);
    expect(clip).not.toMatch(/overflow-y:\s*clip/);
    expect(clip).not.toMatch(/overflow:\s*hidden/);
    expect(fill).toMatch(/flex:\s*0 0 auto/);
    expect(rows).toMatch(/overflow-y:\s*clip/);
    expect(rows).toMatch(/overflow-x:\s*auto/);
    expect(rows).toMatch(/overscroll-behavior-y:\s*auto/);
    expect(toolbar).toMatch(/position:\s*sticky/);
    expect(toolbar).toMatch(/top:\s*0/);
  });

  it("keeps datatable body text at the design-system compact floor (0.75rem)", () => {
    const datatable = readFileSync(join(srcRoot, "styles/components/datatable.css"), "utf8");
    const root = datatable.match(/\.datatable \{[^}]+\}/)?.[0] ?? "";
    const table = datatable.match(/\.datatable-table \{[^}]+\}/)?.[0] ?? "";
    const colKey = datatable.match(/\.col-key \{[^}]+\}/)?.[0] ?? "";
    const colHead = datatable.match(/\.col-head \{[^}]+\}/)?.[0] ?? "";

    expect(root).toMatch(/--datatable-body-font-size:\s*0\.75rem/);
    expect(table).toMatch(/font-size:\s*var\(--datatable-body-font-size\)/);
    expect(colKey).toMatch(/font-size:\s*var\(--datatable-body-font-size\)/);
    expect(colHead).toMatch(/font-size:\s*var\(--datatable-body-font-size\)/);
  });

  it("lets datatable columns hug then scroll instead of squeezing below content", () => {
    const datatable = readFileSync(join(srcRoot, "styles/components/datatable.css"), "utf8");
    const table = datatable.match(/\.datatable-table \{[^}]+\}/)?.[0] ?? "";
    const idCell = datatable.match(/\.datatable-table tbody td\.cell-id \{[^}]+\}/)?.[0] ?? "";
    const hugCells = datatable.match(
      /\.datatable-table tbody td\.cell-content,\s*\.datatable-table tbody td\.cell-state,\s*\.datatable-table tbody td\.col-action \{[^}]+\}/,
    )?.[0] ?? "";
    const idLink = datatable.match(/\.datatable-id \{[^}]+\}/)?.[0] ?? "";

    expect(table).toMatch(/width:\s*100%/);
    expect(table).not.toMatch(/min-width:\s*max-content/);
    expect(idCell).toMatch(/width:\s*100%/);
    expect(hugCells).toMatch(/width:\s*1%/);
    expect(idLink).not.toMatch(/min-width:\s*0/);
    expect(datatable).not.toMatch(/\.datatable-table \{ min-width: 680px; \}/);
    expect(datatable).not.toMatch(/datatable-table--fit/);
    expect(datatable).toMatch(/--datatable-col-min-instant:\s*13rem/);
    expect(datatable).toMatch(/\[data-col-min\]/);
    expect(datatable).toMatch(/\[data-col-min="instant"\]/);
  });

  it("keeps datatable state labels on the shared body type size", () => {
    const state = readFileSync(join(srcRoot, "styles/components/state.css"), "utf8");
    const label = state.match(/\.state-label \{[^}]+\}/)?.[0] ?? "";
    const reviewer = readFileSync(join(srcRoot, "styles/surfaces/reviewer-console.css"), "utf8");
    const reviewerLabel = reviewer.match(/\.state-label \{[^}]+\}/)?.[0] ?? "";

    expect(label).toMatch(/font-size:\s*inherit/);
    expect(reviewerLabel).toBe("");
  });

  it("routes readout microlabels through the shared typography floor token", () => {
    const tokens = readFileSync(join(srcRoot, "styles/tokens.css"), "utf8");
    expect(tokens).toMatch(/--microlabel-font-size:\s*0\.62rem/);

    const readoutDtSheets = [
      "styles/components/readouts.css",
      "styles/app-shell.css",
      "styles/components/datatable.css",
      "styles/surfaces/admin-console.css",
      "styles/surfaces/reviewer-console.css",
      "styles/surfaces/participant-journey.css",
      "styles/components/overlays.css",
    ];

    for (const relative of readoutDtSheets) {
      const css = readFileSync(join(srcRoot, relative), "utf8");
      const dtRules = [...css.matchAll(/[^{}]*\sdt\s*\{[^}]+\}/g)]
        .map((match) => match[0])
        .filter((rule) => /font-size:/.test(rule));
      expect(dtRules.length, relative).toBeGreaterThan(0);
      for (const rule of dtRules) {
        expect(rule, relative).toMatch(/font-size:\s*var\(--microlabel-font-size\)/);
        expect(rule, relative).not.toMatch(/font-size:\s*0\.5[0-9]rem/);
        expect(rule, relative).not.toMatch(/font-size:\s*0\.6rem/);
      }
    }

    const microlabelClassRules = [
      ["styles/components/fields.css", /\.field-label\s*\{[^}]+\}/g],
      ["styles/components/menus.css", /\.select-trigger--context \.seg-label[\s\S]*?\{[^}]+\}/g],
      ["styles/components/plates.css", /\.advisory-label\s*\{[^}]+\}/g],
      ["styles/components/overlays.css", /\.toast-label\s*\{[^}]+\}/g],
    ] as const;

    for (const [relative, pattern] of microlabelClassRules) {
      const css = readFileSync(join(srcRoot, relative), "utf8");
      const rules = [...css.matchAll(pattern)]
        .map((match) => match[0])
        .filter((rule) => /font-size:/.test(rule));
      expect(rules.length, relative).toBeGreaterThan(0);
      for (const rule of rules) {
        expect(rule, relative).toMatch(/font-size:\s*var\(--microlabel-font-size\)/);
      }
    }
  });

  it("binds stacked field labels to their control with the shared label-gap token", () => {
    const tokens = readFileSync(join(srcRoot, "styles/tokens.css"), "utf8");
    expect(tokens).toMatch(/--field-label-gap:\s*var\(--space-2-5\)/);
    expect(tokens).toMatch(/--form-group-gap:\s*var\(--space-4\)/);
    expect(tokens).toMatch(/--operate-bay-gap:\s*var\(--space-6\)/);

    const fields = readFileSync(join(srcRoot, "styles/components/fields.css"), "utf8");
    const stack = fields.match(/\.field-stack\s*\{[^}]+\}/)?.[0] ?? "";
    expect(stack).toMatch(/gap:\s*var\(--field-label-gap\)/);

    const frozen = fields.match(
      /\.field-input\.is-frozen,\s*\.field-textarea\.is-frozen(?:,\s*\.select-shell\.is-frozen [^{]+)*\{[^}]+\}/,
    )?.[0] ?? "";
    expect(frozen).toMatch(/padding:\s*0/);
    expect(frozen).not.toMatch(/padding-left:\s*0/);

    const formRow = fields.match(/^\.form-row\s*\{[^}]+\}/m)?.[0] ?? "";
    expect(formRow).toMatch(/padding:\s*var\(--field-label-gap\)\s+0/);
    expect(fields).toMatch(/\.form-row--pair \{ flex-direction: column; gap: var\(--form-group-gap\)/);

    const sectionBox = fields.match(/^\.form-section\s*\{[^}]+\}/m)?.[0] ?? "";
    expect(sectionBox).not.toMatch(/border-inline-start/);
    expect(sectionBox).not.toMatch(/padding-inline-start/);
    expect(sectionBox).toMatch(/border:\s*0/);

    const section = fields.match(/^\.form-section\s*>\s*legend\s*\{[^}]+\}/m)?.[0] ?? "";
    expect(section).toMatch(/margin:\s*0\s+0\s+var\(--form-group-gap\)/);
    expect(section).toMatch(/padding:\s*0\s+0\s+var\(--space-2\)/);
    expect(section).toMatch(/width:\s*max-content/);
    expect(section).not.toMatch(/^\s*width:\s*100%;/m);
    expect(section).toMatch(/border-block-end:\s*2px\s+solid\s+var\(--hairline\)/);
    expect(section).toMatch(/font-size:\s*0\.72rem/);
    expect(section).toMatch(/color:\s*var\(--text-bright\)/);
    expect(section).not.toMatch(/font-size:\s*0\.62rem/);
    expect(section).not.toMatch(/color:\s*var\(--label\)/);
    expect(fields).not.toMatch(/\.composition-stack\s*>\s*\.form-section\s*\+\s*\.form-section/);
    expect(fields).not.toMatch(
      /\.dialog-body > \.field-stack,\s*\n\.ceremony-body > \.field-stack,\s*\n\.bulkhead-body > \.field-stack\s*\{[^}]*margin-top:\s*2px/,
    );

    const appShell = readFileSync(join(srcRoot, "styles/app-shell.css"), "utf8");
    const operateHead = appShell.match(/\.operate-head\s*\{[^}]+\}/)?.[0] ?? "";
    expect(operateHead).toMatch(/gap:\s*var\(--field-label-gap\)/);
    expect(appShell).not.toMatch(/\.workspace-form \.key\s*\{[^}]*margin-top:\s*4px/);
    const workspaceSectionFollow = appShell.match(/\.workspace-section \+ \.workspace-section\s*\{[^}]+\}/)?.[0] ?? "";
    expect(workspaceSectionFollow).toMatch(/padding-top:\s*var\(--operate-bay-gap\)/);

    const assignmentHead = appShell.match(/\.assignment-head\s*\{[^}]+\}/)?.[0] ?? "";
    expect(assignmentHead).toMatch(/gap:\s*var\(--operate-bay-gap\)/);
    expect(assignmentHead).toMatch(/padding-bottom:\s*var\(--form-group-gap\)/);

    const journey = readFileSync(join(srcRoot, "styles/surfaces/participant-journey.css"), "utf8");
    const journeyHead = journey.match(/\.assignment-head\s*\{[^}]+\}/)?.[0] ?? "";
    expect(journeyHead).toMatch(/gap:\s*var\(--operate-bay-gap\)/);
    expect(journeyHead).toMatch(/padding-bottom:\s*var\(--form-group-gap\)/);

    const demo = readFileSync(join(srcRoot, "styles/components/demo.css"), "utf8");
    const demoHead = demo.match(/\.operate-head\s*\{[^}]+\}/)?.[0] ?? "";
    expect(demoHead).toMatch(/gap:\s*var\(--field-label-gap\)/);
    const plaque = demo.match(/\.operate-head--plaque\s*\{[^}]+\}/)?.[0] ?? "";
    expect(plaque).toMatch(/gap:\s*var\(--form-group-gap\)/);
  });

  it("sizes Home and My work plate bays with Grid fill slots, not auto-fit stretch", () => {
    const primitives = readFileSync(join(srcRoot, "styles/components/layout-primitives.css"), "utf8");
    const fill = primitives.match(
      /\.composition-grid\[data-flow-fit="fill"\]\s*\{[^}]+\}/,
    )?.[0] ?? "";

    expect(fill).toMatch(/repeat\(auto-fill,/);
    expect(fill).not.toMatch(/auto-fit/);
    expect(primitives).toMatch(/\.composition-grid\s*\{[^}]*auto-fit/);
  });

  it("does not keep a combined styles index the candidate can import", () => {
    expect(existsSync(join(srcRoot, "styles/index.css"))).toBe(false);
  });

  it("rejects direct lab-owned stylesheet imports anywhere in the candidate graph", () => {
    const violations = walkCandidateSources(srcRoot).flatMap((file) => {
      const content = readFileSync(file, "utf8");
      return extractImportSpecifiers(content)
        .filter((specifier) => resolvesToLabOwnedStylesheet(file, specifier))
        .map((specifier) => `${relative(srcRoot, file)} imports lab-owned stylesheet '${specifier}'`);
    });
    expect(violations).toEqual([]);
  });

  it("draws hot keys with a clipped hairline, not a sliced rectangular border", () => {
    const keys = readFileSync(join(srcRoot, "styles/components/keys.css"), "utf8");
    const hot = keys.match(
      /\.key--transmit,\s*\.key--begin,\s*\.key--open,\s*\.key--activate,\s*\.key--inspect,\s*\.key--release \{[^}]+\}/,
    )?.[0] ?? "";
    const face = keys.match(
      /\.key--transmit::before,\s*\.key--begin::before,\s*\.key--open::before,\s*\.key--activate::before,\s*\.key--inspect::before,\s*\.key--release::before \{[^}]+\}/,
    )?.[0] ?? "";
    expect(hot).toMatch(/clip-path:\s*polygon\(var\(--key-cut\) 0/);
    expect(hot).toMatch(/border:\s*0/);
    expect(hot).toMatch(/overflow:\s*visible/);
    expect(hot).toMatch(/background:\s*var\(--key-stroke\)/);
    expect(face).toMatch(/inset:\s*0/);
    expect(face).toMatch(/background:\s*var\(--key-face\)/);
    expect(face).toMatch(/calc\(var\(--key-cut\) \+ 1px\) 1px/);
  });
});
