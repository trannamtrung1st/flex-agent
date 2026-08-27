import { useState } from "react";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import {
  AcknowledgmentGate,
  Bulkhead,
  DisclosureMenu,
  DropdownSelect,
  FormField,
  NativeDialog,
  PARTICIPANT_IDENTITY,
  ProfileMenu,
  SearchableDisclosureMenu,
  SearchableDropdownSelect,
  prototypeAccountActions,
} from "../src/components";

describe("ProfileMenu", () => {
  it("opens on click, skips disabled actions, and closes on Escape", async () => {
    const user = userEvent.setup();
    const onSignOut = vi.fn();
    render(
      <ProfileMenu identity={PARTICIPANT_IDENTITY} actions={prototypeAccountActions(onSignOut)} />,
    );
    const trigger = screen.getByRole("button", { name: /operator menu/i });
    await user.click(trigger);
    expect(trigger).toHaveAttribute("aria-expanded", "true");
    expect(screen.getByRole("menuitem", { name: /Profile/ })).toBeDisabled();
    expect(screen.getByRole("menuitem", { name: /Preferences/ })).toBeDisabled();
    await user.keyboard("{ArrowDown}");
    expect(screen.getByRole("menuitem", { name: "Sign out" })).toHaveFocus();
    await user.keyboard("{Escape}");
    expect(trigger).toHaveAttribute("aria-expanded", "false");
  });

  it("invokes Sign out without activating disabled items", async () => {
    const user = userEvent.setup();
    const onSignOut = vi.fn();
    render(
      <ProfileMenu identity={PARTICIPANT_IDENTITY} actions={prototypeAccountActions(onSignOut)} />,
    );
    await user.click(screen.getByRole("button", { name: /operator menu/i }));
    await user.click(screen.getByRole("menuitem", { name: /Profile/ }));
    expect(onSignOut).not.toHaveBeenCalled();
    await user.click(screen.getByRole("menuitem", { name: "Sign out" }));
    expect(onSignOut).toHaveBeenCalledOnce();
  });
});

function DisclosureHarness() {
  const [selectedId, setSelectedId] = useState("all");
  const options = [
    { id: "all", label: "All" },
    { id: "live", label: "Live" },
  ];
  return (
    <DisclosureMenu
      label="Filter:"
      value={options.find((option) => option.id === selectedId)?.label ?? "All"}
      selectedId={selectedId}
      ariaLabel="Filter by stage"
      options={options}
      onSelect={setSelectedId}
    />
  );
}

describe("DisclosureMenu", () => {
  it("opens, moves with arrows, and closes on Escape", async () => {
    const user = userEvent.setup();
    render(<DisclosureHarness />);
    const trigger = screen.getByRole("button");
    await user.click(trigger);
    expect(trigger).toHaveAttribute("aria-expanded", "true");
    await waitFor(() => expect(screen.getByRole("option", { name: "All" })).toHaveFocus());
    await user.keyboard("{ArrowDown}");
    expect(screen.getByRole("option", { name: "Live" })).toHaveFocus();
    await user.keyboard("{Escape}");
    expect(trigger).toHaveAttribute("aria-expanded", "false");
  });

  it("opens with Enter and moves focus to the selected option", async () => {
    const user = userEvent.setup();
    render(<DisclosureHarness />);
    const trigger = screen.getByRole("button");
    trigger.focus();
    await user.keyboard("{Enter}");
    expect(trigger).toHaveAttribute("aria-expanded", "true");
    await waitFor(() => expect(screen.getByRole("option", { name: "All" })).toHaveFocus());
  });

  it("keeps trigger copy and aria-selected on the option id, not the display label", async () => {
    const user = userEvent.setup();
    render(<DisclosureHarness />);
    const trigger = screen.getByRole("button");
    await user.click(trigger);
    expect(screen.getByRole("option", { name: "All" })).toHaveAttribute("aria-selected", "true");
    await user.click(screen.getByRole("option", { name: "Live" }));
    expect(trigger).toHaveTextContent("Filter:Live");
    await user.click(trigger);
    expect(screen.getByRole("option", { name: "Live" })).toHaveAttribute("aria-selected", "true");
    expect(screen.getByRole("option", { name: "All" })).toHaveAttribute("aria-selected", "false");
    await waitFor(() => expect(screen.getByRole("option", { name: "Live" })).toHaveFocus());
  });
});

