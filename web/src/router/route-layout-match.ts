export function layoutIdForPath<T extends Record<string, string>>(
  pathname: string,
  manifest: T,
): T[keyof T] | undefined {
  if (Object.hasOwn(manifest, pathname)) {
    return manifest[pathname as keyof T];
  }

  let longestPrefix: { pattern: string; id: T[keyof T] } | undefined;
  for (const [pattern, id] of Object.entries(manifest) as [string, T[keyof T]][]) {
    if (pattern === "*") {
      continue;
    }
    if (pattern.includes(":")) {
      const re = new RegExp(`^${pattern.replace(/:[^/]+/g, "[^/]+")}$`);
      if (re.test(pathname)) {
        return id;
      }
    }
    if (pattern !== "/" && pathname.startsWith(`${pattern}/`)) {
      if (!longestPrefix || pattern.length > longestPrefix.pattern.length) {
        longestPrefix = { pattern, id };
      }
    }
  }

  if (longestPrefix) {
    return longestPrefix.id;
  }

  if (Object.hasOwn(manifest, "*")) {
    return manifest["*" as keyof T];
  }

  return undefined;
}
