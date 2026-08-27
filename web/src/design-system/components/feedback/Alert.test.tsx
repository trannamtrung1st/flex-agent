import { render, screen } from "@testing-library/react";
import { Alert } from "./Alert";

describe("Alert", () => {
  it("uses alert role and attention advisory for danger", () => {
    render(<Alert variant="danger" title="Request could not be completed">Details</Alert>);
    expect(screen.getByRole("alert")).toHaveClass("workspace-alert");
    expect(screen.queryAllByRole("status")).toHaveLength(0);
    expect(screen.getByText("Error")).toBeInTheDocument();
    expect(screen.getByText("Request could not be completed")).toBeInTheDocument();
    expect(screen.getByText("Details")).toBeInTheDocument();
  });

  it("uses status role for non-danger variants", () => {
    render(<Alert variant="info" title="Note copy" />);
    expect(screen.getByRole("status")).toHaveClass("workspace-alert");
    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
  });
});
