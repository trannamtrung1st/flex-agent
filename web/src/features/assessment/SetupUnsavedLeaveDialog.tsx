import {
  CeremonyDialog,
  DialogPlate,
  DialogPlateBody,
  DialogPlateFooter,
  DialogPlateHead,
  Key,
  KeyGroup,
} from "../../design-system";

export function SetupUnsavedLeaveDialog({
  open,
  busy,
  canSave,
  titleId,
  onClose,
  onSaveAndLeave,
  onLeaveWithoutSaving,
}: {
  open: boolean;
  busy: boolean;
  canSave: boolean;
  titleId: string;
  onClose: () => void;
  onSaveAndLeave: () => void;
  onLeaveWithoutSaving: () => void;
}) {
  return (
    <CeremonyDialog open={open} onClose={onClose} labelledBy={titleId}>
      <DialogPlate width="wide">
        <DialogPlateHead title="Unsaved changes" titleId={titleId} />
        <DialogPlateBody>
          <p>
            Your latest changes have not been saved. Save them before leaving this page, or leave and discard them.
          </p>
        </DialogPlateBody>
        <DialogPlateFooter
          arrangement="split"
          secondary={(
            <KeyGroup aria-label="Leave options">
              <Key variant="quiet" disabled={busy} onClick={onClose}>
                Stay on page
              </Key>
              <Key variant="quiet" destructive disabled={busy} onClick={onLeaveWithoutSaving}>
                Leave without saving
              </Key>
            </KeyGroup>
          )}
          primary={(
            <Key variant="transmit" disabled={busy || !canSave} onClick={onSaveAndLeave}>
              Save draft and leave
            </Key>
          )}
        />
      </DialogPlate>
    </CeremonyDialog>
  );
}
