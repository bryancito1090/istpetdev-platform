#!/usr/bin/env python3
"""Build deterministic n8n exports from reviewed templates and the versioned prompt."""

import argparse
import hashlib
import json
import re
import sys
from pathlib import Path
from typing import Any


REPO_ROOT = Path(__file__).resolve().parents[2]
PROMPT_FILE = REPO_ROOT / "automation/n8n/prompts/agente-logistico-v1.md"
TEMPLATES_DIR = REPO_ROOT / "automation/n8n/templates"
WORKFLOWS_DIR = REPO_ROOT / "automation/n8n/workflows"
PROMPT_VERSION = "v1"
N8N_VERSION = "2.39.5"
REQUIRED_WORKFLOWS = ("vigia", "incidente")
SECRET_KEYS = {
    "apikey",
    "password",
    "secret",
    "clientsecret",
    "token",
    "authorization",
}
SECRET_VALUE_PATTERNS = (
    re.compile(r"(?i)\bBearer\s+[A-Za-z0-9._~+/-]{12,}"),
    re.compile(r"\beyJ[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]{8,}\b"),
    re.compile(r"\bsk-[A-Za-z0-9_-]{16,}\b"),
    re.compile(r"\bAKIA[A-Z0-9]{16}\b"),
)


def canonical_dumps(value: Any) -> str:
    return json.dumps(value, ensure_ascii=False, indent=2, sort_keys=True) + "\n"


def _normalized_key(key: str) -> str:
    return re.sub(r"[^a-z0-9]", "", key.lower())


def assert_no_embedded_secrets(value: Any, path: str = "workflow") -> None:
    """Reject credential payloads while allowing n8n credential references."""
    if isinstance(value, dict):
        for key, child in value.items():
            if _normalized_key(str(key)) in SECRET_KEYS:
                raise ValueError(f"Campo sensible literal en {path}.{key}")
            assert_no_embedded_secrets(child, f"{path}.{key}")
    elif isinstance(value, list):
        for index, child in enumerate(value):
            assert_no_embedded_secrets(child, f"{path}[{index}]")
    elif isinstance(value, str):
        for pattern in SECRET_VALUE_PATTERNS:
            if pattern.search(value):
                raise ValueError(f"Posible credencial literal en {path}")


def _replace_system_message(value: Any, prompt: str) -> int:
    count = 0
    if isinstance(value, dict):
        for key, child in list(value.items()):
            if _normalized_key(str(key)) in {"systemmessage", "systemprompt"}:
                value[key] = prompt
                count += 1
            else:
                count += _replace_system_message(child, prompt)
    elif isinstance(value, list):
        for child in value:
            count += _replace_system_message(child, prompt)
    return count


def _inject_prompt_version(value: Any) -> int:
    count = 0
    if isinstance(value, dict):
        for key, child in list(value.items()):
            if _normalized_key(str(key)) == "promptversion":
                value[key] = PROMPT_VERSION
                count += 1
            else:
                replaced, replacements = _replace_marker(child)
                value[key] = replaced
                count += replacements
                if replacements == 0:
                    count += _inject_prompt_version(child)
    elif isinstance(value, list):
        for index, child in enumerate(value):
            replaced, replacements = _replace_marker(child)
            value[index] = replaced
            count += replacements
            if replacements == 0:
                count += _inject_prompt_version(child)
    return count


def _replace_marker(value: Any) -> tuple[Any, int]:
    if isinstance(value, str) and "{{PROMPT_VERSION}}" in value:
        return value.replace("{{PROMPT_VERSION}}", PROMPT_VERSION), 1
    return value, 0


