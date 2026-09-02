import { fireEvent, render, screen } from "@testing-library/react";
import { DataTablePagination } from "./DataTablePagination";

describe("DataTablePagination", () => {
  it("omits page jump and a fake total for signed cursor pages", () => {
    const onNext = vi.fn();
    render(
      <DataTablePagination
        paging="cursor"
        visibleCount={16}
        pageIndex={0}
        pageSize={16}
        pageSizeOptions={[16, 32]}
        hasMore
        onPageSizeChange={() => undefined}
        onPrevious={() => undefined}
        onNext={onNext}
      />,
    );

    expect(screen.getByText("01–16")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /Page/ })).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Prev" })).toBeDisabled();
    fireEvent.click(screen.getByRole("button", { name: "Next" }));
    expect(onNext).toHaveBeenCalledTimes(1);
  });

  it("keeps numbered page jump when the host knows the full count", () => {
    render(
      <DataTablePagination
        total={17}
        startIndex={0}
        visibleCount={16}
        page={0}
        pageCount={2}
        pageSize={16}
        pageSizeOptions={[16, 32]}
        onPageSizeChange={() => undefined}
        onPageChange={() => undefined}
        onPrevious={() => undefined}
        onNext={() => undefined}
      />,
    );

    expect(screen.getByText("01–16 OF 17")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Page/ })).toBeInTheDocument();
  });
});
