---
tipo: guia-capacitacion
estado: propuesta
actualizado: 2026-10-16
tags: [capacitacion, roles, gerencia, demo-quito, pitch, reto-1]
---

# Hoja de capacitación operativa: Gerencia de Operaciones

**Objetivo del rol:** Supervisar la salud de toda la operación logística con indicadores claros y comprensibles, identificar cuellos de botella antes de que se vuelvan crisis y tomar decisiones de flota y costos respaldadas con datos.

## 1. Qué aprende
- Leer el panel ejecutivo diario «Hoy» para conocer el estado general de abastecimiento en la red de clientes y hospitales.
- Interpretar los 8 indicadores clave de la operación sin jerga técnica: costo por km, faltantes, servicio, tiempos, mermas, exactitud de inventario, costo por entrega y propia frente a contratada.
- Distinguir de dónde proviene cada número en pantalla (si viene de una simulación de 90 días, de recepciones reales en campo o si aún no cuenta con dato registrado).
- Evaluar la conveniencia económica y operativa de utilizar flota propia (diésel vs. camiones eléctricos) o contratar un transporte externo.
- Monitorear el nivel de mermas y rechazos para tomar medidas preventivas con los proveedores o en la estiba.

## 2. Duración
- **30 minutos** en sesión de revisión directiva del tablero interactivo.

## 3. Ejercicio práctico con el escenario Quito (`demo-quito-planificacion`)
- **Escenario de evaluación:** Jornada matutina de distribución en Quito y los valles.
- **Paso a paso en la aplicación web (`/inicio` y `/rutas`):**
  1. Ingresar a la pantalla de **Inicio** («Hoy») y revisar los centros de salud activos (ejemplo ilustrativo: red de 12 establecimientos de salud).
  2. Revisar la tarjeta de **Riesgo de Stock**: constatar cuántos puntos están marcados en rojo (críticos) y requieren reposición inmediata (ejemplo: Hospital General Enrique Garcés, `PTO-0005`).
  3. Revisar el indicador de **Distribución**: verificar las rutas planificadas frente a las rutas ya publicadas y en tránsito.
  4. Pasar al módulo de **Rutas** (`/rutas`) y abrir la tabla de comparación de opciones:
     - Evaluar la opción con camión liviano diésel `VEH-0001` (costo por km de 0,21 USD/km, ejemplo ilustrativo).
     - Comparar contra la opción de camión eléctrico `VEH-0004` (BYD T35, placa `PCE-3021`, costo de energía de 0,075 USD/km).
     - Comparar contra la cotización del operador contratado para la misma ruta (ejemplo ilustrativo: tarifa base más costo por kilómetro y kilogramo transportado).
  5. Concluir cuál es la alternativa más eficiente considerando no solo el combustible, sino también las restricciones horarias y las horas de llegada a los clientes críticos.

*(Nota: Valores monetarios y distancias corresponden a un ejemplo ilustrativo del escenario de prueba).*

## 4. Cómo se comprueba que aprendió
- La persona a cargo explica en 2 minutos el balance de la jornada utilizando sus propias palabras, señalando qué bodega presenta mayor demanda y justificando si conviene operar con flota propia o tercerizada.
- Reconoce con facilidad la etiqueta de origen de cada métrica (simulación, campo o sin dato) sin dar por reales cifras simuladas.

## 5. Referente de dudas por turno
- **Soporte directivo y metodológico:** Líder de Proyecto / Product Owner de IstpetDev.
- **Consultas sobre cálculos y orígenes:** Equipo técnico y analistas de datos.
