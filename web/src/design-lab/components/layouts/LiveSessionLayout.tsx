import { LiveSessionLayout as ProductionLiveSessionLayout, type LiveSessionLayoutProps } from "../../../design-system/patterns/layouts/LiveSessionLayout";

export type { LiveSessionLayoutProps };

/** Lab default home is the channel catalog; production default remains `/`. */
export function LiveSessionLayout(props: LiveSessionLayoutProps) {
  return <ProductionLiveSessionLayout {...props} homeTo={props.homeTo ?? "/surfaces"} />;
}
