import { fireEvent, render, screen, within } from "@testing-library/react";
import { ItemList } from "./ItemList";

const campaigns = [
  { id: "cmp-0042", title: "Access Review" },
  { id: "cmp-0043", title: "Policy Walkthrough" },
];

describe("ItemList", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });
  it("renders custom row content from renderItem as named list items", () => {
    render(
      <ItemList
        items={campaigns}
        itemKey={(item) => item.id}
        label="Campaigns"
        renderItem={(item) => (
          <span>
            {item.title}
            <button type="button">Open {item.title}</button>
          </span>
        )}
      />,
    );

    const list = screen.getByRole("list", { name: "Campaigns" });
    expect(list.tagName).toBe("UL");
    expect(list).toHaveClass("item-list");
    expect(within(list).getAllByRole("listitem")).toHaveLength(2);
    expect(screen.getByRole("button", { name: "Open Access Review" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Open Policy Walkthrough" })).toBeInTheDocument();
  });

  it("places Load more as a trailing list item and invokes onClick", () => {
    const onClick = vi.fn();
    render(
      <ItemList
        items={campaigns}
        itemKey={(item) => item.id}
        label="Campaigns"
        renderItem={(item) => item.title}
        loadMore={{ onClick, children: "Load more campaigns" }}
      />,
    );

    const items = screen.getAllByRole("listitem");
    expect(items).toHaveLength(3);
    expect(items[2]).toHaveClass("item-list__more");
    expect(screen.getByRole("button", { name: "Load more campaigns" })).toHaveClass("item-list__more-key");
    fireEvent.click(screen.getByRole("button", { name: "Load more campaigns" }));
    expect(onClick).toHaveBeenCalledTimes(1);
  });

  it("uses onLoadMore as the canonical request callback", () => {
    const onLoadMore = vi.fn();
    render(
      <ItemList
        items={campaigns}
        itemKey={(item) => item.id}
        label="Campaigns"
        renderItem={(item) => item.title}
        loadMore={{ onLoadMore, children: "Load more campaigns" }}
      />,
    );

    fireEvent.click(screen.getByRole("button", { name: "Load more campaigns" }));
    expect(onLoadMore).toHaveBeenCalledTimes(1);
  });

  it("occupies Load more while waiting and does not fire again", () => {
    const onClick = vi.fn();
    render(
      <ItemList
        items={campaigns}
        itemKey={(item) => item.id}
        label="Campaigns"
        renderItem={(item) => item.title}
        loadMore={{ onClick, waiting: true, children: "Load more campaigns" }}
      />,
    );

    const key = screen.getByRole("button", { name: "Load more campaigns" });
    expect(key).toBeDisabled();
    expect(key).toHaveAttribute("aria-busy", "true");
    expect(key).toHaveClass("is-waiting");
    fireEvent.click(key);
    expect(onClick).not.toHaveBeenCalled();
  });

  it("omits Load more when loadMore is not provided", () => {
    render(
      <ItemList
        items={campaigns}
        itemKey={(item) => item.id}
        label="Campaigns"
        renderItem={(item) => item.title}
      />,
    );

    expect(screen.queryByRole("button", { name: /Load more/i })).not.toBeInTheDocument();
    expect(screen.getAllByRole("listitem")).toHaveLength(2);
  });

  it("names a nested scroll region when scroll is enabled", () => {
    render(
      <ItemList
        items={campaigns}
        itemKey={(item) => item.id}
        label="Campaigns"
        renderItem={(item) => item.title}
        scroll
        loadMore={{ onClick: () => undefined, children: "Load more campaigns" }}
      />,
    );

    const region = screen.getByRole("region", { name: "Campaigns, scrollable" });
    expect(region).toHaveClass("item-list-scroll");
    expect(region).toHaveAttribute("tabindex", "0");
    expect(within(region).getByRole("list", { name: "Campaigns" })).toBeInTheDocument();
    expect(within(region).getByRole("button", { name: "Load more campaigns" })).toBeInTheDocument();
  });

  it("omits the Load more key in end trigger and requests when the sentinel intersects", () => {
    const observed: Element[] = [];
    const observers: IntersectionObserverCallback[] = [];
    vi.stubGlobal(
      "IntersectionObserver",
      class {
        readonly root: Element | null;
        constructor(callback: IntersectionObserverCallback, options?: IntersectionObserverInit) {
          observers.push(callback);
          this.root = (options?.root as Element | null) ?? null;
        }
        observe(target: Element) {
          observed.push(target);
        }
        unobserve() {}
        disconnect() {}
        takeRecords(): IntersectionObserverEntry[] {
          return [];
        }
      },
    );
    const onLoadMore = vi.fn();

    render(
      <ItemList
        items={campaigns}
        itemKey={(item) => item.id}
        label="Campaigns"
        renderItem={(item) => item.title}
        scroll
        loadMore={{ onLoadMore, trigger: "end" }}
      />,
    );

    expect(screen.queryByRole("button", { name: /Load more/i })).not.toBeInTheDocument();
    const sentinel = document.querySelector(".item-list__end");
    expect(sentinel).toBeTruthy();
    expect(observed).toContain(sentinel);
    expect(observers).toHaveLength(1);

    observers[0](
      [{ isIntersecting: true } as IntersectionObserverEntry],
      {} as IntersectionObserver,
    );
    expect(onLoadMore).toHaveBeenCalledTimes(1);
  });

  it("does not auto-request while end trigger is waiting", () => {
    const observers: IntersectionObserverCallback[] = [];
    vi.stubGlobal(
      "IntersectionObserver",
      class {
        constructor(callback: IntersectionObserverCallback) {
          observers.push(callback);
        }
        observe() {}
        unobserve() {}
        disconnect() {}
        takeRecords(): IntersectionObserverEntry[] {
          return [];
        }
      },
    );
    const onLoadMore = vi.fn();

    render(
      <ItemList
        items={campaigns}
        itemKey={(item) => item.id}
        label="Campaigns"
        renderItem={(item) => item.title}
        scroll
        loadMore={{ onLoadMore, trigger: "end", waiting: true }}
      />,
    );

    expect(screen.getByRole("status")).toHaveTextContent("Loading more");
    expect(observers).toHaveLength(0);
    expect(onLoadMore).not.toHaveBeenCalled();
  });
});
