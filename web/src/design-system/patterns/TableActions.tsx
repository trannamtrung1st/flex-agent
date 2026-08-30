import {
  forwardRef,
  useCallback,
  useId,
  useState,
} from "react";
import { ActionMenuGlyph } from "../components/glyphs/ActionMenuGlyph";
import { ChevronGlyph } from "../components/glyphs/ChevronGlyph";
import { NativeDialog } from "../components/overlays/NativeDialog";
import { DialogPlate, DialogPlateBody, DialogPlateFooter, DialogPlateHead } from "../components/overlays/DialogPlate";
import { DropdownMenu, DropdownMenuItem, DropdownMenuSeparator, type DropdownMenuTriggerBind } from "../components/menu";
import { IconButton, Key, KeyGroup, TooltipHost } from "../components/keys";
import {
  deriveHeaderSelectionState,
  headerCheckboxState,
  headerSelectionLabel,
  selectionCopy,
  transitionHeaderSelection,
  type HeaderSelectionState,
  type TableSelection,
} from "./tableSelection";

export type ActionEligibility = { allowed: true } | { allowed: false; reason: string };

export type ActionConfirmation = {
  title: string;
  body: string;
  commitLabel: string;
};

export type ActionResult = { ok: true; message?: string; label?: string } | { ok: false; message: string };

export type TableAction<T> = {
  id: string;
  label: string;
  compactLabel?: string;
  tooltip?: string;
  kind: "standard" | "destructive";
  placement: "primary" | "overflow";
  surfaces?: Array<"row" | "bulk" | "table">;
  eligibility: (records: T[]) => ActionEligibility;
  confirmation?: (records: T[]) => ActionConfirmation;
  run: (records: T[]) => Promise<ActionResult> | ActionResult;
};

export function actionSurfaces<T>(action: TableAction<T>) {
  return action.surfaces ?? (["row", "bulk"] as const);
}

function selectMarkClass(scope?: HeaderSelectionState, indeterminate?: boolean) {
  if (scope === "matching") return " select-mark--matching";
  if (scope === "page") return " select-mark--page";
  if (scope === "partial" || indeterminate) return " select-mark--partial is-indeterminate";
  return "";
}

export function SelectMark({
  checked,
  indeterminate = false,
  label,
  onChange,
  id,
}: {
  checked: boolean;
  indeterminate?: boolean;
  label: string;
  onChange: (checked: boolean) => void;
  id?: string;
}) {
  return (
    <label className={id ? "select-head" : "select-cell"}>
      <input
        id={id}
        type="checkbox"
        className="visually-hidden"
        aria-label={label}
        checked={checked}
        ref={(el) => {
          if (el) el.indeterminate = indeterminate;
        }}
        onChange={(e) => onChange(e.target.checked)}
      />
      <span
        className={`select-mark${indeterminate ? " is-indeterminate select-mark--partial" : ""}`}
        aria-hidden="true"
      />
    </label>
  );
}

export const HeaderSelectionControl = forwardRef<
  HTMLInputElement,
  {
    id: string;
    selection: TableSelection;
    pageIds: string[];
    matchingIds: string[];
    queryKey: string;
    noun: string;
    onTransition: (next: TableSelection) => void;
  }
>(function HeaderSelectionControl(
  { id, selection, pageIds, matchingIds, queryKey, noun, onTransition },
  ref,
) {
  const scope = deriveHeaderSelectionState(selection, pageIds, matchingIds);
  const { checked, indeterminate } = headerCheckboxState(scope);
  const { ariaLabel, tooltip } = headerSelectionLabel(scope, pageIds, matchingIds, selection, noun);

  return (
    <TooltipHost tip={tooltip}>
      <label className="select-head">
        <input
          id={id}
          type="checkbox"
          className="visually-hidden"
          aria-label={ariaLabel}
          checked={checked}
          ref={(el) => {
            if (typeof ref === "function") ref(el);
            else if (ref) ref.current = el;
            if (el) el.indeterminate = indeterminate;
          }}
          onChange={() => {
            onTransition(transitionHeaderSelection(selection, pageIds, matchingIds, queryKey));
          }}
        />
        <span className={`select-mark${selectMarkClass(scope, indeterminate)}`} aria-hidden="true" />
      </label>
    </TooltipHost>
  );
});

type MenuEntry<T> =
  | { type: "action"; action: TableAction<T> }
  | { type: "separator" };

