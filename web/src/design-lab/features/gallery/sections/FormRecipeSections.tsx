import { useState, type FormEvent } from "react";
import {
  ACCOMMODATION_VALUE_PLACEHOLDER,
  ActivationMark,
  ADJUSTED_RATIONALE_PLACEHOLDER,
  BackKey,
  Breaker,
  CAMPAIGN_TITLE_PLACEHOLDER,
  CompactId,
  COOLDOWN_PLACEHOLDER,
  DatePicker,
  DialogPlate,
  DialogPlateBody,
  DialogPlateFooter,
  DialogPlateHead,
  DropdownSelect,
  ErrorSummary,
  FieldInput,
  FieldNumber,
  FieldTextarea,
  FormField,
  FormSection,
  Grid,
  InstantReadout,
  Key,
  MAX_ATTEMPTS_PLACEHOLDER,
  MM_SS_HINT,
  MM_SS_PLACEHOLDER,
  MM_SS_WARNING_PLACEHOLDER,
  OperateArea,
  PlateFoot,
  ReadoutGrid,
  ReadoutGridField,
  ReadoutGridRow,
  SCORE_PLACEHOLDER,
  Stack,
} from "../../../components";
import { GallerySection, Spec } from "./GallerySection";

const AGENTS = ["EXAMINER-CORE", "EXAMINER-STRUCT", "EXAMINER-OPS"] as const;
const HARNESSES = ["GOVERNED-EXAM-01", "GOVERNED-EXAM-02", "GOVERNED-AUDIT-01"] as const;
const TASKS = ["TASK-STRUCT-01", "TASK-OPS-02"] as const;
const RUBRICS = ["RUBRIC-AUDIT-02", "RUBRIC-OPS-01"] as const;
const MODELS = ["MODEL-EXAM-04", "MODEL-OPS-03"] as const;

function RecipeSelect({
  id,
  label,
  value,
  options,
  onChange,
  frozen = false,
}: {
  id: string;
  label: string;
  value: string;
  options: readonly string[];
  onChange: (value: string) => void;
  frozen?: boolean;
}) {
  return (
    <FormField id={id} layout="stack" label={label} labelAssociatesControl={false}>
      {(control, { labelId }) => (
        <DropdownSelect
          id={control.id}
          labelId={labelId}
          describedBy={control["aria-describedby"]}
          value={value}
          options={[...options]}
          onChange={onChange}
          frozen={frozen}
        />
      )}
    </FormField>
  );
}

function CommissionRecipe({
  idPrefix,
  label,
  initialTitle,
  revealErrors = false,
}: {
  idPrefix: string;
  label: string;
  initialTitle: string;
  revealErrors?: boolean;
}) {
  const [title, setTitle] = useState(initialTitle);
  const [agent, setAgent] = useState<string>(AGENTS[0]);
  const [harness, setHarness] = useState<string>(HARNESSES[0]);
  const [task, setTask] = useState<string>(TASKS[0]);
  const [rubric, setRubric] = useState<string>(RUBRICS[0]);
  const [model, setModel] = useState<string>(MODELS[0]);
  const [submitted, setSubmitted] = useState(false);
  const titleId = `${idPrefix}Title`;
  const summaryId = `${idPrefix}Summary`;
  const titleError = (revealErrors || submitted) && !title.trim() ? "Enter a Campaign title" : undefined;
  const summaryErrors = titleError ? [{ message: titleError, href: `#${titleId}` }] : [];

  const onSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSubmitted(true);
    requestAnimationFrame(() => {
      document.getElementById(summaryId)?.focus();
    });
  };

  return (
    <OperateArea
      className="workspace-area work-plane form-recipe"
      label={label}
      title="Create assessment Campaign"
      description="Activity form: Campaign. Configured type: Assessment."
      back={<BackKey label="Activities" type="button" onClick={() => undefined} />}
    >
      <Stack as="form" gap="6" className="workspace-form setup-ceremony" noValidate onSubmit={onSubmit}>
        <ErrorSummary title="Correct the following" headingId={summaryId} errors={summaryErrors} />
        <FormField id={titleId} layout="stack" label="Campaign title" error={titleError}>
          {(control) => (
            <FieldInput
              {...control}
              width="wide"
              value={title}
              placeholder={CAMPAIGN_TITLE_PLACEHOLDER}
              maxLength={200}
              onChange={(event) => setTitle(event.target.value)}
            />
          )}
        </FormField>
        <FormSection legend="Agent and Harness">
          <Grid gap="4" minItemWidth="control">
            <RecipeSelect id={`${idPrefix}Agent`} label="Agent" value={agent} options={AGENTS} onChange={setAgent} />
            <RecipeSelect
              id={`${idPrefix}Harness`}
              label="Harness"
              value={harness}
              options={HARNESSES}
              onChange={setHarness}
            />
          </Grid>
        </FormSection>
        <FormSection legend="Source set">
          <Grid gap="4" minItemWidth="compact">
            <RecipeSelect
              id={`${idPrefix}Task`}
              label="Task and Submission"
              value={task}
              options={TASKS}
              onChange={setTask}
            />
            <RecipeSelect
              id={`${idPrefix}Rubric`}
              label="Rubric"
              value={rubric}
              options={RUBRICS}
              onChange={setRubric}
            />
            <RecipeSelect
              id={`${idPrefix}Model`}
              label="Model"
              value={model}
              options={MODELS}
              onChange={setModel}
            />
          </Grid>
        </FormSection>
        <PlateFoot className="setup-ceremony__foot" arrangement="end">
          <Key type="submit" variant="transmit" size="large">
            Create
          </Key>
        </PlateFoot>
      </Stack>
    </OperateArea>
  );
}

