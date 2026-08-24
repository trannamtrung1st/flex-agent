interface ErrorSummaryProps {
  title?: string;
  errors: string[];
  headingId?: string;
}

export function ErrorSummary({ title = "There is a problem", errors, headingId = "error-summary-title" }: ErrorSummaryProps) {
  if (errors.length === 0) {
    return null;
  }

  return (
    <div className="error-summary" role="alert" aria-labelledby={headingId}>
      <h2 id={headingId} className="error-summary-title" tabIndex={-1}>{title}</h2>
      <ul className="error-summary-list">
        {errors.map((error) => (
          <li key={error}>{error}</li>
        ))}
      </ul>
    </div>
  );
}
