import { formatViewerInstant } from "../../../lib/format";

function isoFromValue(value?: string | Date | null) {
  if (value instanceof Date) {
    return Number.isNaN(value.getTime()) ? undefined : value.toISOString();
  }
  return value;
}

export function InstantReadout({
  value,
  timeZone,
}: {
  value?: string | Date | null;
  timeZone?: string;
}) {
  const display = formatViewerInstant(isoFromValue(value), timeZone);
  if (!display.datetime) {
    return (
      <span title={display.title}>
        <span className="visually-hidden">{display.title}</span>
        <span aria-hidden="true">{display.label}</span>
      </span>
    );
  }

  return (
    <time dateTime={display.datetime} title={display.title}>
      {display.label}
    </time>
  );
}
