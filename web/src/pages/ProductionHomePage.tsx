import { Navigate } from "react-router-dom";
import { AssignmentPlate, Grid, Key, OperateArea } from "../design-system";
import { useProductionApi } from "../api/production-api";
import { CeremonyUnavailable } from "../components/shell/SessionChrome";
import {
  PRODUCTION_DESTINATIONS,
  isProductionDestinationOpen,
  type ProductionDestinationId,
} from "../router/production-navigation";

const HOME_WELLS: Array<{
  id: Exclude<ProductionDestinationId, "home">;
  note: string;
}> = [
  {
    id: "activities",
    note: "Create and resume Assessment Campaign drafts for this organization.",
  },
  {
    id: "review",
    note: "Open assigned Review work. Evaluation, Human revision, and Review decision stay distinct.",
  },
  {
    id: "release",
    note: "Open assigned Release work. Release remains independent of Review approval.",
  },
  {
    id: "results",
    note: "Open Results the server has made visible to this relationship.",
  },
];

const ADMIN_HOME_DESCRIPTION =
  "Current authorized work for this organization. Open the next safe destination.";

function DestinationPlate({
  id,
  note,
}: {
  id: Exclude<ProductionDestinationId, "home">;
  note: string;
}) {
  const destination = PRODUCTION_DESTINATIONS[id];
  return (
    <AssignmentPlate
      label={destination.label}
      rows={[
        { term: "Purpose", value: note, className: "readout--title" },
        { term: "Availability", value: "Available" },
      ]}
      action={(
        <Key variant="open" to={destination.route} ariaLabel={`Open ${destination.label}`}>
          Open
        </Key>
      )}
    />
  );
}

export function ProductionHomePage() {
  const { shell } = useProductionApi();
  if (isProductionDestinationOpen(shell?.navigation, "my-work")) {
    return <Navigate to="/my-work" replace />;
  }

  const available = new Set(
    (shell?.navigation ?? [])
      .filter((item) => item.is_available)
      .map((item) => item.destination_id),
  );
  const wells = HOME_WELLS.filter((well) => available.has(well.id));

  if (wells.length === 0) {
    return (
      <CeremonyUnavailable
        title="Home"
        description="Current authorized work for this organization."
        note="Authorized work is not available for the current authorized relationship."
      />
    );
  }

  return (
    <OperateArea
      className="workspace-area work-plane"
      framed={false}
      label="Home"
      title="Home"
      description={ADMIN_HOME_DESCRIPTION}
    >
      <Grid gap="4" minItemWidth="control" fit="fill">
        {wells.map((well) => (
          <DestinationPlate key={well.id} id={well.id} note={well.note} />
        ))}
      </Grid>
    </OperateArea>
  );
}
