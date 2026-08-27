import { render, screen } from "@testing-library/react";
import { ErrorSummary } from "./ErrorSummary";

describe("ErrorSummary", () => {
  it("keeps string errors as unlinked items", () => {
    render(<ErrorSummary errors={["The Campaign could not be created."]} />);
    expect(screen.getByText("The Campaign could not be created.")).toBeInTheDocument();
    expect(screen.queryByRole("link")).not.toBeInTheDocument();
  });

  it("links structured errors to their fields", () => {
    render(
      <ErrorSummary
        errors={[{ message: "Enter a Campaign title", href: "#campaign-title" }]}
      />,
    );
    expect(screen.getByRole("link", { name: "Enter a Campaign title" })).toHaveAttribute("href", "#campaign-title");
  });

  it("uses the attention advisory strip rather than a banner heading alone", () => {
    render(<ErrorSummary title="Correct the following" errors={["Enter a Campaign title"]} />);
    expect(screen.getByText("Error")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Correct the following" })).toBeInTheDocument();
    expect(screen.getByRole("alert")).toHaveClass("composition-stack");
  });
});
