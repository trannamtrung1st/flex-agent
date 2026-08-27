import { createHash } from "node:crypto";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const stylesRoot = join(dirname(fileURLToPath(import.meta.url)), "../styles");

const copiedSheets: Record<string, string> = {
  "tokens.css": "ea1ba658070a8f21d299d8e3e1b63a1d1256d94cf54d79049cf2f497824fec62",
  "base.css": "cead6c4a9b3e48201ea5c9239f778331df7d3002c904ef8eca695bd61acc0b88",
  "components/chrome.css": "99a8782089e6adbb4cb6226a0fb92d40c2c8e1ac12cb4db074907c57dc7edc20",
  "components/datatable.css": "1bbff6e0498733a6738303a1d02a5501c84158de1238412bb94b53765cead76e",
  "components/demo.css": "d40dfebb185c643de0c342d9375e1921efa021de10c08bf6813b2c88dd648962",
  "components/fields.css": "cb133053484832bcd64041722f948257e4dacf5a92ec39d05afba33a57e8e535",
  "components/keys.css": "ff77a120dcf1bfcaf05bd53933e3d77dff083edce2ea4943cc668bf2cada8f1c",
  "components/menus.css": "f6a97728b378cb34e5a6079e3f79bbdd2024f93bafb0219f6eb9139c342e6be4",
  "components/navigation.css": "67467a6a2b64d2f9f01934a585744e0bb761f1dc580711d7d71ed1c530e23d10",
  "components/overlays.css": "644976755f551902788c563064be86c5df1baf7c3d99d5e34e1c11b7913dc262",
  "components/plates.css": "08fdf477edd67293a3d349c88bedc4464a85bdff7b9776e990522b43def10252",
  "components/readouts.css": "6c23d0728597b7239032e6fc189147fd59f09bc800af1a83d86353447a07fdb8",
  "components/searchable.css": "0af11cea385957f12e877a56a926f3f178b825a8ad303292af8c7103626795ff",
  "components/state.css": "0375ab27c2eb864f43259e19f81201d90957262d8655782d6dde43a0fb13130a",
  "components/temporal.css": "27cd2553c912cd9b68c452518933fabcc1c05343b76a099fc3409469663e08df",
  "surfaces/admin-console.css": "6d22cc033e09800f783f8bb5a6fa0810c273ea37100ac57f937abf054ba9292f",
  "surfaces/gallery.css": "36fddf83c387106544434d5e859d25133cbcaeeada818edd88e7195a9a3a7d49",
  "surfaces/not-found.css": "359a389fb1f0f60143fcd528dab71faf42b5426e1906d32a0295873b600518b4",
  "surfaces/participant-home.css": "8dadfc5da147308ad32f06cadef0cc860273a085e8719aacd81a53fdc05cc4eb",
  "surfaces/participant-journey.css": "3bf6ec0b0fa3527c43598c3caeb29e48c55dfb3cbf37af9e00c922b46ae4c403",
  "surfaces/participant-session.css": "96b010751f93b7639d4a6895d4120c65063e92fee006a801ee3d2a0f00df3b56",
  "surfaces/reviewer-console.css": "43348fda627e810ffafdbbdd6f34e2b39c6eff16b8a0b03935126dc4236a5f00",
  "surfaces/surfaces-index.css": "d1cc4a5210e3355a88577b1c8c33fa7bb6e1001eca789d1dbc8efd7a84fb9f24",
};

describe("shared Shipboard stylesheets", () => {
  it("keeps adopted shared sheets byte-identical except isolation-adjusted demo/temporal sheets", () => {
    for (const [relative, digest] of Object.entries(copiedSheets)) {
      const bytes = readFileSync(join(stylesRoot, relative));
      expect(createHash("sha256").update(bytes).digest("hex"), relative).toBe(digest);
    }
  });

  it("loads copied sheets after semantic aliases and forced-colors adaptations", () => {
    const sharedCss = readFileSync(join(stylesRoot, "shared.css"), "utf8");
    expect(sharedCss).toContain('@import "@fontsource/michroma"');
    expect(sharedCss).toContain('@import "@fontsource/sometype-mono"');
    expect(sharedCss.indexOf('./semantic-aliases.css')).toBeGreaterThan(sharedCss.indexOf("./tokens.css"));
    expect(sharedCss.indexOf("./adaptations.css")).toBeGreaterThan(sharedCss.indexOf("./semantic-aliases.css"));
    expect(sharedCss.indexOf("./base.css")).toBeGreaterThan(sharedCss.indexOf("./adaptations.css"));
    expect(sharedCss).not.toContain("demo.css");
    expect(sharedCss).not.toContain("./surfaces/");
  });
});
