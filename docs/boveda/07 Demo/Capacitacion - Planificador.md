---
tipo: guia-capacitacion
estado: propuesta
actualizado: 2026-10-16
tags: [capacitacion, roles, planificador, demo-quito, pitch, reto-1]
---

# Hoja de capacitación operativa: Planificador

**Objetivo del rol:** Organizar los despachos del día para que ningún centro de salud se quede sin insumos, asignando el camión adecuado a cada recorrido y respondiendo con calma y rapidez ante imprevistos en la vía.

## 1. Qué aprende
- Identificar en pantalla qué centros de salud u hospitales están en riesgo de quedarse sin insumos de limpieza y desinfección antes de que el cliente llame a reclamar.
- Comparar las opciones de ruta que propone el sistema para elegir el orden de visitas más conveniente, considerando horarios de recepción estrictos (hospitales y centros comerciales temprano) y la capacidad de peso y volumen del vehículo.
- Reaccionar ante un vehículo averiado o una calle cerrada reasignando las paradas pendientes a otro camión disponible o a un transporte de apoyo sin perder lo ya entregado.
- Publicar la ruta definitiva para que los conductores la reciban en su teléfono móvil.

## 2. Duración
- **2 horas** en sesión práctica guiada con computadora y datos de prueba.

## 3. Ejercicio práctico con el escenario Quito (`demo-quito-planificacion`)
- **Punto de partida:** Bodega Central Sur - Turubamba (`BOD-0001`).
- **Vehículo asignado:** Hino Dutro City, placa `PCB-2214` (`VEH-0001`, capacidad útil 3.500 kg y 14 m³).
- **Paradas a planificar:**
  1. Hospital General Enrique Garcés (`PTO-0005`), ventana de atención 07:00–11:00, con consumo acelerado de desinfectante.
  2. Hospital Carlos Andrade Marín (`PTO-0004`), ventana de atención 06:30–10:30.
  3. Quicentro Norte (`PTO-0013`), ventana estrecha de 07:00–09:30.
- **Paso a paso en la aplicación web (`/rutas`):**
  1. Entrar al módulo de rutas y revisar los puntos pendientes de despacho.
  2. Pulsar el botón **«+ Nueva Ruta por Pedidos»** o consultar la propuesta del sistema con **«🤖 Pedir al Agente IA»**.
  3. Observar la secuencia sugerida sobre el mapa interactivo: comprobar que Quicentro Norte y los hospitales se visitan antes de las 10:00 para no perder la ventana de descarga.
  4. Simular el incidente `averia-matilde-alvarez`: a los 50 km de recorrido, el camión `PCB-2214` sufre una falla mecánica en la Av. Matilde Álvarez (Quitumbe) con paradas aún pendientes.
  5. En pantalla, pulsar **«Proponer Ruta de Emergencia»**: el sistema aísla las paradas no atendidas y sugiere reasignarlas al camión eléctrico `VEH-0004` (BYD T35, placa `PCE-3021`) de la misma bodega.
  6. Revisar el costo y la factibilidad del relevo, aprobar la reasignación y publicar la nueva ruta.

*(Nota: Cifras de distancia y horario corresponden a un ejemplo ilustrativo del escenario de prueba).*

## 4. Cómo se comprueba que aprendió
- El planificador genera por sí mismo una ruta factible para el día que respeta los horarios de entrega de los dos hospitales y del centro comercial sin sobrecargar el camión.
- Ante la simulación de avería mecánica, reasigna las paradas no entregadas en menos de 10 minutos y explica con claridad por qué priorizó el hospital sobre el comercio.

## 5. Referente de dudas por turno
- **Turnos mañana y tarde:** Coordinador de Logística y Tráfico.
- **Canal de soporte de la plataforma:** Equipo de soporte IstpetDev.
