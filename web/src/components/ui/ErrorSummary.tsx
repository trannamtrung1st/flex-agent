interface ErrorSummaryProps {
  title?: string;
  errors: string[];
}

export function ErrorSummary({ title = "There is a problem", errors }: ErrorSummaryProps) {
  if (errors.length === 0) {
    return null;
  }

  return (
    <div className="error-summary" role="alert" aria-live="polite">
      <p className="error-summary-title">{title}</p>
      <ul className="error-summary-list">
        {errors.map((error) => (
          <li key={error}>{error}</li>
        ))}
      </ul>
    </div>
  );
}
