"""Genera pitch-v7.pptx (con GIF animado) y pitch-v7.pdf (con imágenes fijas).

v7 = v6 con los cambios de la revisión del 17-oct: rótulo del reto y mención de la IA en la
portada, el problema en rojo (láminas 2 y 6), «Siguiente fase» en lugar de «Falta», KPI como
insumo de los informes ejecutivos (sección Reportes) y cierre sin «ensayar sin señal».

Guion: «Propuesta v7» en docs/boveda/07 Demo/Guion del pitch.md. Cuenta un solo caso, de la
alerta a la decisión: 9 láminas. Reutiliza el diseño de generar_v3.py, láminas de generar_v4.py
(portada, validación) y generar_v5.py (cobertura) y el panel A/B/C de generar_v3.py. Las láminas
2 (el caso, con diagrama), 5 (ruta y prueba), 6 (por qué es nuevo), 8 (impacto y modelo) y 9
(cierre) son nuevas.

Uso:
    uv run --with python-pptx --with pillow --with numpy --with scipy \
        python docs/specs/006-validacion-pitch-entrega/materiales/generar_v6.py
"""
from __future__ import annotations

import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))

import generar_v3 as v3  # noqa: E402
import generar_v4 as v4  # noqa: E402
import generar_v5 as v5  # noqa: E402
from pptx.dml.color import RGBColor  # noqa: E402
from generar_v3 import (  # noqa: E402
    ACERO, AZUL, BLANCO, CHILLO_RIGHT, CREMA, DORADO, DORADO_CLARO, DORADO_OSCURO, GRIS,
    GRIS_NIEBLA, LINEA, MARGIN, MSO_ANCHOR, NIEBLA, NOCHE, PP_ALIGN, TITLE_FONT, W, Timeline,
    bubble, card, chillo_full, chillo_peek, chip, oval, peek_bubble, record, rect, text,
)

ROJO = RGBColor(0xB8, 0x2E, 0x2E)         # gravedad del problema; texto blanco encima (6,5:1)
ROJO_CLARO = RGBColor(0xFF, 0x9A, 0x8B)    # resalte rojo sobre fondo azul oscuro (7,2:1)

# El panel A/B/C de v3 conserva su tabla; solo se cambia su título para la historia del caso.
TITLES = {"A / B / C: el motor compara y recomienda": "La más barata no fue la mejor"}
_chrome_v4 = v3.chrome


def _chrome(deck, slide, dark, kicker, title, source, number, **kw):
    return _chrome_v4(deck, slide, dark, kicker, TITLES.get(title, title), source, number, **kw)


v3.chrome = _chrome
chrome = v4.chrome


