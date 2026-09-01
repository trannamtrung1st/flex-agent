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
        self.assertEqual(len(check_docs.HISTORICAL_PLACEHOLDER_SPECS), 0)

    def test_ui_catalog_does_not_require_retirement_ledger(self) -> None:
        self.assertEqual(check_docs.check_ui_current_catalog(), [])
        ui = (ROOT / "docs" / "ui-ux" / "README.md").read_text(encoding="utf-8")
        self.assertIn("Approved v1.0", ui)
        self.assertNotIn("retired-authority.md", ui)

    def test_current_state_and_work_hygiene(self) -> None:
        self.assertEqual(check_docs.check_current_state_index(), [])
        self.assertEqual(check_docs.check_work_hygiene(), [])

    def test_governance_scan_covers_harness_surfaces(self) -> None:
        files = {path.resolve() for path in check_docs.iter_governance_markdown_files()}
        self.assertIn((ROOT / "AGENTS.md").resolve(), files)
        self.assertIn((ROOT / ".work" / "README.md").resolve(), files)
        self.assertIn(
            (ROOT / ".agents" / "skills" / "architect" / "SKILL.md").resolve(),
            files,
        )
        self.assertIn(
            (ROOT / ".cursor" / "skills" / "architect" / "SKILL.md").resolve(),
            files,
        )
        self.assertIn(
            (ROOT / ".cursor" / "rules" / "06-implementation-workflow.mdc").resolve(),
            files,
        )

    def test_prohibited_policy_detects_adr_creation_and_task_retention(self) -> None:
        hits = check_docs.find_prohibited_policy_hits(
            "Compare viable options; record consequential choices as ADRs with "
            "status, rationale, consequences, and supersession links. "
            "Keep the completed task file after completion and external review."
        )
        self.assertIn("record consequential choices as adrs", hits)
        self.assertIn("supersession links", hits)
        self.assertIn("keep the completed task file", hits)

    def test_prohibited_policy_allows_historical_adr_recovery(self) -> None:
        self.assertEqual(
            check_docs.find_prohibited_policy_hits(
                "Historical ADR files are recoverable from Git and are not the "
                "current architecture catalog."
            ),
            [],
        )

    def test_live_governance_has_no_prohibited_policy(self) -> None:
        self.assertEqual(check_docs.check_prohibited_policy(), [])


if __name__ == "__main__":
    unittest.main()
