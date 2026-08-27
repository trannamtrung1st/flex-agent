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

  it("allows design-lab, design-system, lib, and shared style imports", () => {
    assert.equal(specifierResolvesToAllowedDesignLabOutbound(fromLabFeature, "../../components", repoRoot), true);
    assert.equal(specifierResolvesToAllowedDesignLabOutbound(fromLabFeature, "../../../design-system/components/keys/Key", repoRoot), true);
    assert.equal(specifierResolvesToAllowedDesignLabOutbound(fromLabFeature, "../../../lib/cx", repoRoot), true);
    assert.equal(specifierResolvesToAllowedDesignLabOutbound(fromLabFeature, "../../../main.tsx", repoRoot), false);
    assert.equal(specifierResolvesToAllowedDesignLabOutbound(fromLabFeature, "../../../App", repoRoot), false);
    assert.equal(specifierResolvesToAllowedDesignLabOutbound(fromLabFeature, "../../../components/ErrorBoundary", repoRoot), false);
    assert.equal(specifierResolvesToAllowedDesignLabOutbound(fromLabFeature, "../../../api/client", repoRoot), false);
    assert.equal(specifierResolvesToAllowedDesignLabOutbound(fromLabFeature, "../../../features/auth/hooks", repoRoot), false);
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
