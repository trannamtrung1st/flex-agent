import { BrowserRouter } from "react-router-dom";
import { BrowserApiProvider } from "./api/browser-api";
import { AppRoutes } from "./router/routes";

export function App() {
  return (
    <BrowserApiProvider>
      <BrowserRouter>
        <AppRoutes />
      </BrowserRouter>
    </BrowserApiProvider>
  );
}
