import { useRef, useState, type ReactNode } from "react";
import {
  AcknowledgmentGate,
  Breaker,
  CeremonyDialog,
  DatePicker,
  DateTimePicker,
  DialogPlate,
  DialogPlateBody,
  DialogPlateFooter,
  DialogPlateHead,
  DropdownSelect,
  FieldFile,
  FieldInput,
  FieldNumber,
  FieldTextarea,
  FormField,
  FormSection,
  CAMPAIGN_TITLE_PLACEHOLDER,
  Key,
  ACCOMMODATION_VALUE_PLACEHOLDER,
  ADJUSTED_RATIONALE_PLACEHOLDER,
  CALLSIGN_PLACEHOLDER,
  COMPOSER_PLACEHOLDER,
  COOLDOWN_PLACEHOLDER,
  MM_SS_HINT,
  MM_SS_PATTERN,
  MM_SS_PLACEHOLDER,
  MM_SS_WARNING_PLACEHOLDER,
  SCORE_PLACEHOLDER,
  RadioGroup,
  SearchableDisclosureMenu,
  SearchableDropdownSelect,
  SearchableMultiSelect,
  Stack,
  TimePicker,
  mmSsError,
} from "../../../components";
import { GallerySection, Spec } from "./GallerySection";

const harnesses = [
  "GOVERNED-EXAM-01",
  "GOVERNED-EXAM-02",
  "GOVERNED-AUDIT-01",
  "GOVERNED-AUDIT-02 / Structural integrity rehearsal",
  "GOVERNED-OPS-01",
  "GOVERNED-OPS-02 / Cross-region failover harness",
];
const campaigns = [
  { id: "CMP-0042", label: "CMP-0042 / Structural Audit Q3" },
  { id: "CMP-0043", label: "CMP-0043 / Ops Integrity" },
  { id: "CMP-0044", label: "CMP-0044 / Access Review" },
  { id: "CMP-0054", label: "CMP-0054 / Berth Assignment" },
  { id: "CMP-0057", label: "CMP-0057 / Sensor Calibration" },
  { id: "CMP-0061", label: "CMP-0061 / Pilot Recert" },
];
const roles = [
  { value: "Reviewer", label: "Reviewer", id: "demoMultiOptionReviewer" },
  { value: "Auditor", label: "Auditor", id: "demoMultiOptionAuditor" },
  { value: "Escalation lead", label: "Escalation lead", id: "demoMultiOptionEscalationLead" },
  { value: "Evidence specialist", label: "Evidence specialist", id: "demoMultiOptionEvidenceSpecialist" },
  { value: "Release authority", label: "Release authority", id: "demoMultiOptionReleaseAuthority" },
];

const OPTION_MENU_SPECIMEN = ["All stages", "Examination", "Review", "Released"] as const;

function OptionMenuSpecimen() {
  const [selected, setSelected] = useState<(typeof OPTION_MENU_SPECIMEN)[number]>("Examination");
  const listRef = useRef<HTMLUListElement>(null);

  const focusOption = (index: number) => {
    const items = Array.from(listRef.current?.querySelectorAll<HTMLElement>("[role='option']") ?? []);
    const next = Math.max(0, Math.min(index, items.length - 1));
    items.forEach((item, i) => {
      item.tabIndex = i === next ? 0 : -1;
      if (i === next) item.focus();
    });
  };

  return (
    <ul
      ref={listRef}
      className="option-menu menu-surface popover-surface menu-demo"
      role="listbox"
      aria-label="Option menu specimen"
      onKeyDown={(event) => {
        const items = Array.from(listRef.current?.querySelectorAll<HTMLElement>("[role='option']") ?? []);
        const idx = items.indexOf(document.activeElement as HTMLElement);
        if (event.key === "ArrowDown") {
          event.preventDefault();
          focusOption(idx < 0 ? 0 : idx + 1);
        }
        if (event.key === "ArrowUp") {
          event.preventDefault();
          focusOption(idx < 0 ? items.length - 1 : idx - 1);
        }
        if (event.key === "Enter" || event.key === " ") {
          event.preventDefault();
          const label = items[Math.max(0, idx)]?.textContent;
          if (label && OPTION_MENU_SPECIMEN.includes(label as (typeof OPTION_MENU_SPECIMEN)[number])) {
            setSelected(label as (typeof OPTION_MENU_SPECIMEN)[number]);
          }
        }
      }}
    >
      {OPTION_MENU_SPECIMEN.map((label, index) => (
        <li
          key={label}
          role="option"
          tabIndex={label === selected ? 0 : -1}
          aria-selected={label === selected}
          onClick={() => setSelected(label)}
          onFocus={() => {
            const items = Array.from(listRef.current?.querySelectorAll<HTMLElement>("[role='option']") ?? []);
            items.forEach((item, i) => {
              item.tabIndex = i === index ? 0 : -1;
            });
          }}
        >
          {label}
        </li>
      ))}
    </ul>
  );
}

