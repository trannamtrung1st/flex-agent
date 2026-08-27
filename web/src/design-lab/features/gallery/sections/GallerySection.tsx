import type { ReactNode } from "react";
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
    <section
      className="deck-sec"
      id={registryItem.id}
      data-gallery-label={registryItem.label}
      data-gallery-order={gallerySectionIndex(id)}
    >
      <h2 className="sec-title">{title}</h2>
      <p className="sec-note">{note}</p>
      {children}
    </section>
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
    <div className={`spec${wide ? " spec--wide" : ""}${center ? " spec--center" : ""}`}>
      {children}
      <span className="spec-tag">{tag}</span>
    </div>
  );
}
