import { Key, OperateArea } from "../design-system";
import { useProductionApi } from "../api/production-api";
import { AssignmentPlate } from "../components/work/AssignmentPlate";
import { CeremonyUnavailable } from "../components/shell/SessionChrome";
import {
  PRODUCTION_DESTINATIONS,
  productionDestinationUnavailableCopy,
  type ProductionDestinationId,
} from "../router/production-navigation";

const HOME_WELLS: Array<{
  id: Exclude<ProductionDestinationId, "home">;
  note: string;
  always: boolean;
}> = [
  {
    id: "activities",
    note: "Create and resume Assessment Campaign drafts for this organization.",
    always: true,
  },
  {
    id: "my-work",
    note: "Open current Assignments and prepare a Submission version.",
    always: true,
  },
  {
    id: "review",
    note: "Open assigned Review work. Evaluation, Human revision, and Review decision stay distinct.",
    always: false,
  },
  {
    id: "release",
    note: "Open assigned Release work. Release remains independent of Review approval.",
    always: false,
  },
  {
    id: "results",
    note: "Open Results the server has made visible to this relationship.",
    always: false,
  },
];

export function ProductionHomePage() {
  const { shell } = useProductionApi();
  const available = new Set(
    (shell?.navigation ?? [])
      .filter((item) => item.is_available)
      .map((item) => item.destination_id),
  );
  const wells = HOME_WELLS.filter((well) => well.always || available.has(well.id));
  const hasOpenDestination = wells.some((well) => available.has(well.id));

  if (!hasOpenDestination) {
    return (
      <CeremonyUnavailable
        title="Home"
        description="Current authorized work for this organization."
        note="Activities and My work are not available for the current authorized relationship."
      />
    );
  }

  return (
    <OperateArea
      className="workspace-area work-plane"
      framed={false}
      label="Home"
      title="Home"
      description="Current authorized work for this organization. Open the next safe destination."
    >
      <div className="destination-bays plate-bays--hug">
        {wells.map((well) => {
          const destination = PRODUCTION_DESTINATIONS[well.id];
          const open = available.has(well.id);
          return (
            <AssignmentPlate
              key={well.id}
              label={destination.label}
              rows={[
                { term: "Purpose", value: open ? well.note : productionDestinationUnavailableCopy(well.id), className: "assignment-plate-row--title" },
                { term: "Availability", value: open ? "Available" : "Not available" },
              ]}
              action={open ? (
                <Key variant="open" to={destination.route} ariaLabel={`Open ${destination.label}`}>
                  Open
                </Key>
              ) : undefined}
            />
          );
        })}
      </div>
    </OperateArea>
  );
}
