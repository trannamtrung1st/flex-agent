import assert from "node:assert/strict";
import { describe, it } from "node:test";
import {
  candidateHtmlEntryViolations,
  designLabHtmlEntryViolations,
  designLabImportViolations,
  designLabOutboundImportViolations,
  extractHtmlEntryReferences,
  extractImportSpecifiers,
  labOwnedStylesheetImportViolations,
  outerChromeImportViolations,
  operateHeadRouteViolations,
  designLabRouteLayoutComponentViolations,
  productionPageLayoutImportViolations,
  productionReferenceLayoutViolations,
  reservedLayoutCssViolations,
  layoutRootAttributeViolations,
  routeLayoutMappingViolations,
  specifierResolvesToAllowedDesignLabOutbound,
  specifierResolvesToDesignLab,
  specifierResolvesToLabOwnedStylesheet,
} from "./frontend-isolation-lib.mjs";

const repoRoot = "/repo";

describe("design-lab import specifier detection", () => {
  const fromDesignSystem = "/repo/web/src/design-system/components/chrome/Brand.tsx";

  it("treats relative paths that resolve inside design-lab as forbidden", () => {
    assert.equal(specifierResolvesToDesignLab(fromDesignSystem, "../../design-lab/data/fixtures"), true);
    assert.equal(specifierResolvesToDesignLab(fromDesignSystem, "../../../design-lab"), true);
    assert.equal(specifierResolvesToDesignLab(fromDesignSystem, "../keys/Key"), false);
  });

  it("treats aliases and src-rooted specifiers as forbidden", () => {
    assert.equal(specifierResolvesToDesignLab(fromDesignSystem, "src/design-lab/data/fixtures"), true);
    assert.equal(specifierResolvesToDesignLab(fromDesignSystem, "web/src/design-lab/app/router"), true);
    assert.equal(specifierResolvesToDesignLab(fromDesignSystem, "@alias/design-lab/fixtures"), true);
  });

  it("extracts a relative import that substring src/design-lab would miss", () => {
    const source = 'import { HOME_ENROLLMENTS } from "../../design-lab/data/fixtures";\n';
    assert.deepEqual(extractImportSpecifiers(source), ["../../design-lab/data/fixtures"]);
    const violations = designLabImportViolations(fromDesignSystem, source);
    assert.equal(violations.length, 1);
    assert.match(violations[0], /design-lab\/data\/fixtures/);
  });
});

describe("lab-owned stylesheet import detection", () => {
  const fromCandidate = "/repo/web/src/App.tsx";

  it("flags direct imports of demo and surface stylesheets", () => {
    assert.equal(specifierResolvesToLabOwnedStylesheet(fromCandidate, "./styles/components/demo.css", repoRoot), true);
    assert.equal(specifierResolvesToLabOwnedStylesheet(fromCandidate, "./styles/surfaces/participant-home.css", repoRoot), true);
    assert.equal(specifierResolvesToLabOwnedStylesheet(fromCandidate, "../styles/design-lab.css", repoRoot), true);
    assert.equal(specifierResolvesToLabOwnedStylesheet(fromCandidate, "./styles/shared.css", repoRoot), false);
    assert.equal(specifierResolvesToLabOwnedStylesheet(fromCandidate, "./styles/tokens.css", repoRoot), false);
  });

  it("reports violations for direct surface imports from candidate modules", () => {
    const source = 'import "./styles/surfaces/participant-home.css";\n';
    const violations = labOwnedStylesheetImportViolations(fromCandidate, source);
    assert.equal(violations.length, 1);
    assert.match(violations[0], /participant-home\.css/);
  });
});

describe("design-lab outbound import allowlist", () => {
  const fromLabFeature = "/repo/web/src/design-lab/features/admin/SampleArea.tsx";

  it("allows design-lab, design-system, lib, shared style, and production-safe domain composition imports", () => {
    assert.equal(specifierResolvesToAllowedDesignLabOutbound(fromLabFeature, "../../components", repoRoot), true);
    assert.equal(specifierResolvesToAllowedDesignLabOutbound(fromLabFeature, "../../../design-system/components/keys/Key", repoRoot), true);
    assert.equal(specifierResolvesToAllowedDesignLabOutbound(fromLabFeature, "../../../lib/cx", repoRoot), true);
    assert.equal(specifierResolvesToAllowedDesignLabOutbound(fromLabFeature, "../../../components/work/AssignmentPlate", repoRoot), true);
    assert.equal(specifierResolvesToAllowedDesignLabOutbound(fromLabFeature, "../../../content/fieldCopy", repoRoot), true);
    assert.equal(specifierResolvesToAllowedDesignLabOutbound(fromLabFeature, "../../../features/assessment/SetupTrackReadout", repoRoot), true);
    assert.equal(specifierResolvesToAllowedDesignLabOutbound(fromLabFeature, "../../../main.tsx", repoRoot), false);
    assert.equal(specifierResolvesToAllowedDesignLabOutbound(fromLabFeature, "../../../App", repoRoot), false);
    assert.equal(specifierResolvesToAllowedDesignLabOutbound(fromLabFeature, "../../../components/ErrorBoundary", repoRoot), false);
    assert.equal(specifierResolvesToAllowedDesignLabOutbound(fromLabFeature, "../../../api/client", repoRoot), false);
    assert.equal(specifierResolvesToAllowedDesignLabOutbound(fromLabFeature, "../../../features/auth/hooks", repoRoot), false);
    assert.equal(specifierResolvesToAllowedDesignLabOutbound(fromLabFeature, "../../../features/assessment/setupStation", repoRoot), false);
    assert.equal(specifierResolvesToAllowedDesignLabOutbound(fromLabFeature, "../../../../../contracts/something", repoRoot), false);
    assert.equal(specifierResolvesToAllowedDesignLabOutbound(fromLabFeature, "../../../../../build/scripts/foo", repoRoot), false);
  });

  it("reports outbound violations for future production modules", () => {
    const source = 'import { client } from "../../../api/client";\n';
    const violations = designLabOutboundImportViolations(fromLabFeature, source, repoRoot);
    assert.equal(violations.length, 1);
    assert.match(violations[0], /api\/client/);
  });
});

