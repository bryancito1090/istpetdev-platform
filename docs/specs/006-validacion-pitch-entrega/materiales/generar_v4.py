"""Genera pitch-v4.pptx, pitch-v4.pdf y render-v4/ con el guion v4 (17-oct).

Reutiliza el diseño, las animaciones y la medición de texto de generar_v3.py; cambia
el contenido para contar la cadena Detectar → Decidir → Entregar → Probar, el papel
del agente IA orquestado con n8n y el bloque Construido / Probado / Falta.
Guion: «Propuesta v4» en docs/boveda/07 Demo/Guion del pitch.md.

Las capturas del sistema se leen de assets/capturas-v4/ (ver CAPTURES). Si falta una,
la lámina muestra un marco «CAPTURA PENDIENTE» con lo que debe verse; al añadir el
PNG y volver a ejecutar, la captura sustituye al marco.

Uso:
    uv run --with python-pptx --with pillow --with numpy --with scipy \
        python docs/specs/006-validacion-pitch-entrega/materiales/generar_v4.py
"""
from __future__ import annotations

import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))

import generar_v3 as v3  # noqa: E402
from generar_v3 import (  # noqa: E402
    ACERO, AZUL, BLANCO, CREMA, DORADO, DORADO_CLARO, DORADO_OSCURO, EVENT_TAG, GRIS,
    GRIS_NIEBLA, H, LINEA, MARGIN, MSO_ANCHOR, MSO_SHAPE, NIEBLA, NOCHE, PP_ALIGN, TITLE_FONT,
    CHILLO_RIGHT, W, Image, Inches, Timeline, _plain, bubble, card, chillo_full, chillo_peek,
    chip, oval, peek_bubble, picture, record, rect, soft_shadow, text, width_in,
)

v3.PPTX_PATH = HERE / "pitch-v4.pptx"
v3.PDF_PATH = HERE / "pitch-v4.pdf"
v3.RENDER_DIR = HERE / "render-v4"

CAPTURES_DIR = v3.ASSETS / "capturas-v4"
N8N_LOGO = v3.ASSETS / "n8n-logo.png"
# Nombre de archivo esperado → (pantalla, qué debe verse). Ver guion-video-respaldo.md, C1–C7.
CAPTURES = {
    "alerta": ("c1-alerta.png", "C1 · /inicio con la notificación de alerta abierta",
               "Toast de alerta (nivel, punto, SKU, cobertura) y campana con el conteo"),
    "agente": ("c3-agente.png", "C3 · /rutas con «Propuesta del agente logístico»",
               "Estado Completado, modelo y la explicación en texto"),
}

# El número de cada lámina sale de su posición en la presentación, no del valor fijo de v3.
_chrome_v3 = v3.chrome


def _chrome(deck, slide, dark, kicker, title, source, number, **kw):
    return _chrome_v3(deck, slide, dark, kicker, title, source, len(deck.prs.slides), **kw)


v3.chrome = _chrome
chrome = _chrome


def capture_or_placeholder(deck, shapes, key, x, y, w, h):
    """Captura enmarcada que cabe en (w, h); si no existe el PNG, un marco con lo que falta."""
    filename, screen, content = CAPTURES[key]
    path = CAPTURES_DIR / filename
    grp = shapes.add_group_shape()
    if path.exists():
        img = Image.open(path)
        scale = min(w / img.width, h / img.height)
        pw, ph = img.width * scale, img.height * scale
        px, py = x + (w - pw) / 2, y + (h - ph) / 2
        border = rect(grp.shapes, px - 0.08, py - 0.08, pw + 0.16, ph + 0.16, BLANCO, LINEA)
        soft_shadow(border, blur=12, dist=4, alpha=18)
        picture(grp.shapes, path, px, py, w=pw)
        rect(grp.shapes, px - 0.08, py + ph + 0.08, pw + 0.16, 0.05, DORADO)
        return grp
    deck.pending.append(f"{filename} — {screen}")
    rect(grp.shapes, x, y, w, h, NIEBLA, DORADO, line_width=2)
    text(grp.shapes, x + 0.4, y + h / 2 - 0.9, w - 0.8, 0.45, "CAPTURA PENDIENTE", 22,
         DORADO_OSCURO, bold=True, font=TITLE_FONT, align=PP_ALIGN.CENTER, spacing=150)
    text(grp.shapes, x + 0.4, y + h / 2 - 0.3, w - 0.8, 0.45, screen, 16, AZUL, bold=True,
         align=PP_ALIGN.CENTER)
    text(grp.shapes, x + 0.4, y + h / 2 + 0.2, w - 0.8, 0.8, content, 14, GRIS,
         align=PP_ALIGN.CENTER)
    return grp


