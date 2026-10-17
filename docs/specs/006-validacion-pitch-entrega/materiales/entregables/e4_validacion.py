"""Entregable 4 — Validación (Manual §9 y Anexo 5.4). Uso: python3 e4_validacion.py"""
from lib import *

doc = nuevo(4, "Validación", "Qué probamos,", "con qué y qué falta", "Manual §9 y Anexo 5.4")

h1(doc, "1", "¿Qué probamos?")
para(doc, "Probamos el prototipo en cuatro frentes. Cada resultado se presenta con su escenario y su límite; lo que no "
          "se ejecutó se dice como pendiente.", after=6)
tabla(doc, ("Frente", "Qué se hizo", "Resultado", "Límite"), [
    ("Simulación de 90 días", "12 corridas: 3 políticas × 4 escenarios (3 semillas base y 1 adversa)", "Reproducible por semilla", "Datos sintéticos"),
    ("Comparación A/B/C", "Ensayo técnico del motor en el escenario de Quito", "Recomienda la opción C", "Local; no en producción"),
    ("Agente de IA", "Corrida registrada: riesgos, abastecimiento y rutas con explicación", "Completó, con caída al respaldo", "Una corrida registrada"),
    ("Despliegue en producción", "Verificación de salud el 17-oct a las 08:28", "Todo en HTTP 200", "Salud técnica, no uso por usuarios"),
], (Cm(3.4), Cm(5.6), Cm(3.8), Cm(3.8)), tam=9)

h1(doc, "2", "Simulación de 90 días")
para(doc, "Se comparó el fixture nacional canónico: 80 puntos de servicio, 6 bodegas, 12 vehículos y 2 productos "
          "(desinfectante y papel toalla), sobre 90 días desde el 12-oct-2026. Semillas base 42, 101 y 2024; escenario "
          "adverso 9999 (demanda +20 % y un vehículo retirado).", after=6)
tabla(doc, ("Política", "Km totales (mediana)", "Costo de transporte", "Entregas"), [
    ("Reactiva: repone cuando ya falta", "4 562,5", "USD 7 984,38", "365"),
    ("Calendario fijo semanal", "7 800,0", "USD 13 650,00", "624"),
    ("IstpetDev: preventiva", "7 037,5", "USD 12 315,63", "563"),
    ("IstpetDev, escenario adverso", "6 962,5", "USD 12 184,38", "557"),
], (Cm(6.2), Cm(3.6), Cm(3.8), Cm(3.0)))
bullet(doc, [("Frente al calendario fijo: ", True), ("IstpetDev recorre 7 037,5 km en lugar de 7 800 y su costo de transporte es 9,8 % menor "
             "(USD 12 315,63 frente a USD 13 650,00).", False)])
bullet(doc, [("Frente a la política reactiva: ", True), ("la reactiva hace menos entregas, pero tarde, cuando el punto ya está desabastecido. "
             "IstpetDev adelanta la reposición.", False)])
bullet(doc, [("Ante la disrupción: ", True), ("con demanda +20 % y un vehículo menos, completa 557 entregas sin colapsar.", False)])
aviso(doc, "Cómo leer el 9,8 %:", "el costo por entrega es igual en las tres políticas (USD 21,88); el ahorro viene de eliminar "
                                 "visitas innecesarias a puntos con stock suficiente. Es una simulación con datos sintéticos, no un ahorro "
                                 "observado en una empresa, y esta comparación no mide quiebres de stock.")
para(doc, "Las corridas se reproducen con las semillas fijadas y el fixture `national-v1`; el resumen y el paquete exportado "
          "quedan en el repositorio, y una prueba automatizada los genera y verifica.", 9.5, False, GREY)

h1(doc, "3", "¿Con quién probamos?")
bullet(doc, [("Con el sector, en la charla: ", True), ("escuchamos el testimonio de un prestador de servicios de limpieza (16-oct). "
             "Es contexto del problema, no una prueba del producto.", False)])
bullet(doc, [("Con mentores: ", True), ("la sesión «Pitch que convence» del 17-oct orientó la estructura de la presentación. "
             "No hay una mentoría documentada en la que el mentor haya probado el prototipo.", False)])
bullet(doc, [("Con usuarios finales: ", True), ("todavía no. No hay una sesión documentada con un planificador, un conductor o un receptor "
             "usando el sistema.", False)])
aviso(doc, "Importante:", "una conversación con un mentor no equivale a un piloto con una empresa, y las entrevistas de la feria "
                         "no demuestran frecuencia del problema ni disposición de pago.")

h1(doc, "4", "¿Qué modificamos a partir de las pruebas?")
bullet(doc, "Retiramos del discurso el desglose por componente de la comparación A/B/C, porque en esa corrida no suma el total.")
bullet(doc, "Pasamos de rutas con cifras fijas en pantalla a rutas y paradas calculadas por la API real.")
bullet(doc, "Pasamos de un solo almacén a abastecimiento multialmacén con vigencia de lote (FEFO), validación de peso y volumen del vehículo y rebalanceo.")
bullet(doc, "Reforzamos el acceso público por QR: vista limitada a los hitos de custodia y límite de consultas por IP.")
bullet(doc, "Replanteamos la presentación: de «comparar costos» a un ciclo detectar → decidir → entregar → probar, con la IA como explicadora.")

h1(doc, "5", "Lo que falta validar")
tabla(doc, ("Prueba pendiente", "Para qué sirve"), [
    ("Recorrido sin señal en un teléfono real, con reinicio y sincronización", "Confirmar que el conductor cierra una recepción sin ayuda (H-P4)"),
    ("Sesión con una persona del sector usando alertas, rutas y recepción", "Comprobar que entiende el sistema (V-20)"),
    ("Comparación con datos de una empresa real y una línea base", "Convertir la simulación en ahorro observado"),
    ("Medición de carga, tiempos de respuesta y recuperación ante fallos", "Sostener cualquier afirmación de rendimiento o disponibilidad"),
    ("Aceptación del agente: tres escenarios × diez ejecuciones", "Confirmar latencia y fidelidad de las explicaciones de la IA"),
], (Cm(8.8), Cm(7.8)))
para(doc, "El plan completo define 43 casos de validación (V-01 a V-43). Se ejecutan por prioridad: invariantes de inventario, "
          "autorización, trabajo sin señal y comparación justa primero. Los casos no ejecutados se consideran pendientes; "
          "no se dan por aprobados por inspección de documentos.", 9.5, False, GREY)

guardar(doc, "IstpetDev-Entregable-4-Validacion.docx")
