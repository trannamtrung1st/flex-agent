import { Container, EllipsisKey, Grid, Inline, Inset, Key, KeyGroup, SplitBay, Stack } from "../../../../design-system";
import { GallerySection, Spec } from "./GallerySection";

function Tile({ label }: { label: string }) {
  return <div className="composition-demo-tile">{label}</div>;
}

export function LayoutPrimitiveSections() {
  return (
    <>
      <GallerySection id="composition-stack" title="Stack" note="Vertical groups with even spacing between siblings.">
        <Spec wide tag="nested groups">
          <Stack gap="4">
            <Tile label="Heading block" />
            <Stack gap="2">
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

      <GallerySection id="composition-grid" title="Grid" note="Equal tiles that reflow as the frame narrows. Tighter tiles pack more columns; wider tiles stay roomy.">
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
      </GallerySection>

      <GallerySection id="composition-split" title="Split bay" note="Named start, main, and end tracks that stay columns until an explicit drawer collapse. Optional head and foot span the ledger, not the start rail.">
        <Spec wide tag="start · main · end">
          <div className="composition-split-demo">
            <SplitBay
              start={<Tile label="Start rail" />}
              end={<Tile label="End rail" />}
            >
              <Tile label="Main column" />
            </SplitBay>
          </div>
        </Spec>
        <Spec wide tag="head · foot spanning main + end">
          <div className="composition-split-demo composition-split-demo--ledger">
            <SplitBay
              start={<Tile label="Start rail" />}
              end={<Tile label="End rail" />}
              head={<Tile label="Plaque head" />}
              foot={<Tile label="Decision foot" />}
            >
              <Tile label="Main column" />
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

      <GallerySection id="composition-recipes" title="Composition recipes" note="Common inner slots: a form column, and a grouped list with trailing opens.">
        <Spec wide tag="form slot">
          <Container size="form">
            <Stack gap="5">
              <Inline gap="3" justify="between" wrap={false}>
                <span>Campaign draft</span>
                <KeyGroup aria-label="Recipe actions">
                  <Key>Save draft</Key>
                  <Key variant="transmit">Create</Key>
                </KeyGroup>
              </Inline>
              <Inset space="4">
                <Grid gap="3" minItemWidth="control">
                  <Tile label="Title field" />
                  <Tile label="Source set" />
                </Grid>
              </Inset>
            </Stack>
          </Container>
        </Spec>
        <Spec wide tag="grouped list">
          <Stack gap="8">
            <Stack gap="2.5">
              <span>Participant</span>
              <Stack gap="3" role="group" aria-label="Participant channels">
                <Inline gap="4" justify="between">
                  <span>Status Bays</span>
                  <Key variant="open">Open</Key>
                </Inline>
                <Inline gap="4" justify="between">
                  <span>Assignment Station</span>
                  <Key variant="open">Open</Key>
                </Inline>
              </Stack>
            </Stack>
            <Stack gap="2.5">
              <span>Administrator</span>
              <Inline gap="4" justify="between">
                <span>Administration</span>
                <Key variant="open">Open</Key>
              </Inline>
            </Stack>
          </Stack>
        </Spec>
      </GallerySection>
    </>
  );
}
