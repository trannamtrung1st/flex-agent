import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { DesignLabHome } from "./gallery";
import "../styles/app.css";
import "../styles/components.css";

const rootElement = document.getElementById("root");
if (!rootElement) {
  throw new Error("Root element #root was not found.");
}

createRoot(rootElement).render(
  <StrictMode>
    <DesignLabHome />
  </StrictMode>,
);