function InstrumentRecipe() {
  const [limit, setLimit] = useState("60:00");
  const [warning, setWarning] = useState("10:00");
  const [score, setScore] = useState("3");
  const [rationale, setRationale] = useState(
    "Identifies fencing and lease expiry as primary safety mechanisms.",
  );

  return (
    <OperateArea
      className="workspace-area work-plane form-recipe"
      label="Instrument form recipe"
      title="Record adjustment"
      description="Enrollment-scoped score and timing. Other participants never see this adjustment."
    >
      <Stack
        as="form"
        gap="6"
        className="workspace-form setup-ceremony"
        noValidate
        onSubmit={(event) => event.preventDefault()}
      >
        <FormSection legend="Timing">
          <div className="form-row form-row--pair">
            <FormField id="recipePairLimit" label="Session limit" hint={MM_SS_HINT} layout="pair">
              {(control) => (
                <FieldInput
                  {...control}
                  value={limit}
                  placeholder={MM_SS_PLACEHOLDER}
                  onChange={(event) => setLimit(event.target.value)}
                />
              )}
            </FormField>
            <FormField id="recipePairWarning" label="Time warning at" hint={MM_SS_HINT} layout="pair">
              {(control) => (
                <FieldInput
                  {...control}
                  value={warning}
                  placeholder={MM_SS_WARNING_PLACEHOLDER}
                  onChange={(event) => setWarning(event.target.value)}
                />
              )}
            </FormField>
          </div>
        </FormSection>
        <FormSection legend="Adjustment">
          <Stack gap="4">
            <FormField id="recipePairScore" label="Adjusted score" layout="stack">
              {(control) => (
                <FieldNumber
                  {...control}
                  width="narrow"
                  stepperLabel="adjusted score"
                  min={0}
                  max={4}
                  step={1}
                  value={score}
                  placeholder={SCORE_PLACEHOLDER}
                  onChange={(event) => setScore(event.target.value)}
                />
              )}
            </FormField>
            <FormField id="recipePairRationale" label="Adjusted rationale" layout="stack">
              {(control) => (
                <FieldTextarea
                  {...control}
                  rows={4}
                  value={rationale}
                  placeholder={ADJUSTED_RATIONALE_PLACEHOLDER}
                  onChange={(event) => setRationale(event.target.value)}
                />
              )}
            </FormField>
          </Stack>
        </FormSection>
        <PlateFoot className="setup-ceremony__foot" arrangement="end">
          <Key type="submit" variant="transmit">
            Record
          </Key>
        </PlateFoot>
      </Stack>
    </OperateArea>
  );
}