def slide_case(deck):
    """Lámina 2: los mismos pedidos, tres maneras de atenderlos (diagrama)."""
    s = deck.slide(dark=True)
    sh = s.shapes
    tl = Timeline()
    head = chrome(deck, s, True, "El caso", "Los mismos pedidos, tres maneras de atenderlos",
                  "Fuente: ensayo técnico #285 (demo-quito-planificacion): 4 pedidos, 6 entregas "
                  "críticas; opciones A, B y C de 02-suggest-pitch-abc.json · matriz P-02, P-04, "
                  "P-05. Escenario sintético: no son clientes ni operaciones medidas.", 0)
    tl.add(head, "fade", dur=400)

    top, oh, gap = 1.85, 1.0, 0.18           # alto de cada opción y separación
    ys = [top + i * (oh + gap) for i in range(3)]
    mid = (ys[0] + ys[2] + oh) / 2           # centro vertical del bloque de opciones
    sw, sx = 3.3, MARGIN                     # tarjeta de origen
    ox = 5.35                                # x de las opciones
    ow = W - MARGIN - ox
    trunk = (sx + sw + ox) / 2               # x del tronco que reparte

    source = sh.add_group_shape()
    sh_h = 2.2
    rect(source.shapes, sx, mid - sh_h / 2, sw, sh_h, ACERO, DORADO, line_width=1.5)
    rect(source.shapes, sx, mid - sh_h / 2, sw, 0.06, DORADO)
    text(source.shapes, sx + 0.25, mid - sh_h / 2 + 0.2, sw - 0.5, 0.62, "4 pedidos", 28,
         DORADO, bold=True, font=TITLE_FONT, label="pedidos")
    text(source.shapes, sx + 0.25, mid - sh_h / 2 + 0.85, sw - 0.5, 1.2,
         [[("6 entregas críticas", {"color": ROJO_CLARO, "bold": True})],
          "Hospitales y otros clientes", "Quito, datos sintéticos"],
         14.5, GRIS_NIEBLA, line_spacing=1.05, space_after=3, label="detalle pedidos")
    tl.add(source, "fade", dur=450)

    lines = sh.add_group_shape()
    rect(lines.shapes, sx + sw, mid - 0.015, trunk - sx - sw, 0.03, DORADO)
    rect(lines.shapes, trunk - 0.015, ys[0] + oh / 2, 0.03, ys[2] - ys[0], DORADO)
    for y in ys:
        rect(lines.shapes, trunk, y + oh / 2 - 0.015, ox - trunk, 0.03, DORADO)
    tl.add(lines, "wipe", dur=500)

    options = [
        ("A", "Un solo viaje", "Flota propia: todo en un recorrido"),
        ("B", "Los críticos primero", "Flota propia: lo crítico sale en el primer turno"),
        ("C", "Operador contratado", "Un transportista externo cubre todo"),
    ]
    for i, (letter, head_txt, body) in enumerate(options):
        grp = sh.add_group_shape()
        rect(grp.shapes, ox, ys[i], ow, oh, ACERO)
        rect(grp.shapes, ox, ys[i], 0.07, oh, DORADO)
        oval(grp.shapes, ox + 0.3, ys[i] + oh / 2 - 0.34, 0.68, NOCHE, DORADO, line_width=1.5)
        text(grp.shapes, ox + 0.3, ys[i] + oh / 2 - 0.34, 0.68, 0.68, letter, 26, DORADO,
             bold=True, font=TITLE_FONT, align=PP_ALIGN.CENTER, valign=MSO_ANCHOR.MIDDLE)
        text(grp.shapes, ox + 1.25, ys[i] + 0.12, ow - 1.45, 0.48, head_txt, 22, BLANCO,
             bold=True, font=TITLE_FONT, label=f"opción {letter}")
        text(grp.shapes, ox + 1.25, ys[i] + 0.6, ow - 1.45, 0.34, body, 15, GRIS_NIEBLA,
             label=f"detalle {letter}")
        tl.add(grp, "wipe", mode="after", dur=400)

    band = sh.add_group_shape()
    by, bh = 5.62, 0.96
    rect(band.shapes, MARGIN, by, W - 2 * MARGIN, bh, ROJO)
    rect(band.shapes, MARGIN, by, 0.09, bh, DORADO)
    text(band.shapes, MARGIN + 0.4, by, 7.2, bh,
         [[("El problema: hoy se decide mirando kilómetros. ", {"bold": True}),
           ("Lo que cuesta de verdad: personal, combustible, descarga y producto que se daña.",
            {})]],
         16, BLANCO, valign=MSO_ANCHOR.MIDDLE, label="costo real")
    text(band.shapes, MARGIN + 7.8, by, W - 2 * MARGIN - 8.1, bh, "¿Cuál conviene?", 34, BLANCO,
         bold=True, font=TITLE_FONT, align=PP_ALIGN.RIGHT, valign=MSO_ANCHOR.MIDDLE,
         label="pregunta")
    tl.add(band, "wipe", mode="click", dur=550)
    record(deck, s, tl, "El caso · diagrama A / B / C",
           "Auto: título → 4 pedidos → ramas → A, B, C en cascada. Clic 1: banda roja «El problema: "
           "hoy se decide mirando kilómetros» y la pregunta «¿Cuál conviene?».")


