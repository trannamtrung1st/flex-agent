import { useNavigate } from "react-router";
import { CATALOG_ROUTE } from "../chrome/operator";
import { Key } from "../keys/Key";
import { CeremonyDialog } from "./CeremonyDialog";
import { DialogPlate, DialogPlateBody, DialogPlateFooter, DialogPlateHead } from "./DialogPlate";

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
        <DialogPlateHead title="End prototype session" titleId="signOutTitle" />
        <DialogPlateBody>
          <p>
            Sign out returns to the prototype catalog. This is demonstration behavior only — it is not a production
            authentication guarantee.
          </p>
        </DialogPlateBody>
        <DialogPlateFooter>
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
        </DialogPlateFooter>
      </DialogPlate>
    </CeremonyDialog>
  );
}
