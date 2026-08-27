import { RouterProvider } from "react-router-dom";
import { ProductionApiProvider } from "./api/production-api";
import { FlexQueryProvider } from "./api/query-client";
import { ErrorBoundary } from "./components/ErrorBoundary";
import { productionRouter } from "./router/production-routes";

export function App() {
  return (
    <ErrorBoundary>
      <FlexQueryProvider>
        <ProductionApiProvider>
          <RouterProvider router={productionRouter} />
        </ProductionApiProvider>
      </FlexQueryProvider>
    </ErrorBoundary>
  );
}