def inject_prompt_into_workflow(workflow: dict[str, Any], prompt: str, name: str) -> dict[str, Any]:
    nodes = workflow.get("nodes")
    if not isinstance(nodes, list):
        raise ValueError(f"{name}: exportación sin arreglo nodes")

    agents = [node for node in nodes if "agent" in str(node.get("type", "")).lower()]
    if len(agents) != 1:
        raise ValueError(f"{name}: se esperaba un nodo AI Agent y se encontraron {len(agents)}")

    changed = _replace_system_message(agents[0].get("parameters", {}), prompt)
    if changed == 0:
        raise ValueError(f"{name}: el nodo AI Agent debe tener systemMessage en su plantilla")

    result_nodes = [
        node for node in nodes
        if node.get("name") in {"Send Result", "Prepare Result"}
        or "callback" in str(node.get("name", "")).lower()
    ]
    if not result_nodes:
        raise ValueError(f"{name}: falta el nodo fijo Send Result")
    version_fields = sum(_inject_prompt_version(node.get("parameters", {})) for node in result_nodes)
    if version_fields == 0:
        raise ValueError(f"{name}: Send Result debe enviar promptVersion o incluir {{PROMPT_VERSION}}")

    workflow_meta = workflow.setdefault("meta", {})
    workflow_meta["promptVersion"] = PROMPT_VERSION
    workflow_meta["n8nVersion"] = N8N_VERSION
    # Runtime revision IDs are assigned by n8n when importing the source.
    workflow.pop("versionId", None)
    return workflow


def _load_json(path: Path) -> dict[str, Any]:
    value = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(value, dict):
        raise ValueError(f"La plantilla debe ser un objeto JSON: {path}")
    return value


def build_workflows(
    prompt_file: Path = PROMPT_FILE,
    templates_dir: Path = TEMPLATES_DIR,
    output_dir: Path = WORKFLOWS_DIR,
    check_only: bool = False,
) -> int:
    if not prompt_file.is_file():
        raise FileNotFoundError(f"No existe el prompt: {prompt_file}")
    prompt = prompt_file.read_text(encoding="utf-8").strip()
    if not prompt:
        raise ValueError(f"El prompt está vacío: {prompt_file}")

    missing = [name for name in REQUIRED_WORKFLOWS if not (templates_dir / f"{name}.template.json").is_file()]
    if missing:
        raise FileNotFoundError(
            "Faltan las plantillas exportadas de n8n: " + ", ".join(missing)
            + f". Expórtalas desde n8n {N8N_VERSION} antes de generar los workflows."
        )

    generated: dict[str, str] = {}
    for name in REQUIRED_WORKFLOWS:
        template_path = templates_dir / f"{name}.template.json"
        workflow = inject_prompt_into_workflow(_load_json(template_path), prompt, name)
        assert_no_embedded_secrets(workflow, name)
        generated[name] = canonical_dumps(workflow)

    if check_only:
        stale = [
            name for name, content in generated.items()
            if not (output_dir / f"{name}.json").is_file()
            or (output_dir / f"{name}.json").read_text(encoding="utf-8") != content
        ]
        if stale:
            print("Workflows ausentes o desactualizados: " + ", ".join(stale), file=sys.stderr)
            return 1
        return 0

    output_dir.mkdir(parents=True, exist_ok=True)
    for name, content in generated.items():
        path = output_dir / f"{name}.json"
        path.write_text(content, encoding="utf-8")
        digest = hashlib.sha256(content.encode("utf-8")).hexdigest()
        print(f"Generado {path.name} (SHA-256 {digest})")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description="Genera JSON n8n reproducibles desde exportaciones revisadas.")
    parser.add_argument("--check", action="store_true", help="Compara los JSON sin escribirlos.")
    parser.add_argument("--prompt-file", type=Path, default=PROMPT_FILE, help=argparse.SUPPRESS)
    parser.add_argument("--templates-dir", type=Path, default=TEMPLATES_DIR, help=argparse.SUPPRESS)
    parser.add_argument("--output-dir", type=Path, default=WORKFLOWS_DIR, help=argparse.SUPPRESS)
    args = parser.parse_args()
    try:
        return build_workflows(args.prompt_file, args.templates_dir, args.output_dir, args.check)
    except Exception as exc:
        print(f"Error generando workflows: {exc}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
