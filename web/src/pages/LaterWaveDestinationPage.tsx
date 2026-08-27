import { OperateArea } from "../design-system";

export function LaterWaveDestinationPage({
  title,
  note,
}: {
  title: string;
  note: string;
}) {
  return (
    <OperateArea
      className="workspace-area"
      label={title}
      title={title}
      empty={{ label: "Not connected yet", note }}
    />
  );
}
