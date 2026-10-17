"""Entregable 1 — Problema (Manual §9 y Anexo 5.1). Uso: python3 e1_problema.py"""
from lib import *

doc = nuevo(1, "Problema", "Problema, usuario", "y contexto", "Manual §9 y Anexo 5.1")

h1(doc, "1", "¿Qué problema estamos resolviendo?")
para(doc, "Una empresa de limpieza abastece de insumos a muchos puntos de servicio —hospitales, clínicas, oficinas, "
          "comercios— cada uno con consumo, acceso y horarios distintos. La información llega tarde y la cadena se "
          "rompe en cuatro lugares:", after=6)
tabla(doc, ("Quién", "Qué le pasa hoy"), [
    ("Planificador", "Repone por calendario fijo (semanal o quincenal) o por urgencia. Se entera del faltante cuando el cliente llama."),
    ("Bodega", "Pierde visibilidad del insumo apenas sale: despachos sin conciliación de cantidad, lote ni responsable."),
    ("Conductor", "Resuelve imprevistos —cierre vial, retraso, avería— por canales separados y sin registro; en el punto de entrega la señal suele fallar."),
    ("Cliente / receptor", "Recibe sin una cadena de evidencia fácil de consultar: nadie puede probar qué se entregó, a quién y cuándo."),
], (Cm(3.4), Cm(13.2)))
para(doc, "Además, cuando una entrega se apura, se decide mirando kilómetros y no el costo real de servir: personal, "
          "combustible, descarga y producto que puede dañarse. El que paga el error es el cliente final: un hospital "
          "o una clínica sin insumos de limpieza.", after=8)

h1(doc, "2", "¿A quién afecta?")
tabla(doc, ("Usuario", "Dolor", "Resultado que necesita"), [
    ("Planificador", "Rutas fijas y urgencias por llamada", "Prioridad y ruta justificadas con datos"),
    ("Bodega", "Despachos sin conciliación", "Cantidad, lote y responsable por caja"),
    ("Conductor", "Sin señal; incidentes sin registro", "Trabajo sin conexión y replanificación"),
    ("Receptor", "Entrega difícil de verificar", "Consulta y aceptación de lo recibido"),
    ("Gerencia", "Sobrecostos sin explicación", "Indicadores con origen y comparación verificable"),
], (Cm(3.2), Cm(6.4), Cm(7.0)))
para(doc, "El comprador propuesto es una empresa de servicios de limpieza con operación distribuida; el decisor posible "
          "es operaciones o gerencia. Los beneficiarios finales son los puntos que mantienen su servicio de limpieza "
          "sin interrupciones.", after=8)

h1(doc, "3", "Contexto: lo que dijo el sector")
para(doc, "La charla del sector de limpieza en Expo Clean (16-oct-2026) describió la operación desde la perspectiva "
          "del prestador del servicio. Tres testimonios sostienen el problema:", after=6)
cita(doc, "«No es lo mismo limpiar un quirófano, una sala de neonatos o una habitación.»",
     "Prioridad por área crítica. Charla del sector, Expo Clean, 16-oct-2026.")
cita(doc, "«Muchas veces mis clientes no hacen compras programadas.»",
     "Pedidos no programados que rompen las rutas. Charla del sector, Expo Clean, 16-oct-2026.")
h2(doc, "Los tres dolores, en síntesis")
tabla(doc, ("Dolor", "Necesidad que expresa", "Relación con IstpetDev"), [
    ("Aplicación correcta y verificable", "Producto y dosificación según el área; la supervisión visual no comprueba qué se aplicó",
     "La custodia hasta la recepción aporta evidencia logística. Verificar dosificación y eficacia queda fuera de este alcance."),
    ("Consumo y cantidades por área", "Relacionar litros, dilución y m² para saber cuánto suministrar",
     "Se contrastan consumos declarados con datos reales. El rendimiento por superficie requiere medición y ficha técnica."),
    ("Abastecimiento y transporte viables", "Conciliar empaques, pedidos no programados y rutas consolidadas por zona",
     "Es el vínculo directo con el Reto 1: inventario, reposición, capacidad, ventanas y trazabilidad."),
], (Cm(3.6), Cm(6.0), Cm(7.0)), tam=9)

h1(doc, "4", "Qué evidencia tenemos y qué no")
para(doc, "Distinguimos lo que escuchamos de lo que medimos:", after=4)
bullet(doc, [("Tenemos: ", True), ("testimonios del sector recogidos en la charla del 16-oct y el escenario sintético de Quito de 40 puntos "
             "planteado por el equipo (un hospital sin desinfectante cuando llama).", False)])
bullet(doc, [("No tenemos: ", True), ("una entrevista con el comprador, un número de días de anticipación que el sector considere "
             "adecuado, ni cifras de pérdida por faltantes. El escenario de 40 puntos es una ilustración hasta que una empresa lo valide.", False)])
bullet(doc, [("Ejemplos del expositor, no datos nuestros: ", True), ("las casi 400 referencias de producto, las promociones 6+1 y 12+2 y el mínimo "
             "de 15 entregas por camión son ejemplos del expositor, no límites del escenario.", False)])
aviso(doc, "Límite:", "no se validaron los 40 puntos, el caso concreto del hospital, el ahorro, la disposición de pago ni la "
                      "aceptación del producto. Por eso tratamos el problema como hipótesis fundada en testimonio, no como cifra.")

h1(doc, "5", "Hipótesis que queremos validar")
para(doc, "Ninguna está comprobada hoy; el entregable de Validación detalla qué sí se ejecutó.", 9.5, False, GREY, after=4)
tabla(doc, ("ID", "Hipótesis", "Cómo se valida"), [
    ("H-P1", "Con inventario actualizado, la empresa planifica antes de que llame el cliente.", "Piloto: comparar con la línea base de llamadas reactivas"),
    ("H-P2", "Priorizar por cobertura y restricciones reduce urgencias sin empeorar el servicio.", "Simulación (hecha, sintética) y piloto"),
    ("H-P3", "Registrar cantidades y recepción reduce el tiempo de conciliar.", "Piloto: tiempo de conciliación antes y después"),
    ("H-P4", "El conductor puede cerrar una recepción sin señal sin ayuda.", "Prueba en teléfono real (pendiente)"),
    ("H-P5", "La empresa pagaría por evitar interrupciones y medir ahorro.", "Entrevistas y contraparte de piloto (pendiente)"),
], (Cm(1.6), Cm(9.0), Cm(6.0)))

guardar(doc, "IstpetDev-Entregable-1-Problema.docx")
