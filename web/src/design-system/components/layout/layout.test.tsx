import { createRef, type ElementType } from "react";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { fireEvent, render, screen } from "@testing-library/react";
import {
  Container,
  Grid,
  Inline,
  Inset,
  SplitBay,
  Stack,
  type LayoutSpace,
} from "./index";

const LAYOUT_SPACES: LayoutSpace[] = [
  "none", "1", "2", "2.5", "3", "4", "5", "5.5", "6", "6.5", "8", "10", "12", "16", "20", "24",
];

const cssPath = join(dirname(fileURLToPath(import.meta.url)), "../../../styles/components/layout-primitives.css");
const tokenPath = join(dirname(fileURLToPath(import.meta.url)), "../../../styles/tokens.css");

describe("layout primitives", () => {
  it("exports default class and data contracts", () => {
    const { rerender } = render(<Stack>stack</Stack>);
    const stack = screen.getByText("stack");
    expect(stack.tagName).toBe("DIV");
    expect(stack).toHaveClass("composition-stack");
    expect(stack).toHaveAttribute("data-flow-gap", "none");
    expect(stack).toHaveAttribute("data-flow-align", "stretch");

    rerender(<Inline>inline</Inline>);
    const inline = screen.getByText("inline");
    expect(inline).toHaveClass("composition-inline");
    expect(inline).toHaveAttribute("data-flow-gap", "none");
    expect(inline).toHaveAttribute("data-flow-align", "center");
    expect(inline).toHaveAttribute("data-flow-justify", "start");
    expect(inline).toHaveAttribute("data-flow-wrap", "true");

    rerender(<Grid>grid</Grid>);
    const grid = screen.getByText("grid");
    expect(grid).toHaveClass("composition-grid");
    expect(grid).toHaveAttribute("data-flow-gap", "none");
    expect(grid).toHaveAttribute("data-flow-min", "panel");
    expect(grid).toHaveAttribute("data-flow-align", "stretch");
    expect(grid).toHaveAttribute("data-flow-fit", "fit");

    rerender(<Container>container</Container>);
    const container = screen.getByText("container");
    expect(container).toHaveClass("composition-container");
    expect(container).toHaveAttribute("data-flow-size", "content");
    expect(container).toHaveAttribute("data-flow-align", "start");

    rerender(<Inset>inset</Inset>);
    const inset = screen.getByText("inset");
    expect(inset).toHaveClass("composition-inset");
    expect(inset).toHaveAttribute("data-flow-space", "none");
    expect(inset).toHaveAttribute("data-flow-inline", "none");
    expect(inset).toHaveAttribute("data-flow-block", "none");
  });

  it("renders each primitive as a semantic alternate element", () => {
    render(
      <>
        <Stack as="section" aria-label="Stacked region">section</Stack>
        <Inline as="ul"><li>item</li></Inline>
        <Grid as="ol"><li>cell</li></Grid>
        <Container as="article">article</Container>
        <Inset as="header">header</Inset>
      </>,
    );
    expect(screen.getByRole("region", { name: "Stacked region" }).tagName).toBe("SECTION");
    expect(screen.getByText("item").closest("ul")?.tagName).toBe("UL");
    expect(screen.getByText("cell").closest("ol")).toBeTruthy();
    expect(screen.getByText("article").tagName).toBe("ARTICLE");
    expect(screen.getByText("header").tagName).toBe("HEADER");
  });

  it("forwards ref, id, aria, class, and events", () => {
    const ref = createRef<HTMLDivElement>();
    const onClick = vi.fn();
    render(
      <Stack ref={ref} id="flow-root" aria-label="Forwarded" className="extra" onClick={onClick}>
        body
      </Stack>,
    );
    const node = screen.getByLabelText("Forwarded");
    expect(ref.current).toBe(node);
    expect(node).toHaveAttribute("id", "flow-root");
    expect(node).toHaveClass("composition-stack", "extra");
    fireEvent.click(node);
    expect(onClick).toHaveBeenCalledTimes(1);
  });

  it("applies wrap, justify, grid minima, container size, and inset axis override", () => {
    render(
      <>
        <Inline wrap={false} justify="between" gap="3">cluster</Inline>
        <Grid minItemWidth="compact" gap="4" fit="fill">tiles</Grid>
        <Container size="form" align="center">form</Container>
        <Inset space="6" inline="2" block="4">pad</Inset>
      </>,
    );
    expect(screen.getByText("cluster")).toHaveAttribute("data-flow-wrap", "false");
    expect(screen.getByText("cluster")).toHaveAttribute("data-flow-justify", "between");
    expect(screen.getByText("cluster")).toHaveAttribute("data-flow-gap", "3");
    expect(screen.getByText("tiles")).toHaveAttribute("data-flow-min", "compact");
    expect(screen.getByText("tiles")).toHaveAttribute("data-flow-fit", "fill");
    expect(screen.getByText("form")).toHaveAttribute("data-flow-size", "form");
    expect(screen.getByText("form")).toHaveAttribute("data-flow-align", "center");
    expect(screen.getByText("pad")).toHaveAttribute("data-flow-space", "6");
    expect(screen.getByText("pad")).toHaveAttribute("data-flow-inline", "2");
    expect(screen.getByText("pad")).toHaveAttribute("data-flow-block", "4");
  });

  it("does not inject landmarks, roles, or reorder children", () => {
    render(
      <main>
        <Stack>
          <button type="button">First</button>
          <button type="button">Second</button>
        </Stack>
      </main>,
    );
    expect(screen.getAllByRole("main")).toHaveLength(1);
    expect(screen.queryByRole("navigation")).not.toBeInTheDocument();
    const buttons = screen.getAllByRole("button");
    expect(buttons.map((button) => button.textContent)).toEqual(["First", "Second"]);
    expect(screen.getByText("First").parentElement).not.toHaveAttribute("role");
    expect(screen.getByText("First").parentElement?.tabIndex).not.toBe(0);
  });

  it("maps every LayoutSpace to a token and uses logical inset padding", () => {
    const css = readFileSync(cssPath, "utf8");
    const tokens = readFileSync(tokenPath, "utf8");
    for (const space of LAYOUT_SPACES) {
      if (space === "none") {
        expect(css).toMatch(/data-flow-gap="none"/);
        continue;
      }
      const token = `--space-${space.replace(".", "-")}`;
      expect(tokens).toContain(`${token}:`);
      expect(css).toContain(`var(${token})`);
    }
    expect(css).toMatch(/padding-block:/);
    expect(css).toMatch(/padding-inline:/);
    expect(css).toMatch(/min-width:\s*0/);
    expect(css).toMatch(/\.composition-inline > \*\s*\{[^}]*max-width:\s*100%/);
    expect(css).toMatch(/\.composition-inline > \*\s*\{[^}]*flex-shrink:\s*0/);
    expect(css).toMatch(/\.composition-inline\[data-flow-wrap="false"\] > \*\s*\{[^}]*min-width:\s*0/);
    expect(css).toMatch(/\.composition-inline\[data-flow-wrap="false"\] > \*\s*\{[^}]*flex-shrink:\s*1/);
    expect(css).not.toMatch(/(?:^|})\s*\[data-flow-gap=/);
    expect(css).not.toMatch(/(?:^|})\s*\.layout-/);
    expect(css).toMatch(/auto-fit/);
    expect(css).toMatch(/\.composition-grid\[data-flow-fit="fill"\][^{]*\{[^}]*auto-fill/);
    expect(css).toMatch(/grid-template-areas:\s*"start main end"/);
    expect(css).toMatch(/--content-width-prose/);
    expect(css).toMatch(/--content-width-form/);
  });

  it("accepts every documented space on Stack", () => {
    const { rerender } = render(<Stack gap="none">gap</Stack>);
    for (const space of LAYOUT_SPACES) {
      rerender(<Stack gap={space}>gap</Stack>);
      expect(screen.getByText("gap")).toHaveAttribute("data-flow-gap", space);
    }
  });

  it("keeps polymorphic as typed without forcing a default role", () => {
    const as: ElementType = "nav";
    render(<Inline as={as}>nav flow</Inline>);
    expect(screen.getByText("nav flow").tagName).toBe("NAV");
  });

  it("places split-bay start, main, and end without a layout root", () => {
    const ref = createRef<HTMLDivElement>();
    render(
      <SplitBay
        ref={ref}
        start={<aside>Manifest</aside>}
        end={<aside>Criteria</aside>}
        overlay={<svg data-testid="tether" />}
      >
        Transcript
      </SplitBay>,
    );
    const root = screen.getByText("Transcript").closest(".composition-split");
    expect(ref.current).toBe(root);
    expect(root).toHaveAttribute("data-flow-split", "bay");
    expect(root).not.toHaveAttribute("data-layout");
    expect(root?.querySelector(".composition-split__overlay")).toHaveAttribute("aria-hidden", "true");
    expect(screen.getByText("Manifest").closest(".composition-split__start")).toBeTruthy();
    expect(screen.getByText("Transcript").closest(".composition-split__main")).toBeTruthy();
    expect(screen.getByText("Criteria").closest(".composition-split__end")).toBeTruthy();
  });

  it("collapses split-bay rails in drawer mode", () => {
    render(
      <SplitBay drawer toolbar={<div>Panels</div>}>
        Transcript
      </SplitBay>,
    );
    const root = screen.getByText("Transcript").closest(".composition-split");
    expect(root).toHaveAttribute("data-flow-split", "drawer");
    expect(screen.getByText("Panels").closest(".composition-split__toolbar")).toBeTruthy();
    expect(root?.querySelector(".composition-split__start")).toBeNull();
    expect(root?.querySelector(".composition-split__end")).toBeNull();
  });

  it("lets a ledger head and foot span main and end while start stays a rail", () => {
    render(
      <SplitBay
        start={<aside>Manifest</aside>}
        end={<aside>Criteria</aside>}
        head={<h1>Ledger plaque</h1>}
        foot={<footer>Decisions</footer>}
      >
        Transcript
      </SplitBay>,
    );
    const root = screen.getByText("Transcript").closest(".composition-split");
    expect(root).toHaveAttribute("data-flow-head", "true");
    expect(root).toHaveAttribute("data-flow-foot", "true");
    expect(screen.getByRole("heading", { name: "Ledger plaque" }).closest(".composition-split__head")).toBeTruthy();
    expect(screen.getByText("Decisions").closest(".composition-split__foot")).toBeTruthy();
    expect(screen.getByText("Manifest").closest(".composition-split__start")).toBeTruthy();
  });
});