# ---------------------------------------------------------------------------
# Láminas
# ---------------------------------------------------------------------------

def slide_cover(deck):
    s = deck.slide(dark=True)
    sh = s.shapes
    tl = Timeline()
    logo = picture(sh, deck.assets["logo_white"], MARGIN, 0.62, h=1.2)
    tl.add(logo, "fade", dur=500)
    head = sh.add_group_shape()
    rect(head.shapes, MARGIN, 2.32, 0.05, 0.3, DORADO)
    text(head.shapes, MARGIN + 0.17, 2.33, 7.4, 0.3, "ABASTECIMIENTO, RUTAS Y CUSTODIA",
         14, DORADO, bold=True, spacing=200)
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
    text(foot.shapes, MARGIN, 6.82, 9.0, 0.3, EVENT_TAG, 12, GRIS_NIEBLA, spacing=150)
    tl.add(foot, "fade", mode="with", dur=450)
    gallo = chillo_full(deck, s, 9.95, 7.32, 5.05)
    tl.add(gallo, "zoom", dur=550)
    msg = bubble(s, 8.55, 0.72, 4.5, 1.0,
                 [("¡Hola! Soy Chillo y hoy les cuento cómo ", False),
                  ("llegar antes", True), (" que la llamada.", False)],
                 tail_x=11.2, tail_y=2.35, size=15.5)
    tl.add(msg, "fade", mode="click", dur=400)
    record(deck, s, tl, "Portada · gancho",
           "Auto: logo → pregunta → «Cuando el hospital llama» → equipo → Chillo (zoom). "
           "Clic 1: globo de Chillo.",
           chillo="Chillo entra solo (zoom). Globo: clic 1.")


def slide_pain(deck):
    s = deck.slide(dark=False)
    sh = s.shapes
    tl = Timeline()
    right = CHILLO_RIGHT
    head = chrome(deck, s, False, "Problema y usuario",
                  "El faltante se descubre cuando el cliente llama",
                  "Fuente: charla del sector, 16-oct (síntesis en Historia y propuesta de valor); "
                  "hipótesis A-02. No es una cifra medida.", 0, source_right=right)
    tl.add(head, "fade", dur=400)
    gap = 0.25
    cw = (right - MARGIN - 2 * gap) / 3
    pains = [
        ("Planificador", "Repone por calendario o por urgencia, sin saber qué punto se quedará "
                         "sin producto."),
        ("Bodega", "Pierde de vista el insumo en cuanto sale del almacén."),
        ("Cliente", "Nadie puede probar qué se entregó, cuándo ni a quién."),
    ]
    for i, (who, body) in enumerate(pains):
        grp = card(sh, MARGIN + i * (cw + gap), 1.8, cw, 3.0, who, body,
                   tone="navy" if i == 0 else "light", heading_size=24, body_size=16)
        tl.add(grp, "wipe", mode="click" if i == 0 else "after", dur=450)
    gallo = chillo_peek(deck, s)
    tl.add(gallo, "zoom", mode="with", dur=500)
    band = sh.add_group_shape()
    rect(band.shapes, MARGIN, 5.05, right - MARGIN, 1.3, CREMA)
    rect(band.shapes, MARGIN, 5.05, 0.07, 1.3, DORADO)
    text(band.shapes, MARGIN + 0.35, 5.05, right - MARGIN - 0.6, 1.3,
         [[("Y cuando hay que apurar una entrega, ", {}),
           ("se decide mirando kilómetros, no el costo real de servir:", {"color": DORADO_OSCURO}),
           (" personal, combustible, descarga y producto que se daña.", {})]],
         19, AZUL, bold=True, valign=MSO_ANCHOR.MIDDLE, label="banda costo")
    tl.add(band, "fade", mode="click", dur=450)
    msg = peek_bubble(s, [("Quien paga la demora es el ", False), ("hospital", True), (".", False)])
    tl.add(msg, "fade", mode="click", dur=400)
    record(deck, s, tl, "Problema y usuario",
           "Auto: título. Clic 1: tres dolores en cascada → Chillo (zoom). "
           "Clic 2: banda del costo real. Clic 3: globo de Chillo.",
           chillo="Chillo entra con los dolores (zoom). Globo: clic 3.")


