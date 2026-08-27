import react from "@vitejs/plugin-react";
import { defineConfig, type Plugin } from "vite";

function rewriteDesignLabHtml(req: { url?: string }) {
  const [pathname, search = ""] = (req.url ?? "").split("?");
  const query = search ? `?${search}` : "";
  const isDevInternal =
    pathname.startsWith("/@") ||
    pathname.startsWith("/src/") ||
    pathname.startsWith("/node_modules/") ||
    pathname.startsWith("/assets/");
  if (isDevInternal || pathname.includes(".")) {
    return;
  }
  req.url = `/design-lab.html${query}`;
}

function designLabSpa(): Plugin {
  return {
    name: "design-lab-spa",
    configureServer(server) {
      server.middlewares.use((req, _res, next) => {
        rewriteDesignLabHtml(req);
        next();
      });
    },
    configurePreviewServer(server) {
      server.middlewares.use((req, _res, next) => {
        rewriteDesignLabHtml(req);
        next();
      });
    },
  };
}

export default defineConfig({
  plugins: [react(), designLabSpa()],
  server: {
    port: 5275,
    strictPort: true,
  },
  preview: {
    port: 5275,
    strictPort: true,
    host: "127.0.0.1",
  },
  build: {
    outDir: "dist-design-lab",
    sourcemap: false,
    rollupOptions: {
      input: "design-lab.html",
    },
  },
});
