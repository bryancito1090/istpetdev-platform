"""Genera pitch-v5.pptx (con GIF animado) y pitch-v5-estatico.pdf (con imágenes fijas).

Reutiliza el diseño de generar_v3.py y las láminas 1–4 y 8–12 de generar_v4.py. Las láminas
5 (monitoreo de cobertura), 6 (rutas) y 7 (novedades de campo) usan capturas reales de
assets/capturas-v5/. Guion: «Versión vigente v5» en docs/boveda/07 Demo/Guion del pitch.md.

El PPTX lleva el GIF de la vista previa de la ruta (se anima en la presentación); el PDF no
puede animar, así que lleva la captura fija de la misma pantalla.

Uso:
    uv run --with python-pptx --with pillow --with numpy --with scipy \
        python docs/specs/006-validacion-pitch-entrega/materiales/generar_v5.py
"""
from __future__ import annotations

import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))

import generar_v3 as v3  # noqa: E402
import generar_v4 as v4  # noqa: E402
from generar_v3 import (  # noqa: E402
    AZUL, BLANCO, DORADO, LINEA, MARGIN, MSO_ANCHOR, W, Image, Timeline, card, picture, record,
    rect, soft_shadow, text,
)

CAPTURES = v3.ASSETS / "capturas-v5"
MODE = {"animated": False}


def framed(shapes, name, x, y, w, h):
    """Captura enmarcada que cabe en (w, h), centrada, con filete dorado debajo."""
    path = CAPTURES / name
    img = Image.open(path)
    scale = min(w / img.width, h / img.height)
    pw, ph = img.width * scale, img.height * scale
    px, py = x + (w - pw) / 2, y + (h - ph) / 2
    grp = shapes.add_group_shape()
    border = rect(grp.shapes, px - 0.08, py - 0.08, pw + 0.16, ph + 0.16, BLANCO, LINEA)
    soft_shadow(border, blur=12, dist=4, alpha=18)
    picture(grp.shapes, path, px, py, w=pw)
    rect(grp.shapes, px - 0.08, py + ph + 0.08, pw + 0.16, 0.05, DORADO)
    return grp


def callout(shapes, x, y, w, h, message, label):
    grp = shapes.add_group_shape()
    rect(grp.shapes, x, y, w, h, AZUL)
    rect(grp.shapes, x, y, 0.07, h, DORADO)
    text(grp.shapes, x + 0.3, y, w - 0.45, h, message, 16, BLANCO, bold=True,
         valign=MSO_ANCHOR.MIDDLE, label=label)
    return grp


def slide_coverage(deck):
    s = deck.slide(dark=False)
    sh = s.shapes
    tl = Timeline()
    head = v4.chrome(deck, s, False, "Prototipo / demo · detectar",
                     "El aviso llega antes que la llamada",
                     "Fuente: pantalla Monitoreo de cobertura de stock (captura del 17-oct); "
                     "cálculo de cobertura A-04. Datos sintéticos.", 0)
    tl.add(head, "fade", dur=400)
    cap_w = 8.3
    tl.add(framed(sh, "alertas.png", MARGIN, 1.85, cap_w, 4.5), "wipe", dur=600)
    x2 = MARGIN + cap_w + 0.4
    info = card(sh, x2, 1.85, W - MARGIN - x2, 3.15, "Monitoreo de cobertura",
                "Cada punto con sus días de cobertura, su nivel de riesgo y la cantidad "
                "sugerida para reponer.", heading_size=22, body_size=16)
    tl.add(info, "fade", dur=450)
    tl.add(callout(sh, x2, 5.2, W - MARGIN - x2, 1.15,
                   "El planificador sabe qué punto atender primero.", "mensaje cobertura"),
           "fade", mode="click", dur=450)
    record(deck, s, tl, "Demo · monitoreo de cobertura",
           "Auto: título → captura (barrido) → tarjeta. Clic 1: mensaje final.")


