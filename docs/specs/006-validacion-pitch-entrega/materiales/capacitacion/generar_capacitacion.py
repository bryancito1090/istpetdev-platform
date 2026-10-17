"""Generador de hojas de capacitación por rol e indicadores en PDF.

Hackathon Expo Clean Ecuador 2026 - Reto 1
Genera documentos Word y los convierte a PDF usando LibreOffice.
Cada hoja está diseñada para caber exactamente en UNA página imprimible.
"""
from pathlib import Path
import subprocess

from docx import Document
from docx.enum.table import WD_TABLE_ALIGNMENT
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.oxml import OxmlElement
from docx.oxml.ns import qn
from docx.shared import Cm, Pt, RGBColor

AQUI = Path(__file__).resolve().parent
REPO = AQUI.parents[3]
LOGO = REPO / "docs/specs/006-validacion-pitch-entrega/materiales/entregable-2/img/logo.png"

CYAN = "00A7C4"
LIME = "9EC200"
ORANGE = "EA601F"
DARK = "2D3748"
GREY = "64748B"
MIST = "F8FAFC"
BORDER_COLOR = "CBD5E1"
BLUE_BG = "E6F7FA"


def rgb(h):
    return RGBColor.from_string(h)


def shade(cell, color):
    tcPr = cell._tc.get_or_add_tcPr()
    shd = OxmlElement("w:shd")
    shd.set(qn("w:val"), "clear")
    shd.set(qn("w:color"), "auto")
    shd.set(qn("w:fill"), color)
    tcPr.append(shd)


def borders(table, color="CBD5E1", size="4"):
    tblPr = table._tbl.tblPr
    b = OxmlElement("w:tblBorders")
    for edge in ("top", "left", "bottom", "right", "insideH", "insideV"):
        e = OxmlElement(f"w:{edge}")
        e.set(qn("w:val"), "single" if size != "0" else "nil")
        e.set(qn("w:sz"), size)
        e.set(qn("w:color"), color)
        b.append(e)
    tblPr.append(b)


def left_bar(paragraph, color, size="32"):
    pPr = paragraph._p.get_or_add_pPr()
    bdr = OxmlElement("w:pBdr")
    left = OxmlElement("w:left")
    left.set(qn("w:val"), "single")
    left.set(qn("w:sz"), size)
    left.set(qn("w:space"), "6")
    left.set(qn("w:color"), color)
    bdr.append(left)
    pPr.append(bdr)


def fix_widths(table, widths):
    table.autofit = False
    tblPr = table._tbl.tblPr
    layout = OxmlElement("w:tblLayout")
    layout.set(qn("w:type"), "fixed")
    tblPr.append(layout)
    grid = table._tbl.tblGrid
    for gc, w in zip(grid.findall(qn("w:gridCol")), widths):
        gc.set(qn("w:w"), str(int(w.twips)))
    for row in table.rows:
        trPr = row._tr.get_or_add_trPr()
        cs = OxmlElement("w:cantSplit")
        trPr.append(cs)
        for c, w in zip(row.cells, widths):
            c.width = w


def cell_margins(cell, top=80, bottom=80, left=120, right=120):
    tcPr = cell._tc.get_or_add_tcPr()
    tcMar = OxmlElement("w:tcMar")
    for m, val in (("top", top), ("bottom", bottom), ("left", left), ("right", right)):
        node = OxmlElement(f"w:{m}")
        node.set(qn("w:w"), str(val))
        node.set(qn("w:type"), "dxa")
        tcMar.append(node)
    tcPr.append(tcMar)


def para(doc_or_cell, text="", size=9.5, bold=False, color=DARK, align=None, after=2, before=0, italic=False):
    p = doc_or_cell.add_paragraph()
    if text:
        r = p.add_run(text)
        r.font.name = "Calibri"
        r.font.size = Pt(size)
        r.bold = bold
        r.italic = italic
        r.font.color.rgb = rgb(color)
    if align:
        p.alignment = align
    p.paragraph_format.space_before = Pt(before)
    p.paragraph_format.space_after = Pt(after)
    p.paragraph_format.line_spacing = 1.05
    return p