describe("HTML entry reference parsing", () => {
  it("extracts module script and stylesheet href references", () => {
    const html = [
      '<script type="module" src="/src/main.tsx"></script>',
      '<link rel="stylesheet" href="/src/styles/shared.css" />',
    ].join("\n");
    assert.deepEqual(extractHtmlEntryReferences(html), ["/src/main.tsx", "/src/styles/shared.css"]);
    assert.deepEqual(extractImportSpecifiers(html), ["/src/main.tsx", "/src/styles/shared.css"]);
  });

  it("flags candidate HTML that references lab-owned stylesheets or design-lab modules", () => {
    const htmlFile = "/repo/web/index.html";
    const html = [
      '<script type="module" src="/src/design-lab/main.tsx"></script>',
      '<link rel="stylesheet" href="/src/styles/surfaces/participant-home.css" />',
    ].join("\n");
    const violations = candidateHtmlEntryViolations(htmlFile, html, repoRoot);
    assert.ok(violations.some((violation) => violation.includes("module entry")));
    assert.ok(violations.some((violation) => violation.includes("lab-owned stylesheet")));
    assert.ok(violations.some((violation) => violation.includes("design-lab")));
  });
});

describe("shared layout governance", () => {
  it("flags a production page importing CommandStrip", () => {
    const relative = "web/src/pages/ProductionHomePage.tsx";
    const source = 'import { CommandStrip } from "../design-system";\n<CommandStrip homeTo="/" />\n';
    const violations = outerChromeImportViolations(relative, source);
    assert.equal(violations.length, 1);
    assert.match(violations[0], /CommandStrip/);
  });

  it("flags a production page importing a layout family", () => {
    const violations = productionPageLayoutImportViolations(
      "web/src/pages/ProductionHomePage.tsx",
      'import { ManagementLayout } from "../design-system";\n<ManagementLayout />\n',
    );
    assert.ok(violations.some((item) => item.includes("ManagementLayout")));
  });

  it("flags a lab route hand-building Gangway", () => {
    const relative = "web/src/design-lab/routes/HomePage.tsx";
    const source = 'import { Gangway } from "../components";\n<main /><Gangway title="Admin" groups={[]} />\n';
    assert.ok(outerChromeImportViolations(relative, source).some((item) => item.includes("Gangway")));
  });

  it("flags a lab route assembling OperateHead instead of OperateArea", () => {
    const relative = "web/src/design-lab/routes/ReviewerPage.tsx";
    const source = 'import { OperateHead } from "../components";\n<OperateHead title="Review queue" />\n';
    const violations = operateHeadRouteViolations(relative, source);
    assert.equal(violations.length, 1);
    assert.match(violations[0], /OperateHead/);
  });

  it("flags a lab route that does not render its assigned layout family", () => {
    const violations = designLabRouteLayoutComponentViolations(
      "web/src/design-lab/routes/HomePage.tsx",
      "export function HomePage() { return <div>roster</div>; }\n",
    );
    assert.ok(violations.some((item) => item.includes("ManagementLayout")));
  });

  it("flags feature stylesheets that declare reserved layout selectors", () => {
    const violations = reservedLayoutCssViolations(
      "web/src/styles/surfaces/participant-session.css",
      ".layout-session { height: 100dvh; }\n",
    );
    assert.equal(violations.length, 1);
  });

  it("does not treat inner composition selectors as reserved shells", () => {
    assert.deepEqual(
      reservedLayoutCssViolations(
        "web/src/styles/components/layout-primitives.css",
        ".composition-stack, .composition-inline { display: flex; }\n",
      ),
      [],
    );
  });

  it("flags production modules that select the reference layout", () => {
    const violations = productionReferenceLayoutViolations(
      "web/src/router/production-route-layouts.ts",
      'export const x = "reference";\nconst Layout = ReferenceLayout;\n',
    );
    assert.ok(violations.some((item) => item.includes("ReferenceLayout")));
  });

  it("does not flag the lab-only ReferenceLayout entry", () => {
    assert.deepEqual(
      productionReferenceLayoutViolations("web/src/design-system/lab.ts", "export { ReferenceLayout } from './patterns/layouts/ReferenceLayout';\n"),
      [],
    );
  });

  it("flags layout root attributes outside the layout library", () => {
    const violations = layoutRootAttributeViolations(
      "web/src/pages/ProductionHomePage.tsx",
      '<div data-layout="management" />\n',
    );
    assert.equal(violations.length, 1);
  });

  it("allows the lab live-session layout family to own its data-layout root", () => {
    assert.deepEqual(
      layoutRootAttributeViolations(
        "web/src/design-lab/components/layouts/LiveSessionLayout.tsx",
        '<div data-layout="live-session" />\n',
      ),
      [],
    );
  });

  it("reports a newly added route omitted from the manifest", () => {
    const violations = routeLayoutMappingViolations(["/"], [
      { path: "/", redirect: false },
      { path: "/new-leaf", redirect: false },
    ]);
    assert.ok(violations.some((item) => item.includes("/new-leaf")));
  });
});
