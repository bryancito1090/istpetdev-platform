#!/usr/bin/env python3
"""Offline tests for the additive-migration guard."""

import importlib.util
import tempfile
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location("additive", ROOT / "scripts/db/check-additive-migrations.py")
additive = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(additive)

NEW = "20261020120000_Nueva.cs"


def migration(up: str, down: str = "", header: str = "") -> str:
    return (
        f"{header}\npublic partial class Nueva : Migration\n{{\n"
        f"    protected override void Up(MigrationBuilder migrationBuilder)\n    {{\n{up}\n    }}\n"
        f"    protected override void Down(MigrationBuilder migrationBuilder)\n    {{\n{down}\n    }}\n}}\n"
    )


class AdditiveMigrationTests(unittest.TestCase):
    def check(self, files: dict[str, str]):
        with tempfile.TemporaryDirectory() as folder:
            for name, source in files.items():
                (Path(folder) / name).write_text(source, encoding="utf-8")
            return additive.check(Path(folder))

    def test_additive_migration_passes(self):
        errors, _ = self.check({NEW: migration(
            'migrationBuilder.CreateTable(name: "x");\nmigrationBuilder.AddColumn<int>(name: "y", table: "x", nullable: true);\n'
            "migrationBuilder.Sql(DemoGrants.Sql);",
            down='migrationBuilder.DropTable(name: "x");')})
        self.assertEqual([], errors)

    def test_drop_column_in_up_fails(self):
        errors, _ = self.check({NEW: migration('migrationBuilder.DropColumn(name: "y", table: "x");')})
        self.assertEqual(1, len(errors))
        self.assertIn("DropColumn", errors[0])

    def test_rename_and_alter_column_fail(self):
        errors, _ = self.check({NEW: migration(
            'migrationBuilder.RenameColumn(name: "a", table: "x", newName: "b");\n'
            'migrationBuilder.AlterColumn<string>(name: "c", table: "x", nullable: false);')})
        self.assertIn("RenameColumn", errors[0])
        self.assertIn("AlterColumn", errors[0])

    def test_destructive_raw_sql_fails(self):
        for sql in ('"DROP TABLE x;"', '@"ALTER TABLE x ALTER COLUMN y TYPE text"', '"""\ndelete from x;\n"""'):
            with self.subTest(sql=sql):
                errors, _ = self.check({NEW: migration(f"migrationBuilder.Sql({sql});")})
                self.assertEqual(1, len(errors), sql)

    def test_explicit_approval_moves_finding_to_approved(self):
        errors, approved = self.check({NEW: migration(
            'migrationBuilder.DropColumn(name: "y", table: "x");',
            header="// migracion-destructiva-aprobada: columna sin uso desde la versión anterior")})
        self.assertEqual([], errors)
        self.assertEqual(1, len(approved))

    def test_baseline_and_designer_files_are_ignored(self):
        errors, _ = self.check({
            "20261012223838_DemoCore.cs": migration('migrationBuilder.DropTable(name: "old");'),
            "20261020120000_Nueva.Designer.cs": migration('migrationBuilder.DropTable(name: "x");'),
        })
        self.assertEqual([], errors)

    def test_repository_migrations_are_additive(self):
        errors, _ = additive.check(additive.DEFAULT_DIR)
        self.assertEqual([], errors)


if __name__ == "__main__":
    unittest.main()