def slide_chain(deck):
    s = deck.slide(dark=True)
    sh = s.shapes
    tl = Timeline()
    head = chrome(deck, s, True, "Solución", "Una sola cadena, del aviso a la prueba de entrega",
                  "Fuente: matriz A-04 (cobertura y riesgo), P-02 (propia frente a contratada), "
                  "A-07 (rutas), A-13/A-15 (custodia y captura sin señal).", 0)
    tl.add(head, "fade", dur=400)
    steps = [
        ("01", "Detectar", "Revisa el inventario y avisa qué punto se quedará sin producto"),
        ("02", "Decidir", "Compara flota propia y contratada con todo lo que cuesta servir"),
        ("03", "Entregar", "Arma la ruta con capacidad, horarios y jornada"),
        ("04", "Probar", "Cada caja viaja con QR hasta que el cliente la recibe"),
    ]
    gap = 0.28
    cw = (W - 2 * MARGIN - 3 * gap) / 4
    d = 0.9
    line = rect(sh, MARGIN + cw / 2, 1.98 + d / 2, W - 2 * MARGIN - cw, 0.02, DORADO)
    tl.add(line, "wipe", dur=500)
    for i, (num, head_txt, body) in enumerate(steps):
        x = MARGIN + i * (cw + gap)
        grp = sh.add_group_shape()
        oval(grp.shapes, x + (cw - d) / 2, 1.98, d, NOCHE, DORADO, 1.5)
        text(grp.shapes, x + (cw - d) / 2, 1.98, d, d, num, 24, DORADO, bold=True,
             font=TITLE_FONT, align=PP_ALIGN.CENTER, valign=MSO_ANCHOR.MIDDLE)
        rect(grp.shapes, x, 3.12, cw, 1.85, ACERO)
        rect(grp.shapes, x, 3.12, cw, 0.05, DORADO)
        text(grp.shapes, x + 0.2, 3.3, cw - 0.4, 0.55, head_txt, 26, BLANCO, bold=True,
             font=TITLE_FONT, align=PP_ALIGN.CENTER)
        text(grp.shapes, x + 0.2, 3.9, cw - 0.4, 1.0, body, 15.5, GRIS_NIEBLA,
             align=PP_ALIGN.CENTER, line_spacing=1.0, label=f"paso {head_txt}")
        tl.add(grp, "fade", mode="click", dur=450)
    claim = sh.add_group_shape()
    claim_runs = [("El motor calcula, la IA explica, ", {}), ("la persona decide.", {"color": DORADO})]
    tw = sum(width_in(c, 30, TITLE_FONT, True) for c, _ in claim_runs)
    text(claim.shapes, (W - tw - 0.4) / 2, 5.45, tw + 0.4, 0.8, [claim_runs], 30, BLANCO,
         bold=True, font=TITLE_FONT, align=PP_ALIGN.CENTER, valign=MSO_ANCHOR.MIDDLE)
    rect(claim.shapes, (W - tw) / 2 - 1.55, 5.85, 1.2, 0.02, DORADO)
    rect(claim.shapes, (W + tw) / 2 + 0.35, 5.85, 1.2, 0.02, DORADO)
    tl.add(claim, "fade", dur=450)
    record(deck, s, tl, "Solución · cadena",
           "Auto: título → línea dorada. Clic 1: Detectar. Clic 2: Decidir. Clic 3: Entregar. "
           "Clic 4: Probar → (auto) «El motor calcula, la IA explica, la persona decide».")