function DropdownHarness() {
  const [value, setValue] = useState("GOVERNED-EXAM-02");
  return (
    <>
      <span id="dropdownLabel">Harness</span>
      <DropdownSelect
        id="dropdownSelect"
        labelId="dropdownLabel"
        value={value}
        options={["GOVERNED-EXAM-01", "GOVERNED-EXAM-02"]}
        onChange={setValue}
      />
    </>
  );
}

function OptionalDropdownHarness() {
  const [value, setValue] = useState<string | null>("Auditor");
  return (
    <>
      <span id="optionalOwnerLabel">Escalation owner</span>
      <DropdownSelect
        clearable
        id="optionalOwnerSelect"
        labelId="optionalOwnerLabel"
        value={value}
        options={["Reviewer", "Auditor"]}
        placeholder="No owner assigned"
        onChange={setValue}
      />
    </>
  );
}

describe("DropdownSelect", () => {
  it("opens with Space and moves focus to the selected option", async () => {
    const user = userEvent.setup();
    render(<DropdownHarness />);
    const trigger = screen.getByRole("button", { name: /Harness/i });
    trigger.focus();
    await user.keyboard(" ");
    expect(trigger).toHaveAttribute("aria-expanded", "true");
    await waitFor(() =>
      expect(screen.getByRole("option", { name: "GOVERNED-EXAM-02" })).toHaveFocus(),
    );
  });

  it("keeps the menu open when the associated label is clicked", async () => {
    const user = userEvent.setup();
    render(
      <FormField id="harnessSelect" label="Harness" labelAssociatesControl={false}>
        {(controlProps, { labelId }) => (
          <DropdownSelect
            id={controlProps.id}
            labelId={labelId}
            value="GOVERNED-EXAM-02"
            options={["GOVERNED-EXAM-01", "GOVERNED-EXAM-02"]}
            onChange={() => {}}
          />
        )}
      </FormField>,
    );
    const trigger = screen.getByRole("button", { name: /Harness/i });
    await user.click(trigger);
    expect(trigger).toHaveAttribute("aria-expanded", "true");
    await user.click(screen.getByText("Harness"));
    expect(trigger).toHaveAttribute("aria-expanded", "true");
  });

  it("clears an optional selection and exposes its placeholder", async () => {
    const user = userEvent.setup();
    render(<OptionalDropdownHarness />);
    const trigger = screen.getByRole("button", { name: /Escalation owner Auditor/i });

    await user.click(trigger);
    expect(screen.queryByRole("option", { name: "Clear" })).not.toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Clear" }));

    expect(trigger).toHaveAttribute("aria-expanded", "false");
    expect(trigger).toHaveAccessibleName("Escalation owner No owner assigned");

    await user.click(trigger);
    expect(screen.getByRole("button", { name: "Clear" })).toBeDisabled();
  });

  it("focuses the committed optional value when opened from the keyboard", async () => {
    const user = userEvent.setup();
    render(<OptionalDropdownHarness />);
    const trigger = screen.getByRole("button", { name: /Escalation owner Auditor/i });

    trigger.focus();
    await user.keyboard(" ");

    await waitFor(() => expect(screen.getByRole("option", { name: "Auditor" })).toHaveFocus());
  });

  it("opens a clearable select at its first and last rows with arrow keys", async () => {
    const user = userEvent.setup();
    render(<OptionalDropdownHarness />);
    const trigger = screen.getByRole("button", { name: /Escalation owner Auditor/i });

    trigger.focus();
    await user.keyboard("{ArrowDown}");
    await waitFor(() => expect(screen.getByRole("option", { name: "Reviewer" })).toHaveFocus());
    await user.keyboard("{Escape}");

    await user.keyboard("{ArrowUp}");
    await waitFor(() => expect(screen.getByRole("option", { name: "Auditor" })).toHaveFocus());
  });

  it("moves from the last option into the Clear text action", async () => {
    const user = userEvent.setup();
    render(<OptionalDropdownHarness />);
    const trigger = screen.getByRole("button", { name: /Escalation owner Auditor/i });

    trigger.focus();
    await user.keyboard("{ArrowUp}");
    await waitFor(() => expect(screen.getByRole("option", { name: "Auditor" })).toHaveFocus());
    await user.keyboard("{ArrowDown}");
    await waitFor(() => expect(screen.getByRole("button", { name: "Clear" })).toHaveFocus());
  });

  it("uses toolbar shell anatomy when variant is toolbar", () => {
    render(
      <>
        <span id="toolbarLabel">Stage</span>
        <DropdownSelect
          variant="toolbar"
          id="toolbarSelect"
          labelId="toolbarLabel"
          value="Review"
          options={["All stages", "Examination", "Review"]}
          onChange={() => {}}
        />
      </>,
    );
    const shell = screen.getByRole("button", { name: /Review/i }).closest(".select-shell");
    expect(shell).toHaveClass("toolbar-seg", "select-shell--toolbar");
    expect(shell).not.toHaveClass("select-shell--field");
    expect(screen.getByRole("button", { name: /Review/i })).toHaveClass("seg-key", "select-trigger--toolbar");
  });
});

