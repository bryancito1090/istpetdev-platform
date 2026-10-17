"""Entregable 3 — Prototipo / demo (Manual §9 y Anexo 5.3). Uso: python3 e3_prototipo.py"""
from lib import *

doc = nuevo(3, "Prototipo / demo", "Prototipo", "funcionando", "Manual §9 y Anexo 5.3")

h1(doc, "1", "¿Qué construimos?")
p = para(doc, "", after=8)
rich(p, [("IstpetDev", True), (" es un prototipo desplegado en la nube con dos superficies: una ", False),
         ("Torre de Control web", True), (" para planificación, gerencia y auditoría, y una ", False),
         ("aplicación de campo (PWA)", True), (" para bodega, conductor y receptor, que funciona sin conexión. "
          "Un motor determinista calcula riesgo, rutas y costos; un agente de IA orquestado con n8n explica "
          "los resultados; una persona aprueba antes de publicar.", False)], 10.5)
tabla(doc, ("Dónde", "Dirección", "Quién lo usa"), [
    ("Torre de Control (web)", "https://istpetdev.doicela.dev", "Planificador, Gerencia, Auditor"),
    ("Aplicación de campo (PWA)", "https://istpetdev.doicela.dev/campo/", "Bodega, Conductor, Receptor"),
    ("Orquestador del agente", "n8n, servidor propio del equipo", "Agente de IA (DeepSeek)"),
], (Cm(4.6), Cm(6.6), Cm(5.4)))
para(doc, "Versión desplegada y verificada el sábado 17-oct-2026, 08:28 (hora de Ecuador): commit a86d2b8, publicación "
          "del PR #314 a la rama principal. Infraestructura: servidor AWS EC2, base PostgreSQL 16 en Amazon RDS y Redis 7.4.",
     9.5, False, GREY, after=8)

h1(doc, "2", "¿Qué parte es funcional?")
para(doc, "Separamos con honestidad lo que está construido, lo que se probó y lo que falta:", after=6)
tabla(doc, ("Componente", "Estado", "Evidencia y límite"), [
    ("Importación de datos y validación", "Construido", "Catálogo, puntos, bodegas, stock y consumo desde Excel/CSV con validación por fila."),
    ("Riesgo de faltante", "Construido", "Cobertura frente al tiempo de reposición; clasifica Crítico, Alto o Vigilar y explica el cálculo."),
    ("Rutas con restricciones", "Construido; ensayo técnico", "Capacidad, ventanas, jornada y calles reales. No se garantiza un óptimo global."),
    ("Comparación A/B/C de costo", "Ensayo técnico", "Flota propia, propia con críticos primero y operador contratado, sobre el escenario sintético de Quito (ver sección 4)."),
    ("Agente de IA con n8n", "Corrida registrada", "Una ejecución registrada completó riesgos, abastecimiento y propuesta de rutas con explicaciones, incluida su caída al respaldo."),
    ("Aprobación humana", "Construido", "El agente no aprueba ni publica; la política vive en el backend."),
    ("Custodia con QR", "Construido", "Despacho, entrega y recepción con cantidad, lote, responsable, foto y firma."),
    ("Trabajo sin señal", "Construido; falta prueba en teléfono", "Cola local y sincronización idempotente. Falta ensayar el recorrido completo en un teléfono sin señal."),
    ("Simulador de 90 días", "Probado (sintético)", "12 corridas reproducibles por semilla (ver Entregable 4)."),
], (Cm(4.6), Cm(3.6), Cm(8.4)), tam=9)

h1(doc, "3", "El sistema en funcionamiento")
para(doc, "Capturas del prototipo desplegado en https://istpetdev.doicela.dev.", 9.5, False, GREY, after=6)
figura(doc, IMG / "captura-rutas.png", ANCHO,
       "Figura 1. Planificación de rutas en la escena de demostración (datos sintéticos): paradas numeradas sobre calles reales, ventanas horarias y hora estimada de llegada.")