def slide_inside(deck):
    s = deck.slide(dark=False)
    sh = s.shapes
    tl = Timeline()
    head = chrome(deck, s, False, "Innovación", "No es otro inventario con alertas",
                  "Fuente: matriz A-08 (determinismo), A-10 (aprobación humana), A-11 (corrida "
                  "registrada del agente) y A-12 (respaldo sin IA, run n8n 292).", 0)
    tl.add(head, "fade", dur=400)
    lead = text(sh, MARGIN, 1.66, W - 2 * MARGIN, 0.45,
                "La alerta termina en una decisión con costo real y en una entrega comprobable.",
                19, GRIS, label="bajada")
    tl.add(lead, "fade", mode="with", dur=400)
    gap = 0.42
    bw = (W - 2 * MARGIN - 3 * gap) / 4
    top, bh = 2.35, 2.55
    boxes = [
        ("Datos", "Inventario, pedidos, flota y costos del escenario", "light"),
        ("Motor", "Calcula riesgo, rutas y costo. Mismos datos, mismo resultado", "navy"),
        ("IA", "DeepSeek vía n8n: lo explica en lenguaje simple, sin cambiar cifras",
         "light"),
        ("Persona", "El planificador aprueba o rechaza", "navy"),
    ]
    for i, (title, body, tone) in enumerate(boxes):
        x = MARGIN + i * (bw + gap)
        grp = card(sh, x, top, bw, bh, title, body, tone=tone, heading_size=24, body_size=15.5)
        if title == "IA" and N8N_LOGO.exists():
            picture(grp.shapes, N8N_LOGO, x + bw - 1.0, top + 0.12, w=0.85)
        if i:
            arrow = sh.add_shape(MSO_SHAPE.ISOSCELES_TRIANGLE, Inches(x - gap / 2 - 0.1),
                                 Inches(top + bh / 2 - 0.11), Inches(0.24), Inches(0.22))
            arrow.rotation = 90
            _plain(arrow, DORADO, None, 0)
            tl.add(arrow, "fade", mode="click", dur=250)
            tl.add(grp, "wipe", mode="with", dur=450)
        else:
            tl.add(grp, "wipe", dur=450)
    band = sh.add_group_shape()
    rect(band.shapes, MARGIN, 5.25, W - 2 * MARGIN, 1.1, AZUL)
    rect(band.shapes, MARGIN, 5.25, 0.07, 1.1, DORADO)
    cw = chip(band.shapes, MARGIN + 0.35, 5.25 + 0.38, "SI LA IA FALLA")
    text(band.shapes, MARGIN + 0.35 + cw + 0.3, 5.25, W - 2 * MARGIN - cw - 0.9, 1.1,
         "el sistema responde con una explicación de respaldo y la decisión sigue siendo humana. "
         "n8n separa al agente del motor: se cambia de modelo sin tocar el cálculo.",
         16, BLANCO, bold=True, valign=MSO_ANCHOR.MIDDLE, label="banda respaldo")
    tl.add(band, "fade", mode="click", dur=450)
    record(deck, s, tl, "Innovación · cómo funciona por dentro",
           "Auto: título y bajada → Datos. Clics 1–3: flecha + Motor, Agente IA (logo n8n), "
           "Persona. Clic 4: banda «Si la IA falla».")


def slide_alert(deck):
    s = deck.slide(dark=False)
    sh = s.shapes
    tl = Timeline()
    head = chrome(deck, s, False, "Prototipo / demo · detectar",
                  "El aviso llega antes que la llamada",
                  "Fuente: monitoreo y notificaciones en vivo (#318, 17-oct), cálculo de cobertura "
                  "A-04. Escenario sintético; sin ensayo registrado en teléfono.", 0)
    tl.add(head, "fade", dur=400)
    cap_w = 7.9
    frame = capture_or_placeholder(deck, sh, "alerta", MARGIN, 1.85, cap_w, 4.5)
    tl.add(frame, "wipe", dur=600)
    x2 = MARGIN + cap_w + 0.4
    info = card(sh, x2, 1.85, W - MARGIN - x2, 3.15, "Qué muestra la alerta",
                "Punto en riesgo, producto, días de cobertura y la cantidad sugerida para reponer.",
                heading_size=22, body_size=16)
    tl.add(info, "fade", dur=450)
    take = sh.add_group_shape()
    rect(take.shapes, x2, 5.2, W - MARGIN - x2, 1.15, AZUL)
    rect(take.shapes, x2, 5.2, 0.07, 1.15, DORADO)
    text(take.shapes, x2 + 0.3, 5.2, W - MARGIN - x2 - 0.45, 1.15,
         "El planificador sabe qué punto atender primero.", 16, BLANCO, bold=True,
         valign=MSO_ANCHOR.MIDDLE, label="mensaje alerta")
    tl.add(take, "fade", mode="click", dur=450)
    record(deck, s, tl, "Demo · alerta en vivo",
           "Auto: título → captura de la alerta (barrido) → tarjeta. Clic 1: mensaje final.")