function SearchableSelectHarness() {
  const [value, setValue] = useState("GOVERNED-EXAM-01");
  return (
    <SearchableDropdownSelect
      id="harnessSearchSelect"
      labelId="harnessSearchLabel"
      value={value}
      options={[
        "GOVERNED-EXAM-01",
        "GOVERNED-EXAM-02",
        "GOVERNED-AUDIT-01",
        "GOVERNED-OPS-02 / Cross-region failover harness",
      ]}
      onChange={setValue}
      listLabel="Harness options"
      optionNoun="harness"
    />
  );
}

describe("SearchableDropdownSelect", () => {
  it("filters options and commits a single selection", async () => {
    const user = userEvent.setup();
    render(
      <>
        <span id="harnessSearchLabel">Harness</span>
        <SearchableSelectHarness />
      </>,
    );
    const trigger = screen.getByRole("button", { name: /Harness/i });
    await user.click(trigger);
    expect(screen.getByRole("button", { name: "Close" })).toBeVisible();
    const search = screen.getByRole("combobox");
    await user.type(search, "ops");
    expect(screen.getByRole("option", { name: "GOVERNED-EXAM-01" })).toBeVisible();
    await user.click(screen.getByRole("option", { name: /Cross-region failover harness/i }));
    expect(trigger).toHaveAttribute("aria-expanded", "false");
    expect(document.getElementById("harnessSearchValue")).toHaveTextContent(
      "GOVERNED-OPS-02 / Cross-region failover harness",
    );
  });

  it("opens with Enter and focuses the filter", async () => {
    const user = userEvent.setup();
    render(
      <>
        <span id="harnessSearchLabel">Harness</span>
        <SearchableSelectHarness />
      </>,
    );
    const trigger = screen.getByRole("button", { name: /Harness/i });
    trigger.focus();
    await user.keyboard("{Enter}");
    expect(trigger).toHaveAttribute("aria-expanded", "true");
    await waitFor(() => expect(screen.getByRole("combobox")).toHaveFocus());
  });
});

function SearchableDisclosureHarness() {
  const [id, setId] = useState("CMP-0042");
  const options = [
    { id: "CMP-0042", label: "CMP-0042 / Structural Audit Q3" },
    { id: "CMP-0043", label: "CMP-0043 / Ops Integrity" },
    { id: "CMP-0044", label: "CMP-0044 / Harbor Readiness" },
  ];
  const selected = options.find((opt) => opt.id === id) ?? options[0];
  return (
    <SearchableDisclosureMenu
      keyId="campaignSearchKey"
      menuId="campaignSearchMenu"
      valueId="campaignSearchValue"
      label="Campaign"
      value={selected.label}
      selectedId={id}
      ariaLabel="Select campaign"
      searchPlaceholder="Filter campaigns"
      optionNoun="campaign"
      options={options}
      onSelect={setId}
    />
  );
}

