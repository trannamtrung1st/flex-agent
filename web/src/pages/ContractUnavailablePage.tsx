import { CeremonyUnavailable } from "../design-system";

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
    <CeremonyUnavailable
      title={title}
      note={note}
      recovery={{ label: "Return to Home", to: homeTo }}
    />
  );
}