def chip(cell, text, color, text_color="FFFFFF", size=8.5):
    cell.text = ""
    shade(cell, color)
    p = cell.paragraphs[0]
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.space_before = Pt(2)
    p.paragraph_format.space_after = Pt(2)
    r = p.add_run(text)
    r.font.name = "Calibri"
    r.bold = True
    r.font.size = Pt(size)
    r.font.color.rgb = rgb(text_color)


def add_role_page(doc, role_name, subtitle, duration, target, learns, exercise_data, check_data, mentor_data, is_last=False):
    # Header table
    tb = doc.add_table(rows=1, cols=2)
    tb.alignment = WD_TABLE_ALIGNMENT.LEFT
    borders(tb, "FFFFFF", "0")
    c_left, c_right = tb.rows[0].cells
    c_left.width, c_right.width = Cm(13.2), Cm(4.8)
    cell_margins(c_left, 40, 40, 40, 40)
    cell_margins(c_right, 40, 40, 40, 40)

    p0 = para(c_left, "ISTPETDEV · PROTOCOLO DE CAPACITACIÓN OPERATIVA", size=8, bold=True, color=CYAN, after=1)
    p1 = para(c_left, f"Rol: {role_name}", size=15, bold=True, color=DARK, after=1)
    p2 = para(c_left, subtitle, size=9, bold=False, color=GREY, after=0, italic=True)

    c_right.text = ""
    p_box = c_right.paragraphs[0]
    p_box.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    r_dur_lbl = p_box.add_run("DURACIÓN:\n")
    r_dur_lbl.font.size = Pt(8)
    r_dur_lbl.font.color.rgb = rgb(GREY)
    r_dur_lbl.bold = True
    r_dur_val = p_box.add_run(duration)
    r_dur_val.font.size = Pt(12)
    r_dur_val.font.color.rgb = rgb(ORANGE)
    r_dur_val.bold = True
    fix_widths(tb, (Cm(13.2), Cm(4.8)))

    # Objetivo general box
    obj_box = doc.add_table(rows=1, cols=1)
    borders(obj_box, CYAN, "6")
    c_obj = obj_box.cell(0, 0)
    shade(c_obj, BLUE_BG)
    cell_margins(c_obj, 50, 50, 80, 80)
    c_obj.text = ""
    p_obj = c_obj.paragraphs[0]
    p_obj.paragraph_format.space_before = Pt(1)
    p_obj.paragraph_format.space_after = Pt(1)
    r_obj_t = p_obj.add_run("Propósito del puesto: ")
    r_obj_t.bold = True
    r_obj_t.font.size = Pt(8.5)
    r_obj_t.font.color.rgb = rgb(CYAN)
    r_obj_m = p_obj.add_run(target)
    r_obj_m.font.size = Pt(8.5)
    r_obj_m.font.color.rgb = rgb(DARK)
    fix_widths(obj_box, (Cm(18.0),))

    para(doc, "", after=2)

    # 1. Qué aprende
    p_sec1 = para(doc, "1. ¿Qué aprende este rol?", size=10, bold=True, color=DARK, after=2, before=2)
    left_bar(p_sec1, CYAN, "18")

    t_learn = doc.add_table(rows=len(learns), cols=2)
    borders(t_learn, "F1F5F9", "2")
    for idx, (icon, text) in enumerate(learns):
        row = t_learn.rows[idx]
        cell_margins(row.cells[0], 25, 25, 40, 20)
        cell_margins(row.cells[1], 25, 25, 20, 40)
        row.cells[0].width = Cm(0.8)
        row.cells[1].width = Cm(17.2)
        row.cells[0].text = ""
        p_ic = row.cells[0].paragraphs[0]
        p_ic.alignment = WD_ALIGN_PARAGRAPH.CENTER
        r_ic = p_ic.add_run(icon)
        r_ic.font.size = Pt(8)
        r_ic.bold = True
        r_ic.font.color.rgb = rgb(CYAN)
        row.cells[1].text = ""
        p_tx = row.cells[1].paragraphs[0]
        p_tx.paragraph_format.space_after = Pt(1)
        r_tx = p_tx.add_run(text)
        r_tx.font.size = Pt(8.5)
        r_tx.font.color.rgb = rgb(DARK)
    fix_widths(t_learn, (Cm(0.8), Cm(17.2)))

    para(doc, "", after=2)

    # 2. Ejercicio Práctico
    p_sec2 = para(doc, f"2. Ejercicio práctico con datos de Quito (demo-quito-planificacion)", size=10, bold=True, color=DARK, after=2, before=2)
    left_bar(p_sec2, ORANGE, "18")

    t_ex = doc.add_table(rows=len(exercise_data) + 1, cols=2)
    borders(t_ex, "CBD5E1", "4")
    hdr = t_ex.rows[0].cells
    hdr[0].width, hdr[1].width = Cm(4.2), Cm(13.8)
    chip(hdr[0], "Elemento de prueba", DARK, size=8)
    chip(hdr[1], "Paso a paso en la aplicación (acciones verificadas en código)", DARK, size=8)
    cell_margins(hdr[0], 30, 30, 50, 50)
    cell_margins(hdr[1], 30, 30, 50, 50)

    for i, (k, v) in enumerate(exercise_data, 1):
        r_cells = t_ex.rows[i].cells
        r_cells[0].width, r_cells[1].width = Cm(4.2), Cm(13.8)
        cell_margins(r_cells[0], 25, 25, 50, 50)
        cell_margins(r_cells[1], 25, 25, 50, 50)
        r_cells[0].text = ""
        p_k = r_cells[0].paragraphs[0]
        r_k = p_k.add_run(k)
        r_k.bold = True
        r_k.font.size = Pt(8)
        r_k.font.color.rgb = rgb(DARK)
        shade(r_cells[0], MIST)

        r_cells[1].text = ""
        p_v = r_cells[1].paragraphs[0]
        p_v.paragraph_format.space_after = Pt(1)
        r_v = p_v.add_run(v)
        r_v.font.size = Pt(8)
        r_v.font.color.rgb = rgb(DARK)
    fix_widths(t_ex, (Cm(4.2), Cm(13.8)))

    para(doc, "", after=2)

    # 3. Comprobación y Referente de dudas (Dos columnas)
    p_sec3 = para(doc, "3. Evaluación de aprendizaje y soporte por turno", size=10, bold=True, color=DARK, after=2, before=2)
    left_bar(p_sec3, LIME, "18")

    t_eval = doc.add_table(rows=1, cols=2)
    borders(t_eval, "CBD5E1", "4")
    c_chk, c_ref = t_eval.rows[0].cells
    c_chk.width, c_ref.width = Cm(9.0), Cm(9.0)
    cell_margins(c_chk, 35, 35, 60, 60)
    cell_margins(c_ref, 35, 35, 60, 60)

    c_chk.text = ""
    p_chk_t = c_chk.paragraphs[0]
    p_chk_t.paragraph_format.space_after = Pt(1)
    r_ct = p_chk_t.add_run("¿CÓMO SE COMPRUEBA QUE APRENDIÓ?\n")
    r_ct.bold = True
    r_ct.font.size = Pt(8)
    r_ct.font.color.rgb = rgb("0F766E")
    r_cm = p_chk_t.add_run(check_data)
    r_cm.font.size = Pt(8)
    r_cm.font.color.rgb = rgb(DARK)
    shade(c_chk, "F0FDF4")

    c_ref.text = ""
    p_ref_t = c_ref.paragraphs[0]
    p_ref_t.paragraph_format.space_after = Pt(1)
    r_rt = p_ref_t.add_run("¿QUIÉN ES SU REFERENTE POR TURNO?\n")
    r_rt.bold = True
    r_rt.font.size = Pt(8)
    r_rt.font.color.rgb = rgb(ORANGE)
    r_rm = p_ref_t.add_run(mentor_data)
    r_rm.font.size = Pt(8)
    r_rm.font.color.rgb = rgb(DARK)
    shade(c_ref, "FFF7ED")
    fix_widths(t_eval, (Cm(9.0), Cm(9.0)))

    # Pie de hoja
    p_ft = para(doc, f"IstpetDev · Hackathon Expo Clean Ecuador 2026 · Hoja de 1 página por rol · {role_name} · Actualizado: 2026-10-16", size=7.5, color=GREY, align=WD_ALIGN_PARAGRAPH.CENTER, after=0, before=4)

    if not is_last:
        doc.add_page_break()