def slide_validation(deck):
    s = deck.slide(dark=True)
    sh = s.shapes
    tl = Timeline()
    head = chrome(deck, s, True, "Validación", "Lo que hay, sin humo",
                  "Fuente: matriz de afirmaciones P-02, P-04, P-05, A-03, A-11, A-12, A-13, A-15, "
                  "A-21 y #198. Escenario sintético de Quito: 40 puntos, no son clientes.", 0)
    tl.add(head, "fade", dur=400)
    cols = [
        ("Construido", "Inventario con alertas\nMotor de rutas y costo de servir\n"
                       "Agente IA con n8n\nCustodia con QR y captura sin señal"),
        ("Probado", "Motor y comparación A/B/C en un ensayo técnico con escenario sintético "
                    "de Quito\nCorrida registrada del agente, incluida su caída al respaldo"),
        ("Falta", "Ensayar el recorrido en teléfono sin señal\nLlevar la comparación A/B/C a "
                  "producción\nMedir con datos de una empresa real"),
    ]
    gap = 0.3
    cw = (W - 2 * MARGIN - 2 * gap) / 3
    for i, (title, body) in enumerate(cols):
        x = MARGIN + i * (cw + gap)
        grp = sh.add_group_shape()
        rect(grp.shapes, x, 1.85, cw, 3.7, ACERO if i < 2 else NOCHE, None if i < 2 else DORADO,
             line_width=1.5)
        rect(grp.shapes, x, 1.85, cw, 0.06, DORADO)
        text(grp.shapes, x + 0.3, 2.1, cw - 0.6, 0.6, title.upper(), 24,
             DORADO if i < 2 else DORADO_CLARO, bold=True, font=TITLE_FONT, spacing=120)
        text(grp.shapes, x + 0.3, 2.8, cw - 0.6, 2.65,
             [[("•  " + line, {})] for line in body.split("\n")], 15.5, BLANCO,
             line_spacing=1.05, space_after=8, label=f"columna {title}")
        tl.add(grp, "fade", mode="click" if i else "after", dur=450)
    quote = text(sh, MARGIN, 5.8, W - 2 * MARGIN, 0.6,
                 "Preferimos mostrar lo que funciona de verdad que prometer lo que no hemos medido.",
                 19, DORADO_CLARO, bold=True, italic=True, align=PP_ALIGN.CENTER, label="cita")
    tl.add(quote, "fade", mode="click", dur=450)
    record(deck, s, tl, "Validación · construido, probado, falta",
           "Auto: título → Construido. Clic 1: Probado. Clic 2: Falta. Clic 3: frase final.")


def slide_model_risks(deck):
    s = deck.slide(dark=False)
    sh = s.shapes
    tl = Timeline()
    right = CHILLO_RIGHT
    head = chrome(deck, s, False, "Viabilidad, modelo y riesgos",
                  "Viable con lo que la empresa ya tiene",
                  "Fuente: matriz A-22 (importador), P-16 (riesgos); Modelo de negocio y piloto. "
                  "Precio y cliente por validar.", 0, source_right=right)
    tl.add(head, "fade", dur=400)
    gallo = chillo_peek(deck, s)
    tl.add(gallo, "zoom", dur=500)
    lw = 3.3
    model = card(sh, MARGIN, 1.75, lw, 4.6, "Modelo",
                 ["Sin hardware nuevo: web y el teléfono del conductor.",
                  "Implementación inicial más suscripción. Precio por validar en el piloto."],
                 tone="navy", heading_size=26, body_size=16)
    tl.add(model, "fade", dur=450)
    rows = [
        ("Datos incompletos", "Mostrar «sin dato»; nunca inventar"),
        ("Ruta poco realista", "Revisión del planificador antes de publicar"),
        ("Costo mal estimado", "Desglose visible y calibración con viajes reales"),
        ("Error de la IA", "Solo explica cifras del motor; hay respaldo"),
    ]
    rx = MARGIN + lw + 0.3
    rw = right - rx
    y, rh = 1.75, 1.06
    for risk, mitigation in rows:
        grp = sh.add_group_shape()
        rect(grp.shapes, rx, y, rw, rh, NIEBLA)
        rect(grp.shapes, rx, y, 0.06, rh, DORADO)
        text(grp.shapes, rx + 0.25, y + 0.12, rw - 0.4, 0.4, risk, 17, AZUL, bold=True,
             font=TITLE_FONT)
        text(grp.shapes, rx + 0.25, y + 0.55, rw - 0.4, 0.42, mitigation, 14, GRIS,
             label=f"riesgo {risk}")
        tl.add(grp, "wipe", dur=300)
        y += rh + 0.12
    msg = peek_bubble(s, [("Todo plan tiene riesgos; lo clave es ", False),
                          ("mitigarlos", True), (".", False)])
    tl.add(msg, "fade", mode="click", dur=400)
    record(deck, s, tl, "Viabilidad, modelo y riesgos",
           "Auto: título → Chillo (zoom) → Modelo → cuatro riesgos en cascada. "
           "Clic 1: globo de Chillo.",
           chillo="Chillo entra solo (zoom). Globo: clic 1.")


