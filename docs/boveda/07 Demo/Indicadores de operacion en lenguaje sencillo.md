---
tipo: guia-indicadores
estado: propuesta
actualizado: 2026-10-16
tags: [indicadores, operacion, lenguaje-sencillo, pitch, reto-1]
---

# Hoja de indicadores de operación en lenguaje sencillo

**Propósito:** Que cualquier persona del equipo —desde la gerencia hasta quien estiba en la bodega o conduce en la vía— entienda qué miden estos números, para qué sirven y de dónde salen en el sistema. Si una cifra aún no tiene registro en vivo, se marca con honestidad como «sin dato todavía» en lugar de inventar números.

---

## Los 8 indicadores clave de la logística

### 1. Costo por kilómetro
- **Pregunta sencilla:** ¿Cuánto nos cuesta cada kilómetro que rueda el camión?
- **Quién lo usa:** Gerencia de Operaciones y Planificador.
- **Cómo se calcula:** Total de costos de viaje (combustible, desgaste mecánico y mantenimiento) ÷ Total de kilómetros recorridos.
- **De dónde sale el dato en la app:** Proviene del módulo de simulación (`costo_transporte ÷ km_total`) y del cálculo de costo de ruta en el planificador (`/rutas`). En la tarjeta de indicadores de inicio aparece etiquetado como `simulación` (ejemplo ilustrativo: `0,21 USD/km` para camión liviano diésel o `0,075 USD/km` para camión eléctrico).

### 2. Faltantes (quiebres de stock)
- **Pregunta sencilla:** ¿Cuántos clientes o centros de salud se quedaron sin producto para limpiar?
- **Quién lo usa:** Todos los roles (Gerencia, Bodega, Planificador y Receptor).
- **Cómo se calcula:** Cantidad de puntos de atención cuyo inventario llegó a cero o pedidos que no pudieron abastecerse durante el período evaluado.
- **De dónde sale el dato en la app:** En el panel de alertas (`/alertas`) se visualizan los ítems en estado crítico; en la tarjeta de inicio se extrae de la simulación comparativa de 90 días (quiebres evitados frente a la operación tradicional sin planificación anticipada).

### 3. Nivel de servicio (cumplimiento de entrega)
- **Pregunta sencilla:** ¿Cumplimos con lo que prometimos entregar al cliente?
- **Quién lo usa:** Gerencia de Operaciones, Planificador y Conductor.
- **Cómo se calcula:** Total de pedidos entregados completos y a tiempo ÷ Total de pedidos planificados.
- **De dónde sale el dato en la app:** En las rutas individuales se evalúa según las paradas completadas dentro de su ventana horaria. En la tarjeta general de inicio figura como **«sin dato todavía»** mientras no se consolide el historial de recepciones en campo, evitando proyectar un porcentaje irreal.

### 4. Tiempo de ruta
- **Pregunta sencilla:** ¿Cuánto tiempo tarda el camión en hacer todo su recorrido de entrega?
- **Quién lo usa:** Planificador y Conductor.
- **Cómo se calcula:** Hora de retorno a la bodega base − Hora de salida inicial a ruta (suma del tiempo de viaje en calle y el tiempo de descarga y atención en cada parada).
- **De dónde sale el dato en la app:** En la pantalla de rutas (`/rutas`) se muestra la duración estimada a partir de distancias reales por calles (matriz de tramos) y los minutos previstos de atención. En la tarjeta de inicio se rotula con la duración prevista de las rutas publicadas o como **«sin dato todavía»** si no hay rutas activas en la jornada.

### 5. Mermas y producto no conforme
- **Pregunta sencilla:** ¿Cuánto producto se rompió, se derramó o se perdió durante el traslado?
- **Quién lo usa:** Bodega, Gerencia y Auditor.
- **Cómo se calcula:** Cantidad y costo de las unidades rechazadas por daño en empaque o rotura ÷ Total de unidades despachadas.
- **De dónde sale el dato en la app:** En la bandeja de rechazos de rutas (`/rutas`) y en recepciones con novedad (`GET /api/receipts/exceptions`). En la tarjeta de inicio se reporta a partir de los bultos rechazados por motivo de daño físico y su valorización económica (ejemplo ilustrativo: `1 galón rechazado por daño de tapa`).

