"""Genera Entregable/9-Guion-del-video.pdf desde la nota docs/boveda/07 Demo/Guion del video.md
(la nota es la fuente; este PDF se regenera).

Uso:
    uv run --with markdown python docs/specs/006-validacion-pitch-entrega/materiales/generar_guion_video_pdf.py
"""
from __future__ import annotations

import re
import shutil
import subprocess
import tempfile
from pathlib import Path

import markdown

ROOT = Path(__file__).resolve().parents[4]
NOTE = ROOT / "docs/boveda/07 Demo/Guion del video.md"
OUT = ROOT / "docs/boveda/Entregable/9-Guion-del-video.pdf"
CSS = """body{font-family:'Open Sans',sans-serif;font-size:10pt;color:#242D57}
h1{font-size:18pt;color:#242D57}h2{font-size:13pt;color:#7D6520}
table{border-collapse:collapse;width:100%}th{background:#EDF0F7;font-size:9pt}
td,th{border:1px solid #D5DBEA;padding:4px;vertical-align:top;font-size:9pt}"""


def main():
    text = NOTE.read_text(encoding="utf-8")
    text = re.sub(r"\A---.*?---\n", "", text, flags=re.S)
    text = re.sub(r"\[\[([^\]|]+)(\|[^\]]+)?\]\]", r"\1", text)
    body = markdown.markdown(text, extensions=["tables"])
    html = f"<html><head><meta charset='utf-8'><style>{CSS}</style></head><body>{body}</body></html>"
    with tempfile.TemporaryDirectory() as tmp:
        src = Path(tmp) / "9-Guion-del-video.html"
        src.write_text(html, encoding="utf-8")
        soffice = shutil.which("soffice") or shutil.which("libreoffice")
        subprocess.run([soffice, f"-env:UserInstallation=file://{tmp}/lo", "--headless",
                        "--convert-to", "pdf:writer_web_pdf_Export", "--outdir", tmp, str(src)],
                       check=True, capture_output=True)
        shutil.copy(Path(tmp) / "9-Guion-del-video.pdf", OUT)
    print(OUT)


if __name__ == "__main__":
    main()
