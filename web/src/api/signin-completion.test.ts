import { isSignInDeniedSearch, productionLoginReturnPath } from "./signin-completion";

describe("signin completion recovery", () => {
  it("recognizes the coarse denied flag without other reason codes", () => {
    expect(isSignInDeniedSearch("?signin=denied")).toBe(true);
    expect(isSignInDeniedSearch("signin=denied")).toBe(true);
    expect(isSignInDeniedSearch("?signin=denied&tab=setup")).toBe(true);
    expect(isSignInDeniedSearch("")).toBe(false);
    expect(isSignInDeniedSearch("?signin=unknown_subject")).toBe(false);
  });

  it("strips the denied flag from the next login return path", () => {
    expect(productionLoginReturnPath("/", "?signin=denied")).toBe("/");
    expect(productionLoginReturnPath("/work", "?signin=denied&tab=setup")).toBe("/work?tab=setup");
    expect(productionLoginReturnPath("/work", "")).toBe("/work");
    expect(productionLoginReturnPath("//evil", "")).toBe("/");
  });
});
