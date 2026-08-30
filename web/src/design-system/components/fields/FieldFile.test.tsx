import { fireEvent, render, screen, within } from "@testing-library/react";
import { useState } from "react";
import { describe, expect, it } from "vitest";
import { FieldFile, fileMatchesAccept, formatFileBytes, mergeSelectedFiles } from "./FieldFile";

function textFile(name: string, body = "ok") {
  return new File([body], name, { type: "text/plain" });
}

function Harness({
  mode = "multiple",
  accept,
  disabled,
  invalid,
}: {
  mode?: "single" | "multiple";
  accept?: string;
  disabled?: boolean;
  invalid?: boolean;
}) {
  const [files, setFiles] = useState<File[]>([]);
  return (
    <FieldFile
      id="demoFiles"
      labelledBy="demoFilesLabel"
      mode={mode}
      accept={accept}
      hint="UTF-8 .txt or .md"
      files={files}
      disabled={disabled}
      invalid={invalid}
      onFilesChange={setFiles}
    />
  );
}

describe("fileMatchesAccept", () => {
  it("matches extension and mime tokens", () => {
    expect(fileMatchesAccept(textFile("note.txt"), ".txt,.md,text/plain")).toBe(true);
    expect(fileMatchesAccept(new File(["#"], "note.md", { type: "text/markdown" }), ".txt,.md")).toBe(true);
    expect(fileMatchesAccept(new File(["x"], "shot.png", { type: "image/png" }), ".txt,.md")).toBe(false);
  });
});

describe("formatFileBytes", () => {
  it("uses binary units", () => {
    expect(formatFileBytes(48)).toBe("48 B");
    expect(formatFileBytes(2048)).toBe("2.0 KiB");
  });
});

describe("mergeSelectedFiles", () => {
  it("caps multiple selections at maxFiles", () => {
    const current = [textFile("a.txt"), textFile("b.txt")];
    const incoming = [textFile("c.txt")];
    expect(mergeSelectedFiles({ mode: "multiple", current, incoming, maxFiles: 2 }).map((file) => file.name)).toEqual([
      "a.txt",
      "b.txt",
    ]);
  });
});

describe("FieldFile", () => {
  it("offers a Choose files key and a hidden native file input in multiple mode", () => {
    render(
      <>
        <span id="demoFilesLabel">Attachments</span>
        <Harness />
      </>,
    );

    expect(screen.getByRole("button", { name: "Choose files" })).toBeInTheDocument();
    expect(screen.getByText("Drop files onto this bay")).toBeInTheDocument();
    const input = document.querySelector('input[type="file"]') as HTMLInputElement;
    expect(input.parentElement).toHaveClass("visually-hidden");
    expect(input.parentElement).toHaveAttribute("aria-hidden", "true");
    expect(input).toHaveAttribute("multiple");
    expect(input.tabIndex).toBe(-1);
    expect(screen.queryByRole("button", { name: "Choose File" })).not.toBeInTheDocument();
  });

  it("uses Choose file and omits multiple in single mode", () => {
    render(
      <>
        <span id="demoFilesLabel">Portrait</span>
        <Harness mode="single" />
      </>,
    );

    expect(screen.getByRole("button", { name: "Choose file" })).toBeInTheDocument();
    expect(screen.getByText("Drop a file onto this bay")).toBeInTheDocument();
    expect(document.querySelector('input[type="file"]')).not.toHaveAttribute("multiple");
  });

  it("stages selected files and can remove them", () => {
    render(
      <>
        <span id="demoFilesLabel">Attachments</span>
        <Harness />
      </>,
    );

    const input = document.querySelector('input[type="file"]') as HTMLInputElement;
    fireEvent.change(input, { target: { files: [textFile("briefing.txt"), textFile("notes.md")] } });

    const list = screen.getByRole("list", { name: "Selected files" });
    expect(within(list).getByText("briefing.txt")).toBeInTheDocument();
    expect(within(list).getByText("notes.md")).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "Remove briefing.txt" }));
    expect(within(list).queryByText("briefing.txt")).not.toBeInTheDocument();
    expect(within(list).getByText("notes.md")).toBeInTheDocument();
  });

  it("replaces the seated file in single mode", () => {
    render(
      <>
        <span id="demoFilesLabel">Brief</span>
        <Harness mode="single" />
      </>,
    );

    const input = document.querySelector('input[type="file"]') as HTMLInputElement;
    fireEvent.change(input, { target: { files: [textFile("first.txt")] } });
    fireEvent.change(input, { target: { files: [textFile("second.txt")] } });

    const list = screen.getByRole("list", { name: "Selected file" });
    expect(within(list).getByText("second.txt")).toBeInTheDocument();
    expect(within(list).queryByText("first.txt")).not.toBeInTheDocument();
  });

  it("accepts a drop onto the bay and rejects disallowed types", () => {
    render(
      <>
        <span id="demoFilesLabel">Attachments</span>
        <Harness accept=".txt,.md,text/plain,text/markdown" />
      </>,
    );

    const well = screen.getByText("Drop files onto this bay").closest(".field-file-well") as HTMLElement;
    const allowed = textFile("ok.txt");
    const blocked = new File(["x"], "shot.png", { type: "image/png" });
    fireEvent.drop(well, { dataTransfer: { files: [allowed, blocked], types: ["Files"] } });

    const list = screen.getByRole("list", { name: "Selected files" });
    expect(within(list).getByText("ok.txt")).toBeInTheDocument();
    expect(within(list).queryByText("shot.png")).not.toBeInTheDocument();
  });

  it("does not open the picker when disabled", () => {
    render(
      <>
        <span id="demoFilesLabel">Attachments</span>
        <Harness disabled />
      </>,
    );

    expect(screen.getByRole("button", { name: "Choose files" })).toBeDisabled();
    expect(document.querySelector('input[type="file"]')).toBeDisabled();
  });

  it("marks the well invalid", () => {
    const { container } = render(
      <>
        <span id="demoFilesLabel">Attachments</span>
        <Harness invalid />
      </>,
    );

    expect(container.querySelector(".field-file")).toHaveAttribute("aria-invalid", "true");
    expect(container.querySelector(".field-file-well")).toHaveClass("is-invalid");
  });
});