export function rowMenuEntries<T>(actions: TableAction<T>[]): MenuEntry<T>[] {
  const rowActions = actions.filter((action) => actionSurfaces(action).includes("row"));
  const standard = rowActions.filter((action) => action.kind !== "destructive");
  const destructive = rowActions.filter((action) => action.kind === "destructive");
  const entries: MenuEntry<T>[] = standard.map((action) => ({ type: "action", action }));
  if (standard.length && destructive.length) entries.push({ type: "separator" });
  destructive.forEach((action) => entries.push({ type: "action", action }));
  return entries;
}

function actionFace<T>(action: TableAction<T>) {
  return action.compactLabel ?? action.label;
}

function bulkEligibility<T>(
  action: TableAction<T>,
  records: T[],
  noun: string,
): ActionEligibility {
  if (!records.length) {
    return { allowed: false, reason: `Select one or more ${noun}.` };
  }
  return action.eligibility(records);
}

export function CommandMenu<T>({
  open,
  onOpenChange,
  triggerLabel,
  triggerCaption = "More",
  compactTrigger = false,
  triggerId,
  records,
  entries,
  onChoose,
  busyActionId,
  triggerDisabled,
  triggerDisabledReason,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  triggerLabel: string;
  triggerCaption?: string;
  compactTrigger?: boolean;
  triggerSize?: "compact" | "standard";
  triggerId?: string;
  records: T[];
  entries: MenuEntry<T>[];
  onChoose: (action: TableAction<T>, trigger: HTMLElement) => void;
  busyActionId?: string | null;
  triggerDisabled?: boolean;
  triggerDisabledReason?: string;
}) {
  const generatedId = useId();
  const resolvedTriggerId = triggerId ?? generatedId;

  const choose = (action: TableAction<T>, disabled: boolean) => {
    if (disabled) return;
    const trigger = document.getElementById(resolvedTriggerId);
    onOpenChange(false);
    if (!(trigger instanceof HTMLElement)) return;
    trigger.focus();
    onChoose(action, trigger);
  };

  const triggerFace = compactTrigger ? (
    (bind: DropdownMenuTriggerBind) => (
      <IconButton
        ref={bind.ref}
        id={resolvedTriggerId}
        className={`command-menu-trigger command-menu-trigger--icon${open ? " is-open" : ""}`}
        label={triggerLabel}
        tooltip="More actions"
        disabled={triggerDisabled}
        disabledReason={triggerDisabledReason}
        hasPopup="menu"
        expanded={open}
        controls={bind["aria-controls"]}
        onClick={bind.onClick}
        onKeyDown={bind.onKeyDown}
      >
        <ActionMenuGlyph />
      </IconButton>
    )
  ) : (
    (bind: DropdownMenuTriggerBind) => (
      <Key
        ref={bind.ref}
        variant="quiet"
        size="compact"
        className={`command-menu-trigger command-menu-trigger--compact${open ? " is-open" : ""}`}
        id={resolvedTriggerId}
        ariaLabel={triggerLabel}
        ariaHasPopup="menu"
        ariaExpanded={open}
        ariaControls={bind["aria-controls"]}
        disabled={triggerDisabled}
        disabledReason={triggerDisabledReason}
        onClick={bind.onClick}
        onKeyDown={bind.onKeyDown}
      >
        <span>{triggerCaption}</span>
        <ChevronGlyph />
      </Key>
    )
  );

  return (
    <div className="command-menu-root" onClick={(event) => event.stopPropagation()}>
      <DropdownMenu
        open={open}
        onOpenChange={onOpenChange}
        align="end"
        placement={compactTrigger ? "fixed" : "connected"}
        labelledBy={resolvedTriggerId}
        triggerDisabled={triggerDisabled}
        trigger={triggerFace}
      >
        {entries.map((entry, index) => {
          if (entry.type === "separator") {
            return <DropdownMenuSeparator key={`sep-${index}`} />;
          }
          const eligibility = entry.action.eligibility(records);
          const disabled = !eligibility.allowed || busyActionId === entry.action.id;
          return (
            <DropdownMenuItem
              key={entry.action.id}
              disabled={disabled}
              destructive={entry.action.kind === "destructive"}
              onSelect={() => choose(entry.action, disabled)}
            >
              <span className="command-menu-item-label menu-row-label">{entry.action.label}</span>
              {!eligibility.allowed ? (
                <span className="command-menu-item-reason menu-row-reason">{eligibility.reason}</span>
              ) : null}
            </DropdownMenuItem>
          );
        })}
      </DropdownMenu>
    </div>
  );
}

