import { describe, expect, it } from "vitest";
import { shouldRewriteToDesignLabEntry } from "./vite-design-lab-spa";

describe("design-lab SPA rewrite", () => {
  it("rewrites every app route when the dev server is lab-only", () => {
    expect(shouldRewriteToDesignLabEntry("/surfaces", "all")).toBe(true);
    expect(shouldRewriteToDesignLabEntry("/participant-home", "all")).toBe(true);
  });

  it("rewrites only the /design-lab namespace on the candidate dev server", () => {
    expect(shouldRewriteToDesignLabEntry("/design-lab", "prefixed")).toBe(true);
    expect(shouldRewriteToDesignLabEntry("/design-lab/", "prefixed")).toBe(true);
    expect(shouldRewriteToDesignLabEntry("/design-lab/surfaces", "prefixed")).toBe(true);
    expect(shouldRewriteToDesignLabEntry("/", "prefixed")).toBe(false);
    expect(shouldRewriteToDesignLabEntry("/activities", "prefixed")).toBe(false);
    expect(shouldRewriteToDesignLabEntry("/design-labfoo", "prefixed")).toBe(false);
  });

  it("does not rewrite module assets or dotted paths", () => {
    for (const mode of ["all", "prefixed"] as const) {
      expect(shouldRewriteToDesignLabEntry("/src/design-lab/main.tsx", mode)).toBe(false);
      expect(shouldRewriteToDesignLabEntry("/favicon.svg", mode)).toBe(false);
      expect(shouldRewriteToDesignLabEntry("/@vite/client", mode)).toBe(false);
      expect(shouldRewriteToDesignLabEntry("/design-lab.html", mode)).toBe(false);
    }
  });
});