function LedgerRecipe() {
  const [title, setTitle] = useState("Structural Audit Q3");
  const [limit, setLimit] = useState("60:00");
  const [warning, setWarning] = useState("10:00");
  const [attempts, setAttempts] = useState("3");
  const [windowStart, setWindowStart] = useState("2026-09-18");
  const [score, setScore] = useState("3");
  const [notes, setNotes] = useState("Keep the shoreline brief seated for the next cohort.");
  const [warnings, setWarnings] = useState(true);

  return (
    <OperateArea
      className="workspace-area work-plane form-recipe"
      label="Ledger form recipe"
      title="Campaign configuration"
      description="Identity and committed sources are display. Title, timing, window, and notes stay editable."
      back={<BackKey label="Activities" type="button" onClick={() => undefined} />}
    >
      <ReadoutGrid label="Campaign identity" columns={4} className="assignment-instruments">
        <ReadoutGridRow label="Identity">
          <ReadoutGridField term="Campaign">CMP-0042</ReadoutGridField>
          <ReadoutGridField term="Enrollment">
            <CompactId tabbable value="a1000000-0000-4000-8000-000000000007" />
          </ReadoutGridField>
          <ReadoutGridField term="Revision">4</ReadoutGridField>
            <ReadoutGridField term="Activation">
              <ActivationMark frozen className="readout-grid-state" />
            </ReadoutGridField>
        </ReadoutGridRow>
        <ReadoutGridRow label="Timing">
          <ReadoutGridField term="Timezone">UTC</ReadoutGridField>
          <ReadoutGridField term="Activated">
            <InstantReadout value="2026-08-26T14:30:00.000Z" timeZone="UTC" />
          </ReadoutGridField>
          <ReadoutGridField term="Eligibility">Open</ReadoutGridField>
          <ReadoutGridField term="Accommodation">None seated</ReadoutGridField>
        </ReadoutGridRow>
      </ReadoutGrid>
      <Stack
        as="form"
        gap="6"
        className="workspace-form setup-ceremony"
        noValidate
        onSubmit={(event) => event.preventDefault()}
      >
        <FormField
          id="recipeMixTitle"
          layout="stack"
          label="Campaign title"
          hint="Saved as revision 4"
        >
          {(control) => (
            <FieldInput
              {...control}
              width="wide"
              value={title}
              placeholder={CAMPAIGN_TITLE_PLACEHOLDER}
              maxLength={200}
              onChange={(event) => setTitle(event.target.value)}
            />
          )}
        </FormField>
        <FormSection legend="Committed sources">
          <Grid gap="4" minItemWidth="control">
            <RecipeSelect
              id="recipeMixAgent"
              label="Agent"
              value={AGENTS[0]}
              options={AGENTS}
              onChange={() => undefined}
              frozen
            />
            <RecipeSelect
              id="recipeMixHarness"
              label="Harness"
              value={HARNESSES[0]}
              options={HARNESSES}
              onChange={() => undefined}
              frozen
            />
            <RecipeSelect
              id="recipeMixTask"
              label="Task and Submission"
              value={TASKS[0]}
              options={TASKS}
              onChange={() => undefined}
              frozen
            />
          </Grid>
        </FormSection>
        <Grid gap="6" minItemWidth="panel">
          <FormSection legend="Timing and attempts">
            <Stack gap="4">
              <div className="form-row form-row--pair">
                <FormField id="recipeMixLimit" label="Session limit" hint={MM_SS_HINT} layout="pair">
                  {(control) => (
                    <FieldInput
                      {...control}
                      value={limit}
                      placeholder={MM_SS_PLACEHOLDER}
                      onChange={(event) => setLimit(event.target.value)}
                    />
                  )}
                </FormField>
                <FormField id="recipeMixWarning" label="Time warning at" hint={MM_SS_HINT} layout="pair">
                  {(control) => (
                    <FieldInput
                      {...control}
                      value={warning}
                      placeholder={MM_SS_WARNING_PLACEHOLDER}
                      onChange={(event) => setWarning(event.target.value)}
                    />
                  )}
                </FormField>
              </div>
              <Grid gap="4" minItemWidth="compact">
                <FormField id="recipeMixAttempts" label="Max attempts" layout="stack">
                  {(control) => (
                    <FieldInput
                      {...control}
                      width="narrow"
                      value={attempts}
                      placeholder={MAX_ATTEMPTS_PLACEHOLDER}
                      onChange={(event) => setAttempts(event.target.value)}
                    />
                  )}
                </FormField>
                <FormField id="recipeMixCooldown" label="Cooldown" layout="stack">
                  {(control) => (
                    <FieldInput
                      {...control}
                      width="narrow"
                      value="24H"
                      placeholder={COOLDOWN_PLACEHOLDER}
                      frozen
                    />
                  )}
                </FormField>
              </Grid>
            </Stack>
          </FormSection>
          <FormSection legend="Window">
            <Stack gap="4">
              <FormField
                id="recipeMixOpened"
                label="Opened on"
                layout="stack"
                labelAssociatesControl={false}
              >
                {(control, { labelId }) => (
                  <DatePicker
                    id={control.id}
                    labelId={labelId}
                    value="2026-07-01"
                    onChange={() => undefined}
                    frozen
                    now="2026-08-26"
                  />
                )}
              </FormField>
              <FormField
                id="recipeMixWindow"
                label="Window start"
                layout="stack"
                labelAssociatesControl={false}
              >
                {(control, { labelId }) => (
                  <DatePicker
                    id={control.id}
                    labelId={labelId}
                    describedBy={control["aria-describedby"]}
                    value={windowStart}
                    onChange={setWindowStart}
                    now="2026-08-26"
                  />
                )}
              </FormField>
              <Breaker id="recipeMixWarnings" checked={warnings} onChange={setWarnings}>
                Time warnings
              </Breaker>
            </Stack>
          </FormSection>
        </Grid>
        <FormSection legend="Score and notes">
          <Stack gap="4">
            <FormField id="recipeMixScore" label="Adjusted score" layout="stack">
              {(control) => (
                <FieldNumber
                  {...control}
                  width="narrow"
                  stepperLabel="adjusted score"
                  min={0}
                  max={4}
                  step={1}
                  value={score}
                  placeholder={SCORE_PLACEHOLDER}
                  onChange={(event) => setScore(event.target.value)}
                />
              )}
            </FormField>
            <FormField id="recipeMixNotes" label="Operator notes" layout="stack">
              {(control) => (
                <FieldTextarea
                  {...control}
                  rows={3}
                  value={notes}
                  placeholder={ADJUSTED_RATIONALE_PLACEHOLDER}
                  onChange={(event) => setNotes(event.target.value)}
                />
              )}
            </FormField>
          </Stack>
        </FormSection>
        <PlateFoot className="setup-ceremony__foot" arrangement="split" secondary={<Key type="button">Save draft</Key>}>
          <Key type="submit" variant="transmit">
            Record
          </Key>
        </PlateFoot>
      </Stack>
    </OperateArea>
  );
}

