import { labAccountActions } from "./operator";

describe("labAccountActions", () => {
  it("keeps lab stubs above the shared theme and sign-out actions", () => {
    const actions = labAccountActions("dark", () => undefined, () => undefined);
    expect(actions.map((action) => action.id)).toEqual(["profile", "preferences", "theme", "signout"]);
    expect(actions.find((action) => action.id === "theme")?.label).toBe("Switch to light theme");
    expect(actions.find((action) => action.id === "signout")?.intent).toBe("signout");
  });
});