def slide_routes(deck):
    s = deck.slide(dark=False)
    sh = s.shapes
    tl = Timeline()
    head = v4.chrome(deck, s, False, "Prototipo / demo · entregar", "De la alerta a la ruta",
                     "Fuente: pantallas Planificar rutas y Rutas (capturas del 17-oct); "
                     "escenario sintético de Quito, Bodega Sur · VEH-02. Sin tráfico en vivo.", 0)
    tl.add(head, "fade", dur=400)
    main_w = 6.9
    if MODE["animated"]:
        main = CAPTURES / "ruta-paso2.gif"
        img = Image.open(main)
        mh = main_w / img.width * img.height
        grp = sh.add_group_shape()
        border = rect(grp.shapes, MARGIN - 0.08, 1.85 - 0.08, main_w + 0.16, mh + 0.16, BLANCO, LINEA)
        soft_shadow(border, blur=12, dist=4, alpha=18)
        picture(grp.shapes, main, MARGIN, 1.85, w=main_w)
        rect(grp.shapes, MARGIN - 0.08, 1.85 + mh + 0.08, main_w + 0.16, 0.05, DORADO)
        tl.add(grp, "wipe", dur=600)
    else:
        tl.add(framed(sh, "ruta-paso2-modal.png", MARGIN, 1.85, main_w, 4.5), "wipe", dur=600)
    x2 = MARGIN + main_w + 0.45
    w2 = W - MARGIN - x2
    tl.add(framed(sh, "ruta-vehiculo.png", x2, 1.85, w2, 1.75), "fade", dur=450)
    tl.add(framed(sh, "ruta-seguimiento.png", x2, 3.8, w2, 1.4), "fade", mode="after", dur=450)
    tl.add(callout(sh, x2, 5.4, w2, 0.95,
                   "El planificador revisa la ruta y la aprueba antes de publicarla.",
                   "mensaje rutas"), "fade", mode="click", dur=450)
    record(deck, s, tl, "Demo · rutas",
           "Auto: título → vista previa de la ruta (GIF en el PPTX, captura fija en el PDF) → "
           "vehículo y seguimiento. Clic 1: mensaje final.")


def slide_field_news(deck):
    s = deck.slide(dark=False)
    sh = s.shapes
    tl = Timeline()
    head = v4.chrome(deck, s, False, "Prototipo / demo · probar",
                     "Lo que pasa en el camino queda a la vista",
                     "Fuente: pantalla Novedades de campo (captura del 17-oct); datos "
                     "sintéticos. El impacto de reentrada es una estimación del sistema.", 0)
    tl.add(head, "fade", dur=400)
    cap_w = 8.3
    tl.add(framed(sh, "novedades.png", MARGIN, 1.85, cap_w, 4.5), "wipe", dur=600)
    x2 = MARGIN + cap_w + 0.4
    info = card(sh, x2, 1.85, W - MARGIN - x2, 3.15, "Novedades",
                "Rechazos en destino, impacto estimado de reentrada, ajustes de percha y "
                "pedidos hechos en el punto de servicio.", heading_size=22, body_size=16)
    tl.add(info, "fade", dur=450)
    tl.add(callout(sh, x2, 5.2, W - MARGIN - x2, 1.15,
                   "Cada rechazo queda con su motivo: daño, error de rotulado.",
                   "mensaje novedades"), "fade", mode="click", dur=450)
    record(deck, s, tl, "Demo · novedades de campo",
           "Auto: título → captura (barrido) → tarjeta. Clic 1: mensaje final.")


def build(animated: bool, pptx: Path, export: bool):
    MODE["animated"] = animated
    v3.PPTX_PATH = pptx
    v3.PDF_PATH = pptx.with_suffix(".pdf")
    v3.RENDER_DIR = HERE / "render-v5"
    deck = v3.Deck(v3.prepare_assets())
    deck.pending = []
    deck.prs.core_properties.title = "IstpetDev · Pitch Expo Clean · v5"
    deck.prs.core_properties.subject = "Detectar, decidir, entregar y probar; guion v5"
    # Orden del guion v5: 1 gancho, 2 dolor, 3 cadena, 4 innovación, 5 cobertura, 6 rutas,
    # 7 novedades, 8 A/B/C, 9 validación, 10 indicadores, 11 modelo y riesgos, 12 próximo paso.
    for builder in (v4.slide_cover, v4.slide_pain, v4.slide_chain, v4.slide_inside,
                    slide_coverage, slide_routes, slide_field_news, v3.slide_abc,
                    v4.slide_validation, v3.slide_impact, v4.slide_model_risks, v4.slide_next):
        builder(deck)
    deck.prs.save(pptx)
    assert len(v3.Presentation(str(pptx)).slides) == len(deck.notes) == 12
    if export:
        v3.export(pptx)
    return deck


if __name__ == "__main__":
    build(False, HERE / "pitch-v5-estatico.pptx", export=True)
    (HERE / "pitch-v5-estatico.pptx").unlink()
    deck = build(True, HERE / "pitch-v5.pptx", export=False)
    for note in deck.notes:
        print(f"{note['n']:>2}. {note['title']}: {note['clicks']} clic(s)")
    for warning in v3.FIT_WARNINGS:
        print(f"AVISO de ajuste · {warning}")
