import type { Ref } from "react";
import { Bulkhead, Key } from "../../components";
import type { ReviewSession } from "../../data/types";
import { ManifestPanel, MarginaliaStack } from "./RecordPanels";

export function ReviewerDrawerOverlays({
  session,
  open,
  manifestOpen,
  marginaliaOpen,
  adjustMode,
  activeCriterionId,
  stackRef,
  onCloseManifest,
  onCloseMarginalia,
  onSelectCriterion,
  onCancelAdjust,
  onSaveAdjust,
}: {
  session: ReviewSession;
  open: boolean;
  manifestOpen: boolean;
  marginaliaOpen: boolean;
  adjustMode: boolean;
  activeCriterionId: string | null;
  stackRef: Ref<HTMLDivElement>;
  onCloseManifest: () => void;
  onCloseMarginalia: () => void;
  onSelectCriterion: (id: string) => void;
  onCancelAdjust: () => void;
  onSaveAdjust: () => void;
}) {
  if (!open) return null;
  return (
    <>
      <Bulkhead
        id="recordManifestBulkhead"
        open={manifestOpen}
        onClose={onCloseManifest}
        side="leading"
        title="Session manifest"
        titleId="recordManifestTitle"
      >
        <ManifestPanel session={session} />
      </Bulkhead>
      <Bulkhead
        id="recordMarginaliaBulkhead"
        open={marginaliaOpen}
        onClose={onCloseMarginalia}
        side="trailing"
        wide={adjustMode}
        title="Criterion Marginalia"
        titleId="recordMarginaliaTitle"
        footer={
          adjustMode ? (
            <>
              <Key onClick={onCancelAdjust}>Cancel</Key>
              <Key onClick={onSaveAdjust}>Save adjustment</Key>
            </>
          ) : undefined
        }
      >
        <MarginaliaStack
          ref={stackRef}
          session={session}
          activeCriterionId={activeCriterionId}
          onSelectCriterion={onSelectCriterion}
          showLabel={false}
          adjustMode={adjustMode}
        />
      </Bulkhead>
    </>
  );
}
