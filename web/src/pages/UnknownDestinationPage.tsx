import { CeremonyUnavailable } from "../design-system";

const UNKNOWN_NOTE = "The current authorized relationship cannot use this locator.";

export function UnknownDestinationPage() {
  return (
    <CeremonyUnavailable
      title="This destination is not available"
      note={UNKNOWN_NOTE}
      recovery={{ label: "Return to Home", to: "/" }}
    />
  );
}
