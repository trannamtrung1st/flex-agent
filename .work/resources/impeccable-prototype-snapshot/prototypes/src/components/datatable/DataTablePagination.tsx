import { ChevronGlyph } from "../glyphs";
import { DisclosureMenu } from "../select";
import { Key } from "../keys";
import { pad } from "../../lib/format";

export function DataTablePagination({
  total,
  startIndex,
  visibleCount,
  page,
  pageCount,
  pageSize,
  pageSizeOptions,
  onPageSizeChange,
  onPageChange,
  onPrevious,
  onNext,
}: {
  total: number;
  startIndex: number;
  visibleCount: number;
  page: number;
  pageCount: number;
  pageSize: number;
  pageSizeOptions: readonly number[];
  onPageSizeChange: (pageSize: number) => void;
  onPageChange: (page: number) => void;
  onPrevious: () => void;
  onNext: () => void;
}) {
  const maxPage = Math.max(0, pageCount - 1);

  return (
    <footer className="datatable-foot">
      <span className="datatable-range">
        {total === 0
          ? "00 OF 00"
          : `${pad(startIndex + 1)}–${pad(startIndex + visibleCount)} OF ${pad(total)}`}
      </span>
      <div className="datatable-page-controls">
        <div className="toolbar-group datatable-page-group">
          <DisclosureMenu
            label="Rows"
            value={pad(pageSize)}
            selectedId={String(pageSize)}
            ariaLabel="Rows per page"
            options={pageSizeOptions.map((size) => ({
              id: String(size),
              label: `${pad(size)} per page`,
            }))}
            onSelect={(id) => onPageSizeChange(Number(id))}
          />
          <DisclosureMenu
            label="Page"
            value={pageCount === 0 ? "00" : pad(page + 1)}
            selectedId={String(page)}
            ariaLabel="Select page"
            disabled={pageCount <= 1}
            options={Array.from({ length: pageCount }, (_, index) => ({
              id: String(index),
              label: `${pad(index + 1)} OF ${pad(pageCount)}`,
            }))}
            onSelect={(id) => onPageChange(Number(id))}
          />
        </div>
        <Key
          size="compact"
          className="datatable-step datatable-step--prev"
          disabled={page <= 0 || total === 0}
          onClick={onPrevious}
        >
          <ChevronGlyph />
          <span>Prev</span>
        </Key>
        <Key
          size="compact"
          className="datatable-step datatable-step--next"
          disabled={page >= maxPage || total === 0}
          onClick={onNext}
        >
          <span>Next</span>
          <ChevronGlyph />
        </Key>
      </div>
    </footer>
  );
}
