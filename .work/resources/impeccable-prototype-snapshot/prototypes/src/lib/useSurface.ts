import { useEffect } from "react";

export type SurfaceName =
  | "surfaces-index"
  | "participant-home"
  | "participant-journey"
  | "participant-session"
  | "admin-console"
  | "reviewer-console"
  | "gallery"
  | "not-found";

const VIEW_CLASSES = new Set(["view-queue", "view-record", "view-unfolding"]);

function setBodyViewClass(bodyClass?: string) {
  document.body.classList.forEach((cls) => {
    if (VIEW_CLASSES.has(cls)) document.body.classList.remove(cls);
  });
  if (bodyClass) document.body.classList.add(bodyClass);
}

export function useSurface(name: SurfaceName, bodyClass?: string) {
  useEffect(() => {
    const root = document.documentElement;
    root.dataset.surface = name;
    setBodyViewClass(bodyClass);
    document.title = titleFor(name);
    return () => {
      delete root.dataset.surface;
      if (bodyClass) document.body.classList.remove(bodyClass);
    };
  }, [name, bodyClass]);
}

function titleFor(name: SurfaceName) {
  switch (name) {
    case "surfaces-index":
      return "Flex Agent — Channel Index";
    case "participant-home":
      return "Flex Agent — Home";
    case "participant-journey":
      return "Flex Agent — Assignment";
    case "participant-session":
      return "Flex Agent — Examination Session 07";
    case "admin-console":
      return "Flex Agent — Administration";
    case "reviewer-console":
      return "Flex Agent — Review";
    case "gallery":
      return "Flex Agent — Component Deck";
    default:
      return "Flex Agent — Prototypes";
  }
}
