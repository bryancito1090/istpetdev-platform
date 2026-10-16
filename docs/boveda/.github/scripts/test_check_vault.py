"""Pruebas del verificador de la bóveda."""

import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

import check_vault


class VaultCountTests(unittest.TestCase):
    def test_new_event_note_does_not_require_editing_shared_history(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / "Fuente y control documental.md").write_text(
                "---\ntipo: fuente\nestado: vigente\nactualizado: 2026-10-16\n"
                "tags: [evento]\n---\n\n"
                "Verificación anterior: **1 notas Markdown**, "
                "**0 enlaces internos resueltos**.\n",
                encoding="utf-8",
            )
            event_dir = root / "NotasEvento"
            event_dir.mkdir()
            (event_dir / "2026-10-16-1432-jd-mentor.md").write_text(
                "---\ntipo: nota-evento\nestado: pendiente\n"
                "actualizado: 2026-10-16\ntags: [evento]\n---\n\n"
                "# Apunte de mentoría\n\n[[Fuente y control documental]]\n",
                encoding="utf-8",
            )
            errors, warnings = [], []
            with patch.object(check_vault, "ROOT", root):
                counts = check_vault.check_vault(errors, warnings)

            self.assertEqual(counts, (2, 1))
            self.assertEqual(errors, [])


if __name__ == "__main__":
    unittest.main()
