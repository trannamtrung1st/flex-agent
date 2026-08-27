import assert from "node:assert/strict";
import { describe, it } from "node:test";
import {
  designLabImportViolations,
  extractImportSpecifiers,
  specifierResolvesToDesignLab,
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
