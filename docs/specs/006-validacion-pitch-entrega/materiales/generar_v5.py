"""Pitch v5: amplía la demo de v4 a tres capturas, sin ampliar sus 25 segundos.

Uso: uv run --with python-pptx --with pillow --with numpy --with scipy python generar_v5.py
Las capturas existentes son ilustrativas: no acreditan una alerta activa ni recepción.
"""
import generar_v4 as v4

v3 = v4.v3
v3.PPTX_PATH = v4.HERE / 'pitch-v5.pptx'
v3.PDF_PATH = v4.HERE / 'pitch-v5.pdf'
v3.RENDER_DIR = v4.HERE / 'render-v5'
v4.CAPTURES_DIR = v4.HERE / 'entregable-2' / 'img'
v4.CAPTURES.update({
    'consola': ('captura-consola.png', 'Consola de auditoría', 'Alertas y rutas'),
    'mapa': ('captura-rutas.png', 'Mapa de rutas', 'Paradas numeradas'),
    'qr': ('captura-qr.png', 'Escáner QR', 'Lectura o ingreso manual'),
})


def slide_capture(deck, key, stage, title, heading, description, takeaway, source):
    s = deck.slide(dark=False)
    sh = s.shapes
    tl = v4.Timeline()
    head = v4.chrome(deck, s, False, stage, title, source, 0)
    tl.add(head, 'fade', dur=300)
    frame = v4.capture_or_placeholder(deck, sh, key, v4.MARGIN, 1.85, 7.9, 4.5)
    tl.add(frame, 'wipe', dur=400)
    x = v4.MARGIN + 8.3
    width = v4.W - v4.MARGIN - x
    info = v4.card(sh, x, 1.85, width, 3.15, heading, description,
                   heading_size=22, body_size=16)
    tl.add(info, 'fade', mode='with', dur=300)
    band = sh.add_group_shape()
    v4.rect(band.shapes, x, 5.2, width, 1.15, v4.AZUL)
    v4.rect(band.shapes, x, 5.2, 0.07, 1.15, v4.DORADO)
    v4.text(band.shapes, x + 0.3, 5.2, width - 0.45, 1.15, takeaway,
            16, v4.BLANCO, bold=True, valign=v4.MSO_ANCHOR.MIDDLE)
    tl.add(band, 'fade', mode='with', dur=300)
    v4.record(deck, s, tl, title, 'Entrada automática. Sin clics internos; avanzar con la frase del guion.')


def console(deck):
    slide_capture(deck, 'consola', 'Prototipo / demo · 1 de 3',
                  'Alertas y rutas, en una misma vista', 'Supervisar la operación',
                  'La consola reúne el estado del stock, las rutas y el acceso a la custodia digital.',
                  'Una vista para revisar qué requiere atención.',
                  'Captura existente del prototipo · rol Auditor · muestra 0 alertas activas. No es una alerta disparada.')


def routes(deck):
    slide_capture(deck, 'mapa', 'Prototipo / demo · 2 de 3',
                  'La planificación se ve en el mapa', 'Entender el recorrido',
                  'Rutas por color, paradas numeradas y detalle del itinerario para revisar la distribución.',
                  'Del plan al recorrido visible.',
                  'Captura existente · escenario sintético de Ambato. Es distinto del ensayo A/B/C de Quito; sin tráfico en vivo.')


def qr(deck):
    slide_capture(deck, 'qr', 'Prototipo / demo · 3 de 3',
                  'El bulto se identifica con un QR', 'Conectar con la custodia',
                  'El escáner permite leer el QR del bulto o ingresar su identificador manualmente.',
                  'La identificación es el inicio de la trazabilidad.',
                  'Captura existente del escáner. No muestra una recepción completada ni acredita un ensayo sin señal.')


def build_deck():
    deck = v3.Deck(v3.prepare_assets())
    deck.pending = []
    deck.prs.core_properties.title = 'IstpetDev · Pitch Expo Clean · v5'
    deck.prs.core_properties.subject = 'Guion v5 · demo visual en tres capturas'
    for builder in (v4.slide_cover, v4.slide_pain, v4.slide_chain, v4.slide_inside,
                    console, routes, qr, v3.slide_abc, v4.slide_validation,
                    v3.slide_impact, v4.slide_model_risks, v4.slide_next):
        builder(deck)
    deck.prs.save(v3.PPTX_PATH)
    assert len(v3.Presentation(str(v3.PPTX_PATH)).slides) == 12
    assert not deck.pending, deck.pending
    v3.export(v3.PPTX_PATH)
    for note in deck.notes:
        print(f"{note['n']}. {note['title']}: {note['clicks']} clic(s)")
    for warning in v3.FIT_WARNINGS:
        print(f'AVISO de ajuste · {warning}')


if __name__ == '__main__':
    build_deck()
