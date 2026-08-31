import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { render, screen } from "@testing-library/react";
import { StatusBay, StatusBays } from "./StatusBays";

const homeCss = readFileSync(
  join(dirname(fileURLToPath(import.meta.url)), "../../../styles/surfaces/participant-home.css"),
  "utf8",
);

describe("StatusBays", () => {
  it("owns the four-column status bay hull, not Grid", () => {
    render(
      <StatusBays>
        <StatusBay id="open" label="Open">
          <p>Plate</p>
        </StatusBay>
        <StatusBay id="idle" label="Idle" empty="No enrollments in this bay" />
      </StatusBays>,
    );

    const host = document.querySelector(".bays");
    expect(host).toHaveClass("bays");
    expect(host).not.toHaveClass("composition-grid");
    expect(host).not.toHaveClass("bays--dense");
    expect(screen.getByRole("region", { name: "Open" })).toHaveClass("bay");
    expect(screen.getByRole("heading", { name: "Open" })).toHaveClass("bay-head");
    expect(document.querySelector("#bay-open")?.closest(".bay")?.querySelector(".bay-plates")).toHaveTextContent("Plate");
    expect(screen.getByText("No enrollments in this bay")).toHaveClass("bay-empty");
  });

  it("adds dense when the roster is long", () => {
    render(
      <StatusBays dense>
        <StatusBay id="open" label="Open" />
      </StatusBays>,
    );

    expect(document.querySelector(".bays")).toHaveClass("bays", "bays--dense");
  });

  it("pins the plate foot and scrolls horizon copy on stretched bays", () => {
    expect(homeCss).toMatch(
      /\.bays \.assignment-plate \.readout-stack--horizon \{[^}]*min-height:\s*0/,
    );
    expect(homeCss).toMatch(
      /\.bays \.assignment-plate \.readout-stack--horizon \{[^}]*overflow-y:\s*auto/,
    );
    expect(homeCss).toMatch(
      /\.bays \.assignment-plate \.assignment-plate-keys \{[^}]*flex-shrink:\s*0/,
    );
    expect(homeCss).toMatch(
      /\.bays--dense \.assignment-plate \.readout-stack--horizon \{[^}]*overflow:\s*visible/,
    );
    expect(homeCss).toMatch(
      /html\[data-surface="participant-home"\] \.bays \.assignment-plate \.readout-stack--horizon \{[^}]*overflow:\s*visible/,
    );
  });

  it("keeps equal inline gutters around column hairlines", () => {
    const baysBlock = homeCss.match(/\.bays \{[^}]+\}/)?.[0] ?? "";
    const bayBlock = homeCss.match(/\.bay \{[^}]+\}/)?.[0] ?? "";
    expect(baysBlock).not.toMatch(/--frame-content-pad-/);
    expect(bayBlock).toMatch(/padding:\s*var\(--form-group-gap\)/);
    expect(homeCss).toMatch(/\.bay-head \{[^}]*margin:\s*0 0 var\(--form-group-gap\)/);
    expect(homeCss).toMatch(/\.bay-plates \{[^}]*padding:\s*0 0 2px/);
    expect(homeCss).not.toMatch(
      /\.bay-plates \{[^}]*padding:\s*0 var\(--frame-content-pad-inline-end\)/,
    );
  });
});
