import { BrowserRouter, RouterProvider } from "react-router-dom";
import { BrowserApiProvider } from "./api/browser-api";
import { isProductionApiMode, ProductionApiProvider } from "./api/production-api";
import { FlexQueryProvider } from "./api/query-client";
import { AppRoutes } from "./router/routes";
import { productionRouter } from "./router/production-routes";

export function App() {
  return (
    <FlexQueryProvider>
      {isProductionApiMode() ? (
        <ProductionApiProvider>
          <RouterProvider router={productionRouter} />
        </ProductionApiProvider>
      ) : (
        <BrowserApiProvider>
          <BrowserRouter>
            <AppRoutes />
          </BrowserRouter>
        </BrowserApiProvider>
      )}
    </FlexQueryProvider>
  );
}
