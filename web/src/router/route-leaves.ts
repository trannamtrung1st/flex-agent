import type { ReactNode } from "react";
import type { RouteObject } from "react-router-dom";
import { Navigate } from "react-router-dom";

function joinPath(parent: string, segment: string | undefined, index: boolean | undefined) {
  if (index) return parent || "/";
  if (!segment) return parent || "/";
  if (segment === "*") return "*";
  if (segment.startsWith("/")) return segment;
  if (parent === "/" || parent === "") return `/${segment}`;
  return `${parent.replace(/\/$/, "")}/${segment}`;
}

export function isRedirectElement(element: ReactNode) {
  return Boolean(element && typeof element === "object" && "type" in element && element.type === Navigate);
}

export type RouteLeaf = {
  path: string;
  redirect: boolean;
  layoutHost: boolean;
};

export function collectRouteLeaves(routes: readonly RouteObject[], parent = ""): RouteLeaf[] {
  const leaves: RouteLeaf[] = [];
  for (const route of routes) {
    const path = joinPath(parent, route.path, route.index);
    if (route.children && route.children.length > 0) {
      if (route.element && !isRedirectElement(route.element)) {
        leaves.push({ path, redirect: false, layoutHost: true });
      }
      leaves.push(...collectRouteLeaves(route.children, path === "" ? "/" : path));
      continue;
    }
    leaves.push({
      path: path || "/",
      redirect: isRedirectElement(route.element),
      layoutHost: false,
    });
  }
  return leaves;
}
