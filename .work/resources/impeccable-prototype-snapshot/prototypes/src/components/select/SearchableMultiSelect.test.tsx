import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState } from "react";
import { describe, expect, it } from "vitest";
import { SearchableMultiSelect } from "./SearchableMultiSelect";

const options = [
  { value: "reviewer", label: "Reviewer", id: "role-reviewer" },
  { value: "auditor", label: "Auditor", id: "role-auditor" },
  { value: "release", label: "Release authority", id: "role-release" },
];

function Harness() {
  const [values, setValues] = useState<string[]>(["reviewer"]);
  return (
    <>
      <span id="roles-label">Review roles</span>
      <SearchableMultiSelect
        id="roles"
        labelId="roles-label"
        searchId="roles-search"
        options={options}
        values={values}
        onChange={setValues}
        optionNoun="role"
      />
    </>
  );
}

describe("SearchableMultiSelect", () => {
  it("filters, toggles options, clears, and closes with Done", async () => {
    const user = userEvent.setup();
    render(<Harness />);

    const trigger = screen.getByRole("button", { name: /Review roles Reviewer/ });
    await user.click(trigger);
    const search = screen.getByRole("combobox");
    expect(search).toHaveFocus();
    await user.type(search, "aud");
    expect(screen.getByRole("option", { name: "Auditor" })).toBeVisible();
    expect(screen.queryByRole("option", { name: "Reviewer" })).not.toBeInTheDocument();
    await user.click(screen.getByRole("option", { name: "Auditor" }));
    expect(screen.getByText("2 selected")).toBeVisible();
    await user.click(screen.getByRole("button", { name: "Clear" }));
    expect(screen.getByText("0 selected")).toBeVisible();
    await user.click(screen.getByRole("button", { name: "Done" }));
    expect(trigger).toHaveFocus();
    expect(trigger).toHaveAttribute("aria-expanded", "false");
  });

  it("supports arrow navigation, Escape, and outside-click focus return", async () => {
    const user = userEvent.setup();
    render(
      <div>
        <Harness />
        <button type="button">Outside</button>
      </div>,
    );

    const trigger = screen.getByRole("button", { name: /Review roles Reviewer/ });
    await user.click(trigger);
    await user.keyboard("{ArrowDown}{Enter}");
    expect(screen.getByRole("option", { name: "Reviewer" })).toHaveAttribute("aria-selected", "false");
    await user.keyboard("{Escape}");
    expect(trigger).toHaveFocus();

    await user.click(trigger);
    expect(screen.getByRole("combobox")).toHaveFocus();
    await user.pointer({ keys: "[MouseLeft]", target: screen.getByRole("button", { name: "Outside" }) });
    expect(trigger).toHaveAttribute("aria-expanded", "false");
    await waitFor(() => expect(trigger).toHaveFocus());
  });

  it("supports exact-case filtering when configured", async () => {
    const user = userEvent.setup();
    render(
      <>
        <span id="exact-roles-label">Exact review roles</span>
        <SearchableMultiSelect
          id="exact-roles"
          labelId="exact-roles-label"
          options={[
            { value: "Reviewer", label: "Reviewer" },
            { value: "Auditor", label: "Auditor" },
          ]}
          values={[]}
          onChange={() => undefined}
          caseSensitive
        />
      </>,
    );

    await user.click(screen.getByRole("button", { name: /Exact review roles/i }));
    const search = screen.getByRole("combobox");
    await user.type(search, "aud");
    expect(screen.queryByRole("option", { name: "Auditor" })).not.toBeInTheDocument();
    await user.clear(search);
    await user.type(search, "Aud");
    expect(screen.getByRole("option", { name: "Auditor" })).toBeVisible();
  });
});
