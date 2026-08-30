export const SIGN_IN_QUERY = "signin";
export const SIGN_IN_DENIED = "denied";
export const SignInDeniedCopy = "Sign-in could not be completed. No application session was created.";

export function isSignInDeniedSearch(search: string): boolean {
  const params = new URLSearchParams(search.startsWith("?") ? search : `?${search}`);
  return params.get(SIGN_IN_QUERY) === SIGN_IN_DENIED;
}

export function productionLoginReturnPath(pathname: string, search: string): string {
  const params = new URLSearchParams(search.startsWith("?") ? search.slice(1) : search);
  params.delete(SIGN_IN_QUERY);
  const query = params.toString();
  const path = `${pathname}${query ? `?${query}` : ""}`;
  return path.startsWith("/") && !path.startsWith("//") && !path.includes("://") ? path : "/";
}