def add_indicators_page(doc):
    tb = doc.add_table(rows=1, cols=2)
    tb.alignment = WD_TABLE_ALIGNMENT.LEFT
    borders(tb, "FFFFFF", "0")
    c_left, c_right = tb.rows[0].cells
    c_left.width, c_right.width = Cm(13.2), Cm(4.8)
    cell_margins(c_left, 40, 40, 40, 40)
    cell_margins(c_right, 40, 40, 40, 40)

    p0 = para(c_left, "ISTPETDEV · TABLERO DE INDICADORES OPERATIVOS", size=8, bold=True, color=CYAN, after=1)
    p1 = para(c_left, "Indicadores en lenguaje de toda la empresa", size=14, bold=True, color=DARK, after=1)
    p2 = para(c_left, "Desde la gerencia hasta quien estiba en bodega: preguntas sencillas, fórmulas y origen honesto del dato", size=8.5, bold=False, color=GREY, after=0, italic=True)

    c_right.text = ""
    p_box = c_right.paragraphs[0]
    p_box.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    r_lbl = p_box.add_run("METODOLOGÍA:\n")
    r_lbl.font.size = Pt(8)
    r_lbl.font.color.rgb = rgb(GREY)
    r_lbl.bold = True
    r_val = p_box.add_run("Sin datos inventados")
    r_val.font.size = Pt(10)
    r_val.font.color.rgb = rgb(ORANGE)
    r_val.bold = True
    fix_widths(tb, (Cm(13.2), Cm(4.8)))

    # Banner de transparencia
    obj_box = doc.add_table(rows=1, cols=1)
    borders(obj_box, CYAN, "6")
    c_obj = obj_box.cell(0, 0)
    shade(c_obj, BLUE_BG)
    cell_margins(c_obj, 40, 40, 70, 70)
    c_obj.text = ""
    p_obj = c_obj.paragraphs[0]
    p_obj.paragraph_format.space_before = Pt(1)
    p_obj.paragraph_format.space_after = Pt(1)
    r_obj_t = p_obj.add_run("Regla de transparencia (#235): ")
    r_obj_t.bold = True
    r_obj_t.font.size = Pt(8)
    r_obj_t.font.color.rgb = rgb(CYAN)
    r_obj_m = p_obj.add_run("Si un indicador aún no cuenta con registro en vivo o consolidación en base de datos, la tarjeta de la app dice con honestidad «sin dato todavía» en lugar de inventar números o mostrar ceros ficticios.")
    r_obj_m.font.size = Pt(8)
    r_obj_m.font.color.rgb = rgb(DARK)
    fix_widths(obj_box, (Cm(18.0),))

    para(doc, "", after=2)

    indicators = [
        ("Costo por km", "¿Cuánto nos cuesta cada km que rueda el camión?", "Gerencia, Planificador", "Costo ruta ÷ km recorridos", "Simulación / Detalle ruta en /rutas"),
        ("Faltantes", "¿Cuántos clientes se quedaron sin producto?", "Todos los roles", "Puntos en stock 0 o pedidos no cubiertos", "Alertas stock crítico en /alertas / Simulación"),
        ("Servicio", "¿Cumplimos con lo prometido al cliente?", "Gerencia, Planificador, Conductor", "Pedidos a tiempo y completos ÷ Total pedidos", "«sin dato todavía» en inicio; por parada en ruta"),
        ("Tiempo", "¿Cuánto tiempo tarda todo el recorrido?", "Planificador, Conductor", "Hora retorno bodega − Hora salida inicial", "Tramos OSRM en /rutas o «sin dato todavía»"),
        ("Mermas", "¿Cuánto producto se dañó o perdió en el camino?", "Bodega, Gerencia, Auditor", "Unidades dañadas/rotas ÷ Unidades despachadas", "Bandeja de rechazos por motivo «Daño» (/rutas)"),
        ("Exactitud", "¿Lo del sistema coincide con la percha física?", "Bodega, Auditor", "Recepciones y conteos conformes ÷ Total", "Recepciones conformes sin reclamo posterior"),
        ("Costo por entrega", "¿Cuánto nos cuesta llevar un pedido al cliente?", "Gerencia, Planificador", "Costo total servir ruta ÷ Entregas completadas", "Costeo de servicio (#240) o «sin dato todavía»"),
        ("Propia vs. Contratada", "¿Conviene camión propio o tercerizar?", "Gerencia, Planificador", "Costo propio ruta − Tarifa carrier contratado", "Comparación de opciones A/B/C en /rutas"),
    ]

    t_ind = doc.add_table(rows=len(indicators) + 1, cols=5)
    borders(t_ind, "CBD5E1", "4")
    hdr = t_ind.rows[0].cells
    chip(hdr[0], "Indicador", DARK, size=8)
    chip(hdr[1], "Pregunta sencilla", DARK, size=8)
    chip(hdr[2], "Quién lo usa", DARK, size=8)
    chip(hdr[3], "Cómo se calcula", DARK, size=8)
    chip(hdr[4], "Origen en la app", DARK, size=8)

    widths = (Cm(2.6), Cm(4.8), Cm(2.8), Cm(4.0), Cm(3.8))
    for c, w in zip(hdr, widths):
        c.width = w
        cell_margins(c, 25, 25, 30, 30)

    for i, row_data in enumerate(indicators, 1):
        cells = t_ind.rows[i].cells
        for c, w in zip(cells, widths):
            c.width = w
            cell_margins(c, 22, 22, 35, 35)

        for col_idx, text in enumerate(row_data):
            cells[col_idx].text = ""
            p = cells[col_idx].paragraphs[0]
            p.paragraph_format.space_after = Pt(1)
            r = p.add_run(text)
            r.font.name = "Calibri"
            r.font.size = Pt(7.8)
            if col_idx == 0:
                r.bold = True
                r.font.color.rgb = rgb(DARK)
                shade(cells[col_idx], MIST)
            elif col_idx == 4 and "sin dato" in text:
                r.bold = True
                r.font.color.rgb = rgb(ORANGE)
            else:
                r.font.color.rgb = rgb(DARK)

    fix_widths(t_ind, widths)

    para(doc, "", after=2)

    # Box de ejemplos ilustrativos
    ex_box = doc.add_table(rows=1, cols=1)
    borders(ex_box, LIME, "4")
    c_ex = ex_box.cell(0, 0)
    shade(c_ex, "F9FEE8")
    cell_margins(c_ex, 30, 30, 50, 50)
    c_ex.text = ""
    p_ex = c_ex.paragraphs[0]
    p_ex.paragraph_format.space_before = Pt(1)
    p_ex.paragraph_format.space_after = Pt(1)
    r_ex_t = p_ex.add_run("Ejemplo ilustrativo de aplicación: ")
    r_ex_t.bold = True
    r_ex_t.font.size = Pt(8)
    r_ex_t.font.color.rgb = rgb("4D7C0F")
    r_ex_m = p_ex.add_run("En la ruta matutina desde Bodega Sur hacia Hospital Enrique Garcés y Quicentro Norte, el costo de energía del camión eléctrico BYD T35 es de USD 0,075/km frente a USD 0,21/km del camión diésel. En la entrega del hospital, el receptor rechaza 1 galón dañado (merma registrada con foto); la exactitud de percha física se actualiza y el indicador de faltantes se mantiene en 0 porque el reabastecimiento llegó antes del quiebre.")
    r_ex_m.font.size = Pt(7.8)
    r_ex_m.font.color.rgb = rgb(DARK)
    fix_widths(ex_box, (Cm(18.0),))

    para(doc, "IstpetDev · Hackathon Expo Clean Ecuador 2026 · Hoja de Indicadores de Operación en Lenguaje Sencillo · Actualizado: 2026-10-16", size=7.5, color=GREY, align=WD_ALIGN_PARAGRAPH.CENTER, after=0, before=4)


