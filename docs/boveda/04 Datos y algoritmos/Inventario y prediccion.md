---
tipo: especificacion-datos
estado: propuesta
actualizado: 2026-10-16
tags: [datos, algoritmo, validacion]
---

# Inventario y predicción

## Objetivo

Detectar **cuándo debe decidirse una reposición**, con valores verificables, antes de que la demanda no pueda atenderse. Comenzar con un modelo determinista y medible.

La IA explicativa no es el pronóstico. Si se agrega un modelo predictivo después, compararlo contra esta base con datos fuera del período de entrenamiento.

## Entradas

**Contraste sectorial — 16-oct:** [[Hallazgos de la charla para el Reto 1]] documenta entradas en cajas, salidas fraccionadas y promociones, además de consumo por área sin rendimiento cuantificado. Confirmar la unidad física/base por producto y qué representa el consumo antes de usar esos ejemplos como datos. La pregunta sobre cuántos días antes necesitan prever un agotamiento no obtuvo un número; la charla no calibra el lead time ni los cinco días del escenario.

Punto/SKU, stock utilizable, reservas, consumos por día, entregas confirmadas pendientes, lead time, ventana de recepción, stock de seguridad, criticidad y frescura del registro.

No considerar una reposición solo propuesta como stock ya disponible. El mismo producto puede tener lead time distinto según punto y fecha.

## Fórmula inicial

- Demanda diaria estimada = media móvil de consumo observado durante una ventana configurable.
- Cobertura = stock disponible / demanda diaria estimada, si demanda > 0.
- Horizonte de decisión = lead time + margen de seguridad temporal.
- Riesgo si cobertura ≤ horizonte de decisión, o si existe demanda no atendida.
- Cantidad sugerida = máximo(0, demanda estimada × horizonte objetivo + seguridad en unidades − posición de inventario).
- Posición de inventario = stock utilizable + entradas confirmadas − reservas/salidas comprometidas.

Evitar descontar reservas dos veces: para cobertura usar stock disponible; para posición partir del stock utilizable y sus compromisos. Expresar cada cantidad en unidad base.

La cantidad final se limita por stock de bodega, empaques, capacidad de transporte y vencimiento. El sistema debe explicar si no puede cubrirla.

## Ejemplo de la historia

Dataset ilustrativo: un punto tiene 60 L disponibles y consumo estimado de 12 L/día. Cobertura: 5 días. Lead time: 2 días; margen temporal configurado: 3 días. El umbral de decisión es 5 días: la alerta se activa ahora.

Esto permite decir «identifica un riesgo a cinco días de agotar el stock» bajo ese supuesto, no «hemos probado que siempre avisa cinco días antes». Registrar las entradas y comparar con lo que habría ocurrido en la política tradicional.

## Datos insuficientes

| Caso | Respuesta |
|---|---|
| Demanda observada cero | Cobertura no calculable; mostrar sin consumo observado y calidad del dato |
| Falta historial | Usar consumo declarado como supuesto o pedir registro; indicar fuente |
| Stock desactualizado | Alertar incertidumbre; no mostrar precisión inexistente |
| Demanda censurada por faltante | Registrar demanda solicitada y no satisfecha, no solo consumo realizado |
| Estacionalidad | Separar días/servicios cuando haya datos suficientes |
| Vencimiento antes de uso | Excluir cantidad no utilizable y explicar motivo |
| Entrada sin confirmación | No reduce el riesgo como si ya estuviera recibida |

## Prioridad

Orden inicial: interrupción actual → días hasta faltante respecto al lead time → criticidad validada → antigüedad de necesidad. El peso de criticidad se configura como regla de negocio, no lo decide el LLM.

No prometer «cero quiebres» si stock de bodega, flota o acceso no lo permiten. El resultado correcto puede ser una alerta de inviabilidad.

## Anomalías y mermas

Comparar consumo/saldo con historial usando umbral robusto configurable y mínimo de muestras. Tratar variabilidad cero sin dividir por cero; registrar regla y motivo.

Una alerta indica **anomalía por investigar**. Merma confirmada requiere conciliación, evidencia y cierre humano. En datos sintéticos con etiqueta de anomalía, medir precisión y cobertura; no extrapolar a fraude real.

## Calidad del pronóstico

Comparar demanda estimada y real por punto/SKU y período. Usar MAE como base; porcentajes requieren tratamiento de demanda cero. En backtesting, la predicción del día t solo conoce datos anteriores a t.

Guardar snapshotId, inputCutoff, formulaVersion y parámetros. La explicación n8n/DeepSeek enlaza ese snapshot. Ver [[n8n e IA]], [[Simulador y metricas]] y [[Plan de validacion]].

## Contraste adicional: logística integral — recibido el 16-oct

[[Logistica integral para el Reto 1 - hallazgos y evidencia]] explica que la falta de visibilidad puede empezar antes de la recepción en bodega: horarios y especificaciones del proveedor, presentación/ergonomía, catálogo común, movimientos y conteos físicos por muestreo. Propone anticipar necesidades del cliente y del almacén central con históricos y mínimos/máximos; no aporta valores calibrados ni un algoritmo nuevo.

Para contrastar la recomendación, registrar unidad base/conversiones validadas, saldo y fecha, consumo, entradas confirmadas, merma investigada y diferencia físico/digital. El ejemplo de cajas de 12/6 y dimensiones tiene unidades ambiguas: no usarlo como ficha técnica. El inventario “en tiempo real” descrito en la charla es un objetivo operativo; la captura offline debe conservar la distinción entre dato local y saldo confirmado. Estas observaciones orientan V-01/V-03/V-04/V-19, sin certificar ejecución ni modificar fórmulas o contratos.

## Propuesta del 16-oct — medir el error del pronóstico

La lámina de riesgos de la charla recomienda «iniciar con modelos de referencia, medir el error y actualizar los parámetros periódicamente». #248 propone medir el MAE de la media móvil actual con ventanas de 7, 14 y 28 días sobre el dataset propio (#243), sin anticipación artificial. El resultado será sintético. La estacionalidad queda como hoja de ruta del pitch. Ver [[Planificacion potenciada - propuesta del evento]].
