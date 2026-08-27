import type { To } from "react-router-dom";
import type { ComponentProps } from "react";
import { CommandStrip as SharedCommandStrip } from "../../../design-system/components/chrome/CommandStrip";
import { CATALOG_ROUTE } from "./operator";

export type { CommandStripNavItem } from "../../../design-system/components/chrome/CommandStrip";

type SharedProps = ComponentProps<typeof SharedCommandStrip>;

export function CommandStrip({
  homeTo = CATALOG_ROUTE,
  homeLabel = "Channel index",
  ...props
}: Omit<SharedProps, "homeTo" | "homeLabel"> & { homeTo?: To; homeLabel?: string }) {
  return <SharedCommandStrip {...props} homeTo={homeTo} homeLabel={homeLabel} />;
}
