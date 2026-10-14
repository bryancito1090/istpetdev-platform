"""Genera la presentación provisional de siete bloques del pitch (spec 006).

Solo usa frases autorizadas por matriz-afirmaciones.md (usables con su límite
o frase de sustitución) y la «Frase vigente» de escena-y-contingencias.md.
No es formato oficial: O-04/O-06 siguen pendientes.

Uso:  python generar_provisional.py  (requiere python-pptx 1.0.2)
      soffice --headless --convert-to pdf pitch-provisional-v0.pptx
"""

from pptx import Presentation
from pptx.dml.color import RGBColor
from pptx.util import Inches, Pt

VERSION = "v0 · 14-oct-2026"
AVISO = (f"PROVISIONAL {VERSION} · IstpetDev · no es formato oficial "
         "(O-04/O-06 pendientes) · no entregado")

TINTA = RGBColor(0x1F, 0x29, 0x37)
GRIS = RGBColor(0x6B, 0x72, 0x80)
ROTULO = RGBColor(0xB4, 0x53, 0x09)

DIAPOSITIVAS = [
    {
        "bloque": "1 · Problema",
        "titulo": "Reponer antes de la llamada",
        "rotulo": "Caso sintético",
        "lineas": [
            "En contratos públicos de limpieza hospitalaria, el proveedor está "
            "obligado a mantener los químicos de desinfección y los insumos "
            "fungibles con frecuencias de reposición definidas por el hospital.",
            "Nuestra hipótesis es que hoy la reposición empieza cuando el "
            "hospital ya avisó la falta.",
            "Caso sintético: una caja de desinfectante que va a un hospital.",
        ],
        "fuente": "Fuente: fichas técnicas SERCOP (bóveda, 99 Fuentes). Matriz A-01, A-02.",
    },
    {
        "bloque": "2 · Solución",
        "titulo": "Calcula, propone, una persona decide",
        "rotulo": "",
        "lineas": [
            "Calcula qué punto se va a quedar sin producto y por qué.",
            "Cada alerta muestra los datos que la explican: stock, consumo y "
            "días de cobertura.",
            "Si un cambio supera los límites de costo o retraso, no se ejecuta: "
            "espera la aprobación de una persona.",
            "El cálculo es una regla verificable; la IA no aprueba nada.",
        ],
        "fuente": "Matriz A-06, A-10 (sustitución), A-11 (sustitución).",
    },
    {
        "bloque": "3 · Prototipo / demo",
        "titulo": "Una caja hacia el hospital norte",
        "rotulo": "Resultados de pruebas automatizadas · no es demo en vivo",
        "lineas": [
            "Escenario sintético: una bodega, 15 puntos y dos vehículos.",
            "En nuestras pruebas, el hospital norte tiene cinco días de cobertura "
            "de desinfectante: riesgo alto.",
            "La propuesta asigna una ruta a cada vehículo respetando capacidad, "
            "horarios de atención y jornada.",
            "Ante un cierre de vía se conservan las paradas hechas y se crea una "
            "nueva versión de la ruta.",
            "Diseño: la caja queda ligada a su lote y a la entrega.",
            "La consulta pública devuelve producto, estado e hitos sin datos "
            "privados; un token alterado recibe 404 en la prueba de API.",
        ],
        "fuente": "Matriz A-03 (sust.), A-04, A-07, A-09, A-13 (propuesta), A-16 (sust.).",
    },
    {
        "bloque": "4 · Impacto",
        "titulo": "Cómo mediremos el impacto",
        "rotulo": "Simulación · resultado pendiente",
        "lineas": [
            "Diseñamos la comparación antes de ver resultados: 90 días "
            "sintéticos, tres semillas y un escenario adverso aparte.",
            "Contra reposición reactiva y calendario fijo.",
            "Contamos los quiebres evitados y también los nuevos, sin restar "
            "unos de otros.",
            "El resultado solo lo mostraremos con su archivo verificable.",
        ],
        "fuente": "Matriz A-19 (sustitución), A-20. A-21 retirada: sin corrida oficial de 005.",
    },
    {
        "bloque": "5 · Viabilidad",
        "titulo": "Qué ya funciona y qué no",
        "rotulo": "Prueba local",
        "lineas": [
            "Cargamos archivos de otro formato escribiendo solo un perfil, "
            "sin programar.",
            "En prueba local, la ruta de 80 puntos se calcula en menos de "
            "cinco segundos.",
            "Límite: es una heurística; no prometemos la ruta óptima.",
        ],
        "fuente": "Matriz A-22, A-24, A-33.",
    },
    {
        "bloque": "6 · Modelo de negocio",
        "titulo": "Quién paga y por qué",
        "rotulo": "Hipótesis",
        "lineas": [
            "Nuestro cliente por validar es la empresa de limpieza.",
            "Cobraríamos implementación y una suscripción.",
            "El precio lo definiremos con costos reales del piloto.",
            "El costo de operación lo mediremos en el piloto.",
        ],
        "fuente": "Matriz A-31 (propuesta), A-30 y A-26 (sustitución).",
    },
    {
        "bloque": "7 · Próximo paso",
        "titulo": "Piloto de cuatro semanas",
        "rotulo": "Propuesta",
        "lineas": [
            "Semana 1: acordar bodega, puntos y productos; medir la operación "
            "actual sin cambiarla.",
            "Semana 2: recomendaciones en paralelo, con aprobación humana.",
            "Semana 3: operar un grupo pequeño con caja y QR.",
            "Semana 4: comparar contra la operación actual y decidir si seguir.",
            "Buscamos una empresa que quiera medirlo con nosotros.",
        ],
        "fuente": "Matriz A-32; ficha de piloto del plan 006.",
    },
]


def texto(slide, x, y, w, h, contenido, tam, color=TINTA, negrita=False):
    caja = slide.shapes.add_textbox(Inches(x), Inches(y), Inches(w), Inches(h))
    marco = caja.text_frame
    marco.word_wrap = True
    parrafos = contenido if isinstance(contenido, list) else [contenido]
    for i, linea in enumerate(parrafos):
        p = marco.paragraphs[0] if i == 0 else marco.add_paragraph()
        p.text = linea
        p.font.size = Pt(tam)
        p.font.color.rgb = color
        p.font.bold = negrita
        p.space_after = Pt(8)
    return caja


def main():
    prs = Presentation()
    prs.slide_width, prs.slide_height = Inches(13.333), Inches(7.5)
    vacia = prs.slide_layouts[6]
    for d in DIAPOSITIVAS:
        s = prs.slides.add_slide(vacia)
        texto(s, 0.6, 0.35, 8, 0.4, d["bloque"], 14, GRIS, True)
        if d["rotulo"]:
            texto(s, 7.4, 0.35, 5.3, 0.4, d["rotulo"], 14, ROTULO, True)
        texto(s, 0.6, 0.85, 12, 0.9, d["titulo"], 34, TINTA, True)
        texto(s, 0.6, 1.95, 12.1, 4.4, ["• " + l for l in d["lineas"]], 20)
        texto(s, 0.6, 6.35, 12.1, 0.4, d["fuente"], 11, GRIS)
        texto(s, 0.6, 6.8, 12.1, 0.4, AVISO, 11, ROTULO, True)
    prs.save("pitch-provisional-v0.pptx")


if __name__ == "__main__":
    main()
