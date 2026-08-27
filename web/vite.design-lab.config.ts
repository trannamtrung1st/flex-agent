import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";
import { designLabSpaPlugin } from "./vite-design-lab-spa";

export default defineConfig({
  plugins: [react(), designLabSpaPlugin("all")],
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
