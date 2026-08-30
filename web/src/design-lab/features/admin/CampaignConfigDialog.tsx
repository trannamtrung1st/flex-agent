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
  EllipsisKey,
  FieldInput,
  FormField,
  FormSection,
  Grid,
  Key,
  KeyGroup,
  COOLDOWN_PLACEHOLDER,
  MAX_ATTEMPTS_PLACEHOLDER,
  MM_SS_HINT,
  MM_SS_PLACEHOLDER,
  MM_SS_WARNING_PLACEHOLDER,
  Stack,
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
          <DialogPlate width="wide" className={`ceremony-plate${campaign.frozen ? " is-frozen" : ""}`}>
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
            <Stack gap="6">
            <FormSection legend="Agent and Harness">
              <Grid gap="4" minItemWidth="control">
                <FormField id="harnessSelect" label="Harness" layout="stack" labelAssociatesControl={false}>
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
                <FormField id="agentSelect" label="Agent identity" layout="stack" labelAssociatesControl={false}>
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
              </Grid>
            </FormSection>
            <FormSection legend="Timing and attempts">
              <Grid gap="4" minItemWidth="control" className="ceremony-config-grid">
                <FormField
                  id="sessionLimit"
                  label="Session limit"
                  hint={campaign.frozen ? undefined : MM_SS_HINT}
                  error={form.formState.errors.sessionLimit?.message}
                  layout="stack"
                >
                  {(controlProps) => (
                    <FieldInput
                      {...form.register("sessionLimit")}
                      {...controlProps}
                      placeholder={MM_SS_PLACEHOLDER}
                      frozen={campaign.frozen}
                    />
                  )}
                </FormField>
                <FormField
                  id="timeWarning"
                  label="Time warning at"
                  hint={campaign.frozen ? undefined : MM_SS_HINT}
                  error={form.formState.errors.timeWarning?.message}
                  layout="stack"
                >
                  {(controlProps) => (
                    <FieldInput
                      {...form.register("timeWarning")}
                      {...controlProps}
                      placeholder={MM_SS_WARNING_PLACEHOLDER}
                      frozen={campaign.frozen}
                    />
                  )}
                </FormField>
                <FormField
                  id="maxAttempts"
                  label="Max attempts"
                  error={form.formState.errors.maxAttempts?.message}
                  layout="stack"
                >
                  {(controlProps) => (
                    <FieldInput
                      {...form.register("maxAttempts")}
                      {...controlProps}
                      width="narrow"
                      placeholder={MAX_ATTEMPTS_PLACEHOLDER}
                      frozen={campaign.frozen}
                    />
                  )}
                </FormField>
                <FormField id="cooldown" label="Cooldown" layout="stack">
                  {(controlProps) => (
                    <FieldInput
                      {...form.register("cooldown")}
                      {...controlProps}
                      width="narrow"
                      placeholder={COOLDOWN_PLACEHOLDER}
                      frozen={campaign.frozen}
                    />
                  )}
                </FormField>
              </Grid>
            </FormSection>
            {campaign.frozen ? <p className="frozen-line">Configuration frozen at activation</p> : null}
            {readinessCopy ? <p className="ceremony-note" role="status">{readinessCopy}</p> : null}
            <p className="ceremony-note">
              {confirming
                ? "Confirm activation. This design lab freezes local state only; production revalidates and activates on the server."
                : "Save a draft and check readiness before activation. Browser frozen state is never authority."}
            </p>
            </Stack>
            </DialogPlateBody>
            <DialogPlateFooter className="ceremony-foot">
            <Stack gap="3" className="ceremony-foot-actions">
              {confirming ? (
                <KeyGroup className="ceremony-foot-row" aria-label="Configuration step">
                  <Key id="cancelKey" type="button" onClick={() => setConfirming(false)}>
                    Back
                  </Key>
                </KeyGroup>
              ) : (
                <KeyGroup className="ceremony-foot-row" aria-label="Draft actions">
                  <Key id="cancelKey" type="button" onClick={onClose}>
                    Cancel
                  </Key>
                  {!campaign.frozen ? (
                    <EllipsisKey
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
                    </EllipsisKey>
                  ) : null}
                </KeyGroup>
              )}
              {campaign.frozen ? (
                <KeyGroup className="ceremony-foot-row" aria-label="Activation status">
                  <EllipsisKey id="activateKey" variant="activate" type="button" disabled>
                    Activated
                  </EllipsisKey>
                </KeyGroup>
              ) : confirming ? (
                <KeyGroup className="ceremony-foot-row" aria-label="Activation commit">
                  <EllipsisKey
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
                  </EllipsisKey>
                </KeyGroup>
              ) : (
                <KeyGroup className="ceremony-foot-row" aria-label="Readiness and activation">
                  <EllipsisKey
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
                  </EllipsisKey>
                  <EllipsisKey
                    id="activateKey"
                    variant="activate"
                    type="button"
                    disabled={readiness !== "ready"}
                    disabledReason={readiness !== "ready" ? "Check readiness before activation" : undefined}
                    onClick={() => setConfirming(true)}
                  >
                    Confirm activation
                  </EllipsisKey>
                </KeyGroup>
              )}
            </Stack>
            </DialogPlateFooter>
          </DialogPlate>
        </form>
    </CeremonyDialog>
  );
}
