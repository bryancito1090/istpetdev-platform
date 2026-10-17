"""Entregable 6 — Modelo de negocio (Manual §9 y Anexo 5.6). Uso: python3 e6_modelo_negocio.py"""
from lib import *

doc = nuevo(6, "Modelo de negocio", "Modelo de negocio", "y piloto", "Manual §9 y Anexo 5.6")

aviso(doc, "Estado:", "hipótesis para validar con usuarios y mentores. No hay cliente comprometido, venta, acuerdo comercial "
                    "ni precio definido; lo pendiente está marcado.", CYAN)

h1(doc, "1", "¿Quién es el cliente o beneficiario?")
tabla(doc, ("", "Descripción"), [
    ("Cliente propuesto", "Empresa de servicios de limpieza con operación distribuida, que abastece a muchos puntos (hospitales, clínicas, oficinas, comercios)."),
    ("Decisor posible", "Operaciones o gerencia."),
    ("Usuarios", "Planificación, bodega, conductores y receptores."),
    ("Beneficiarios", "Los puntos que mantienen su servicio y los equipos que evitan urgencias."),
    ("Estado", "Hipótesis, sin cliente comprometido. El decisor, la necesidad y el presupuesto se verifican con una contraparte de piloto."),
], (Cm(3.6), Cm(13.0)))

h1(doc, "2", "¿Cuál es la propuesta de valor?")
box = doc.add_table(rows=1, cols=1)
borders(box, CYAN, "8")
cell = box.cell(0, 0)
shade(cell, "E0F7FB")
cell.text = ""
p = cell.paragraphs[0]
p.paragraph_format.space_before = Pt(6)
p.paragraph_format.space_after = Pt(6)
rich(p, [("Detectamos el faltante antes de que el cliente llame, ", True),
         ("proponemos cómo atenderlo al costo real y dejamos prueba de cada entrega. ", True),
         ("El motor calcula, la IA explica y la persona decide.", False)], 11)
fix_widths(box, (Cm(16.6),))
para(doc, "", after=4)
tabla(doc, ("Hoy", "Con IstpetDev"), [
    ("Se entera del faltante cuando el cliente llama", "Anticipa el riesgo con días de cobertura y la causa"),
    ("Repone con calendarios fijos o por urgencia", "Prioriza por criticidad y restricciones, con ruta justificada"),
    ("Decide mirando kilómetros", "Compara flota propia y contratada con el costo total estimado de servir"),
    ("No puede comprobar qué se entregó", "Custodia con QR, foto y firma, con o sin señal"),
], (Cm(8.0), Cm(8.6)), negrita_col0=False)
para(doc, "Diferencia: no es solo un inventario con alertas ni solo un ruteador; la alerta termina en una decisión con costo y "
          "en una entrega comprobable.", after=6)

h1(doc, "3", "¿Cómo genera ingresos o sostenibilidad?")
bullet(doc, [("Implementación inicial: ", True), ("integración con los datos de la empresa, configuración y capacitación por rol.", False)])
bullet(doc, [("Suscripción: ", True), ("por operación o por puntos activos.", False)])
bullet(doc, [("Entrada: ", True), ("piloto de cuatro semanas sobre una bodega y un grupo de puntos, para medir antes de ampliar.", False)])
aviso(doc, "Precio: PENDIENTE.", "Se decide después de validar el ahorro, la disposición de pago y el costo real de soporte. "
                                "No asignamos un precio ficticio como si fuera un dato.")

h1(doc, "4", "¿Cuáles son los costos principales?")
tabla(doc, ("Componente", "Estado de la cifra"), [
    ("Servidor de la aplicación (perfil A)", "≈ USD 37 al mes solo en cómputo, disco e IP pública (AWS, verificado el 7-oct-2026). Parcial: no incluye la base RDS."),
    ("Base de datos RDS y almacenamiento de evidencias (S3)", "PENDIENTE: depende del tamaño y la retención del piloto"),
    ("Mapas y distancias", "Sin costo de licencia (OpenStreetMap). Distancias propias o de proveedor: PENDIENTE"),
    ("Modelo de IA (DeepSeek)", "PENDIENTE: tokens y frecuencia de uso"),
    ("Desarrollo, integración y soporte", "PENDIENTE: horas con la empresa contraparte"),
    ("Operación logística del cliente", "PENDIENTE: costo por kilómetro, conductor y urgencias"),
], (Cm(6.4), Cm(10.2)))

h1(doc, "5", "Cómo mediremos el valor")
bullet(doc, [("Ahorro operativo ", True), ("= costo tradicional − costo con la plataforma, para un nivel comparable de servicio.", False)])
bullet(doc, [("Beneficio neto ", True), ("= ahorro operativo − costo incremental del sistema y su operación.", False)])
bullet(doc, [("ROI ", True), ("= beneficio neto / inversión relevante, indicando el período y lo que integra la inversión.", False)])
para(doc, "Separamos el ahorro simulado del observado. Una ruta más corta puede empeorar la puntualidad; no vendemos ahorro "
          "si solo traslada el costo a faltantes o a más horas de trabajo. Indicadores del piloto: quiebres evitados y nuevos, "
          "demanda no atendida, costo de transporte y puntualidad, cada uno con unidad, período y denominador.", after=6)
tabla(doc, ("Evidencia disponible hoy", "Alcance"), [
    ("Simulación de 90 días en Quito: IstpetDev gasta menos que la política reactiva y más que el calendario fijo semanal", "Sintética; no demuestra ahorro. El efecto sobre quiebres se mide en el piloto"),
    ("Comparación A/B/C: la opción recomendada cuesta 4,3 % menos que la propia con críticos primero (USD 2,99)", "Una sola corrida técnica sobre un escenario sintético"),
], (Cm(10.2), Cm(6.4)), negrita_col0=False)

h1(doc, "6", "Próximo paso")
para(doc, "Buscamos una empresa de limpieza como aliado para el piloto: acordar alcance, línea base e indicadores, y un umbral "
          "de avance que incluya la tolerancia a nuevos quiebres y a la carga operativa adicional. Solo después de esa "
          "medición hablaremos de resultados de la empresa y fijaremos el precio.", after=6)

guardar(doc, "IstpetDev-Entregable-6-Modelo-de-negocio.docx")
