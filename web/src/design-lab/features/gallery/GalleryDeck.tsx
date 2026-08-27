import type { MouseEvent } from "react";
import { CATALOG_ROUTE } from "../../data/fixtures/surfaces";
import { Stack, useToasts } from "../../components";
import { ReferenceLayout } from "../../../design-system/lab";
import { DataSections } from "./sections/DataSections";
import { FeedbackSections } from "./sections/FeedbackSections";
import { FoundationsSections } from "./sections/FoundationsSections";
import { InputSections } from "./sections/InputSections";
import { LayoutPrimitiveSections } from "./sections/LayoutPrimitiveSections";
import { LayoutSections } from "./sections/LayoutSections";
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
    <ReferenceLayout
      commandStrip={{
        homeTo: CATALOG_ROUTE,
        homeLabel: "Channel index",
        origin: true,
        brandSuffix: "Component Deck",
        readout: "SHARED LAYER · SHIPBOARD TERMINAL",
      }}
      index={{
        groups: gallerySections,
        activeId,
        onDeckClick,
      }}
      footerNote="Component deck — synthetic specimen content. Consumed by all five prototype surfaces."
    >
      <Stack gap="3">
        <h1 className="deck-title">Shared component deck</h1>
        <p className="deck-note">Every interactive specimen below renders a promoted shared module from the design system. Specimens marked amber are rationed on live surfaces: one hot key, one attention voice per region.</p>
      </Stack>
      <FoundationsSections />
      <NavigationSections />
      <DataSections announce={pushToast} />
      <FeedbackSections toasts={toasts} pushToast={pushToast} />
      <LayoutSections />
      <LayoutPrimitiveSections />
      <InputSections />
    </ReferenceLayout>
  );
}