export function RowActionMenu<T>({
  open,
  onOpenChange,
  label,
  records,
  actions,
  onChoose,
  busyActionId,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  label: string;
  records: T[];
  actions: TableAction<T>[];
  onChoose: (action: TableAction<T>, trigger: HTMLElement) => void;
  busyActionId?: string | null;
}) {
  const triggerId = useId();
  return (
    <CommandMenu
      open={open}
      onOpenChange={onOpenChange}
      triggerLabel={label}
      compactTrigger
      triggerId={triggerId}
      records={records}
      entries={rowMenuEntries(actions)}
      onChoose={onChoose}
      busyActionId={busyActionId}
      triggerDisabled={Boolean(busyActionId)}
    />
  );
}

export function TableSelectionBand({
  selection,
  pageIds,
  matchingIds,
  noun,
  onClear,
  headerSelectId,
}: {
  selection: TableSelection;
  pageIds: string[];
  matchingIds: string[];
  noun: string;
  onClear: () => void;
  headerSelectId?: string;
}) {
  const copy = selectionCopy(selection, pageIds, matchingIds, noun);
  if (copy.count === 0) return null;

  return (
    <div className="datatable-selection-band" aria-live="polite">
      <span className="datatable-selection-note">
        {copy.label}
        <span className="datatable-selection-sep" aria-hidden="true">
          {" "}
          ·{" "}
        </span>
        <button
          type="button"
          className="clear-action"
          onClick={() => {
            onClear();
            if (headerSelectId) document.getElementById(headerSelectId)?.focus();
          }}
        >
          Clear
        </button>
      </span>
    </div>
  );
}

export function TableActionBar<T>({
  selection,
  pageIds,
  matchingIds,
  noun,
  actions,
  records,
  onChoose,
  busyActionId,
}: {
  selection: TableSelection;
  pageIds: string[];
  matchingIds: string[];
  noun: string;
  actions: TableAction<T>[];
  records: T[];
  onChoose: (action: TableAction<T>, trigger: HTMLElement) => void;
  busyActionId?: string | null;
}) {
  const copy = selectionCopy(selection, pageIds, matchingIds, noun);
  const selected = copy.count > 0;
  const tableLevel = actions.filter((action) => actionSurfaces(action).includes("table"));
  const bulkActions = actions.filter((action) => actionSurfaces(action).includes("bulk"));
  const primary = bulkActions.filter((action) => action.placement === "primary");
  const overflow = bulkActions.filter((action) => action.placement === "overflow");
  const [moreOpen, setMoreOpen] = useState(false);
  const hasActions = tableLevel.length > 0 || bulkActions.length > 0;

  if (!hasActions) return null;

  const busy = Boolean(busyActionId);
  const overflowDisabled =
    !selected ||
    overflow.every((action) => {
      const eligibility = bulkEligibility(action, records, noun);
      return !eligibility.allowed;
    });
  const overflowReason = !selected ? `Select one or more ${noun}.` : undefined;

  return (
    <div className="datatable-actions" aria-label="Table actions">
      <KeyGroup className="datatable-actions-keys">
        {tableLevel.map((action) => {
          const eligibility = action.eligibility(records);
          const waiting = busyActionId === action.id;
          const face = actionFace(action);
          return (
            <Key
              key={action.id}
              size="compact"
              waiting={waiting}
              disabled={!eligibility.allowed || busy}
              ariaLabel={action.label}
              tooltip={action.tooltip ?? (face !== action.label ? action.label : undefined)}
              disabledReason={!eligibility.allowed ? eligibility.reason : undefined}
              onClick={() => {
                const trigger = document.activeElement instanceof HTMLElement ? document.activeElement : null;
                if (trigger) onChoose(action, trigger);
              }}
            >
              {actionFace(action)}
            </Key>
          );
        })}
        {primary.map((action) => {
          const eligibility = bulkEligibility(action, records, noun);
          const domain = selected ? action.eligibility(records) : eligibility;
          const allowed = domain.allowed;
          const reason = !allowed ? domain.reason : undefined;
          const waiting = busyActionId === action.id;
          const face = actionFace(action);
          return (
            <Key
              key={action.id}
              size="compact"
              waiting={waiting}
              disabled={!allowed || busy}
              ariaLabel={action.label}
              tooltip={action.tooltip ?? (face !== action.label ? action.label : undefined)}
              disabledReason={reason}
              onClick={() => {
                const trigger = document.activeElement instanceof HTMLElement ? document.activeElement : null;
                if (trigger) onChoose(action, trigger);
              }}
            >
              {actionFace(action)}
            </Key>
          );
        })}
        {overflow.length ? (
          <CommandMenu
            open={moreOpen}
            onOpenChange={setMoreOpen}
            triggerLabel="More actions"
            triggerCaption="More"
            triggerSize="compact"
            triggerDisabled={overflowDisabled || busy}
            triggerDisabledReason={overflowReason}
            records={records}
            entries={overflow.map((action) => ({ type: "action" as const, action }))}
            onChoose={onChoose}
            busyActionId={busyActionId}
          />
        ) : null}
      </KeyGroup>
    </div>
  );
}

