#!/usr/bin/env python3
"""Focused tests for snapshot-first documentation catalog checks."""

from __future__ import annotations

import unittest
from pathlib import Path

import check_docs

ROOT = Path(__file__).resolve().parents[1]


class CheckDocsCatalogTests(unittest.TestCase):
    def test_current_catalog_is_p0_only(self) -> None:
        self.assertEqual(list(check_docs.SPEC_CATALOG.keys()), ["P0"])
        self.assertEqual(len(check_docs.ALL_SPEC_FILES), 7)
        self.assertEqual(len(check_docs.HISTORICAL_PLACEHOLDER_SPECS), 12)

    def test_ui_catalog_does_not_require_retirement_ledger(self) -> None:
        self.assertEqual(check_docs.check_ui_current_catalog(), [])
        ui = (ROOT / "docs" / "ui-ux" / "README.md").read_text(encoding="utf-8")
        self.assertIn("Approved v1.0", ui)
        self.assertNotIn("retired-authority.md", ui)

    def test_current_state_and_work_hygiene(self) -> None:
        self.assertEqual(check_docs.check_current_state_index(), [])
        self.assertEqual(check_docs.check_work_hygiene(), [])


if __name__ == "__main__":
    unittest.main()
