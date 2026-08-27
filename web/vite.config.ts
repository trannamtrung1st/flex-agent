import react from "@vitejs/plugin-react";
import { defineConfig } from "vitest/config";
import { designLabSpaPlugin } from "./vite-design-lab-spa";

const devApiProxy = process.env.VITE_DEV_API_PROXY ?? "http://localhost:8080";

export default defineConfig({
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
});
