import type { ReactNode } from "react";
import { Stack } from "../../../../design-system";
import {
  gallerySectionIndex,
  gallerySectionItem,
  type GallerySectionId,
} from "../gallerySections";

export function GallerySection({
  id,
  title,
  note,
  children,
}: {
  id: GallerySectionId;
  title: string;
  note: ReactNode;
  children: ReactNode;
}) {
  const registryItem = gallerySectionItem(id);
  return (
    <Stack
      as="section"
      className="deck-sec"
      gap="none"
      id={registryItem.id}
      data-gallery-label={registryItem.label}
      data-gallery-order={gallerySectionIndex(id)}
    >
      <h2 className="sec-title">{title}</h2>
      <p className="sec-note">{note}</p>
      {children}
    </Stack>
  );
}

export function Spec({
  tag,
  wide,
  center,
  children,
}: {
  tag: ReactNode;
  wide?: boolean;
  center?: boolean;
  children: ReactNode;
}) {
  return (
    <Stack
      className={`spec${wide ? " spec--wide" : ""}${center ? " spec--center" : ""}`}
      gap="2.5"
      align={center ? "center" : wide ? "stretch" : "start"}
    >
      {children}
      <span className="spec-tag">{tag}</span>
    </Stack>
  );
}
