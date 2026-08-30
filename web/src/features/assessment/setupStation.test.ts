import type { AssessmentSetupView } from "../../api/production-assessment";
import {
  setupBlockerHref,
  setupCeremonySummary,
  setupDurationLabel,
  setupMemoryCopy,
  setupNextAction,
  setupOpaqueCaption,
  setupSourceCaption,
  setupTracks,
} from "./setupStation";

function view(overrides: Partial<AssessmentSetupView> = {}): AssessmentSetupView {
  return {
    activity_id: "act-1",
    title: "Campaign A",
    revision_number: 1,
    memory_mode: "stable",
    has_activated_cohort: false,
    permitted_actions: ["save_draft", "check_readiness"],
    ...overrides,
  };
}

describe("setupStation", () => {
  it("asks to check readiness on the current revision when the draft is seated", () => {
    expect(setupNextAction(view(), "Campaign A", null)).toBe(
      "Check readiness on revision 1, then activate this cohort.",
    );
  });

  it("lights the readiness track until the cohort can be activated", () => {
    const tracks = setupTracks(view(), "Campaign A", null);
    expect(tracks.map((track) => `${track.id}:${track.value}:${track.now}`)).toEqual([
      "local:Seated:false",
      "draft:Revision 1:false",
      "readiness:Not checked:true",
      "cohort:Unactivated:false",
    ]);
  });

  it("does not present Disabled as a memory mode", () => {
    expect(setupMemoryCopy("disabled")).toBe("Stable — new long-term learning disabled");
  });

  it("captions a bound source as revision identity, not a raw digest", () => {
    expect(setupSourceCaption(view({
      sources: [{
        category: "agent",
        source_id: "agent-core",
        version_id: "v3",
        content_digest: "a".repeat(64),
      }],
    }), "agent")).toBe("agent-core · v3");
    expect(setupSourceCaption(view(), "agent")).toBe("Not bound");
  });

  it("formats attempt duration and omits an absent digest as not seated", () => {
    expect(setupDurationLabel(90)).toBe("01:30");
    expect(setupDurationLabel(null)).toBe("Not seated");
    expect(setupOpaqueCaption()).toBe("Not seated");
  });

  it("arms activation only after a current ready result", () => {
    const ready = view({
      permitted_actions: ["save_draft", "check_readiness", "activate_cohort"],
      issues: [],
    });
    expect(setupNextAction(ready, "Campaign A", null)).toBe(
      "Activate this cohort. The browser is not activation authority.",
    );
    expect(setupTracks(ready, "Campaign A", null).find((track) => track.now)?.id).toBe("cohort");
    expect(setupTracks(ready, "Campaign A", null).find((track) => track.id === "cohort")?.variant).toBe("live");
  });

  it("keeps readiness Ready after the cohort is activated", () => {
    const tracks = setupTracks(view({
      has_activated_cohort: true,
      permitted_actions: [],
    }), "Campaign A", null);
    expect(tracks.map((track) => `${track.id}:${track.value}:${track.now}`)).toEqual([
      "local:Seated:false",
      "draft:Revision 1:false",
      "readiness:Ready:false",
      "cohort:Activated:true",
    ]);
  });

  it("points a timing blocker at the timezone field and folds a save error into one summary", () => {
    expect(setupBlockerHref("setup-title", "timing")).toBe("#setup-title-timezone");
    expect(setupBlockerHref("setup-title", "unknown")).toBe("#setup-title");
    expect(setupCeremonySummary("setup-title", "This draft could not be saved.", [{
      category: "timing",
      severity: "blocker",
      reason_code: "window",
      recovery_hint: "Set a valid session window.",
    }])).toEqual({
      headingId: "setup-title-summary",
      title: "Readiness blocked",
      errors: [
        "This draft could not be saved.",
        { message: "Set a valid session window.", href: "#setup-title-timezone" },
      ],
    });
    expect(setupCeremonySummary("setup-title", "This draft could not be saved.", [])).toEqual({
      headingId: "setup-title-summary",
      title: "Correct the following",
      errors: ["This draft could not be saved."],
    });
  });
});