grid = doc.add_table(rows=1, cols=2)
borders(grid, "FFFFFF", "0")
a, b = grid.cell(0, 0), grid.cell(0, 1)
a.paragraphs[0].add_run().add_picture(str(IMG / "captura-consola.png"), width=Cm(11.2))
a.paragraphs[0].alignment = WD_ALIGN_PARAGRAPH.CENTER
para(a, "Figura 2. Consola de control con datos sintéticos: riesgo de stock, rutas y custodia digital.", 8.5, False, GREY, WD_ALIGN_PARAGRAPH.CENTER, after=4)
pic = b.paragraphs[0].add_run().add_picture(str(IMG / "captura-qr.png"), height=Cm(6.6))
src = OxmlElement("a:srcRect"); src.set("r", "18000")
pic._inline.graphic.graphicData.pic.blipFill.insert(1, src)
pic.width = int(pic.width * 0.82)
b.paragraphs[0].alignment = WD_ALIGN_PARAGRAPH.CENTER
para(b, "Figura 3. Aplicación de campo: escaneo del QR de cada bulto.", 8.5, False, GREY, WD_ALIGN_PARAGRAPH.CENTER, after=4)
fix_widths(grid, (Cm(11.8), Cm(4.8)))
para(doc, "", after=4)

salto(doc)
h1(doc, "4", "Evidencia que presentamos")
h2(doc, "Comparación A/B/C — ensayo técnico del motor")
para(doc, "Escenario sintético de Quito (40 puntos; 12 usan referencias de establecimientos públicos), semilla 20261016. "
          "Son costos estimados, no mediciones de una empresa.", 9.5, False, GREY, after=4)
tabla(doc, ("Opción", "Costo total", "Críticos a tiempo", "Costo frente a A", "Puntaje"), [
    ("A · Flota propia", "USD 57,89", "5 de 6 (83 %)", "—", "81,7"),
    ("B · Propia, críticos primero", "USD 69,89", "6 de 6 (100 %)", "+20,7 %", "89,1"),
    ("C · Operador contratado", "USD 66,90", "6 de 6 (100 %)", "+15,6 %", "90,8"),
], (Cm(4.6), Cm(2.8), Cm(3.4), Cm(3.0), Cm(2.8)))
para(doc, "El motor recomienda C con un puntaje que pondera costo (35 %), críticos a tiempo (35 %), pedidos pendientes (20 %) y riesgo (10 %); "
          "los pesos son configurables y no están calibrados con datos de una empresa. La opción más barata no siempre es la mejor. "
          "C cuesta 4,3 % menos que B (USD 2,99).", after=6)
aviso(doc, "Límite:", "es un ensayo técnico local sobre datos sintéticos; no representa clientes ni viajes medidos y todavía "
                     "no corre en producción. Es una sola corrida: con otra semilla o con otros pesos el orden podría cambiar. El desglose por componente de esa corrida no suma el total y no se presenta.")

h2(doc, "Recorrido de la demostración")
for n, t in enumerate([
    "El sistema detecta que un punto entra en riesgo y lo avisa en pantalla, con días de cobertura y cantidad sugerida.",
    "El planificador compara las opciones A/B/C y la IA explica la recomendación del motor.",
    "La persona aprueba y se publica la ruta; la bodega despacha escaneando el QR de cada caja.",
    "El conductor entrega y el receptor emite su dictamen («Todo conforme» o «Pasó algo malo»), con o sin señal.",
    "Gerencia y auditoría consultan indicadores y la cadena de custodia desde la Torre de Control.",
], 1):
    bullet(doc, [(f"Paso {n}. ", True), (t, False)])

h1(doc, "5", "Plan de respaldo para la presentación")
bullet(doc, [("Conectividad: ", True), ("la sede tiene la red saturada; la demostración usa un punto de acceso móvil 4G propio. La navegación y el consumo de la API se verificaron sobre esa red.", False)])
bullet(doc, [("Falla total de la nube: ", True), ("existe un entorno espejo local (base, caché, API y web en una laptop) al que los teléfonos acceden por red local.", False)])
bullet(doc, [("Falla del agente: ", True), ("el cálculo se conserva y la explicación vuelve a una plantilla determinista en 10 s o menos.", False)])

h1(doc, "6", "Qué falta")
bullet(doc, "Ensayar el recorrido sin señal en un teléfono real y medir los tiempos de la presentación.")
bullet(doc, "Llevar la comparación A/B/C a producción y conectarla a la pantalla.")
bullet(doc, "Medir con datos de una empresa real: el prototipo usa escenarios sintéticos.")

guardar(doc, "IstpetDev-Entregable-3-Prototipo.docx")
