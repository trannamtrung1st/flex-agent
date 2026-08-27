export type ErrorSummaryItem = string | { message: string; href?: string };

interface ErrorSummaryProps {
  title?: string;
  errors: ErrorSummaryItem[];
  headingId?: string;
}

function itemMessage(error: ErrorSummaryItem) {
  return typeof error === "string" ? error : error.message;
}

function itemHref(error: ErrorSummaryItem) {
  return typeof error === "string" ? undefined : error.href;
}

export function ErrorSummary({ title = "There is a problem", errors, headingId = "error-summary-title" }: ErrorSummaryProps) {
  if (errors.length === 0) {
    return null;
  }

  return (
    <div className="error-summary" role="alert" aria-labelledby={headingId}>
      <div className="advisory advisory--attention">
        <span className="advisory-label">Error</span>
        <h2 id={headingId} className="error-summary-title" tabIndex={-1}>{title}</h2>
      </div>
      <ul className="error-summary-list">
        {errors.map((error) => {
          const message = itemMessage(error);
          const href = itemHref(error);
          return (
            <li key={`${href ?? ""}:${message}`}>
              {href ? <a href={href}>{message}</a> : message}
            </li>
          );
        })}
      </ul>
    </div>
  );
}
