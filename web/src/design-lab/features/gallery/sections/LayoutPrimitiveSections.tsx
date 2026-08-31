import { Container, EllipsisKey, Grid, Inline, Inset, Key, SplitBay, Stack } from "../../../../design-system";
import { GallerySection, Spec } from "./GallerySection";
import { LayoutSlot } from "./LayoutSlot";

function Tile({ label }: { label: string }) {
  return <div className="composition-demo-tile">{label}</div>;
}

export function LayoutPrimitiveSections() {
  return (
    <>
      <GallerySection id="composition-stack" title="Stack" note="Vertical groups with even spacing between siblings.">
        <Spec wide tag="nested groups">
          <Stack gap="6">
            <Tile label="Heading block" />
            <Stack gap="4">
              <Tile label="Field" />
              <Tile label="Field" />
            </Stack>
            <Tile label="Commit key" />
          </Stack>
        </Spec>
      </GallerySection>

      <GallerySection id="composition-inline" title="Inline" note="Horizontal groups that wrap when the line runs out.">
        <Spec wide tag="wrapping keys">
          <Inline gap="3">
            <Key>Save draft</Key>
            <Key>Inspect</Key>
            <EllipsisKey>Confirm activation after readiness checks complete</EllipsisKey>
            <Key variant="transmit">Transmit</Key>
          </Inline>
        </Spec>
        <Spec wide tag="split row">
          <Inline gap="3" justify="between">
            <span>Leading status</span>
            <span>Trailing action</span>
          </Inline>
        </Spec>
      </GallerySection>

      <GallerySection id="composition-grid" title="Grid" note="Equal tiles that reflow as the frame narrows. Default fit packs leftover space (auto-fit). Fill keeps empty slots so a lone plate does not stretch (auto-fill). Compact fill grids stay one column.">
        <Spec wide tag="reflowing tiles">
          <Grid gap="3" minItemWidth="compact">
            <Tile label="Compact A" />
            <Tile label="Compact B" />
            <Tile label="Compact C" />
            <Tile label="Long unbreakable_token_name_that_must_wrap" />
          </Grid>
        </Spec>
        <Spec wide tag="density steps">
          <Stack gap="5">
            <Grid gap="3" minItemWidth="compact">
              <Tile label="Tight" />
              <Tile label="Tight" />
              <Tile label="Tight" />
              <Tile label="Tight" />
              <Tile label="Tight" />
              <Tile label="Tight" />
            </Grid>
            <Grid gap="3" minItemWidth="control">
              <Tile label="Field" />
              <Tile label="Field" />
              <Tile label="Field" />
              <Tile label="Field" />
              <Tile label="Field" />
              <Tile label="Field" />
            </Grid>
            <Grid gap="3" minItemWidth="panel">
              <Tile label="Panel" />
              <Tile label="Panel" />
              <Tile label="Panel" />
              <Tile label="Panel" />
              <Tile label="Panel" />
              <Tile label="Panel" />
            </Grid>
            <Grid gap="3" minItemWidth="wide">
              <Tile label="Wide" />
              <Tile label="Wide" />
              <Tile label="Wide" />
              <Tile label="Wide" />
              <Tile label="Wide" />
              <Tile label="Wide" />
            </Grid>
          </Stack>
        </Spec>
        <Spec wide tag="fit fill · one tile keeps a slot">
          <Grid gap="3" minItemWidth="control" fit="fill">
            <Tile label="Fill A" />
          </Grid>
        </Spec>
      </GallerySection>

      <GallerySection id="composition-split" title="Split bay" note="Named start, main, and end tracks that stay columns until an explicit drawer collapse. Optional head and foot span the ledger, not the start rail.">
        <Spec wide tag="start · main · end">
          <div className="composition-split-demo">
            <SplitBay
              start={<LayoutSlot label="Start rail" variant="rail" />}
              end={<LayoutSlot label="End rail" variant="rail" />}
            >
              <LayoutSlot label="Main column" />
            </SplitBay>
          </div>
        </Spec>
        <Spec wide tag="head · foot spanning main + end">
          <div className="composition-split-demo composition-split-demo--ledger">
            <SplitBay
              start={<LayoutSlot label="Start rail" variant="rail" />}
              end={<LayoutSlot label="End rail" variant="rail" />}
              head={<LayoutSlot label="Plaque head" variant="heading" />}
              foot={<LayoutSlot label="Decision foot" variant="foot" />}
            >
              <LayoutSlot label="Main column" />
            </SplitBay>
          </div>
        </Spec>
      </GallerySection>

      <GallerySection id="composition-container" title="Container" note="A content column at reading, form, shell, or full width.">
        <Spec wide tag="width comparison">
          <Stack gap="3">
            <Container size="prose"><Tile label="Reading column" /></Container>
            <Container size="form"><Tile label="Form column" /></Container>
            <Container size="content"><Tile label="Shell column" /></Container>
            <Container size="full"><Tile label="Full width" /></Container>
          </Stack>
        </Spec>
      </GallerySection>

      <GallerySection id="composition-inset" title="Inset" note="Inner padding around a region.">
        <div className="spec-row">
          <Spec tag="uniform">
            <Inset space="5"><Tile label="Uniform" /></Inset>
          </Spec>
          <Spec tag="more side padding">
            <Inset block="4" inline="8"><Tile label="Wider sides" /></Inset>
          </Spec>
        </div>
      </GallerySection>
    </>
  );
}
