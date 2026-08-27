import { GalleryDeck } from "../features/gallery/GalleryDeck";
import { useSurface } from "../lib/useSurface";

export function GalleryPage() {
  useSurface("gallery");
  return <GalleryDeck />;
}
