"""Entregable 4 — Validación (Manual §9 y Anexo 5.4). Uso: python3 e4_validacion.py"""
from lib import *

doc = nuevo(4, "Validación", "Qué probamos,", "con qué y qué falta", "Manual §9 y Anexo 5.4")

h1(doc, "1", "¿Qué probamos?")
para(doc, "Probamos el prototipo en cuatro frentes. Cada resultado se presenta con su escenario y su límite; lo que no "
          "se ejecutó se dice como pendiente.", after=6)
tabla(doc, ("Frente", "Qué se hizo", "Resultado", "Límite"), [
    ("Simulación de 90 días", "12 corridas: 3 políticas × 4 escenarios (3 semillas base y 1 adversa)", "Sin ahorro frente al calendario fijo", "Datos sintéticos"),
    ("Comparación A/B/C", "Ensayo técnico del motor en el escenario de Quito", "Recomienda la opción C", "Local; no en producción"),
    ("Agente de IA", "Corrida registrada: riesgos, abastecimiento y rutas con explicación", "Completó, con caída al respaldo", "Una corrida registrada"),
    ("Despliegue en producción", "Verificación de salud el 17-oct a las 08:28", "Todo en HTTP 200", "Salud técnica, no uso por usuarios"),
], (Cm(3.4), Cm(5.6), Cm(3.8), Cm(3.8)), tam=9)

h1(doc, "2", "Simulación de 90 días")
para(doc, "Estas son las escalas que usamos en todo el paquete; cada cifra se refiere a una sola de ellas.", after=4)
tabla(doc, ("Uso", "Escala"), [
    ("Comparación A/B/C", "Escenario sintético de Quito: 40 puntos; el ensayo usa 4 pedidos y 6 entregas críticas"),
    ("Simulación de 90 días", "El mismo escenario: 40 puntos, 3 bodegas, 10 vehículos y 12 productos"),
    ("Demostración en vivo", "Región de demostración: unos 15 puntos y 2 vehículos"),
], (Cm(4.4), Cm(12.2)), tam=9)
para(doc, "", after=2)
para(doc, "Se comparó el escenario sintético de Quito importado en la base de datos (12 de los 40 puntos usan referencias de "
          "establecimientos públicos), sobre 90 días. Semillas base 42, 101 y 2024; escenario adverso 9999, con un vehículo "
          "retirado (VEH-0001). Corrida del 17-oct, commit ceeaf03; cifras en mediana de las semillas base.", after=6)
tabla(doc, ("Política", "Costo de transporte", "Km totales", "Entregas", "Quiebres"), [
    ("Reactiva: repone cuando ya falta", "USD 8 507,86", "9 699,4", "1 966", "16 608"),
    ("Calendario fijo semanal", "USD 4 889,23", "5 589,1", "1 611", "16 117"),
    ("IstpetDev: preventiva", "USD 6 077,22", "6 571,2", "939", "16 589"),
], (Cm(5.4), Cm(3.6), Cm(2.6), Cm(2.4), Cm(2.6)))
bullet(doc, [("Frente al calendario fijo semanal: ", True), ("no hay ahorro. IstpetDev cuesta más (USD 6 077,22 frente a USD 4 889,23), "
             "recorre más km y tiene más quiebres netos en las semillas base. En el escenario adverso la diferencia neta de quiebres "
             "se invierte (+168 a favor de IstpetDev).", False)])
bullet(doc, [("Frente a la política reactiva: ", True), ("IstpetDev gasta menos en transporte, pero también entrega menos veces "
             "(939 frente a 1 966) y su diferencia neta de quiebres no mejora (−20 en la mediana).", False)])
bullet(doc, [("Puntualidad: ", True), ("similar entre políticas (38,6 % en IstpetDev y reactiva, 40,4 % en el calendario fijo).", False)])
aviso(doc, "Cómo leer estas cifras:", "es una simulación con datos sintéticos, no un ahorro observado. No sostiene que IstpetDev reduzca "
                                      "el costo frente a un calendario fijo: ese efecto, y su relación con los quiebres, se medirá con datos "
                                      "de una empresa en el piloto. Una versión anterior de este entregable citaba un 9,8 % de ahorro "
                                      "calculado sobre otro conjunto de datos; se retira.")
para(doc, "Las corridas se reproducen con las semillas fijadas y el dataset `demo-quito-planificacion`; el resumen queda en "
          "`docs/evidence/simulation-quito/` y las cifras comentadas en la nota «Cifras del simulador 90 días».", 9.5, False, GREY)

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
bullet(doc, "Retiramos la cifra de 9,8 % de ahorro: venía de otro conjunto de datos y no se sostiene con la corrida del 17-oct sobre el escenario de Quito.")
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