function AccommodationDialogRecipe() {
  const [extension, setExtension] = useState("15:00");
  const [reason, setReason] = useState("Extended reading time for this enrollment only.");

  return (
    <div className="form-recipe-dialog-well">
      <form className="form-recipe-dialog" noValidate onSubmit={(event) => event.preventDefault()}>
        <DialogPlate width="wide">
          <DialogPlateHead title="Record accommodation" titleId="recipeDialogTitle" />
          <DialogPlateBody>
            <Stack gap="6">
              <p>
                Timing accommodations are enrollment-scoped and recorded with administrator identity.
                Other participants never see this adjustment.
              </p>
              <Stack gap="4">
                <FormField id="recipeDialogExtension" label="Time extension" hint={MM_SS_HINT} layout="stack">
                  {(control) => (
                    <FieldInput
                      {...control}
                      value={extension}
                      placeholder={ACCOMMODATION_VALUE_PLACEHOLDER}
                      onChange={(event) => setExtension(event.target.value)}
                    />
                  )}
                </FormField>
                <FormField id="recipeDialogReason" label="Reason" layout="stack">
                  {(control) => (
                    <FieldTextarea
                      {...control}
                      rows={3}
                      value={reason}
                      placeholder="Extended reading time for this enrollment only."
                      onChange={(event) => setReason(event.target.value)}
                    />
                  )}
                </FormField>
              </Stack>
            </Stack>
          </DialogPlateBody>
          <DialogPlateFooter
            arrangement="split"
            secondary={
              <Key type="button" onClick={() => undefined}>
                Cancel
              </Key>
            }
            primary={
              <Key type="submit" variant="transmit">
                Record accommodation
              </Key>
            }
          />
        </DialogPlate>
      </form>
    </div>
  );
}

export function FormRecipeSections() {
  return (
    <GallerySection
      id="form-recipes"
      title="Form recipes"
      note="Clone these compositions. Form controls below is the parts catalog. Commission is OperateArea, ErrorSummary, stacked title, FormSection + Grid, PlateFoot — same rungs as create Campaign (control 10px, group 16px, bay 24px). FormSection grouping is a 2px hairline underline under the legend words. Production create adds the remaining required sources into Source set and owns submit with React Hook Form. Pair instruments share one horizon. Ledger mix places ReadoutGrid identity on the etched plate, etches frozen sources and cooldown as FormFields, and keeps timing, window, score, and notes editable on a two-up Grid. Dialog forms reuse DialogPlate with the same stacked fields; this plate stays seated so the recipe is visible without opening a modal."
    >
      <Spec wide tag="OperateArea · ErrorSummary · FormSection · Grid · PlateFoot · ready">
        <CommissionRecipe
          idPrefix="recipeCommission"
          label="Commission form recipe"
          initialTitle="Structural Audit Q3"
        />
      </Spec>
      <Spec wide tag="same commission stack · invalid submit · summary links to the title slot">
        <CommissionRecipe
          idPrefix="recipeInvalid"
          label="Commission form recipe — invalid"
          initialTitle=""
          revealErrors
        />
      </Spec>
      <Spec wide tag="OperateArea · FormSection · .form-row--pair · stacked rationale">
        <InstrumentRecipe />
      </Spec>
      <Spec wide tag="ReadoutGrid identity · frozen FormFields · two-up sections · split Save draft / Record">
        <LedgerRecipe />
      </Spec>
      <Spec wide tag="DialogPlate · stacked fields · split Cancel / commit">
        <AccommodationDialogRecipe />
      </Spec>
    </GallerySection>
  );
}
