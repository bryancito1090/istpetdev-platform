#!/usr/bin/env python3
"""Verificador de la bóveda IstpetDev.

Uso (desde cualquier carpeta):
    python .github/scripts/check_vault.py

Errores (salida 1): enlaces internos rotos, nombres de nota duplicados, frontmatter
incompleto, estados no válidos, IDs definidos fuera de su nota dueña o repetidos,
rutas personales, bloques de código sin cerrar, conteos desactualizados en
«Fuente y control documental».

Avisos (no bloquean): lenguaje absoluto en notas de propuesta.

Solo usa la biblioteca estándar de Python 3.9+.
"""
from __future__ import annotations

import argparse
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]

# Archivos locales de asistentes de IA: no se versionan ni forman parte de la bóveda.
LOCAL_AI_FILES = {"AGENTS.md", "CLAUDE.md", "GEMINI.md"}
ATTACHMENTS = {"Manual oficial.pdf"}
REQUIRED_FIELDS = ("tipo", "estado", "actualizado", "tags")
VALID_STATES = {"vigente", "propuesta", "pendiente", "aceptado", "validado", "reemplazado", "plantilla"}

# Prefijo de ID -> nota dueña (única nota donde se define en una fila de tabla).
ID_OWNERS = {
    "RF": "Requisitos y aceptacion",
    "RNF": "Requisitos y aceptacion",
    "V": "Plan de validacion",
    "B": "Backlog",
    "S": "Dataset y escenarios",
    "ADR": "Decisiones de arquitectura",
    "O": "Consultas para la organizacion",
    "R": "Riesgos y decisiones pendientes",
    "EXP": "Simulador y metricas",
    "WF": "n8n e IA",
}
ID_ROW = re.compile(r"^\|\s*(RF|RNF|V|B|S|ADR|O|R|EXP|WF)-(\d+)\s*\|")
WIKILINK = re.compile(r"\[\[([^\]|#]+)")
PERSONAL_PATH = re.compile(r"/home/[A-Za-z0-9_.-]+|[A-Za-z]:[\\/]+Users[\\/]+[A-Za-z0-9_.-]+", re.IGNORECASE)
ABSOLUTE = re.compile(r"\b(garantiz\w*|instant[áa]ne\w*|imposible|perfectamente|absolut\w*|nivel senior)\b", re.IGNORECASE)
NEGATION = re.compile(r"\b(no|sin|nunca|ni)\b", re.IGNORECASE)
COUNT_NOTES = re.compile(r"\*\*(\d+) notas Markdown\*\*")
COUNT_LINKS = re.compile(r"\*\*(\d+) enlaces internos resueltos\*\*")


def vault_notes() -> list[Path]:
    """Notas visibles para Obsidian: Markdown fuera de carpetas ocultas."""
    notes = []
    for path in ROOT.rglob("*.md"):
        rel = path.relative_to(ROOT)
        if any(part.startswith(".") for part in rel.parts[:-1]):
            continue
        if len(rel.parts) == 1 and rel.name in LOCAL_AI_FILES:
            continue
        notes.append(path)
    return sorted(notes)


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8").replace("\r\n", "\n")


def frontmatter(text: str) -> dict[str, str] | None:
    match = re.match(r"---\n(.*?)\n---\n", text, re.S)
    if not match:
        return None
    fields = {}
    for line in match.group(1).splitlines():
        if ":" in line and not line.startswith(" "):
            key, value = line.split(":", 1)
            fields[key.strip()] = value.strip()
    return fields


def check_vault(errors: list[str], warnings: list[str]) -> tuple[int, int]:
    notes = vault_notes()
    names: dict[str, Path] = {}
    for note in notes:
        if note.stem in names:
            errors.append(f"Nombre de nota duplicado: {note.relative_to(ROOT)} y {names[note.stem].relative_to(ROOT)}")
        names[note.stem] = note
    valid_targets = set(names) | ATTACHMENTS

    links = 0
    definitions: dict[str, list[str]] = {}
    for note in notes:
        rel = note.relative_to(ROOT).as_posix()
        text = read(note)

        if not (note.parent == ROOT and note.name == "README.md"):
            fields = frontmatter(text)
            if fields is None:
                errors.append(f"{rel}: falta frontmatter")
            else:
                missing = [f for f in REQUIRED_FIELDS if not fields.get(f)]
                if missing:
                    errors.append(f"{rel}: faltan campos de frontmatter {missing}")
                state = fields.get("estado")
                if state and state not in VALID_STATES:
                    errors.append(f"{rel}: estado «{state}» no válido ({', '.join(sorted(VALID_STATES))})")

        for target in WIKILINK.findall(text):
            links += 1
            if target.strip() not in valid_targets:
                errors.append(f"{rel}: enlace roto [[{target}]]")

        if text.count("```") % 2:
            errors.append(f"{rel}: bloque de código sin cerrar")

        for match in PERSONAL_PATH.finditer(text):
            errors.append(f"{rel}: ruta personal «{match.group(0)}»")

        in_code = False
        fields = frontmatter(text) or {}
        for number, line in enumerate(text.splitlines(), 1):
            if line.startswith("```"):
                in_code = not in_code
                continue
            if in_code:
                continue
            row = ID_ROW.match(line)
            if row:
                prefix, num = row.groups()
                key = f"{prefix}-{num}"
                definitions.setdefault(key, []).append(f"{rel}:{number}")
                owner = ID_OWNERS[prefix]
                if note.stem != owner:
                    errors.append(f"{rel}:{number}: {key} se define fuera de su nota dueña «{owner}»")
            if fields.get("estado") == "propuesta":
                for word in ABSOLUTE.findall(line):
                    if not NEGATION.search(line):
                        warnings.append(f"{rel}:{number}: lenguaje absoluto «{word}»")

    for key, places in definitions.items():
        if len(places) > 1:
            errors.append(f"{key} definido más de una vez: {', '.join(places)}")

    control = names.get("Fuente y control documental")
    if control:
        text = read(control)
        noted = COUNT_NOTES.findall(text)
        linked = COUNT_LINKS.findall(text)
        if not noted or int(noted[-1]) != len(notes):
            errors.append(f"Fuente y control documental: el último conteo de notas no es {len(notes)}")
        if not linked or int(linked[-1]) != links:
            errors.append(f"Fuente y control documental: el último conteo de enlaces no es {links}")
    return len(notes), links


def main() -> int:
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    parser = argparse.ArgumentParser(description="Verifica la bóveda de IstpetDev.")
    parser.add_argument("--quiet-warnings", action="store_true", help="no muestra los avisos de lenguaje absoluto")
    args = parser.parse_args()

    errors: list[str] = []
    warnings: list[str] = []
    notes, links = check_vault(errors, warnings)

    if warnings and not args.quiet_warnings:
        print(f"Avisos ({len(warnings)}):")
        for warning in warnings:
            print(f"  - {warning}")
    if errors:
        print(f"Errores ({len(errors)}):")
        for error in errors:
            print(f"  - {error}")
    print(f"Resumen: {notes} notas Markdown, {links} enlaces internos, "
          f"{len(errors)} errores, {len(warnings)} avisos.")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main())
