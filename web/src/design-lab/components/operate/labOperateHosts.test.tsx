import { render, screen } from "@testing-library/react";
import { CampaignsOperateArea } from "./CampaignsOperateArea";
import { EnrollmentWallOperateArea } from "./EnrollmentWallOperateArea";
import { FormRecipeOperateArea } from "./FormRecipeOperateArea";
import { HomeBoardOperateArea } from "./HomeBoardOperateArea";
import { SampleWallOperateArea } from "./SampleWallOperateArea";

describe("lab OperateArea host wrappers", () => {
  it("owns campaigns-wall without workspace-area and hugs short registries", () => {
    render(
      <CampaignsOperateArea variant="registry" label="Campaigns" title="Campaigns" hug="registry">
        <p>Rows</p>
      </CampaignsOperateArea>,
    );

    const region = screen.getByRole("region", { name: "Campaigns" });
    expect(region).toHaveClass("campaigns-wall", "registry-wall--hug");
    expect(region).not.toHaveClass("workspace-area");
  });

  it("owns enrollment wall host classes", () => {
    render(
      <EnrollmentWallOperateArea label="Enrollments" title="Enrollments" hug="registry">
        <p>Rows</p>
      </EnrollmentWallOperateArea>,
    );

    const region = screen.getByRole("region", { name: "Enrollments" });
    expect(region).toHaveClass("wall", "registry-wall--hug");
    expect(region).not.toHaveClass("workspace-area");
  });

  it("owns sample-wall without registry hug", () => {
    render(
      <SampleWallOperateArea label="Sample" title="Sample">
        <p>Body</p>
      </SampleWallOperateArea>,
    );

    const region = screen.getByRole("region", { name: "Sample" });
    expect(region).toHaveClass("campaigns-wall", "sample-wall");
    expect(region).not.toHaveClass("workspace-area");
    expect(region).not.toHaveClass("registry-wall--hug");
  });

  it("owns the home board host and board hug", () => {
    render(
      <HomeBoardOperateArea label="Assigned work" title="Assigned work" hug="board" framed={false}>
        <p>Roster</p>
      </HomeBoardOperateArea>,
    );

    const region = screen.getByRole("region", { name: "Assigned work" });
    expect(region).toHaveClass("workspace-area", "board", "assignment-board--hug");
    expect(region).not.toHaveClass("work-plane");
  });

  it("adds form-recipe on the workspace work-plane", () => {
    render(
      <FormRecipeOperateArea label="Create assessment Campaign" title="Create assessment Campaign">
        <p>Form</p>
      </FormRecipeOperateArea>,
    );

    const region = screen.getByRole("region", { name: "Create assessment Campaign" });
    expect(region).toHaveClass("workspace-area", "work-plane", "form-recipe");
  });
});
