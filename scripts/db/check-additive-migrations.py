#!/usr/bin/env python3
"""Fail CI if a new EF Core migration is not additive (Runbook de DevOps, §8).

rollback.yml returns containers to a previous image but never reverts the database, so a
migration that drops, renames or retypes something breaks the previous API version.
Only the Up() method is checked (Down() is destructive by nature). Migrations up to
BASELINE (the Schema95 → DemoCore transition) are historical and intentionally destructive.

A reviewed exception is declared inside the migration file itself:
    // migracion-destructiva-aprobada: <motivo y plan de dos pasos>
"""
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DEFAULT_DIR = ROOT / "src/Infrastructure/Persistence/Migrations"
BASELINE = "20261012223838"  # DemoCore: última transición destructiva aceptada.

BUILDER_OPS = (
    "DropTable", "DropColumn", "DropPrimaryKey", "DropSchema", "DropSequence",
    "RenameTable", "RenameColumn", "RenameSchema", "RenameSequence", "AlterColumn",
)
BUILDER_RE = re.compile(r"migrationBuilder\s*\.\s*(" + "|".join(BUILDER_OPS) + r")\b")
SQL_RE = re.compile(
    r"\b(DROP\s+(?:TABLE|COLUMN|SCHEMA|VIEW|MATERIALIZED\s+VIEW|TYPE|FUNCTION|SEQUENCE)"
    r"|RENAME\s+(?:TO|COLUMN)|ALTER\s+COLUMN|TRUNCATE|DELETE\s+FROM)\b",
    re.IGNORECASE,
)
STRING_RE = re.compile(r'"""[\s\S]*?"""|@"(?:[^"]|"")*"|"(?:[^"\\\n]|\\.)*"')
APPROVAL_RE = re.compile(r"//\s*migracion-destructiva-aprobada:\s*\S")
NAME_RE = re.compile(r"^(\d{14})_\w+\.cs$")


def up_body(source: str) -> str:
    start = source.find("override void Up(")
    if start < 0:
        return ""
    end = source.find("override void Down(", start)
    return source[start:end if end >= 0 else len(source)]


def findings_for(source: str) -> list[str]:
    body = up_body(source)
    found = [f"migrationBuilder.{m.group(1)}" for m in BUILDER_RE.finditer(body)]
    for literal in STRING_RE.findall(body):
        found += [f"SQL «{m.group(1).upper()}»" for m in SQL_RE.finditer(literal)]
    return found


def check(directory: Path) -> tuple[list[str], list[str]]:
    errors, approved = [], []
    for path in sorted(directory.glob("*.cs")):
        match = NAME_RE.match(path.name)
        if not match or match.group(1) <= BASELINE:
            continue
        source = path.read_text(encoding="utf-8")
        found = findings_for(source)
        if not found:
            continue
        summary = f"{path.name}: {', '.join(sorted(set(found)))}"
        (approved if APPROVAL_RE.search(source) else errors).append(summary)
    return errors, approved


def main(argv: list[str]) -> int:
    directory = Path(argv[1]) if len(argv) > 1 else DEFAULT_DIR
    errors, approved = check(directory)
    for line in approved:
        print(f"Aprobada explícitamente: {line}")
    if errors:
        print("Migraciones no aditivas (Runbook de DevOps, §8). El rollback no revierte la base:")
        for line in errors:
            print(f"  - {line}")
        print("Usa un ciclo de dos pasos o, tras revisión y snapshot, declara en el archivo:")
        print("  // migracion-destructiva-aprobada: <motivo>")
        return 1
    print("Migraciones aditivas: OK")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