def slide_route_proof(deck):
    """Lámina 5: de la decisión a la ruta, y la prueba de lo que pasa en el camino."""
    s = deck.slide(dark=False)
    sh = s.shapes
    tl = Timeline()
    head = chrome(deck, s, False, "Prototipo / demo · entregar y probar",
                  "Elegida la opción: ruta aprobada y entrega con prueba",
                  "Fuente: pantallas Planificar rutas, Rutas y Novedades de campo (capturas del "
                  "17-oct); escenario sintético de Quito, Bodega Sur · VEH-02. Sin tráfico en "
                  "vivo; el impacto de reentrada es una estimación del sistema.", 0)
    tl.add(head, "fade", dur=400)
    main_w = 5.6
    if v5.MODE["animated"]:
        main = v5.CAPTURES / "ruta-paso2.gif"
        img = v3.Image.open(main)
        mh = main_w / img.width * img.height
        grp = sh.add_group_shape()
        border = rect(grp.shapes, MARGIN - 0.08, 1.85 - 0.08, main_w + 0.16, mh + 0.16, BLANCO,
                      LINEA)
        v3.soft_shadow(border, blur=12, dist=4, alpha=18)
        v3.picture(grp.shapes, main, MARGIN, 1.85, w=main_w)
        rect(grp.shapes, MARGIN - 0.08, 1.85 + mh + 0.08, main_w + 0.16, 0.05, DORADO)
        tl.add(grp, "wipe", dur=600)
    else:
        tl.add(v5.framed(sh, "ruta-paso2-modal.png", MARGIN, 1.85, main_w, 4.0), "wipe", dur=600)
    x2 = MARGIN + main_w + 0.4
    w2 = W - MARGIN - x2
    tl.add(v5.framed(sh, "ruta-seguimiento.png", x2, 1.85, w2, 1.5), "fade", dur=450)
    tl.add(v5.framed(sh, "novedades.png", x2, 3.55, w2, 2.0), "wipe", mode="click", dur=550)
    tl.add(v5.callout(sh, MARGIN, 6.0, W - 2 * MARGIN, 0.62,
                      "Cada caja viaja con QR y cada rechazo queda con su motivo.",
                      "mensaje ruta y prueba"), "fade", mode="click", dur=450)
    record(deck, s, tl, "Demo · ruta y prueba",
           "Auto: título → vista previa de la ruta (GIF en el PPTX, captura fija en el PDF) → "
           "seguimiento. Clic 1: novedades de campo. Clic 2: mensaje final.")


def slide_new(deck):
    """Lámina 6: por qué es nuevo — costo completo en la entrega y la división de papeles."""
    s = deck.slide(dark=False)
    sh = s.shapes
    tl = Timeline()
    head = chrome(deck, s, False, "Innovación", "A la entrega le ponemos costo completo",
                  "Fuente: matriz A-02 (hipótesis del sector), A-06, A-08, A-10, A-11 y A-12 "
                  "(run n8n 292, respaldo sin IA). Lo del sector es lo que escuchamos, no una "
                  "medición.", 0)
    tl.add(head, "fade", dur=400)
    gap = 0.4
    cw = (W - 2 * MARGIN - gap) / 2
    top, ch = 1.8, 2.75
    left = card(sh, MARGIN, top, cw, ch, "El problema, según el sector",
                ["Se repone por calendario o por urgencia.",
                 "La entrega se decide mirando kilómetros.",
                 "Nadie puede probar qué se entregó."],
                tone="light", heading_size=22, body_size=18)
    rect(left.shapes, MARGIN, top, cw, 0.1, ROJO)
    tl.add(left, "fade", dur=450)
    right = card(sh, MARGIN + cw + gap, top, cw, ch, "Con IstpetDev",
                 ["Avisa antes de que el cliente llame.",
                  "Decide con costo completo: personal, combustible, descarga y merma.",
                  "Deja la entrega comprobable, caja por caja."],
                 tone="navy", heading_size=22, body_size=18)
    tl.add(right, "fade", mode="click", dur=450)
    band = sh.add_group_shape()
    by = 4.85
    rect(band.shapes, MARGIN, by, W - 2 * MARGIN, 1.55, AZUL)
    rect(band.shapes, MARGIN, by, 0.07, 1.55, DORADO)
    text(band.shapes, MARGIN + 0.4, by + 0.2, W - 2 * MARGIN - 0.8, 0.6,
         [[("El motor calcula, ", {}), ("la IA explica, ", {"color": DORADO_CLARO}),
           ("la persona decide.", {})]],
         28, BLANCO, bold=True, font=TITLE_FONT, label="papeles")
    text(band.shapes, MARGIN + 0.4, by + 0.85, W - 2 * MARGIN - 0.8, 0.6,
         "Mismos datos, mismo resultado. La IA no inventa cifras y, si falla, hay una "
         "explicación de respaldo.", 16, GRIS_NIEBLA, label="detalle papeles")
    tl.add(band, "wipe", mode="click", dur=550)
    record(deck, s, tl, "Innovación · costo completo en la entrega",
           "Auto: título → «El problema, según el sector» (filete rojo). Clic 1: «Con IstpetDev». Clic 2: banda «El motor "
           "calcula, la IA explica, la persona decide».")


