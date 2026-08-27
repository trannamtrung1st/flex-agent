import { createHash } from "node:crypto";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const stylesRoot = join(dirname(fileURLToPath(import.meta.url)), "../styles");

const copiedSheets: Record<string, string> = {
  "tokens.css": "b93fcd874f2a6302e16d8f1144b7372d8a331f586fa7e67b95d2180576f9f398",
  "base.css": "cead6c4a9b3e48201ea5c9239f778331df7d3002c904ef8eca695bd61acc0b88",
  "components/chrome.css": "99a8782089e6adbb4cb6226a0fb92d40c2c8e1ac12cb4db074907c57dc7edc20",
  "components/datatable.css": "1bbff6e0498733a6738303a1d02a5501c84158de1238412bb94b53765cead76e",
  "components/demo.css": "8cd240419d7b00b2de5208ed282b26d5a69fc1d01e003ac77f87a5de50b6a364",
  "components/fields.css": "cb133053484832bcd64041722f948257e4dacf5a92ec39d05afba33a57e8e535",
  "components/keys.css": "ff77a120dcf1bfcaf05bd53933e3d77dff083edce2ea4943cc668bf2cada8f1c",
  "components/menus.css": "f6a97728b378cb34e5a6079e3f79bbdd2024f93bafb0219f6eb9139c342e6be4",
  "components/navigation.css": "67467a6a2b64d2f9f01934a585744e0bb761f1dc580711d7d71ed1c530e23d10",
  "components/overlays.css": "644976755f551902788c563064be86c5df1baf7c3d99d5e34e1c11b7913dc262",
  "components/plates.css": "08fdf477edd67293a3d349c88bedc4464a85bdff7b9776e990522b43def10252",
  "components/readouts.css": "6c23d0728597b7239032e6fc189147fd59f09bc800af1a83d86353447a07fdb8",
  "components/searchable.css": "0af11cea385957f12e877a56a926f3f178b825a8ad303292af8c7103626795ff",
  "components/state.css": "0375ab27c2eb864f43259e19f81201d90957262d8655782d6dde43a0fb13130a",
  "components/temporal.css": "96de67b860ba67a528ac490479edcd5a14777f607e243c66f3ee0ac45b7f19a7",
  "surfaces/admin-console.css": "6d22cc033e09800f783f8bb5a6fa0810c273ea37100ac57f937abf054ba9292f",
  "surfaces/gallery.css": "36fddf83c387106544434d5e859d25133cbcaeeada818edd88e7195a9a3a7d49",
  "surfaces/not-found.css": "359a389fb1f0f60143fcd528dab71faf42b5426e1906d32a0295873b600518b4",
  "surfaces/participant-home.css": "8dadfc5da147308ad32f06cadef0cc860273a085e8719aacd81a53fdc05cc4eb",
  "surfaces/participant-journey.css": "409d7e6b4c774f09eaee67477316458dd866ba5d41f1508d927743a9d5158d02",
  "surfaces/participant-session.css": "54e6aa5ab450ccd988baf1bc37714c8426a6144b598939dd59725e94c47e92fb",
  "surfaces/reviewer-console.css": "43348fda627e810ffafdbbdd6f34e2b39c6eff16b8a0b03935126dc4236a5f00",
  "surfaces/surfaces-index.css": "d1cc4a5210e3355a88577b1c8c33fa7bb6e1001eca789d1dbc8efd7a84fb9f24",
};

describe("shared Shipboard stylesheets", () => {
  it("keeps adopted shared sheets byte-identical", () => {
    for (const [relative, digest] of Object.entries(copiedSheets)) {
      const bytes = readFileSync(join(stylesRoot, relative));
      expect(createHash("sha256").update(bytes).digest("hex"), relative).toBe(digest);
    }
  });

  it("loads copied sheets after semantic aliases and forced-colors adaptations", () => {
    const indexCss = readFileSync(join(stylesRoot, "index.css"), "utf8");
    expect(indexCss).toContain('@import "@fontsource/michroma"');
    expect(indexCss).toContain('@import "@fontsource/sometype-mono"');
    expect(indexCss.indexOf('./semantic-aliases.css')).toBeGreaterThan(indexCss.indexOf("./tokens.css"));
    expect(indexCss.indexOf("./adaptations.css")).toBeGreaterThan(indexCss.indexOf("./semantic-aliases.css"));
    expect(indexCss.indexOf("./base.css")).toBeGreaterThan(indexCss.indexOf("./adaptations.css"));
  });
});