def slide_next(deck):
    s = deck.slide(dark=True)
    sh = s.shapes
    tl = Timeline()
    right = CHILLO_RIGHT
    head = chrome(deck, s, True, "Próximo paso", "Un piloto con una empresa de limpieza",
                  "Fuente: matriz P-02/P-05/P-14, A-15 y A-21. Propuestas sujetas a validación.",
                  0, source_right=right)
    tl.add(head, "fade", dur=400)
    gallo = chillo_full(deck, s, 10.42, 6.72, 4.3)
    tl.add(gallo, "zoom", dur=550)
    steps = [
        ("Línea base", "Acordar indicadores y medir el punto de partida"),
        ("Producción", "Llevar la comparación propia / contratada a operación"),
        ("Campo", "Ensayar la entrega sin señal con conductores reales"),
        ("Calibrar", "Pronóstico y costos con datos conciliados"),
    ]
    gap = 0.28
    cw = (right - MARGIN - gap) / 2
    for i, (head_txt, body) in enumerate(steps):
        x = MARGIN + (i % 2) * (cw + gap)
        y = 1.78 + (i // 2) * 1.76
        grp = card(sh, x, y, cw, 1.64, head_txt, body, tone="dark", heading_size=22,
                   body_size=15.5, number=f"{i + 1:02d}")
        tl.add(grp, "fade", dur=350)
    close = sh.add_group_shape()
    rect(close.shapes, MARGIN, 5.35, right - MARGIN, 1.15, DORADO)
    text(close.shapes, MARGIN + 0.35, 5.35, right - MARGIN - 0.7, 1.15,
         [[("Detectar antes, decidir con el costo real ", {"font": TITLE_FONT, "size": 22}),
           ("y probar cada entrega. Buscamos ese aliado aquí.", {})]],
         18, NOCHE, bold=True, valign=MSO_ANCHOR.MIDDLE, label="cierre")
    tl.add(close, "wipe", mode="click", dur=550)
    msg = bubble(s, CHILLO_RIGHT + 0.18, 1.3, 3.5, 1.0,
                 [("¿Lo validamos juntos en un ", False), ("piloto", True),
                  ("? ¡Kikirikí!", False)],
                 tail_x=11.55, tail_y=2.48, size=14)
    tl.add(msg, "fade", mode="click", dur=400)
    record(deck, s, tl, "Próximo paso y cierre",
           "Auto: título → Chillo (zoom) → cuatro pasos en cascada. "
           "Clic 1: banda dorada de cierre. Clic 2: globo de Chillo.",
           chillo="Chillo entra solo (zoom). Globo: clic 2.")


def build_deck():
    deck = v3.Deck(v3.prepare_assets())
    deck.pending = []
    deck.prs.core_properties.title = "IstpetDev · Pitch Expo Clean · v4"
    deck.prs.core_properties.subject = "Detectar, decidir, entregar y probar; guion v4"
    # Orden del guion v4: 1 gancho, 2 dolor, 3 cadena, 4 innovación, 5 alerta, 6 A/B/C,
    # 7 validación, 8 indicadores, 9 modelo y riesgos, 10 próximo paso.
    for builder in (slide_cover, slide_pain, slide_chain, slide_inside, slide_alert,
                    v3.slide_abc, slide_validation, v3.slide_impact, slide_model_risks,
                    slide_next):
        builder(deck)
    deck.prs.save(v3.PPTX_PATH)
    reopened = v3.Presentation(str(v3.PPTX_PATH))
    assert len(reopened.slides) == len(deck.notes)
    v3.export(v3.PPTX_PATH)
    print(f"PPTX: {v3.PPTX_PATH}")
    print(f"PDF:  {v3.PDF_PATH}")
    print(f"PNG:  {v3.RENDER_DIR}")
    for note in deck.notes:
        print(f"{note['n']:>2}. {note['title']}: {note['clicks']} clic(s) · {note['detail']}")
    for item in deck.pending:
        print(f"CAPTURA PENDIENTE · {CAPTURES_DIR.name}/{item}")
    for warning in v3.FIT_WARNINGS:
        print(f"AVISO de ajuste · {warning}")


if __name__ == "__main__":
    build_deck()
