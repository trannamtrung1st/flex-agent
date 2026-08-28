import { useMemo } from "react";
import { RouterProvider } from "react-router-dom";
import { ProductionApiProvider } from "./api/production-api";
import { FlexQueryProvider } from "./api/query-client";
import { ErrorBoundary } from "./components/ErrorBoundary";
import { createProductionRouter } from "./router/production-routes";

export function App() {
  const router = useMemo(() => createProductionRouter(), []);
  return (
    <ErrorBoundary>
      <FlexQueryProvider>
        <ProductionApiProvider>
          <RouterProvider router={router} />
        </ProductionApiProvider>
      </FlexQueryProvider>
    </ErrorBoundary>
  );
}
