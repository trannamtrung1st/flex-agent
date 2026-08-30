import { useRef, type DragEvent } from "react";
import { cx } from "../../../lib/cx";
import { Inline } from "../layout/Inline";
import { Stack } from "../layout/Stack";
import { Key } from "../keys/Key";
import { DocumentGlyph } from "../glyphs/DocumentGlyph";

export type FieldFileMode = "single" | "multiple";

export function fileMatchesAccept(file: File, accept?: string) {
  if (!accept || accept.trim() === "*" || accept.trim() === "") return true;
  const name = file.name.toLowerCase();
  const type = file.type.toLowerCase();
  return accept.split(",").some((raw) => {
    const token = raw.trim().toLowerCase();
    if (!token) return false;
    if (token.startsWith(".")) return name.endsWith(token);
    if (token.endsWith("/*")) return type.startsWith(token.slice(0, -1));
    return type === token;
  });
}

export function formatFileBytes(bytes: number) {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KiB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MiB`;
}

function filesFromList(list: ArrayLike<File> | FileList | null | undefined): File[] {
  if (!list) return [];
  return Array.from(list);
}

function fileKey(file: File) {
  return `${file.name}:${file.size}:${file.lastModified}`;
}

export function mergeSelectedFiles({
  mode,
  current,
  incoming,
  maxFiles,
}: {
  mode: FieldFileMode;
  current: File[];
  incoming: File[];
  maxFiles?: number;
}) {
  const source = mode === "single" ? incoming.slice(0, 1) : [...current, ...incoming];
  const seen = new Set<string>();
  const next: File[] = [];
  for (const file of source) {
    const key = fileKey(file);
    if (seen.has(key)) continue;
    seen.add(key);
    next.push(file);
    if (mode === "single") break;
    if (maxFiles != null && next.length >= maxFiles) break;
  }
  return next;
}

export function FieldFile({
  id,
  labelledBy,
  mode = "multiple",
  accept,
  hint,
  files,
  disabled,
  invalid,
  describedBy,
  maxFiles,
  chooseLabel,
  onFilesChange,
}: {
  id: string;
  labelledBy: string;
  mode?: FieldFileMode;
  accept?: string;
  hint?: string;
  files: File[];
  disabled?: boolean;
  invalid?: boolean;
  describedBy?: string;
  maxFiles?: number;
  chooseLabel?: string;
  onFilesChange: (files: File[]) => void;
}) {
  const inputRef = useRef<HTMLInputElement>(null);
  const dragDepth = useRef(0);
  const wellRef = useRef<HTMLDivElement>(null);
  const draggingRef = useRef(false);

  const setDragging = (value: boolean) => {
    draggingRef.current = value;
    wellRef.current?.classList.toggle("is-dragging", value);
  };

  const applyIncoming = (incoming: File[]) => {
    const allowed = incoming.filter((file) => fileMatchesAccept(file, accept));
    if (allowed.length === 0) return;
    onFilesChange(mergeSelectedFiles({ mode, current: files, incoming: allowed, maxFiles }));
  };

  const onDrop = (event: DragEvent<HTMLDivElement>) => {
    event.preventDefault();
    dragDepth.current = 0;
    setDragging(false);
    if (disabled) return;
    applyIncoming(filesFromList(event.dataTransfer?.files));
  };

  const onDragEnter = (event: DragEvent<HTMLDivElement>) => {
    event.preventDefault();
    if (disabled) return;
    dragDepth.current += 1;
    setDragging(true);
  };

  const onDragLeave = (event: DragEvent<HTMLDivElement>) => {
    event.preventDefault();
    dragDepth.current = Math.max(0, dragDepth.current - 1);
    if (dragDepth.current === 0) setDragging(false);
  };

  const onDragOver = (event: DragEvent<HTMLDivElement>) => {
    event.preventDefault();
    if (disabled) return;
    event.dataTransfer.dropEffect = "copy";
  };

  const seated = files.length > 0;
  const choose = chooseLabel ?? (mode === "single" ? "Choose file" : "Choose files");
  const listLabel = mode === "single" ? "Selected file" : "Selected files";
  const dropLead = mode === "single" ? "Drop a file onto this bay" : "Drop files onto this bay";

  return (
    <Stack
      className="field-file"
      gap="2"
      role="group"
      aria-labelledby={labelledBy}
      aria-describedby={describedBy}
      aria-invalid={invalid || undefined}
      aria-disabled={disabled || undefined}
    >
      <div className="visually-hidden" aria-hidden="true">
        <input
          ref={inputRef}
          id={id}
          type="file"
          accept={accept}
          multiple={mode === "multiple"}
          disabled={disabled}
          tabIndex={-1}
          aria-hidden="true"
          onChange={(event) => {
            applyIncoming(filesFromList(event.target.files));
            event.target.value = "";
          }}
        />
      </div>
      <div
        ref={wellRef}
        className={cx(
          "field-file-well",
          seated && "has-files",
          invalid && "is-invalid",
          disabled && "is-disabled",
        )}
        onDragEnter={onDragEnter}
        onDragOver={onDragOver}
        onDragLeave={onDragLeave}
        onDrop={onDrop}
      >
        <Inline className="field-file-row" gap="3" align="center" justify="between" wrap>
          <Stack className="field-file-copy" gap="1">
            <span className="field-file-lead">{dropLead}</span>
            {hint ? <span className="field-file-hint">{hint}</span> : null}
          </Stack>
          <Key variant="quiet" disabled={disabled} onClick={() => inputRef.current?.click()}>
            {choose}
          </Key>
        </Inline>
      </div>
      {seated ? (
        <ul className="field-file-list" aria-label={listLabel}>
          {files.map((file) => (
            <li className="field-file-item" key={fileKey(file)}>
              <DocumentGlyph current />
              <span className="field-file-name" title={file.name}>
                {file.name}
              </span>
              <span className="field-file-meta">
                {file.type || "unknown type"} · {formatFileBytes(file.size)}
              </span>
              <Key
                variant="quiet"
                size="compact"
                disabled={disabled}
                ariaLabel={`Remove ${file.name}`}
                onClick={() => onFilesChange(files.filter((item) => fileKey(item) !== fileKey(file)))}
              >
                Remove
              </Key>
            </li>
          ))}
        </ul>
      ) : null}
    </Stack>
  );
}