### 6. Exactitud de inventario
- **Pregunta sencilla:** ¿Lo que dice el sistema en pantalla es exactamente lo que está físicamente en la percha?
- **Quién lo usa:** Bodega y Auditor.
- **Cómo se calcula:** Número de recepciones y conteos físicos sin diferencias ni ajustes ÷ Total de recepciones y conteos efectuados.
- **De dónde sale el dato en la app:** Se obtiene de la conciliación entre las unidades despachadas desde bodega y las unidades recibidas conformes en destino sin reclamo posterior. En la tarjeta de inicio se muestra el porcentaje de recepciones conformes registradas en campo.

### 7. Costo por entrega
- **Pregunta sencilla:** ¿Cuánto nos cuesta poner un pedido completo en la puerta de un cliente?
- **Quién lo usa:** Gerencia de Operaciones y Planificador.
- **Cómo se calcula:** Costo total de servir la ruta (combustible, personal de conducción, ayudantes de estiba requeridos para cargas mayores a 25 kg y merma esperada) ÷ Número de entregas completadas en ese viaje.
- **De dónde sale el dato en la app:** Se calcula en el motor de costos de servicio y se desglosa en el detalle de cada ruta planificada. En el resumen general de inicio se etiqueta como **«sin dato todavía»** hasta que se confirme el cierre del ciclo de entregas de la jornada.

### 8. Flota propia frente a contratada
- **Pregunta sencilla:** ¿Nos conviene hacer la ruta con nuestro camión o contratar a un transportista externo?
- **Quién lo usa:** Gerencia de Operaciones y Planificador.
- **Cómo se calcula:** Costo total de servir la ruta con camión de la empresa (combustible, ayudantes y retorno) − Tarifa estimada del transporte contratado (tarifa base fija + costo por kilómetro + costo por kilogramo transportado).
- **De dónde sale el dato en la app:** Se presenta en la tabla de comparación de opciones de ruta (`/rutas`), permitiendo al planificador y a la gerencia decidir con criterio objetivo cuándo absorber la ruta y cuándo delegarla a un tercero según la distancia y el peso de la carga.

---

## Tabla comparativa de consulta rápida

| Indicador | Pregunta en lenguaje sencillo | Quién lo usa | Origen en la app |
|---|---|---|---|
| **Costo por km** | ¿Cuánto cuesta cada kilómetro que rueda el camión? | Gerencia, Planificador | Simulación y detalle de ruta (`/rutas`) |
| **Faltantes** | ¿Cuántos clientes se quedaron sin producto? | Todos los roles | Alertas de stock crítico (`/alertas`) |
| **Servicio** | ¿Cumplimos con lo prometido al cliente? | Gerencia, Planificador, Conductor | «Sin dato todavía» en panel general; por parada en ruta |
| **Tiempo** | ¿Cuánto tiempo tarda todo el recorrido? | Planificador, Conductor | Duración estimada de tramos OSRM en `/rutas` |
| **Mermas** | ¿Cuánto producto se dañó o perdió en el camino? | Bodega, Gerencia, Auditor | Bandeja de rechazos por motivo «Daño» |
| **Exactitud** | ¿Lo del sistema coincide con lo que hay en la percha? | Bodega, Auditor | Conciliación de recepciones sin novedad |
| **Costo por entrega** | ¿Cuánto cuesta poner un pedido en el cliente? | Gerencia, Planificador | Desglose de costo de servicio por ruta |
| **Propia vs. Contratada** | ¿Conviene usar camión propio o contratar a un tercero? | Gerencia, Planificador | Comparación de opciones A/B/C en `/rutas` |
