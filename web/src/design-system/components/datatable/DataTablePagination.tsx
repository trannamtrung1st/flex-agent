import { ChevronGlyph } from "../glyphs";
import { DisclosureMenu } from "../select";
import { Key, KeyGroup } from "../keys";
import { pad } from "../../../lib/format";

type PaginationChrome = {
  pageSize: number;
  pageSizeOptions: readonly number[];
  onPageSizeChange: (pageSize: number) => void;
  onPrevious: () => void;
  onNext: () => void;
  waiting?: boolean;
};

export type NumberedDataTablePagination = PaginationChrome & {
  paging?: "numbered";
  total: number;
  startIndex: number;
  visibleCount: number;
  page: number;
  pageCount: number;
  onPageChange: (page: number) => void;
};

export type CursorDataTablePagination = PaginationChrome & {
  paging: "cursor";
  visibleCount: number;
  pageIndex: number;
  hasMore: boolean;
};

export type DataTablePaginationProps = NumberedDataTablePagination | CursorDataTablePagination;

function isCursorPaging(props: DataTablePaginationProps): props is CursorDataTablePagination {
  return props.paging === "cursor";
}

export function DataTablePagination(props: DataTablePaginationProps) {
  const waiting = Boolean(props.waiting);
  const cursor = isCursorPaging(props);
  const empty = cursor ? props.visibleCount === 0 && props.pageIndex === 0 : props.total === 0;
  const canPrevious = cursor ? props.pageIndex > 0 : props.page > 0 && !empty;
  const canNext = cursor ? props.hasMore : props.page < Math.max(0, props.pageCount - 1) && !empty;
  const range = cursor
    ? cursorRange(props.pageIndex, props.pageSize, props.visibleCount)
    : numberedRange(props.total, props.startIndex, props.visibleCount);

  return (
    <footer className="datatable-foot" aria-busy={waiting || undefined}>
      <span className="datatable-range">{range}</span>
      <div className="datatable-page-controls">
        <div className="toolbar-group datatable-page-group">
          <DisclosureMenu
            label="Rows"
            value={pad(props.pageSize)}
            selectedId={String(props.pageSize)}
            ariaLabel="Rows per page"
            disabled={waiting}
            options={props.pageSizeOptions.map((size) => ({
              id: String(size),
              label: `${pad(size)} per page`,
            }))}
            onSelect={(id) => props.onPageSizeChange(Number(id))}
          />
          {cursor ? null : (
            <DisclosureMenu
              label="Page"
              value={props.pageCount === 0 ? "00" : pad(props.page + 1)}
              selectedId={String(props.page)}
              ariaLabel="Select page"
              disabled={props.pageCount <= 1 || waiting}
              options={Array.from({ length: props.pageCount }, (_, index) => ({
                id: String(index),
                label: `${pad(index + 1)} OF ${pad(props.pageCount)}`,
              }))}
              onSelect={(id) => props.onPageChange(Number(id))}
            />
          )}
        </div>
        <KeyGroup>
          <Key
            size="compact"
            className="datatable-step datatable-step--prev"
            disabled={!canPrevious || waiting}
            onClick={props.onPrevious}
          >
            <ChevronGlyph />
            <span>Prev</span>
          </Key>
          <Key
            size="compact"
            className="datatable-step datatable-step--next"
            disabled={!canNext || waiting}
            onClick={props.onNext}
          >
            <span>Next</span>
            <ChevronGlyph />
          </Key>
        </KeyGroup>
      </div>
    </footer>
  );
}

function numberedRange(total: number, startIndex: number, visibleCount: number) {
  return total === 0
    ? "00 OF 00"
    : `${pad(startIndex + 1)}–${pad(startIndex + visibleCount)} OF ${pad(total)}`;
}

function cursorRange(pageIndex: number, pageSize: number, visibleCount: number) {
  if (visibleCount === 0) {
    return "00 OF 00";
  }

  const start = pageIndex * pageSize + 1;
  return `${pad(start)}–${pad(start + visibleCount - 1)}`;
}
