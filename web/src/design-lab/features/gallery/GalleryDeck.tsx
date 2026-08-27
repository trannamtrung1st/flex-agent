import type { MouseEvent } from "react";
import { IndexRail } from "../../../design-system/components/navigation";
import { CommandStrip, ConsoleFoot, Key, useToasts } from "../../components";
import { DataSections } from "./sections/DataSections";
import { FeedbackSections } from "./sections/FeedbackSections";
import { FoundationsSections } from "./sections/FoundationsSections";
import { InputSections } from "./sections/InputSections";
import { NavigationSections } from "./sections/NavigationSections";
import { gallerySections } from "./gallerySections";
import { useGalleryScrollSpy } from "./useGalleryScrollSpy";

export function GalleryDeck() {
  const { activeId, navigate } = useGalleryScrollSpy();
  const { toasts, pushToast } = useToasts();

  const onDeckClick = (event: MouseEvent<HTMLDivElement>) => {
    const link = (event.target as HTMLElement).closest<HTMLAnchorElement>(".deck-rail a[href^='#']");
    if (!link) return;
    const id = link.hash.slice(1);
    if (!gallerySections.some((group) => group.items.some((item) => item.id === id))) return;
    event.preventDefault();
    navigate(id);
  };

  return (
    <>
      <CommandStrip
        origin
        brandSuffix="Component Deck"
        readout="SHARED LAYER · SHIPBOARD TERMINAL"
        identLeading={<Key to="/surfaces" size="compact">Index</Key>}
        className="page-strip"
      />
      <div className="deck" onClick={onDeckClick}>
        <IndexRail groups={gallerySections} activeId={activeId} />
        <main className="deck-main">
          <h1 className="deck-title">Shared component deck</h1>
          <p className="deck-note">Every interactive specimen below renders a promoted shared module from the design system. Specimens marked amber are rationed on live surfaces: one hot key, one attention voice per region.</p>
          <FoundationsSections />
          <NavigationSections />
          <DataSections announce={pushToast} />
          <FeedbackSections toasts={toasts} pushToast={pushToast} />
          <InputSections />
          <ConsoleFoot note="Component deck — synthetic specimen content. Consumed by all five prototype surfaces." />
        </main>
      </div>
    </>
  );
}
