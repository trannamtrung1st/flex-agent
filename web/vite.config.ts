import react from "@vitejs/plugin-react";
import { loadEnv } from "vite";
import { defineConfig } from "vitest/config";
import { designLabSpaPlugin } from "./vite-design-lab-spa";

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), "");
  const configuredProxy = env.VITE_DEV_API_PROXY;
  const devApiProxy = configuredProxy ? configuredProxy : "http://localhost:8080";

  return {
    plugins: [react(), designLabSpaPlugin("prefixed")],
    server: {
      port: 5274,
      strictPort: true,
      proxy: {
        "/browser": {
          target: devApiProxy,
          changeOrigin: true,
        },
        "/auth": {
          target: devApiProxy,
          changeOrigin: true,
        },
        "/v1": {
          target: devApiProxy,
          changeOrigin: true,
        },
        "/v2": {
          target: devApiProxy,
          changeOrigin: true,
        },
        "/sessions": {
          target: devApiProxy,
          changeOrigin: true,
          bypass(req) {
            const path = (req.url ?? "").split("?")[0] ?? "";
            if (/^\/sessions\/[^/]+\/events(?:\/|$)/.test(path)) {
              return;
            }
            return "/index.html";
          },
        },
      },
    },
    build: {
      outDir: "dist",
      sourcemap: false,
      rollupOptions: {
        input: "index.html",
      },
    },
    // Design-lab sources live under src/design-lab/** and use vite.design-lab.config.ts.
    // Candidate dev also serves /design-lab/* anonymously via the prefixed SPA rewrite.
    test: {
      environment: "jsdom",
      setupFiles: ["./vitest.setup.ts"],
      globals: true,
      exclude: ["**/node_modules/**", "**/dist/**", "**/dist-design-lab/**", "src/design-lab/**", "e2e/**"],
    },
  };
});
