import { fireEvent, render, screen } from "@testing-library/react";
import { SearchableDropdownSelect } from "./SearchableDropdownSelect";
import { DisclosureMenu } from "./listboxMenus";

describe("searchable select dismissal", () => {
  it("keeps the panel open while the option list scrolls and closes on window scroll", () => {
    render(
      <>
        <span id="harness-label">Harness</span>
        <SearchableDropdownSelect
          labelId="harness-label"
          value="Alpha"
          options={["Alpha", "Bravo", "Charlie", "Delta", "Echo", "Foxtrot"]}
          onChange={() => undefined}
        />
      </>,
    );
    fireEvent.click(screen.getByRole("button", { name: /Alpha/ }));
    const list = screen.getByRole("listbox");
    fireEvent.scroll(list);
    expect(screen.getByRole("listbox")).toBeInTheDocument();
    fireEvent.scroll(window);
    expect(screen.queryByRole("listbox")).not.toBeInTheDocument();
  });
});

describe("pagination listbox dismissal", () => {
  it("closes the rows menu on external scroll", () => {
    render(
      <DisclosureMenu
        label="Rows"
        value="8 / page"
        selectedId="8"
        options={[
          { id: "8", label: "8 / page" },
          { id: "16", label: "16 / page" },
        ]}
        onSelect={() => undefined}
        ariaLabel="Rows per page"
      />,
    );
    fireEvent.click(screen.getByRole("button", { name: /8 \/ page/ }));
    expect(screen.getByRole("listbox")).toBeInTheDocument();
    fireEvent.scroll(window);
    expect(screen.queryByRole("listbox")).not.toBeInTheDocument();
  });
});
