import tempfile
import unittest
from pathlib import Path

from impeccable_context import (
    SECRET_PATTERN,
    check_adapters,
    check_impeccable_tracked_secrets,
    complete_sentences,
    render_design,
    render_product,
)


class ImpeccableContextTests(unittest.TestCase):
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
        self.assertIn("Not authoritative", body)
        self.assertIn("Shipboard Terminal", body)

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


if __name__ == "__main__":
    unittest.main()
