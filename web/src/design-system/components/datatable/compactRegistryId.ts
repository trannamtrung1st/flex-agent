/** Center-truncate a hyphenated registry identifier, keeping head and tail. */
export function compactRegistryId(resourceId: string) {
  const segments = resourceId.split("-");
  if (segments.length < 2) {
    return resourceId;
  }
  const head = segments[0];
  const tail = segments[segments.length - 1];
  const compactTail = tail.length > 6 ? tail.slice(-6) : tail;
  return `${head}…${compactTail}`;
}
