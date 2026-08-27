import { useNavigate } from "react-router-dom";
import { CATALOG_ROUTE } from "../chrome/operator";
import { Key, KeyGroup } from "../../../design-system/components/keys";
import { CeremonyDialog } from "../../../design-system/components/overlays/CeremonyDialog";
import {
  DialogPlate,
  DialogPlateBody,
  DialogPlateFooter,
  DialogPlateHead,
} from "../../../design-system/components/overlays/DialogPlate";

export function SignOutCeremony({
  open,
  onClose,
}: {
  open: boolean;
  onClose: () => void;
}) {
  const navigate = useNavigate();
  return (
    <CeremonyDialog open={open} onClose={onClose} labelledBy="signOutTitle" id="signOutDialog">
      <DialogPlate width="narrow">
        <DialogPlateHead title="End design-lab session" titleId="signOutTitle" />
        <DialogPlateBody>
          <p>
            Sign out returns to the channel catalog. This is demonstration behavior only — it is not a production
            authentication guarantee.
          </p>
        </DialogPlateBody>
        <DialogPlateFooter>
          <KeyGroup>
            <Key variant="quiet" onClick={onClose}>
              Remain signed in
            </Key>
            <Key
              variant="quiet"
              onClick={() => {
                onClose();
                navigate(CATALOG_ROUTE);
              }}
            >
              Sign out
            </Key>
          </KeyGroup>
        </DialogPlateFooter>
      </DialogPlate>
    </CeremonyDialog>
  );
}