function DemoDialog({
  open,
  onClose,
  id,
  width = "default",
  title,
  titleId,
  children,
  commit,
}: {
  open: boolean;
  onClose: () => void;
  id: string;
  width?: "narrow" | "default" | "wide";
  title: string;
  titleId: string;
  children: ReactNode;
  commit: string;
}) {
  return (
    <CeremonyDialog id={id} open={open} onClose={onClose} labelledBy={titleId}>
      <DialogPlate width={width}>
        <DialogPlateHead title={title} titleId={titleId} />
        <DialogPlateBody>{children}</DialogPlateBody>
        <DialogPlateFooter
          arrangement="split"
          secondary={<Key onClick={onClose}>Cancel</Key>}
          primary={
            <Key variant={width === "default" ? "release" : "transmit"} onClick={onClose}>{commit}</Key>
          }
        />
      </DialogPlate>
    </CeremonyDialog>
  );
}

function FileIntakeSpecimens() {
  const [many, setMany] = useState<File[]>([
    new File(["# Shoreline brief"], "briefing.md", { type: "text/markdown" }),
    new File(["local candidate text"], "notes.txt", { type: "text/plain" }),
  ]);
  const [one, setOne] = useState<File[]>([]);

  return (
    <div className="spec-row spec-row--files">
      <Spec wide tag=".field-file · multiple · seated rows · Remove">
        <FormField id="demoFilesMany" label="Attachments (.txt or .md)" layout="stack" labelAssociatesControl={false}>
          {(control, { labelId }) => (
            <FieldFile
              id={control.id}
              labelledBy={labelId}
              mode="multiple"
              accept=".txt,.md,text/plain,text/markdown"
              hint="UTF-8 .txt or .md"
              files={many}
              describedBy={control["aria-describedby"]}
              invalid={control["aria-invalid"]}
              onFilesChange={setMany}
            />
          )}
        </FormField>
      </Spec>
      <Spec tag=".field-file · single · empty bay">
        <FormField id="demoFileOne" label="Briefing file" layout="stack" labelAssociatesControl={false}>
          {(control, { labelId }) => (
            <FieldFile
              id={control.id}
              labelledBy={labelId}
              mode="single"
              accept=".txt,.md,text/plain,text/markdown"
              hint="One UTF-8 .txt or .md"
              files={one}
              describedBy={control["aria-describedby"]}
              invalid={control["aria-invalid"]}
              onFilesChange={setOne}
            />
          )}
        </FormField>
      </Spec>
      <Spec tag=".field-file · disabled">
        <FormField id="demoFileDisabled" label="Locked intake" layout="stack" labelAssociatesControl={false}>
          {(control, { labelId }) => (
            <FieldFile
              id={control.id}
              labelledBy={labelId}
              mode="multiple"
              hint="Receiving version"
              files={[]}
              disabled
              describedBy={control["aria-describedby"]}
              invalid={control["aria-invalid"]}
              onFilesChange={() => undefined}
            />
          )}
        </FormField>
      </Spec>
      <Spec tag=".field-file · invalid">
        <FormField
          id="demoFileInvalid"
          label="Required attachment"
          layout="stack"
          labelAssociatesControl={false}
          error="Seat a permitted .txt or .md file."
        >
          {(control, { labelId }) => (
            <FieldFile
              id={control.id}
              labelledBy={labelId}
              mode="single"
              hint="UTF-8 .txt or .md"
              files={[]}
              invalid={control["aria-invalid"]}
              describedBy={control["aria-describedby"]}
              onFilesChange={() => undefined}
            />
          )}
        </FormField>
      </Spec>
    </div>
  );
}