export function ActionConfirmDialog({
  open,
  confirmation,
  error,
  waiting,
  onCancel,
  onConfirm,
}: {
  open: boolean;
  confirmation: ActionConfirmation | null;
  error: string | null;
  waiting: boolean;
  onCancel: () => void;
  onConfirm: () => void;
}) {
  if (!confirmation) return null;
  return (
    <NativeDialog open={open} onClose={waiting ? () => undefined : onCancel} className="dialog" labelledBy="tableActionConfirmTitle">
      <DialogPlate>
        <DialogPlateHead title={confirmation.title} titleId="tableActionConfirmTitle" />
        <DialogPlateBody>
          <p>{confirmation.body}</p>
          {error ? (
            <p className="field-error" id="tableActionConfirmError" role="alert">
              {error}
            </p>
          ) : null}
        </DialogPlateBody>
        <DialogPlateFooter
          arrangement="split"
          secondary={
            <Key disabled={waiting} onClick={onCancel}>
              Cancel
            </Key>
          }
          primary={
            <Key variant="activate" waiting={waiting} disabled={waiting} onClick={onConfirm}>
              {confirmation.commitLabel}
            </Key>
          }
        />
      </DialogPlate>
    </NativeDialog>
  );
}

export function useTableActionRunner<T>(headerSelectId?: string) {
  const [pending, setPending] = useState<{
    action: TableAction<T>;
    records: T[];
    trigger: HTMLElement | null;
  } | null>(null);
  const [running, setRunning] = useState(false);
  const [busyActionId, setBusyActionId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const restore = useCallback((trigger: HTMLElement | null) => {
    requestAnimationFrame(() => {
      if (trigger && document.contains(trigger)) {
        trigger.focus();
        return;
      }
      if (headerSelectId) {
        const header = document.getElementById(headerSelectId);
        if (header) {
          header.focus();
          return;
        }
      }
      document.querySelector<HTMLElement>(".operate-title")?.focus();
    });
  }, [headerSelectId]);

  const runAction = useCallback(async (action: TableAction<T>, records: T[], trigger: HTMLElement | null) => {
    setRunning(true);
    setBusyActionId(action.id);
    setError(null);
    try {
      const result = await action.run(records);
      if (!result.ok) {
        setError(result.message);
        return result;
      }
      setPending(null);
      restore(trigger);
      return result;
    } catch (err) {
      const message = err instanceof Error ? err.message : "The action could not complete. Selection is preserved.";
      setError(message);
      return { ok: false, message } satisfies ActionResult;
    } finally {
      setRunning(false);
      setBusyActionId(null);
    }
  }, [restore]);

  const choose = useCallback((action: TableAction<T>, records: T[], trigger: HTMLElement) => {
    const eligibility = action.eligibility(records);
    if (!eligibility.allowed) return;
    if (action.confirmation) {
      requestAnimationFrame(() => {
        setError(null);
        setPending({ action, records, trigger });
      });
      return;
    }
    void runAction(action, records, trigger);
  }, [runAction]);

  const cancel = useCallback(() => {
    if (running) return;
    const trigger = pending?.trigger ?? null;
    setPending(null);
    setError(null);
    restore(trigger);
  }, [pending, restore, running]);

  const confirm = useCallback(() => {
    if (!pending || running) return;
    void runAction(pending.action, pending.records, pending.trigger);
  }, [pending, runAction, running]);

  return {
    pending,
    running,
    error,
    busyActionId,
    choose,
    cancel,
    confirm,
    confirmation: pending?.action.confirmation?.(pending.records) ?? null,
  };
}

export type { TableSelection, HeaderSelectionState };
