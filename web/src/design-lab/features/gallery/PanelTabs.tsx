import { useId, useRef, useState, type KeyboardEvent, type ReactNode } from "react";

export type PanelTab = {
  id: string;
  label: string;
  panel: ReactNode;
};

export function PanelTabs({ label, tabs }: { label: string; tabs: readonly PanelTab[] }) {
  const uid = useId();
  const [selected, setSelected] = useState(0);
  const refs = useRef<Array<HTMLButtonElement | null>>([]);

  const focusAndSelect = (index: number) => {
    const next = (index + tabs.length) % tabs.length;
    setSelected(next);
    refs.current[next]?.focus();
  };

  const onKeyDown = (event: KeyboardEvent<HTMLButtonElement>, index: number) => {
    if (event.key === "ArrowRight") {
      event.preventDefault();
      focusAndSelect(index + 1);
    } else if (event.key === "ArrowLeft") {
      event.preventDefault();
      focusAndSelect(index - 1);
    } else if (event.key === "Home") {
      event.preventDefault();
      focusAndSelect(0);
    } else if (event.key === "End") {
      event.preventDefault();
      focusAndSelect(tabs.length - 1);
    } else if (event.key === "Enter" || event.key === " ") {
      event.preventDefault();
      setSelected(index);
    }
  };

  return (
    <div className="panel-tabs">
      <div className="panel-tablist" role="tablist" aria-label={label}>
        {tabs.map((tab, index) => (
          <button
            ref={(node) => { refs.current[index] = node; }}
            type="button"
            className={`panel-tab${selected === index ? " is-current" : ""}`}
            role="tab"
            id={`${uid}-tab-${tab.id}`}
            aria-selected={selected === index}
            aria-controls={`${uid}-panel-${tab.id}`}
            tabIndex={selected === index ? 0 : -1}
            key={tab.id}
            onClick={() => setSelected(index)}
            onKeyDown={(event) => onKeyDown(event, index)}
          >
            {tab.label}
          </button>
        ))}
      </div>
      <div className="panel-panels">
        {tabs.map((tab, index) => (
          <div
            className="panel-panel"
            role="tabpanel"
            id={`${uid}-panel-${tab.id}`}
            aria-labelledby={`${uid}-tab-${tab.id}`}
            hidden={selected !== index}
            key={tab.id}
          >
            {tab.panel}
          </div>
        ))}
      </div>
    </div>
  );
}
