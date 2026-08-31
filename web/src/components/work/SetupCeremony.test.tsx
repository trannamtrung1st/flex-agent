import { render, screen } from "@testing-library/react";
import { SetupCeremony, SetupCeremonyFoot, SetupCeremonyScroll } from "./SetupCeremony";

describe("SetupCeremony", () => {
  it("owns the ceremony shell and frozen modifier", () => {
    const { rerender } = render(
      <SetupCeremony data-testid="shell">
        <SetupCeremonyScroll>Body</SetupCeremonyScroll>
      </SetupCeremony>,
    );

    expect(screen.getByTestId("shell")).toHaveClass("setup-ceremony", "plate-bleed", "composition-stack");
    expect(screen.getByText("Body").closest(".create-ceremony__scroll")).toBeTruthy();

    rerender(
      <SetupCeremony as="form" frozen data-testid="shell">
        <SetupCeremonyFoot arrangement="end">
          <button type="submit">Create</button>
        </SetupCeremonyFoot>
      </SetupCeremony>,
    );

    expect(screen.getByTestId("shell")).toHaveClass("setup-ceremony", "workspace-form", "is-frozen");
    expect(screen.getByRole("contentinfo")).toHaveClass("setup-ceremony__foot", "plate-foot");
  });
});
