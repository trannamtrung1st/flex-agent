import { render, screen } from "@testing-library/react";
import { WorkWellSection } from "../../design-system";
import { SubmissionVersionList } from "./SubmissionVersionList";

describe("SubmissionVersionList", () => {
  it("composes a WorkWell ordered list with sequence marks and Stack rows", () => {
    const { container } = render(
      <WorkWellSection>
        <SubmissionVersionList
          label="Accepted submission versions"
          reversed
          rows={[
            {
              key: "ver-1",
              versionNumber: 1,
              name: "Accepted version 1 remains immutable.",
              meta: "1 item, accepted 29 Aug 2026, 16:13 UTC.",
            },
          ]}
        />
      </WorkWellSection>,
    );

    const list = screen.getByRole("list", { name: "Accepted submission versions" });
    expect(list.tagName).toBe("OL");
    expect(list).toHaveAttribute("reversed");
    expect(list).not.toHaveClass("version-list");
    const item = list.querySelector(":scope > li");
    expect(item).toHaveAttribute("data-sequence", "1");
    expect(item).toHaveAttribute("value", "1");
    expect(item?.querySelector(":scope > .composition-stack")).not.toBeNull();
    expect(item?.querySelector("time")).toBeNull();
    expect(container.querySelector(".work-well__section > ol")).toBe(list);
  });

  it("keeps newest-first document order with explicit version sequence marks", () => {
    render(
      <SubmissionVersionList
        reversed
        label="Accepted submission versions"
        rows={[
          { key: "v2", versionNumber: 2, name: "Accepted version 2 remains immutable.", meta: "2 items." },
          { key: "v1", versionNumber: 1, name: "Accepted version 1 remains immutable.", meta: "1 item." },
        ]}
      />,
    );

    const list = screen.getByRole("list", { name: "Accepted submission versions" });
    const items = [...list.querySelectorAll(":scope > li")];
    expect(items.map((item) => item.getAttribute("data-sequence"))).toEqual(["2", "1"]);
    expect(items[0]).toHaveTextContent("Accepted version 2 remains immutable.");
    expect(items[1]).toHaveTextContent("Accepted version 1 remains immutable.");
  });
});
