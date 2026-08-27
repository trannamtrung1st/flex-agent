import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import {
  CeremonyDialog,
  DialogPlate,
  DialogPlateBody,
  DialogPlateFooter,
  DialogPlateHead,
  DropdownSelect,
  FieldInput,
  FormField,
  Key,
  MM_SS_HINT,
} from "../../components";
import type { Campaign } from "../../data/types";
import { campaignSchema, type CampaignForm } from "./campaignSchema";

export function CampaignConfigDialog({
  open,
  onClose,
  campaign,
  onSaveDraft,
  onActivate,
}: {
  open: boolean;
  onClose: () => void;
  campaign: Campaign;
  onSaveDraft: (config: CampaignForm) => void;
  onActivate: (config: CampaignForm) => void;
}) {
  const form = useForm<CampaignForm>({
    resolver: zodResolver(campaignSchema),
    defaultValues: campaign.config,
    mode: "onChange",
    reValidateMode: "onChange",
  });
  const [readiness, setReadiness] = useState<"idle" | "ready" | "blocked">("idle");
  const [confirming, setConfirming] = useState(false);
  const [readinessCopy, setReadinessCopy] = useState("");

  useEffect(() => {
    if (!open) return;
    form.reset(campaign.config);
    setReadiness("idle");
    setConfirming(false);
    setReadinessCopy("");
  }, [campaign.id, form, open]);

  const harness = form.watch("harness");
  const agent = form.watch("agent");

  return (
    <CeremonyDialog open={open} onClose={onClose} labelledBy="configTitle" id="configDialog" variant="ceremony">
        <form
          onSubmit={(event) => event.preventDefault()}
          noValidate
        >
          <DialogPlate className={`ceremony-plate${campaign.frozen ? " is-frozen" : ""}`}>
            <DialogPlateHead
              title="Campaign Configuration"
              titleId="configTitle"
              marker={false}
              className="ceremony-head"
              titleClassName="ceremony-title"
            >
            <span className="ceremony-trace" aria-hidden="true">
              <span className="ceremony-trace-node" />
            </span>
            </DialogPlateHead>
            <DialogPlateBody className="ceremony-body">
            <FormField id="harnessSelect" label="Harness" labelAssociatesControl={false}>
              {(controlProps, { labelId }) => (
                <DropdownSelect
                  id={controlProps.id}
                  labelId={labelId}
                  describedBy={controlProps["aria-describedby"]}
                  value={harness}
                  frozen={campaign.frozen}
                  options={["GOVERNED-EXAM-01", "GOVERNED-EXAM-02", "GOVERNED-AUDIT-01"]}
                  onChange={(v) => form.setValue("harness", v)}
                />
              )}
            </FormField>
            <FormField id="agentSelect" label="Agent identity" labelAssociatesControl={false}>
              {(controlProps, { labelId }) => (
                <DropdownSelect
                  id={controlProps.id}
                  labelId={labelId}
                  describedBy={controlProps["aria-describedby"]}
                  value={agent}
                  frozen={campaign.frozen}
                  options={["EXAMINER-CORE", "EXAMINER-STRUCT", "EXAMINER-OPS"]}
                  onChange={(v) => form.setValue("agent", v)}
                />
              )}
            </FormField>
            <span className="form-divider" aria-hidden="true" />
            <div className="form-row form-row--pair">
              <FormField
                id="sessionLimit"
                label="Session limit"
                hint={campaign.frozen ? undefined : MM_SS_HINT}
                error={form.formState.errors.sessionLimit?.message}
                className="field-pair"
              >
                {(controlProps) => (
                  <FieldInput {...form.register("sessionLimit")} {...controlProps} frozen={campaign.frozen} />
                )}
              </FormField>
              <FormField
                id="timeWarning"
                label="Time warning at"
                hint={campaign.frozen ? undefined : MM_SS_HINT}
                error={form.formState.errors.timeWarning?.message}
                className="field-pair"
              >
                {(controlProps) => (
                  <FieldInput {...form.register("timeWarning")} {...controlProps} frozen={campaign.frozen} />
                )}
              </FormField>
            </div>
            <div className="form-row form-row--pair">
              <FormField
                id="maxAttempts"
                label="Max attempts"
                error={form.formState.errors.maxAttempts?.message}
                className="field-pair"
              >
                {(controlProps) => (
                  <FieldInput {...form.register("maxAttempts")} {...controlProps} width="narrow" frozen={campaign.frozen} />
                )}
              </FormField>
              <FormField id="cooldown" label="Cooldown" className="field-pair">
                {(controlProps) => (
                  <FieldInput {...form.register("cooldown")} {...controlProps} width="narrow" frozen={campaign.frozen} />
                )}
              </FormField>
            </div>
            <span className="form-divider" aria-hidden="true" />
            {campaign.frozen ? <p className="frozen-line">Configuration frozen at activation</p> : null}
            {readinessCopy ? <p className="ceremony-note" role="status">{readinessCopy}</p> : null}
            <p className="ceremony-note">
              {confirming
                ? "Confirm activation. This design lab freezes local state only; production revalidates and activates on the server."
                : "Save a draft and check readiness before activation. Browser frozen state is never authority."}
            </p>
            </DialogPlateBody>
            <DialogPlateFooter className="ceremony-foot">
            <Key id="cancelKey" type="button" onClick={confirming ? () => setConfirming(false) : onClose}>
              {confirming ? "Back" : "Cancel"}
            </Key>
            {campaign.frozen ? (
              <Key id="activateKey" variant="activate" type="button" disabled>
                Activated
              </Key>
            ) : confirming ? (
              <Key
                id="activateKey"
                variant="activate"
                type="button"
                onClick={() => {
                  void form.handleSubmit((values) => {
                    onActivate(values);
                    onClose();
                  })();
                }}
              >
                Activate campaign
              </Key>
            ) : (
              <>
                <Key
                  id="saveDraftKey"
                  type="button"
                  onClick={() => {
                    void form.handleSubmit((values) => {
                      onSaveDraft(values);
                      setReadiness("idle");
                      setReadinessCopy("Draft saved locally. Production persists the exact saved revision on the server.");
                    })();
                  }}
                >
                  Save draft
                </Key>
                <Key
                  id="readinessKey"
                  type="button"
                  onClick={() => {
                    void form.handleSubmit(
                      (values) => {
                        onSaveDraft(values);
                        setReadiness("ready");
                        setReadinessCopy("Readiness check passed for this specimen. Production reauthorizes and revalidates on the server.");
                      },
                      () => {
                        setReadiness("blocked");
                        setReadinessCopy("Readiness blocked. Resolve field errors before activation.");
                      },
                    )();
                  }}
                >
                  Check readiness
                </Key>
                <Key
                  id="activateKey"
                  variant="activate"
                  type="button"
                  disabled={readiness !== "ready"}
                  disabledReason={readiness !== "ready" ? "Check readiness before activation" : undefined}
                  onClick={() => setConfirming(true)}
                >
                  Confirm activation
                </Key>
              </>
            )}
            </DialogPlateFooter>
          </DialogPlate>
        </form>
    </CeremonyDialog>
  );
}
