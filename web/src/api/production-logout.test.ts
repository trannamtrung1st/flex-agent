import { ProductionApiError } from "./production-api-error";
import {
  completeProductionLogout,
  isKnownPreLogoutRejection,
  SignOutFailedCopy,
  SignOutUnconfirmedCopy,
} from "./production-logout";

describe("completeProductionLogout", () => {
  it("returns home after a successful local-only logout", async () => {
    const fetchImpl = vi.fn(() => Promise.resolve(new Response(JSON.stringify({ logged_out: true, end_session_url: null }), { status: 200 })));

    await expect(completeProductionLogout("csrf", fetchImpl)).resolves.toBe("/");
    expect(fetchImpl).toHaveBeenCalledWith("/auth/logout", expect.objectContaining({ method: "POST" }));
  });

  it("returns the provider end-session URL after a successful revoke", async () => {
    const endSession = "https://issuer.example/realms/flex/protocol/openid-connect/logout?client_id=flex-agent-api";
    const fetchImpl = vi.fn(() => Promise.resolve(new Response(JSON.stringify({ logged_out: true, end_session_url: endSession }), { status: 200 })));

    await expect(completeProductionLogout("csrf", fetchImpl)).resolves.toBe(endSession);
  });

  it("rejects an antiforgery failure without treating logout as complete", async () => {
    const fetchImpl = vi.fn(() => Promise.resolve(new Response("", { status: 400 })));

    await expect(completeProductionLogout("csrf", fetchImpl)).rejects.toEqual(
      expect.objectContaining({ message: SignOutFailedCopy, status: 400 }),
    );
  });

  it("rejects a transport failure as an unconfirmed logout", async () => {
    const fetchImpl = vi.fn(() => Promise.reject(new TypeError("Failed to fetch")));

    await expect(completeProductionLogout("csrf", fetchImpl)).rejects.toBeInstanceOf(TypeError);
    expect(isKnownPreLogoutRejection(new TypeError("Failed to fetch"))).toBe(false);
    expect(SignOutUnconfirmedCopy).toContain("could not be confirmed");
  });

  it("treats antiforgery 400 as a known pre-logout rejection", () => {
    expect(isKnownPreLogoutRejection(new ProductionApiError(400, SignOutFailedCopy))).toBe(true);
    expect(isKnownPreLogoutRejection(new ProductionApiError(500, SignOutFailedCopy))).toBe(false);
  });

  it("rejects a non-https next location", async () => {
    const fetchImpl = vi.fn(() => Promise.resolve(new Response(JSON.stringify({
      logged_out: true,
      end_session_url: "javascript:alert(1)",
    }), { status: 200 })));

    await expect(completeProductionLogout("csrf", fetchImpl)).resolves.toBe("/");
  });
});

describe("SignOutFailedCopy", () => {
  it("is a bounded failure message", () => {
    expect(SignOutFailedCopy).toBe("Sign out could not be completed.");
    expect(new ProductionApiError(400, SignOutFailedCopy).message).toBe(SignOutFailedCopy);
  });
});