def build_document():
    doc = Document()
    st = doc.styles["Normal"]
    st.font.name = "Calibri"
    st.element.rPr.rFonts.set(qn("w:eastAsia"), "Calibri")
    st.font.size = Pt(9)
    st.font.color.rgb = rgb(DARK)

    for s in doc.sections:
        s.page_height, s.page_width = Cm(29.7), Cm(21.0)
        s.top_margin = s.bottom_margin = Cm(1.2)
        s.left_margin = s.right_margin = Cm(1.5)

    # 1. PLANIFICADOR
    add_role_page(
        doc,
        role_name="Planificador",
        subtitle="Organización de rutas, evaluación de opciones A/B/C y respuesta ante incidentes en vía",
        duration="2 horas (práctica guiada)",
        target="Evitar que los centros de salud se queden sin insumos de limpieza, respetando ventanas de descarga y capacidad vehicular.",
        learns=[
            ("1", "Interpretar alertas tempranas de stock crítico antes de que ocurra un quiebre en hospitales."),
            ("2", "Comparar opciones A/B/C (costos, ventanas horarias tempranas y capacidad de peso y volumen)."),
            ("3", "Reaccionar ante averías mecánicas reasignando paradas pendientes sin perder lo ya entregado."),
            ("4", "Aprobar y publicar la ruta definitiva hacia los teléfonos de los conductores.")
        ],
        exercise_data=[
            ("Base y Vehículo", "Bodega Central Sur (BOD-0001) y camión Hino Dutro City PCB-2214 (VEH-0001, capacidad 3.500 kg / 14 m³)."),
            ("Paradas a planificar", "Hospital Enrique Garcés (PTO-0005, ventana 07:00–11:00, crítico), Carlos Andrade Marín (PTO-0004, 06:30–10:30) y Quicentro Norte (PTO-0013, ventana estrecha 07:00–09:30)."),
            ("Paso en app (/rutas)", "Pulsar «+ Nueva Ruta por Pedidos» o «🤖 Pedir al Agente IA» para generar la secuencia factible."),
            ("Simulación avería", "Incidente averia-matilde-alvarez (falla de embrague en Av. Matilde Álvarez con 50 km recorridos y entregas pendientes)."),
            ("Acción en app (/rutas)", "Pulsar «Proponer Ruta de Emergencia»: reasignar paradas al camión eléctrico VEH-0004 (BYD T35 PCE-3021) y publicar la ruta corregida.")
        ],
        check_data="Genera en la app una ruta factible que cumple las ventanas horarias de los hospitales y del comercio sin sobrecargar el camión. Ante la avería simulada, reasigna paradas en menos de 10 minutos justificando por qué priorizó la salud.",
        mentor_data="Coordinador de Logística y Tráfico (turnos mañana y tarde). Soporte continuo de plataforma IstpetDev."
    )

    # 2. BODEGA
    add_role_page(
        doc,
        role_name="Bodega (Despacho y Estiba)",
        subtitle="Preparación de pedidos, doble verificación física, bultos pesados >25 kg y rotulado QR",
        duration="1 hora (en mesa de preparación y rampa)",
        target="Asegurar que cada bulto físico coincida exactamente con el sistema, con su lote y su código QR de custodia, incluso sin red wifi.",
        learns=[
            ("1", "Consultar entregas pendientes asignadas a la bodega en la aplicación móvil/PWA."),
            ("2", "Efectuar doble verificación física de producto, cantidad y fecha de vencimiento (control FIFO/FEFO)."),
            ("3", "Identificar sacos mayores a 25 kg (detergente 30 kg) para coordinar estiba entre dos personas."),
            ("4", "Generar el rótulo QR de la unidad de entrega (HU) y despachar al conductor en rampa."),
            ("5", "Trabajar en modo sin conexión cuando no haya señal de internet en el galpón.")
        ],
        exercise_data=[
            ("Bodega y Destino", "Bodega Central Sur (BOD-0001) para el Hospital General Enrique Garcés (PTO-0005)."),
            ("Insumos a preparar", "10 galones de Hipoclorito (PRD-0001, lote próximo a vencer L-0001-2609) y 2 sacos de detergente de 30 kg (PRD-0003, carga pesada)."),
            ("Camión en rampa", "Hino Dutro City PCB-2214 (VEH-0001)."),
            ("Paso en app móvil", "En componente Bodega, abrir la entrega, verificar lotes, pulsar «Preparar bulto» para emitir QR y luego pulsar «Despachar al vehículo»."),
            ("Prueba sin conexión", "Desactivar wifi: comprobar que la app muestra aviso «Modo sin conexión» y permite despachar bultos listos guardando en memoria local.")
        ],
        check_data="Prepara 3 bultos de prueba sin discrepancias entre lo físico y la pantalla. Identifica de inmediato el lote de vencimiento prioritario y completa un despacho sin señal.",
        mentor_data="Supervisor de Bodega y Despacho en turno diurno; Jefe de Turno de Almacén en turno nocturno."
    )

    # 3. CONDUCTOR
    add_role_page(
        doc,
        role_name="Conductor (Transportista)",
        subtitle="Descarga de ruta local, custodia digital por QR y entregas fluidas en modo avión",
        duration="30 minutos (con teléfono celular)",
        target="Completar las paradas respetando los horarios de los clientes, escaneando bultos y registrando entregas con o sin cobertura móvil.",
        learns=[
            ("1", "Descargar la ruta en el celular antes de salir de la bodega para trabajar con datos locales."),
            ("2", "Escanear el código QR de los bultos en rampa para certificar la custodia de la carga."),
            ("3", "Seguir el orden de paradas respetando las horas de llegada prioritarias a hospitales."),
            ("4", "Escanear la entrega en el punto de destino frente al cliente receptor."),
            ("5", "Trabajar en modo sin conexión (modo avión) con sincronización automática al recuperar señal.")
        ],
        exercise_data=[
            ("Camión y Ruta", "Hino Dutro City PCB-2214 (VEH-0001) con salida desde Bodega Sur (BOD-0001)."),
            ("Paradas asignadas", "Parada 1: Hospital Enrique Garcés (PTO-0005, 07:00–11:00); Parada 2: Quicentro Norte (PTO-0013, 07:00–09:30)."),
            ("Paso en app móvil", "Pulsar «Descargar Ruta» en la barra de inicio. En rampa, pulsar «Escanear QR» para confirmar carga."),
            ("Prueba de corte", "Activar Modo Avión al salir hacia Av. Maldonado; llegar al hospital y pulsar «Escanear QR» para entregar."),
            ("Sincronización", "Desactivar Modo Avión: verificar que el contador de entregas pendientes en la barra superior se envía automáticamente al servidor.")
        ],
        check_data="Descarga su ruta, activa modo avión en su teléfono, ejecuta un escaneo de entrega de prueba y comprueba que se sincroniza solo al reconectar.",
        mentor_data="Monitor de Tráfico y Despacho (canal de radio/móvil); Supervisor de Flota para auxilio mecánico en vía."
    )

    # 4. RECEPTOR
    add_role_page(
        doc,
        role_name="Receptor (Cliente y Farmacia)",
        subtitle="Recepción ágil, validación de lote, rechazo con motivo y foto en menos de dos minutos",
        duration="20 minutos (en área de recepción)",
        target="Comprobar que lo entregado coincida con lo pedido, documentando cualquier daño con foto de respaldo para reposición inmediata.",
        learns=[
            ("1", "Escanear el código QR del bulto recibido o ingresar el código de entrega en pantalla."),
            ("2", "Verificar la lista de productos, cantidades y números de lote en la pantalla del celular."),
            ("3", "Confirmar conformidad con un toque o registrar novedades en caso de producto dañado."),
            ("4", "Adjuntar fotografía obligatoria de evidencia si existe rechazo parcial de producto."),
            ("5", "Anotar el saldo en percha y guardar la recepción para cerrar la entrega digitalmente.")
        ],
        exercise_data=[
            ("Punto de recepción", "Hospital General Enrique Garcés (PTO-0005, Sur de Quito)."),
            ("Bulto recibido", "Caja con 10 galones de Hipoclorito (PRD-0001, Lote L-0001-2609)."),
            ("Novedad simulada", "Uno de los envases presenta rotura de tapa con fuga de desinfectante."),
            ("Paso en app móvil", "En módulo Receptor, pulsar «Escanear QR con cámara», verificar líneas, desmarcar conformidad del ítem."),
            ("Registro de rechazo", "Ingresar cantidad aceptada = 9 (rechazadas = 1), seleccionar motivo «Daño», tomar foto con la cámara y pulsar «Guardar recepción de la entrega completa».")
        ],
        check_data="Escanea un bulto de prueba, aprueba líneas conformes y registra un rechazo parcial con motivo y fotografía en menos de 3 minutos.",
        mentor_data="Analista de Servicio al Cliente y Recepciones de IstpetDev. Conductor en el punto de descarga."
    )

    # 5. GERENCIA
    add_role_page(
        doc,
        role_name="Gerencia de Operaciones",
        subtitle="Supervisión directiva con el panel «Hoy», 8 indicadores claros y propia vs. contratada",
        duration="30 minutos (revisión de tablero directivo)",
        target="Evaluar la salud de la red logística mediante indicadores sencillos, decidiendo flota y costos con evidencia verificable.",
        learns=[
            ("1", "Monitorear el estado general de abastecimiento y centros de salud activos en el panel «Hoy»."),
            ("2", "Interpretar los 8 indicadores operativos en lenguaje sencillo sin tecnicismos."),
            ("3", "Identificar el origen del dato (simulación, campo o sin dato todavía) para evitar sesgos."),
            ("4", "Evaluar el costo de flota propia (diésel vs. camiones eléctricos BYD) frente a carrier contratado."),
            ("5", "Detectar aumentos en la tasa de mermas para exigir acciones correctivas a proveedores.")
        ],
        exercise_data=[
            ("Panel /inicio", "Revisar centros de salud activos y tarjeta de Riesgo de Stock (identificar hospital Enrique Garcés en nivel crítico)."),
            ("Panel /rutas", "Abrir la tabla de opciones comparativas A/B/C."),
            ("Comparación flota", "Contrastar camión liviano diésel VEH-0001 (0,21 USD/km) vs. camión eléctrico VEH-0004 (0,075 USD/km de energía)."),
            ("Tercerización", "Comparar costo de viaje propio vs. tarifa cotizada por operador contratado (tarifa base + km + kg)."),
            ("Decisión", "Justificar verbalmente cuál opción ofrece el menor costo cumpliendo la ventana crítica del hospital.")
        ],
        check_data="Explica en 2 minutos el balance de la jornada usando sus palabras, señalando la bodega más exigida y si conviene camión propio o contratado.",
        mentor_data="Líder de Proyecto / Product Owner de IstpetDev. Analistas técnicos para consultas de datos."
    )

    # 6. AUDITOR
    add_role_page(
        doc,
        role_name="Auditor (Control Interno y Calidad)",
        subtitle="Trazabilidad forense de bultos (HU), inspección de firmas, fotos y modo solo lectura",
        duration="30 minutos (práctica de auditoría)",
        target="Reconstruir la historia completa de cualquier entrega desde su armado hasta su recepción, inspeccionando novedades sin riesgo de alterar datos.",
        learns=[
            ("1", "Consultar la trazabilidad de cualquier bulto ingresando su código o escaneando su QR."),
            ("2", "Verificar la cadena de custodia: responsable de bodega, camión, conductor y hora de entrega."),
            ("3", "Auditar causas de rechazo y fotografías de no conformidad tomadas por los clientes."),
            ("4", "Navegar por el sistema en modo seguro de Solo Lectura sin modificar la operación en curso.")
        ],
        exercise_data=[
            ("Caso a auditar", "Entrega de desinfectante al Hospital Enrique Garcés (PTO-0005)."),
            ("Paso en app (/public-trace)", "Ingresar al módulo de trazabilidad pública, escribir el código de bulto y revisar la línea de tiempo."),
            ("Inspección de hitos", "Constatar: preparación en Bodega Sur, salida en camión PCB-2214, llegada y confirmación del receptor."),
            ("Auditoría en /rutas", "Ingresar con rol auditor (verificar aviso «Panel de Auditoría (Solo Lectura)») y abrir la pestaña «Bandeja de Capturas Rechazadas» para inspeccionar foto y motivo del galón dañado.")
        ],
        check_data="Localiza un bulto al azar en menos de 2 minutos y resume fecha, hora, responsable de empaque, camión y resultado de la entrega.",
        mentor_data="Oficial de Cumplimiento / Control Interno. Administrador de Plataforma IstpetDev para dudas de logs."
    )

    # 7. INDICADORES EN LENGUAJE SENCILLO
    add_indicators_page(doc)

    docx_out = AQUI / "IstpetDev-Hojas-Capacitacion-e-Indicadores.docx"
    doc.save(docx_out)
    print(f"Generado DOCX: {docx_out}")

    # Convertir a PDF con soffice
    cmd = ["soffice", "--headless", "--convert-to", "pdf", str(docx_out), "--outdir", str(AQUI)]
    res = subprocess.run(cmd, capture_output=True, text=True)
    pdf_out = AQUI / "IstpetDev-Hojas-Capacitacion-e-Indicadores.pdf"
    if pdf_out.exists():
        print(f"Generado PDF: {pdf_out}")
    else:
        print(f"Error generando PDF: {res.stderr}")


if __name__ == "__main__":
    build_document()
