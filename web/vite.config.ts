import react from "@vitejs/plugin-react";
import { defineConfig } from "vitest/config";

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5274,
    strictPort: true,
    proxy: {
      "/browser": {
        target: "http://localhost:8080",
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
  test: {
    environment: "jsdom",
    setupFiles: ["./vitest.setup.ts"],
    globals: true,
    exclude: ["**/node_modules/**", "**/dist/**", "**/dist-design-lab/**", "src/design-lab/**", "e2e/**"],
  },
});