def slide_impact_model(deck):
    """Lámina 8: qué se mide en el piloto y cómo llega a la empresa."""
    s = deck.slide(dark=False)
    sh = s.shapes
    tl = Timeline()
    head = chrome(deck, s, False, "Impacto y modelo", "Lo medimos en un piloto, contra una línea base",
                  "Fuente: matriz A-21 (sin ahorro prometido), A-26 y A-30 (costo y precio por "
                  "validar), A-31 (modelo), P-07 (indicadores en curso); pantalla Reportes "
                  "(código en develop, sin ensayo de la escena). Sujeto a validación.", 0)
    tl.add(head, "fade", dur=400)
    lw = 7.2
    items = [
        ("Faltantes que se descubren tarde", "Menos quiebres que llegan por llamada del cliente"),
        ("Entregas críticas a tiempo", "Cuántas llegan dentro de la ventana acordada"),
        ("Costo real de cada entrega", "Con su desglose, antes y después"),
    ]
    y = 1.8
    for i, (title, body) in enumerate(items):
        grp = card(sh, MARGIN, y, lw, 1.28, title, body, tone="light", heading_size=20,
                   body_size=15, number=f"{i + 1:02d}")
        tl.add(grp, "fade", dur=400)
        y += 1.28 + 0.12
    rx = MARGIN + lw + 0.4
    rw = W - MARGIN - rx
    model = card(sh, rx, 1.8, rw, 4.08, "Cómo llega",
                 ["Sin hardware nuevo: web y el teléfono del conductor.",
                  "Implementación inicial más suscripción; el precio se valida en el piloto.",
                  "Si falta un dato, dice «sin dato»."],
                 tone="navy", heading_size=26, body_size=16)
    tl.add(model, "wipe", mode="click", dur=500)
    band = sh.add_group_shape()
    rect(band.shapes, MARGIN, 6.02, W - 2 * MARGIN, 0.68, AZUL)
    rect(band.shapes, MARGIN, 6.02, 0.07, 0.68, DORADO)
    text(band.shapes, MARGIN + 0.35, 6.02, W - 2 * MARGIN - 0.7, 0.68,
         [[("Los indicadores alimentan ", {}),
           ("informes ejecutivos", {"color": DORADO_CLARO}),
           (": ya tenemos la sección Reportes.", {})]],
         20, BLANCO, bold=True, valign=MSO_ANCHOR.MIDDLE, label="reportes")
    tl.add(band, "wipe", mode="click", dur=500)
    record(deck, s, tl, "Impacto y modelo",
           "Auto: título → tres indicadores en cascada. Clic 1: tarjeta «Cómo llega». Clic 2: "
           "banda de informes ejecutivos (sección Reportes).")


