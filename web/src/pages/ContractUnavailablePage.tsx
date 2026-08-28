import { CeremonyArea, CeremonyEmpty } from "../components/shell/SessionChrome";
import { Key } from "../design-system";

export function ContractUnavailablePage({
  title,
  note,
  homeTo = "/",
}: {
  title: string;
  note: string;
  homeTo?: string;
}) {
  return (
    <CeremonyArea label={title} title={title}>
      <CeremonyEmpty note={note}>
        <Key variant="open" to={homeTo}>Return to Home</Key>
      </CeremonyEmpty>
    </CeremonyArea>
  );
}
