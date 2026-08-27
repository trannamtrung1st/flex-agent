import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect } from "react";
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
  onActivate,
}: {
  open: boolean;
  onClose: () => void;
  campaign: Campaign;
  onActivate: (config: CampaignForm) => void;
}) {
  const form = useForm<CampaignForm>({
    resolver: zodResolver(campaignSchema),
    defaultValues: campaign.config,
    mode: "onChange",
    reValidateMode: "onChange",
  });

  useEffect(() => {
    if (open) form.reset(campaign.config);
  }, [campaign.config, form, open]);

  const harness = form.watch("harness");
  const agent = form.watch("agent");

  return (
    <CeremonyDialog open={open} onClose={onClose} labelledBy="configTitle" id="configDialog" variant="ceremony">
        <form
          onSubmit={form.handleSubmit((values) => {
            onActivate(values);
            onClose();
          })}
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
            <p className="ceremony-note">
              Activation freezes this configuration for the whole cohort. Every participant sits the same examination.
            </p>
            </DialogPlateBody>
            <DialogPlateFooter className="ceremony-foot">
            <Key id="cancelKey" type="button" onClick={onClose}>
              Cancel
            </Key>
            <Key id="activateKey" variant="activate" type="submit" disabled={campaign.frozen}>
              {campaign.frozen ? "Activated" : "Activate"}
            </Key>
            </DialogPlateFooter>
          </DialogPlate>
        </form>
    </CeremonyDialog>
  );
}