export function InputSections() {
  const [limit, setLimit] = useState("60:00");
  const [pairLimit, setPairLimit] = useState("60:00");
  const [pairWarning, setPairWarning] = useState("10:00");
  const [harness, setHarness] = useState(harnesses[0]);
  const [context, setContext] = useState(campaigns[0].id);
  const [toolbar, setToolbar] = useState("All stages");
  const [menuStage, setMenuStage] = useState<(typeof OPTION_MENU_SPECIMEN)[number]>("Examination");
  const [optionalOwner, setOptionalOwner] = useState<string | null>("Auditor");
  const [searchHarness, setSearchHarness] = useState(harnesses[0]);
  const [searchCampaign, setSearchCampaign] = useState(campaigns[0].id);
  const [selectedRoles, setSelectedRoles] = useState<string[]>(["Reviewer", "Auditor"]);
  const [dialog, setDialog] = useState<"narrow" | "default" | "wide" | null>(null);
  const [acked, setAcked] = useState(true);
  const [agent, setAgent] = useState("EXAMINER-CORE");
  const [warnings, setWarnings] = useState(true);
  const [deadline, setDeadline] = useState("2026-09-18");
  const [sessionMark, setSessionMark] = useState("09:00");
  const [syncMark, setSyncMark] = useState("09:00:00");
  const [activationAt, setActivationAt] = useState("2026-08-26T14:30");
  const [windowStart, setWindowStart] = useState("");
  const [score, setScore] = useState("3");
  const invalid = !MM_SS_PATTERN.test(limit.trim());
  const pairLimitInvalid = !MM_SS_PATTERN.test(pairLimit.trim());
  const pairWarningInvalid = !MM_SS_PATTERN.test(pairWarning.trim());

  return (
    <>
      <GallerySection id="form" title="Form controls" note="Parts catalog. Clone whole OperateArea compositions from Form recipes above. Dark slot fills on inputs, teal focus bezels. Text stays a typed slot; numbers get authored inc/dec chevrons instead of native spin buttons. Ceremony, create, and dialogs use FormField layout=stack (label over slot, --field-label-gap / 10px). Titled clusters use FormSection: plate-title legend with a 2px hairline underline under the name only, then --form-group-gap to the fields. Horizon rows remain .form-row / .form-demo-row. Field, context, and toolbar selects share one popover grammar. Validation speaks amber; helpers stay dim. Frozen etches the committed value — bezels drop, nothing turns red.">
        <div className="spec-row spec-row--fields">
          <Spec tag=".field-input · text slot · example placeholder">
            <FormField id="demoText" label="Callsign" className="form-demo-row">
              {(controlProps) => (
                <FieldInput {...controlProps} type="text" placeholder={CALLSIGN_PLACEHOLDER} />
              )}
            </FormField>
          </Spec>
          <Spec tag=".field-number · authored inc/dec">
            <FormField id="demoNumber" label="Score" className="form-demo-row">
              {(controlProps) => (
                <FieldNumber
                  {...controlProps}
                  stepperLabel="score"
                  min={0}
                  max={4}
                  step={1}
                  value={score}
                  placeholder={SCORE_PLACEHOLDER}
                  onChange={(event) => setScore(event.target.value)}
                />
              )}
            </FormField>
          </Spec>
        </div>
        <div className="spec-row spec-row--fields">
          <Spec tag=".form-section · titled cluster">
            <Stack gap="6">
              <FormSection legend="Agent and Harness">
                <FormField id="demoSectionAgent" label="Agent" layout="stack">
                  {(controlProps) => (
                    <FieldInput
                      {...controlProps}
                      type="text"
                      defaultValue="EXAMINER-CORE"
                      placeholder="EXAMINER-CORE"
                    />
                  )}
                </FormField>
              </FormSection>
              <FormSection legend="Source set">
                <FormField id="demoSectionTask" label="Task" layout="stack">
                  {(controlProps) => (
                    <FieldInput
                      {...controlProps}
                      type="text"
                      defaultValue="TASK-STRUCT-01"
                      placeholder="TASK-STRUCT-01"
                    />
                  )}
                </FormField>
              </FormSection>
            </Stack>
          </Spec>
        </div>
        <div className="spec-row spec-row--field-stacks">
          <Spec tag=".field-stack · FormField layout=stack · label over slot">
            <FormField id="demoStackTitle" layout="stack" label="Campaign title">
              {(controlProps) => (
                <FieldInput
                  {...controlProps}
                  width="wide"
                  defaultValue="Structural Audit Q3"
                  placeholder={CAMPAIGN_TITLE_PLACEHOLDER}
                />
              )}
            </FormField>
          </Spec>
          <Spec tag=".field-stack · .field-input.is-frozen · etch after activation">
            <FormField id="demoStackTitleFrozen" layout="stack" label="Campaign title">
              {(controlProps) => (
                <FieldInput
                  {...controlProps}
                  width="wide"
                  value="Accessibility Standards Review"
                  placeholder={CAMPAIGN_TITLE_PLACEHOLDER}
                  frozen
                />
              )}
            </FormField>
          </Spec>
          <Spec tag=".field-stack · .select-shell--field">
            <FormField id="demoStackHarness" layout="stack" label="Harness" labelAssociatesControl={false}>
              {(controlProps, { labelId }) => (
                <DropdownSelect
                  id={controlProps.id}
                  labelId={labelId}
                  describedBy={controlProps["aria-describedby"]}
                  value={harness}
                  options={harnesses.slice(0, 3)}
                  onChange={setHarness}
                />
              )}
            </FormField>
          </Spec>
        </div>
        <div className="form-demo-grid form-demo-grid--states">
          <Spec tag=".field-input · type mm:ss — clear it to see amber validation">
            <FormField
              id="demoLimit"
              label="Session limit"
              hint={MM_SS_HINT}
              error={invalid ? mmSsError("Session limit") : undefined}
              className="form-demo-row"
            >
              {(controlProps) => (
                <FieldInput
                  {...controlProps}
                  value={limit}
                  placeholder={MM_SS_PLACEHOLDER}
                  onChange={(event) => setLimit(event.target.value)}
                />
              )}
            </FormField>
          </Spec>
          <Spec tag="disabled + .field-hint — helper before error in aria-describedby">
            <FormField
              id="demoDisabled"
              label="Cooldown"
              hint="Until the next attempt window opens."
              className="form-demo-row"
            >
              {(controlProps) => (
                <FieldInput {...controlProps} value="24H" placeholder={COOLDOWN_PLACEHOLDER} disabled />
              )}
            </FormField>
          </Spec>
          <Spec tag=".field-input.is-frozen · control etch — sealed records use readout, not this slot">
            <FormField id="demoFrozen" label="Session limit" className="form-demo-row">
              {(controlProps) => (
                <FieldInput {...controlProps} value="60:00" placeholder={MM_SS_PLACEHOLDER} frozen />
              )}
            </FormField>
          </Spec>
          <Spec tag=".field-number.is-frozen · stepper withdrawn">
            <FormField id="demoFrozenNumber" label="Committed score" className="form-demo-row">
              {(controlProps) => <FieldNumber {...controlProps} value={3} placeholder={SCORE_PLACEHOLDER} frozen />}
            </FormField>
          </Spec>
          <Spec tag=".select-shell.is-frozen · chevron withdrawn">
            <FormField id="demoFrozenDrop" label="Harness" className="form-demo-row" labelAssociatesControl={false}>
              {(controlProps, { labelId }) => (
                <DropdownSelect
                  id={controlProps.id}
                  labelId={labelId}
                  describedBy={controlProps["aria-describedby"]}
                  value="GOVERNED-EXAM-01"
                  options={harnesses.slice(0, 3)}
                  onChange={() => undefined}
                  frozen
                />
              )}
            </FormField>
          </Spec>
        </div>
        <Spec wide tag=".form-row--pair · .field-pair — two fields, one horizon">
          <div className="form-demo-pair">
            <div className="form-row form-row--pair">
              <FormField
                id="demoPairLimit"
                label="Session limit"
                hint={MM_SS_HINT}
                error={pairLimitInvalid ? mmSsError("Session limit") : undefined}
                layout="pair"
              >
                {(controlProps) => (
                  <FieldInput
                    {...controlProps}
                    value={pairLimit}
                    placeholder={MM_SS_PLACEHOLDER}
                    onChange={(event) => setPairLimit(event.target.value)}
                  />
                )}
              </FormField>
              <FormField
                id="demoPairWarning"
                label="Time warning at"
                hint={MM_SS_HINT}
                error={pairWarningInvalid ? mmSsError("Time warning", MM_SS_WARNING_PLACEHOLDER) : undefined}
                layout="pair"
              >
                {(controlProps) => (
                  <FieldInput
                    {...controlProps}
                    value={pairWarning}
                    placeholder={MM_SS_WARNING_PLACEHOLDER}
                    onChange={(event) => setPairWarning(event.target.value)}
                  />
                )}
              </FormField>
            </div>
          </div>
        </Spec>
        <Spec wide tag=".field-group · .field-stack · locked stack vs .field-textarea--resize-y">
          <div className="form-demo-textareas">
            <div className="field-group form-demo-stack">
              <FormField id="demoStackScore" label="Adjusted score" layout="stack">
                {(controlProps) => (
                  <FieldNumber
                    {...controlProps}
                    width="narrow"
                    stepperLabel="adjusted score"
                    min={0}
                    max={4}
                    step={1}
                    defaultValue={3}
                    placeholder={SCORE_PLACEHOLDER}
                  />
                )}
              </FormField>
              <FormField id="demoStackRationale" label="Adjusted rationale" layout="stack">
                {(controlProps) => (
                  <FieldTextarea
                    {...controlProps}
                    rows={4}
                    placeholder={ADJUSTED_RATIONALE_PLACEHOLDER}
                    defaultValue="Identifies fencing and lease expiry as primary safety mechanisms."
                  />
                )}
              </FormField>
            </div>
            <FormField id="demoResizeRationale" label="Adjusted rationale" layout="stack" className="form-demo-stack">
              {(controlProps) => (
                <FieldTextarea
                  {...controlProps}
                  resize="vertical"
                  rows={4}
                  placeholder={ADJUSTED_RATIONALE_PLACEHOLDER}
                  defaultValue="Identifies fencing and lease expiry as primary safety mechanisms."
                />
              )}
            </FormField>
          </div>
        </Spec>
        <div className="spec-row spec-row--selects">
          <Spec tag=".select-shell--field · fills column · popover matches trigger">
            <FormField id="demoDropKey" label="Harness" className="form-demo-row" labelAssociatesControl={false}>
              {(_, { labelId }) => (
                <DropdownSelect id="demoDropKey" valueId="demoDropValue" labelId={labelId} value={harness} options={harnesses.slice(0, 3)} onChange={setHarness} />
              )}
            </FormField>
          </Spec>
          <Spec tag=".select-shell--context · min-width match · max-width grow">
            <SearchableDisclosureMenu label="Campaign context" value={campaigns.find((item) => item.id === context)?.label ?? ""} selectedId={context} options={campaigns.slice(0, 3)} onSelect={setContext} ariaLabel="Select campaign context" />
          </Spec>
          <Spec tag=".select-shell--toolbar · hug trigger · overlay min 16rem grow">
            <DropdownSelect
              variant="toolbar"
              labelId="demoToolbarLabel"
              id="demoToolbarKey"
              value={toolbar}
              options={["All stages", "Examination in progress", "Review and release"]}
              onChange={setToolbar}
            />
          </Spec>
          <Spec tag="DropdownSelect clearable · nullable value · unframed Clear">
            <FormField
              id="demoOptionalOwner"
              label="Escalation owner"
              className="form-demo-row"
              labelAssociatesControl={false}
            >
              {(_, { labelId }) => (
                <DropdownSelect
                  clearable
                  id="demoOptionalOwner"
                  valueId="demoOptionalOwnerValue"
                  labelId={labelId}
                  value={optionalOwner}
                  options={["Reviewer", "Auditor", "Release authority"]}
                  placeholder="No owner assigned"
                  onChange={setOptionalOwner}
                />
              )}
            </FormField>
          </Spec>
        </div>
        <div className="spec-row spec-row--marks">
          <Spec tag="AcknowledgmentGate · amber = commitment voice">
            <AcknowledgmentGate id="demoAck" className="control-line" checked={acked} onChange={setAcked}>
              I acknowledge the session rules and consent terms.
            </AcknowledgmentGate>
          </Spec>
          <Spec tag="RadioGroup · selection speaks teal">
            <RadioGroup
              legend="Agent identity"
              name="demoAgent"
              value={agent}
              onChange={setAgent}
              options={[
                { value: "EXAMINER-CORE", label: "Examiner-Core" },
                { value: "EXAMINER-STRUCT", label: "Examiner-Struct" },
                { value: "EXAMINER-OPS", label: "Examiner-Ops" },
              ]}
            />
          </Spec>
          <Spec tag="Breaker · a square breaker, never a pill">
            <Breaker id="demoBreaker" checked={warnings} onChange={setWarnings}>Time warnings</Breaker>
          </Spec>
        </div>
        <Spec wide tag=".composer · the commit key shares the slot's right edge"><form className="composer" onSubmit={(event) => event.preventDefault()}><label className="visually-hidden" htmlFor="demoComposer">Compose reply</label><textarea id="demoComposer" rows={1} placeholder={COMPOSER_PLACEHOLDER} /><Key type="submit" variant="transmit">Transmit</Key></form></Spec>
      </GallerySection>

      <GallerySection id="file" title="File intake" note="Native file input plus a dashed empty bay. Drag-and-drop may seat files but is never the only method — Choose file / Choose files stays the keyboard path. Empty uses a dashed hairline (absence). A seated selection or a live drop uses a solid bezel. Single mode replaces; multiple appends. Rows show the document glyph, filename, type/size, and Remove. Do not treat local selection as an accepted Submission version.">
        <FileIntakeSpecimens />
      </GallerySection>

      <GallerySection id="datetime" title="Date & time" note="Field-slot triggers with authored calendar and chrono plates — not native browser pickers. The trigger shrinks to the mark; the plate keeps its own instrument width. Selected day is a rectangular teal inset bezel; today is a circular teal ring on the numeral. Time wheels keep option-menu hairline and teal-glass hover, but selected is the inset bezel — the 7×1px tick stays on menus and nav. Session mark uses HH/MM; Sync mark opts into HH/MM/SS via withSeconds. Amber still owns invalid; frozen etches the committed mark.">
        <div className="spec-row spec-row--temporal-times">
          <Spec tag=".select-shell--time · HH/MM wheels · tabular 24h · Clear / Done">
            <FormField id="demoTime" label="Session mark" className="form-demo-row" labelAssociatesControl={false}>
              {(controlProps, { labelId }) => (
                <TimePicker
                  id={controlProps.id}
                  valueId="demoTimeValue"
                  labelId={labelId}
                  describedBy={controlProps["aria-describedby"]}
                  value={sessionMark}
                  onChange={setSessionMark}
                />
              )}
            </FormField>
          </Spec>
          <Spec tag=".select-shell--time · HH/MM/SS wheels · withSeconds · Clear / Done">
            <FormField id="demoTimeSeconds" label="Sync mark" className="form-demo-row" labelAssociatesControl={false}>
              {(controlProps, { labelId }) => (
                <TimePicker
                  id={controlProps.id}
                  valueId="demoTimeSecondsValue"
                  labelId={labelId}
                  describedBy={controlProps["aria-describedby"]}
                  value={syncMark}
                  onChange={setSyncMark}
                  withSeconds
                />
              )}
            </FormField>
          </Spec>
        </div>
        <div className="spec-row spec-row--temporal-dates">
          <Spec tag=".select-shell--date · calendar grid · Monday start · Now / Clear / Done">
            <FormField id="demoDate" label="Cohort deadline" className="form-demo-row" labelAssociatesControl={false}>
              {(controlProps, { labelId }) => (
                <DatePicker
                  id={controlProps.id}
                  valueId="demoDateValue"
                  labelId={labelId}
                  describedBy={controlProps["aria-describedby"]}
                  value={deadline}
                  onChange={setDeadline}
                  now="2026-08-26"
                />
              )}
            </FormField>
          </Spec>
        </div>
        <Spec wide tag=".select-shell--datetime · calendar + chrono · Now / Clear / Done">
          <FormField id="demoDateTime" label="Activation at" className="form-demo-row" labelAssociatesControl={false}>
            {(controlProps, { labelId }) => (
              <DateTimePicker
                id={controlProps.id}
                valueId="demoDateTimeValue"
                labelId={labelId}
                describedBy={controlProps["aria-describedby"]}
                mode="datetime"
                value={activationAt}
                onChange={setActivationAt}
                now="2026-08-26"
              />
            )}
          </FormField>
        </Spec>
        <div className="form-demo-grid form-demo-grid--states">
          <Spec tag="empty + .field-error · amber bezel">
            <FormField
              id="demoDateInvalid"
              label="Window start"
              error={windowStart ? undefined : "Enter a window start — pick a date from the calendar."}
              className="form-demo-row"
              labelAssociatesControl={false}
            >
              {(controlProps, { labelId }) => (
                <DatePicker
                  id={controlProps.id}
                  labelId={labelId}
                  describedBy={controlProps["aria-describedby"]}
                  invalid={controlProps["aria-invalid"]}
                  value={windowStart}
                  onChange={setWindowStart}
                  now="2026-08-26"
                />
              )}
            </FormField>
          </Spec>
          <Spec tag=".select-shell--date.is-frozen · post-activation etch">
            <FormField id="demoDateFrozen" label="Opened on" className="form-demo-row" labelAssociatesControl={false}>
              {(controlProps, { labelId }) => (
                <DatePicker
                  id={controlProps.id}
                  labelId={labelId}
                  value="2026-07-01"
                  onChange={() => undefined}
                  frozen
                  now="2026-08-26"
                />
              )}
            </FormField>
          </Spec>
        </div>
      </GallerySection>

      <GallerySection id="searchable-select" title="Searchable select" note="Single-selection field with the same search plate as the multiselect: combobox filter, result readout, teal tick on the active row. Choosing a row commits and closes. Close dismisses without changing the value. The committed row stays in the list when the filter would hide it.">
        <div className="form-demo-grid searchable-select-spec">
          <Spec tag=".searchable-select · shared multiselect plate · single commit on row pick">
            <div className="form-demo-row form-demo-row--fit"><span className="field-label" id="demoSearchLabel">Harness</span><SearchableDropdownSelect id="demoSearchKey" valueId="demoSearchValue" searchId="demoSearchFilter" listboxId="demoSearchOptions" optionId={(_, index) => `demoSearchOpt${index}`} labelId="demoSearchLabel" value={searchHarness} options={harnesses} onChange={setSearchHarness} searchPlaceholder="Filter harness" listLabel="Harness options" optionNoun="harness" /></div>
          </Spec>
          <Spec tag=".searchable-disclosure · context trigger · shared searchable plate">
            <SearchableDisclosureMenu label="Campaign context" value={campaigns.find((item) => item.id === searchCampaign)?.label ?? ""} selectedId={searchCampaign} options={campaigns} onSelect={setSearchCampaign} ariaLabel="Campaign options" keyId="demoContextSearchKey" menuId="demoContextSearchOptions" valueId="demoContextSearchValue" searchId="demoContextSearchFilter" optionId={(_, index) => `demoContextSearchOpt${index}`} searchPlaceholder="Filter campaigns" optionNoun="campaign" />
          </Spec>
        </div>
      </GallerySection>

      <GallerySection id="multiselect" title="Searchable multiselect" note="A multiple-selection field with persistent teal marks, keyboard filtering, and an explicit close action. Matching is configured in code; no preference control leaks into the product UI.">
        <Spec wide tag=".select-shell--field + shared popover · case-insensitive filter">
          <div className="form-demo-row form-demo-row--fit"><span className="field-label" id="demoMultiLabel">Review roles</span><SearchableMultiSelect id="demoMultiKey" valueId="demoMultiValue" panelId="demoMultiPanel" searchId="demoMultiSearch" listboxId="demoMultiOptions" labelId="demoMultiLabel" options={roles} values={selectedRoles} onChange={setSelectedRoles} placeholder="Filter roles" optionNoun="role" /></div>
        </Spec>
      </GallerySection>

      <GallerySection id="menu" title="Option menu" note="Single-select listbox grammar: popover sheen, hairline row dividers, teal-glass hover and keyboard focus, 7×1px teal tick plus Bright Text on the selected option. Selected rest is the tick, not the fill. Live placement uses DropdownSelect / placeFloating. The always-open specimen is row chrome only. .command-menu is the action cousin — same hover and hairlines, no tick.">
        <Spec tag="DropdownSelect · placeFloating">
          <div className="form-demo-row form-demo-row--fit">
            <span className="field-label" id="demoMenuLabel">Stage</span>
            <DropdownSelect
              id="demoMenuKey"
              valueId="demoMenuValue"
              labelId="demoMenuLabel"
              value={menuStage}
              options={[...OPTION_MENU_SPECIMEN]}
              onChange={(value) => setMenuStage(value as (typeof OPTION_MENU_SPECIMEN)[number])}
            />
          </div>
        </Spec>
        <Spec tag=".option-menu.popover-surface · row grammar (not placement)">
          <OptionMenuSpecimen />
        </Spec>
      </GallerySection>

      <GallerySection id="dialog" title="Dialog" note="Native dialog over an 82% ground scrim; the standard plate divides head, body, and foot with hairlines and leads its title with the warn triangle. Plate width comes in narrow, default, and wide sizes.">
        <div className="spec-row">
          <Spec tag=".dialog-plate--narrow"><Key onClick={() => setDialog("narrow")}>Open narrow confirm</Key></Spec>
          <Spec tag=".dialog > .dialog-plate · head / body / readout / foot"><Key id="dialogOpenKey" onClick={() => setDialog("default")}>Open confirm dialog</Key></Spec>
          <Spec tag=".dialog-plate--wide · .form-row inside"><Key onClick={() => setDialog("wide")}>Open form dialog</Key></Spec>
        </div>
      </GallerySection>

      <DemoDialog open={dialog === "narrow"} onClose={() => setDialog(null)} id="deckDialogNarrow" width="narrow" title="Discard Reply" titleId="deckDialogNarrowTitle" commit="Discard"><p>Your unsent reply will be removed. Unsent text is not part of the examination record.</p></DemoDialog>
      <DemoDialog open={dialog === "default"} onClose={() => setDialog(null)} id="deckDialog" title="Confirm Release" titleId="deckDialogTitle" commit="Release result"><p>Release makes the Result visible to the participant after audited transition. This action is recorded with reviewer identity, timestamp, and evaluation revision.</p><dl className="dialog-readout"><div><dt>Candidate</dt><dd>CND-8842-19</dd></div><div><dt>Review decision</dt><dd>Approve unchanged</dd></div></dl></DemoDialog>
      <DemoDialog open={dialog === "wide"} onClose={() => setDialog(null)} id="deckDialogWide" width="wide" title="Record Accommodation" titleId="deckDialogWideTitle" commit="Record accommodation"><p>Timing accommodations are enrollment-scoped and recorded with administrator identity. Other participants never see this adjustment.</p><FormField id="dlgExtension" label="Time extension" layout="stack">{(controlProps) => <FieldInput {...controlProps} placeholder={ACCOMMODATION_VALUE_PLACEHOLDER} />}</FormField></DemoDialog>
    </>
  );
}