def slide_close(deck):
    """Lámina 9: cierre con la tesis y la petición del piloto."""
    s = deck.slide(dark=True)
    sh = s.shapes
    tl = Timeline()
    right = CHILLO_RIGHT
    head = chrome(deck, s, True, "Próximo paso", "Un piloto con una empresa de limpieza",
                  "Fuente: matriz A-21, A-32 y P-02. Propuesta sin contraparte confirmada; "
                  "sujeta a validación.", 0, source_right=right)
    tl.add(head, "fade", dur=400)
    gallo = chillo_full(deck, s, 10.42, 6.72, 4.3)
    tl.add(gallo, "zoom", dur=550)
    quote = text(sh, MARGIN, 1.85, right - MARGIN, 1.9,
                 [[("Una buena cotización suma el costo completo. ", {"color": GRIS_NIEBLA}),
                   ("Nosotros se lo ponemos a la entrega.", {"color": DORADO})]],
                 32, BLANCO, bold=True, font=TITLE_FONT, line_spacing=1.0, label="tesis")
    tl.add(quote, "wipe", dur=600)
    steps = [("Línea base", "Medir el punto de partida"),
             ("Indicadores", "Acordados con la empresa"),
             ("Su empresa", "Cargar sus datos y operar")]
    gap = 0.25
    cw = (right - MARGIN - 2 * gap) / 3
    for i, (title, body) in enumerate(steps):
        grp = card(sh, MARGIN + i * (cw + gap), 3.7, cw, 1.85, title, body, tone="dark",
                   heading_size=20, body_size=14, number=f"{i + 1:02d}")
        tl.add(grp, "fade", dur=350)
    band = sh.add_group_shape()
    rect(band.shapes, MARGIN, 5.6, right - MARGIN, 0.95, DORADO)
    text(band.shapes, MARGIN + 0.35, 5.6, right - MARGIN - 0.7, 0.95,
         "Buscamos una empresa de limpieza para ese piloto.", 20, NOCHE, bold=True,
         valign=MSO_ANCHOR.MIDDLE, label="petición")
    tl.add(band, "wipe", mode="click", dur=550)
    msg = bubble(s, CHILLO_RIGHT + 0.18, 1.3, 3.5, 1.0,
                 [("¿Lo validamos juntos? ", False), ("¡Kikirikí!", True)],
                 tail_x=11.55, tail_y=2.48, size=14)
    tl.add(msg, "fade", mode="click", dur=400)
    record(deck, s, tl, "Cierre y petición del piloto",
           "Auto: título → Chillo (zoom) → tesis → tres pasos. Clic 1: banda «Buscamos una empresa "
           "de limpieza». Clic 2: globo de Chillo.",
           chillo="Chillo entra solo (zoom). Globo: clic 2.")


def slide_cover(deck):
    """Lámina 1: portada con el nombre del reto y el globo que menciona la IA."""
    s = deck.slide(dark=True)
    sh = s.shapes
    tl = Timeline()
    logo = v3.picture(sh, deck.assets["logo_white"], MARGIN, 0.62, h=1.2)
    tl.add(logo, "fade", dur=500)
    head = sh.add_group_shape()
    rect(head.shapes, MARGIN, 2.32, 0.05, 0.3, DORADO)
    text(head.shapes, MARGIN + 0.17, 2.33, 8.0, 0.3, "OPTIMIZACIÓN Y TRAZABILIDAD LOGÍSTICA",
         14, DORADO, bold=True, spacing=200, label="reto")
    text(head.shapes, MARGIN, 2.72, 8.6, 2.5,
         "¿Cómo se entera una empresa de limpieza de que a un hospital le falta desinfectante?",
         40, BLANCO, bold=True, font=TITLE_FONT, line_spacing=0.95, label="pregunta")
    tl.add(head, "wipe", dur=600)
    sub = text(sh, MARGIN, 5.3, 8.6, 0.5,
               [[("Cuando el hospital llama. ", {}),
                 ("Ese día ya es tarde.", {"color": DORADO_CLARO, "bold": True})]],
               22, GRIS_NIEBLA)
    tl.add(sub, "fade", dur=450)
    foot = sh.add_group_shape()
    rect(foot.shapes, MARGIN, 6.2, 1.1, 0.025, DORADO)
    text(foot.shapes, MARGIN, 6.4, 9.0, 0.35,
         "Equipo IstpetDev · Instituto Superior Tecnológico Traversari", 15, BLANCO)
    text(foot.shapes, MARGIN, 6.82, 9.0, 0.3, v3.EVENT_TAG, 12, GRIS_NIEBLA, spacing=150)
    tl.add(foot, "fade", mode="with", dur=450)
    gallo = chillo_full(deck, s, 9.95, 7.32, 5.05)
    tl.add(gallo, "zoom", dur=550)
    msg = bubble(s, 8.0, 0.55, 5.05, 1.2,
                 [("¡Hola! Soy Chillo: usamos ", False),
                  ("IA para planificar rutas", True), (" y llegar antes que la llamada.", False)],
                 tail_x=11.2, tail_y=2.4, size=15)
    tl.add(msg, "fade", mode="click", dur=400)
    record(deck, s, tl, "Portada · gancho",
           "Auto: logo → pregunta → «Cuando el hospital llama» → equipo → Chillo (zoom). "
           "Clic 1: globo de Chillo.",
           chillo="Chillo entra solo (zoom). Globo: clic 1.")