describe("SearchableDisclosureMenu", () => {
  it("filters campaigns and commits a single selection", async () => {
    const user = userEvent.setup();
    render(<SearchableDisclosureHarness />);
    const trigger = screen.getByRole("button", { name: /Campaign/i });
    await user.click(trigger);
    const search = screen.getByPlaceholderText("Filter campaigns");
    await user.type(search, "ops");
    await user.click(screen.getByRole("option", { name: /CMP-0043 \/ Ops Integrity/i }));
    expect(trigger).toHaveAttribute("aria-expanded", "false");
    expect(document.getElementById("campaignSearchValue")).toHaveTextContent("CMP-0043 / Ops Integrity");
  });

  it("opens with Space and focuses the filter", async () => {
    const user = userEvent.setup();
    render(<SearchableDisclosureHarness />);
    const trigger = screen.getByRole("button", { name: /Campaign/i });
    trigger.focus();
    await user.keyboard(" ");
    expect(trigger).toHaveAttribute("aria-expanded", "true");
    await waitFor(() => expect(screen.getByPlaceholderText("Filter campaigns")).toHaveFocus());
  });
});

function DialogHarness() {
  const [open, setOpen] = useState(false);
  return (
    <>
      <button type="button" onClick={() => setOpen(true)}>
        Open dialog
      </button>
      <NativeDialog open={open} onClose={() => setOpen(false)} className="dialog" labelledBy="dlg-title">
        <h2 id="dlg-title">Ceremony</h2>
        <button type="button" onClick={() => setOpen(false)}>
          Cancel
        </button>
      </NativeDialog>
    </>
  );
}

describe("NativeDialog", () => {
  it("opens and restores trigger focus on cancel", async () => {
    const user = userEvent.setup();
    render(<DialogHarness />);
    const trigger = screen.getByRole("button", { name: "Open dialog" });
    await user.click(trigger);
    expect(screen.getByRole("heading", { name: "Ceremony" })).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Cancel" }));
    await waitFor(() => expect(trigger).toHaveFocus());
  });

  it("does not restore focus when the trigger is disabled", async () => {
    function LockedHarness() {
      const [open, setOpen] = useState(false);
      const [locked, setLocked] = useState(false);
      return (
        <>
          <button type="button" disabled={locked} onClick={() => setOpen(true)}>
            Open dialog
          </button>
          <NativeDialog
            open={open}
            onClose={() => {
              setOpen(false);
              setLocked(true);
            }}
            className="dialog"
            labelledBy="dlg-title"
          >
            <h2 id="dlg-title">Ceremony</h2>
            <button
              type="button"
              onClick={() => {
                setOpen(false);
                setLocked(true);
              }}
            >
              Confirm
            </button>
          </NativeDialog>
        </>
      );
    }

    const user = userEvent.setup();
    render(<LockedHarness />);
    const trigger = screen.getByRole("button", { name: "Open dialog" });
    await user.click(trigger);
    await user.click(screen.getByRole("button", { name: "Confirm" }));
    await waitFor(() => expect(trigger).toBeDisabled());
    expect(trigger).not.toHaveFocus();
  });
});

function BulkheadHarness() {
  const [open, setOpen] = useState(false);
  return (
    <>
      <button type="button" onClick={() => setOpen(true)}>
        Open navigation
      </button>
      <Bulkhead
        id="navigation-drawer"
        open={open}
        onClose={() => setOpen(false)}
        title="Navigation"
        titleId="navigation-drawer-title"
      >
        <a href="#campaigns">Campaigns</a>
      </Bulkhead>
    </>
  );
}

describe("Bulkhead", () => {
  it("closes on Escape and restores focus to its trigger", async () => {
    const user = userEvent.setup();
    render(<BulkheadHarness />);
    const trigger = screen.getByRole("button", { name: "Open navigation" });
    await user.click(trigger);
    expect(screen.getByRole("dialog", { name: "Navigation" })).toBeVisible();

    await user.keyboard("{Escape}");
    await waitFor(() => expect(screen.queryByRole("dialog", { name: "Navigation" })).not.toBeInTheDocument());
    expect(trigger).toHaveFocus();
  });
});

function AckHarness() {
  const [checked, setChecked] = useState(false);
  return (
    <AcknowledgmentGate id="ack" checked={checked} onChange={setChecked}>
      I acknowledge the rules
    </AcknowledgmentGate>
  );
}

describe("AcknowledgmentGate", () => {
  it("toggles the commitment checkbox", async () => {
    const user = userEvent.setup();
    render(<AckHarness />);
    const box = screen.getByRole("checkbox");
    expect(box).not.toBeChecked();
    await user.click(box);
    expect(box).toBeChecked();
  });
});
