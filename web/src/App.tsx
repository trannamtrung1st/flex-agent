import { ErrorBoundary } from "./components/ErrorBoundary";

export function App() {
  return (
    <ErrorBoundary>
      <main className="candidate-workspace">
        <p className="candidate-workspace__kicker">Candidate SPA</p>
        <h1>Flex Agent candidate workspace</h1>
        <p>
          This package is the rebuild candidate. Production traffic continues to
          use the frozen legacy SPA until an explicit cutover.
        </p>
      </main>
    </ErrorBoundary>
  );
}
