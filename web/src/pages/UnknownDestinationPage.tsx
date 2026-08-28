import { CeremonyArea, CeremonyEmpty } from "../components/shell/SessionChrome";
import { Key } from "../design-system";

const UNKNOWN_NOTE = "The current authorized relationship cannot use this locator.";

export function UnknownDestinationPage() {
  return (
    <CeremonyArea label="This destination is not available" title="This destination is not available">
      <CeremonyEmpty note={UNKNOWN_NOTE}>
        <Key variant="open" to="/">Return to Home</Key>
      </CeremonyEmpty>
    </CeremonyArea>
  );
}