def slide_validation(deck):
    """Lámina 7: construido, probado y siguiente fase (antes «Falta»)."""
    s = deck.slide(dark=True)
    sh = s.shapes
    tl = Timeline()
    head = chrome(deck, s, True, "Validación", "Lo que hay, sin humo",
                  "Fuente: matriz de afirmaciones P-02, P-04, P-05, A-03, A-11, A-12, A-13, A-15, "
                  "A-21 y #198. Escenario sintético de Quito: 40 puntos, no son clientes.", 0)
    tl.add(head, "fade", dur=400)
    cols = [
        ("Construido", [("Inventario con alertas", False), ("Motor de rutas y costo de servir", False),
                        ("Agente IA con n8n", False), ("Custodia con QR y captura sin señal", False)]),
        ("Probado", [("Motor y comparación A/B/C en un ensayo técnico con escenario sintético "
                      "de Quito", False),
                     ("Corrida registrada del agente, incluida su caída al respaldo", False)]),
        ("Siguiente fase", [("Usarlo en una empresa real: cargar sus datos y medir contra su "
                             "línea base", True),
                            ("Llevar la comparación A/B/C a producción", False)]),
    ]
    gap = 0.3
    cw = (W - 2 * MARGIN - 2 * gap) / 3
    for i, (title, bullets) in enumerate(cols):
        x = MARGIN + i * (cw + gap)
        grp = sh.add_group_shape()
        rect(grp.shapes, x, 1.85, cw, 3.7, ACERO if i < 2 else NOCHE, None if i < 2 else DORADO,
             line_width=1.5)
        rect(grp.shapes, x, 1.85, cw, 0.06, DORADO)
        text(grp.shapes, x + 0.3, 2.1, cw - 0.6, 0.6, title.upper(), 24,
             DORADO if i < 2 else DORADO_CLARO, bold=True, font=TITLE_FONT, spacing=120)
        text(grp.shapes, x + 0.3, 2.8, cw - 0.6, 2.65,
             [[("•  " + line, {"bold": True, "color": DORADO_CLARO} if hi else {})]
              for line, hi in bullets], 15.5, BLANCO,
             line_spacing=1.05, space_after=8, label=f"columna {title}")
        tl.add(grp, "fade", mode="click" if i else "after", dur=450)
    quote = text(sh, MARGIN, 5.8, W - 2 * MARGIN, 0.6,
                 "Preferimos mostrarles lo que funciona de verdad que prometer lo que no hemos medido.",
                 19, DORADO_CLARO, bold=True, italic=True, align=PP_ALIGN.CENTER, label="cita")
    tl.add(quote, "fade", mode="click", dur=450)
    record(deck, s, tl, "Validación · construido, probado, siguiente fase",
           "Auto: título → Construido. Clic 1: Probado. Clic 2: Siguiente fase. Clic 3: frase final.")


def build(animated: bool, pptx: Path, export: bool):
    v5.MODE["animated"] = animated
    v3.PPTX_PATH = pptx
    v3.PDF_PATH = pptx.with_suffix(".pdf")
    v3.RENDER_DIR = HERE / "render-v7"
    deck = v3.Deck(v3.prepare_assets())
    deck.pending = []
    deck.prs.core_properties.title = "IstpetDev · Pitch Expo Clean · v7"
    deck.prs.core_properties.subject = "Un caso, de la alerta a la decisión; guion v7"
    # Orden del guion v7: 1 gancho, 2 caso, 3 alerta, 4 decisión A/B/C, 5 ruta y prueba,
    # 6 por qué es nuevo, 7 validación, 8 impacto y modelo, 9 cierre.
    for builder in (slide_cover, slide_case, v5.slide_coverage, v3.slide_abc,
                    slide_route_proof, slide_new, slide_validation, slide_impact_model,
                    slide_close):
        builder(deck)
    deck.prs.save(pptx)
    assert len(v3.Presentation(str(pptx)).slides) == len(deck.notes) == 9
    if export:
        v3.export(pptx)
    return deck


if __name__ == "__main__":
    build(False, HERE / "pitch-v7-estatico.pptx", export=True)
    (HERE / "pitch-v7-estatico.pptx").unlink()
    (HERE / "pitch-v7-estatico.pdf").replace(HERE / "pitch-v7.pdf")
    deck = build(True, HERE / "pitch-v7.pptx", export=False)
    for note in deck.notes:
        print(f"{note['n']:>2}. {note['title']}: {note['clicks']} clic(s)")
    for warning in v3.FIT_WARNINGS:
        print(f"AVISO de ajuste · {warning}")
