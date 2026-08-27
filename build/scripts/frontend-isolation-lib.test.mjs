import assert from "node:assert/strict";
import { describe, it } from "node:test";
import {
  designLabImportViolations,
  designLabOutboundImportViolations,
  extractImportSpecifiers,
  labOwnedStylesheetImportViolations,
  specifierResolvesToAllowedDesignLabOutbound,
  specifierResolvesToDesignLab,
  specifierResolvesToLabOwnedStylesheet,
} from "./frontend-isolation-lib.mjs";

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
    assert.equal(specifierResolvesToLabOwnedStylesheet(fromCandidate, "./styles/components/demo.css"), true);
    assert.equal(specifierResolvesToLabOwnedStylesheet(fromCandidate, "./styles/surfaces/participant-home.css"), true);
    assert.equal(specifierResolvesToLabOwnedStylesheet(fromCandidate, "../styles/design-lab.css"), true);
    assert.equal(specifierResolvesToLabOwnedStylesheet(fromCandidate, "./styles/shared.css"), false);
    assert.equal(specifierResolvesToLabOwnedStylesheet(fromCandidate, "./styles/tokens.css"), false);
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
    assert.equal(specifierResolvesToAllowedDesignLabOutbound(fromLabFeature, "../../components"), true);
    assert.equal(specifierResolvesToAllowedDesignLabOutbound(fromLabFeature, "../../../design-system/components/keys/Key"), true);
    assert.equal(specifierResolvesToAllowedDesignLabOutbound(fromLabFeature, "../../../lib/cx"), true);
    assert.equal(specifierResolvesToAllowedDesignLabOutbound(fromLabFeature, "../../../main.tsx"), false);
    assert.equal(specifierResolvesToAllowedDesignLabOutbound(fromLabFeature, "../../../App"), false);
    assert.equal(specifierResolvesToAllowedDesignLabOutbound(fromLabFeature, "../../../components/ErrorBoundary"), false);
    assert.equal(specifierResolvesToAllowedDesignLabOutbound(fromLabFeature, "../../../api/client"), false);
    assert.equal(specifierResolvesToAllowedDesignLabOutbound(fromLabFeature, "../../../features/auth/hooks"), false);
  });

  it("reports outbound violations for future production modules", () => {
    const source = 'import { client } from "../../../api/client";\n';
    const violations = designLabOutboundImportViolations(fromLabFeature, source);
    assert.equal(violations.length, 1);
    assert.match(violations[0], /api\/client/);
  });
});
