import { operatorAccountActions } from "./operator";

describe("operatorAccountActions", () => {
  it("puts theme then sign-out in the operator menu", () => {
    const toggleTheme = vi.fn();
    const onSignOut = vi.fn();
    const actions = operatorAccountActions("dark", toggleTheme, onSignOut);

    expect(actions.map((action) => action.id)).toEqual(["theme", "signout"]);
    expect(actions[0]?.label).toBe("Switch to light theme");
    expect(actions[1]?.intent).toBe("signout");

    actions[0]?.onSelect?.();
    actions[1]?.onSelect?.();
    expect(toggleTheme).toHaveBeenCalledOnce();
    expect(onSignOut).toHaveBeenCalledOnce();
  });

  it("keeps extra operator actions above theme and sign-out", () => {
    const actions = operatorAccountActions("light", () => undefined, () => undefined, [
      { id: "profile", label: "Profile", state: "disabled" },
    ]);

    expect(actions.map((action) => action.id)).toEqual(["profile", "theme", "signout"]);
    expect(actions[1]?.label).toBe("Switch to dark theme");
  });
});
