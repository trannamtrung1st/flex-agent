import type { Plugin } from "vite";

const DESIGN_LAB_HTML_ENTRY = "design-lab.html";

export type DesignLabSpaRewriteMode = "all" | "prefixed";

function isViteInternalPath(pathname: string) {
  return (
    pathname.startsWith("/@")
    || pathname.startsWith("/src/")
    || pathname.startsWith("/node_modules/")
    || pathname.startsWith("/assets/")
  );
}

export function shouldRewriteToDesignLabEntry(pathname: string, mode: DesignLabSpaRewriteMode) {
  if (isViteInternalPath(pathname) || pathname.includes(".")) {
    return false;
  }

  if (mode === "all") {
    return true;
  }

  return pathname === "/design-lab" || pathname.startsWith("/design-lab/");
}

function rewriteDesignLabHtml(req: { url?: string }, mode: DesignLabSpaRewriteMode) {
  const [pathname, search = ""] = (req.url ?? "").split("?");
  if (!shouldRewriteToDesignLabEntry(pathname, mode)) {
    return;
  }

  req.url = `/${DESIGN_LAB_HTML_ENTRY}${search ? `?${search}` : ""}`;
}

export function designLabSpaPlugin(mode: DesignLabSpaRewriteMode): Plugin {
  return {
    name: "design-lab-spa",
    configureServer(server) {
      server.middlewares.use((req, _res, next) => {
        rewriteDesignLabHtml(req, mode);
        next();
      });
    },
    configurePreviewServer(server) {
      server.middlewares.use((req, _res, next) => {
        rewriteDesignLabHtml(req, mode);
        next();
      });
    },
  };
}
