import hashlib
import tempfile
import unittest
from pathlib import Path

from impeccable_context import (
    GENERATOR_ID,
    PRODUCT_SOURCES,
    ROOT,
    SECRET_PATTERN,
    check_adapters,
    check_impeccable_tracked_paths,
    check_impeccable_tracked_secrets,
    complete_sentences,
    fingerprint,
    is_impeccable_guard_relpath,
    render_design,
    render_product,
    relative_impeccable_parts,
)


class ImpeccableContextTests(unittest.TestCase):
    def test_fingerprint_is_stable_across_checkout_prefixes(self) -> None:
        digest = hashlib.sha256()
        digest.update(GENERATOR_ID.encode("utf-8"))
        for path in PRODUCT_SOURCES:
            digest.update(path.relative_to(ROOT).as_posix().encode("utf-8"))
            digest.update(path.read_bytes())
        self.assertEqual(fingerprint(PRODUCT_SOURCES), digest.hexdigest())

    def test_product_adapter_is_non_authoritative_and_fingerprinted(self) -> None:
        body = render_product()
        self.assertIn("Not authoritative", body)
        self.assertIn("docs/product/overview.md", body)
        self.assertIn("Content fingerprint:", body)
        self.assertTrue(body.startswith("<!-- impeccable-context: product -->"))
        projection = body.split("## Projection", 1)[1].strip().split("\n\n", 1)[0]
        self.assertRegex(projection.strip(), r"[.!?]$")

    def test_complete_sentences_strips_a_trailing_clause(self) -> None:
        self.assertEqual(complete_sentences("One sentence. A dangling"), "One sentence.")

    def test_design_adapter_points_at_design_system(self) -> None:
        body = render_design()
        self.assertIn("docs/ui-ux/design-system/README.md", body)
        self.assertIn("docs/ui-ux/design-system/foundation/colors.md", body)
        self.assertIn("docs/ui-ux/design-system/components/tables.md", body)
        self.assertIn("Not authoritative", body)
        self.assertIn("Do not treat this file as a token sheet", body)
        self.assertIn("Approved v1.1", body)
        self.assertNotIn("#3CC0BF", body)

    def test_design_adapter_projects_v1_tokens(self) -> None:
        body = render_design()
        self.assertIn("Shipboard Terminal", body)
        self.assertIn("foundation/typography.md", body)
        self.assertIn("foundation/layout.md", body)
        self.assertIn("components/layouts.md", body)
        self.assertIn("components/alerts.md", body)
        self.assertIn("components/lists.md", body)
        self.assertIn("components/tables.md", body)
        self.assertIn("components/inputs.md", body)
        self.assertIn("foundation/borders.md", body)
        self.assertIn("product/empty-loading.md", body)
        self.assertIn("clones a matching existing production page", body)
        self.assertNotIn("for the frontend rebuild", body)
        self.assertNotIn("--ground:", body)
        self.assertNotIn("Semantic tokens (dark canonical)", body)
        self.assertNotIn("Token projection is deferred until design-system v1.0", body)

    def test_check_detects_drift(self) -> None:
        product = Path(__file__).resolve().parents[1] / "PRODUCT.md"
        original = product.read_text(encoding="utf-8") if product.exists() else None
        product.write_text("stale\n", encoding="utf-8")
        try:
            errors = check_adapters()
            self.assertTrue(any("PRODUCT.md drifted" in error for error in errors))
        finally:
            if original is None:
                product.unlink()
            else:
                product.write_text(original, encoding="utf-8")

    def test_secret_scanner_rejects_private_key_material(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / "leak.txt"
            path.write_text("-----BEGIN PRIVATE KEY-----\nAAAA\n", encoding="utf-8")
            errors = check_impeccable_tracked_secrets([path])
            self.assertEqual(len(errors), 1)

    def test_secret_pattern_ignores_design_token_copy(self) -> None:
        self.assertIsNone(SECRET_PATTERN.search("CSS token strip and teal token chips"))

    def test_runtime_shot_path_is_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / ".impeccable" / "shots" / "session.png"
            path.parent.mkdir(parents=True)
            path.write_bytes(b"\x89PNG")
            errors = check_impeccable_tracked_paths([path])
            self.assertTrue(any("runtime artifact" in error for error in errors), errors)

    def test_upstream_critique_markdown_path_is_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / ".impeccable" / "critique" / "2026-08-27__session.md"
            path.parent.mkdir(parents=True)
            path.write_text("# Critique\nSynthetic session findings.\n", encoding="utf-8")
            errors = check_impeccable_tracked_paths([path])
            self.assertTrue(any("runtime artifact" in error for error in errors), errors)

    def test_legacy_impeccable_live_state_is_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            live_json = Path(tmp) / ".impeccable-live.json"
            live_json.write_text("{}\n", encoding="utf-8")
            live_file = Path(tmp) / ".impeccable-live" / "sessions" / "state.json"
            live_file.parent.mkdir(parents=True)
            live_file.write_text("{}\n", encoding="utf-8")
            errors = check_impeccable_tracked_paths([live_json, live_file])
            self.assertEqual(len(errors), 2, errors)
            self.assertTrue(all("runtime artifact" in error for error in errors), errors)

    def test_participant_email_in_impeccable_text_is_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            path = Path(tmp) / ".impeccable" / "surfaces" / "leak.md"
            path.parent.mkdir(parents=True)
            path.write_text("Contact jane.doe@university.edu about the evaluation.\n", encoding="utf-8")
            errors = check_impeccable_tracked_paths([path])
            self.assertTrue(any("participant-like" in error for error in errors), errors)

    def test_synthetic_example_email_and_config_are_allowed(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp) / ".impeccable"
            (root / "surfaces").mkdir(parents=True)
            (root / "config.json").write_text('{"stalenessCheck": false}\n', encoding="utf-8")
            (root / "surfaces" / "ok.md").write_text(
                "Synthetic participant CND-8842. Contact ops@example.com.\n", encoding="utf-8"
            )
            errors = check_impeccable_tracked_paths([root / "config.json", root / "surfaces" / "ok.md"])
            self.assertEqual(errors, [])

    def test_retired_prototype_snapshot_is_not_a_live_dependency(self) -> None:
        snapshot = (
            Path(__file__).resolve().parents[1]
            / ".work"
            / "resources"
            / "impeccable-prototype-snapshot"
        )
        self.assertFalse(snapshot.exists(), "Phase 7.5 must delete the raw prototype snapshot")

    def test_guard_relpaths_include_legacy_live_and_root_impeccable(self) -> None:
        self.assertTrue(is_impeccable_guard_relpath(".impeccable/critique/2026-08-27__session.md"))
        self.assertTrue(is_impeccable_guard_relpath(".impeccable-live.json"))
        self.assertTrue(is_impeccable_guard_relpath(".impeccable-live/sessions/state.json"))
        self.assertFalse(is_impeccable_guard_relpath("docs/ui-ux/design-system/README.md"))

    def test_relative_impeccable_parts(self) -> None:
        self.assertEqual(
            relative_impeccable_parts(Path("/repo/.impeccable/shots/a.png")),
            "shots/a.png",
        )


if __name__ == "__main__":
    unittest.main()
