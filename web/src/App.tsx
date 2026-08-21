import { BrowserRouter, RouterProvider } from "react-router-dom";
import { BrowserApiProvider } from "./api/browser-api";
import { isProductionApiMode, ProductionApiProvider } from "./api/production-api";
import { AppRoutes } from "./router/routes";
import { productionRouter } from "./router/production-routes";

export function App() {
  if (isProductionApiMode()) {
    return (
      <ProductionApiProvider>
        <RouterProvider router={productionRouter} />
      </ProductionApiProvider>
    );
  }

  return (
    <BrowserApiProvider>
      <BrowserRouter>
        <AppRoutes />
      </BrowserRouter>
    </BrowserApiProvider>
  );
}
