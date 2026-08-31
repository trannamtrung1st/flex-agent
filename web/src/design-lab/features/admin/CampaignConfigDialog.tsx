import { zodResolver } from "@hookform/resolvers/zod";
import { useEffect, useState } from "react";
import { useForm, type FieldErrors } from "react-hook-form";
import {
  CampaignCeremonyBody,
  CampaignCeremonyConfigGrid,
  CampaignCeremonyFootActions,
  CampaignCeremonyFootRow,
  CampaignCeremonyFooter,
  CampaignCeremonyHead,
  CampaignCeremonyNote,
  CampaignCeremonyDialog,
  CampaignCeremonyPlate,
  DropdownSelect,
  EllipsisKey,
  ErrorSummary,
  FieldInput,
  FormField,
  FormSection,
  FrozenLine,
  Grid,
  Key,
  COOLDOWN_PLACEHOLDER,
  MAX_ATTEMPTS_PLACEHOLDER,
  MM_SS_HINT,
  MM_SS_PLACEHOLDER,
  MM_SS_WARNING_PLACEHOLDER,
  Stack,
  type ErrorSummaryItem,
} from "../../components";
import type { Campaign } from "../../data/types";
import { campaignSchema, type CampaignForm } from "./campaignSchema";

const READINESS_SUMMARY_ID = "config-readiness-summary";

const FIELD_HREFS = {
  sessionLimit: "#sessionLimit",
  timeWarning: "#timeWarning",
  maxAttempts: "#maxAttempts",
  cooldown: "#cooldown",
} as const;

function readinessSummaryErrors(errors: FieldErrors<CampaignForm>): ErrorSummaryItem[] {
  return (Object.keys(FIELD_HREFS) as Array<keyof typeof FIELD_HREFS>).flatMap((field) => {
    const message = errors[field]?.message;
    if (!message) return [];
    return [{ message: String(message), href: FIELD_HREFS[field] }];
  });
}

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

  useEffect(() => {
    if (readiness !== "blocked") return;
    let inner = 0;
    const outer = window.requestAnimationFrame(() => {
      inner = window.requestAnimationFrame(() => {
        document.getElementById(READINESS_SUMMARY_ID)?.focus();
      });
    });
    return () => {
      window.cancelAnimationFrame(outer);
      window.cancelAnimationFrame(inner);
    };
  }, [readiness, form.formState.submitCount]);

  const harness = form.watch("harness");
  const agent = form.watch("agent");
  const blockedErrors = readiness === "blocked" ? readinessSummaryErrors(form.formState.errors) : [];

  return (
    <CampaignCeremonyDialog open={open} onClose={onClose} labelledBy="configTitle" id="configDialog">
        <form
          onSubmit={(event) => event.preventDefault()}
          noValidate
        >
          <CampaignCeremonyPlate frozen={campaign.frozen}>
            <CampaignCeremonyHead title="Campaign Configuration" titleId="configTitle" />
            <CampaignCeremonyBody>
            <Stack gap="6">
            {blockedErrors.length > 0 ? (
              <ErrorSummary
                title="Readiness blocked"
                headingId={READINESS_SUMMARY_ID}
                errors={blockedErrors}
              />
            ) : null}
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
              <CampaignCeremonyConfigGrid>
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
              </CampaignCeremonyConfigGrid>
            </FormSection>
            {campaign.frozen ? <FrozenLine>Configuration frozen at activation</FrozenLine> : null}
            </Stack>
            <CampaignCeremonyNote>
              {confirming
                ? "Confirm activation. This design lab freezes local state only; production revalidates and activates on the server."
                : "Save a draft and check readiness before activation. Browser frozen state is never authority."}
            </CampaignCeremonyNote>
            </CampaignCeremonyBody>
            <CampaignCeremonyFooter>
            <CampaignCeremonyFootActions>
            {readinessCopy ? <CampaignCeremonyNote role="status">{readinessCopy}</CampaignCeremonyNote> : null}
              {confirming ? (
                <CampaignCeremonyFootRow aria-label="Configuration step">
                  <Key id="cancelKey" type="button" onClick={() => setConfirming(false)}>
                    Back
                  </Key>
                </CampaignCeremonyFootRow>
              ) : (
                <CampaignCeremonyFootRow aria-label="Draft actions">
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
                </CampaignCeremonyFootRow>
              )}
              {campaign.frozen ? (
                <CampaignCeremonyFootRow aria-label="Activation status">
                  <EllipsisKey id="activateKey" variant="activate" type="button" disabled>
                    Activated
                  </EllipsisKey>
                </CampaignCeremonyFootRow>
              ) : confirming ? (
                <CampaignCeremonyFootRow aria-label="Activation commit">
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
                </CampaignCeremonyFootRow>
              ) : (
                <CampaignCeremonyFootRow aria-label="Readiness and activation">
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
                          setReadinessCopy("");
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
                    onClick={() => {
                      setConfirming(true);
                      setReadinessCopy("");
                    }}
                  >
                    Confirm activation
                  </EllipsisKey>
                </CampaignCeremonyFootRow>
              )}
            </CampaignCeremonyFootActions>
            </CampaignCeremonyFooter>
          </CampaignCeremonyPlate>
        </form>
    </CampaignCeremonyDialog>
  );
}
