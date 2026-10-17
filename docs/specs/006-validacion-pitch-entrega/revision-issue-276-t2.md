# Issue #276 — entrega T2

Estado: **implementada; revisión manual pendiente**. El usuario indicó continuar después de T1; no se registra una prueba visual de T1 como aprobada porque no comunicó resultados.

## Cómo funciona y qué cambió

`POST /api/v1/route-plans/suggest` conserva su propuesta operativa y añade `comparison`, obtenido del servicio de abastecimiento de #241. Este contrato contiene las alternativas, recomendación, justificación y referencia de combustible. La propuesta incorpora `sourcingOptionId` para identificar qué alternativa originó sus paradas; el identificador operativo anterior (`1`) se conserva.

La tabla consume `comparison` mediante `mapSourcingComparison`. Se retiraron del mapeo en vivo los costos por kilómetro/hora, cantidades de ayudantes, merma fija, críticos, pendientes, riesgos, horas del operador y recomendaciones de respaldo. Tampoco se deduce la modalidad buscando letras o palabras en títulos. Se añadió una fila explícita de tarifa del operador.

La selección del planificador se conserva al entrar a la comparación. Al elegir otra alternativa propia se solicita una nueva propuesta con `optionId`; se limpian paradas, vehículo y evaluación anteriores mientras se prepara. Si la respuesta no corresponde a esa alternativa, no se permite avanzar con otra propuesta. Si se pulsa Publicar recomendada y todavía no está preparada, primero se prepara; después se puede continuar al paso de asignación.

La ausencia se muestra como «sin dato». Un cero explícito se conserva como cero. No se afirma que combustible, desgaste o conductor estén incluidos en una tarifa solo porque la modalidad sea contratada. Las notas recibidas del motor se muestran debajo de la tabla.

La referencia de combustible en vivo muestra valor, unidad, fecha y fuente de la API. La referencia ilustrativa sigue exclusivamente en diseño hasta retirar ese modo en T5. Se quitó el encabezado fijo de bodega/fecha del pitch.

## Correspondencia con el contrato realmente integrado

Los campos siguientes están bajo `comparison` en la respuesta de `/suggest`:

| Elemento visible | Campo recibido |
|---|---|
| Modalidad | `options[].carrierType` |
| Kilómetros | `options[].totalKm` |
| Combustible / desgaste / conductor | `options[].costBreakdown.fuel / wear / driver` |
| Número y costo de ayudantes | `options[].costBreakdown.helperCount / helpers` |
| Merma | `options[].costBreakdown.shrink` |
| Tarifa operador | `options[].costBreakdown.carrierTariff` |
| Total | `options[].costBreakdown.total`, o `options[].totalCost` si el primero falta |
| Críticos a tiempo | `options[].criticalDeliveriesOnTime / criticalDeliveriesTotal` |
| Pendientes y riesgos | `options[].pendingOrdersCount / risks` |
| Recomendación | `recommendedOptionId`; si falta, indicador explícito `options[].isRecommended` |
| Justificación | `options[].justification` o justificación de la respuesta para la recomendada |
| Precio, fecha, fuente, unidad | `fuelPrice.value / date / source / unit` |
| Notas | `options[].costBreakdown.notes` |

**Diferencia frente al texto original del issue:** #241 entrega `fuelPrice` en la raíz de `DemandSourcingResult`, no dentro del desglose de cada opción. Se usa esa ubicación real sin inventar un campo ni una constante. No se mezclan el desglose comparativo de #241 y el desglose operativo del evaluador #240.

Si se conecta una API anterior sin `comparison`, se muestran únicamente distancia y total efectivamente recibidos de la propuesta, se informa la falta del contrato y no se atribuye una recomendación A/B/C.

## Límites detectados

- C se muestra cuando el contrato la devuelve. Si no existe, se indica que no está disponible en esa respuesta y se debe revisar selección/configuración; no se fabrican importes ni se afirma que #241 sigue sin integrar.
- La confirmación vigente usa `vehicleId` y rutas de flota propia. C es consultable pero no se publica por ese flujo. Al seleccionarla se limpia mapa/evaluación/vehículo y se explica la limitación. Esto evita publicar una alternativa contratada como si fuera una ruta propia. La contratación operativa no se implementó en este issue.
- El motor de #241 usa hoy componentes cero y supuestos en su cálculo: entre otros, incrementos fijos para B y una distancia de respaldo para C. El frontend ahora los transmite, no los genera. No se modificó ese algoritmo ni se certifica su validez con esta entrega. La suma de componentes del desglose tampoco debe asumirse completa sin validar el motor.
- `comparison.operationalDate` usa el reloj operativo del servicio. La futura corrida debe fijar y documentar ese reloj además de cualquier fecha solicitada a `/suggest`; no asumir que ambas fechas coinciden automáticamente.
- Agregar `comparison` implica otra consulta al servicio de abastecimiento en `/suggest`; no se cambió su algoritmo. #275 se integró después en `develop` (`33387ca`, PR #283).
- Corrida guardada, aprobación de la selección del pitch y evidencia final siguen pendientes. Esta entrega no acredita una demo final ni sustituye su validación.

## Verificación técnica

- `npm run test --workspace=apps/web`: **159 pruebas aprobadas, 27 archivos**.
- `dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~ContractedCarrierAndRecommendationTests|FullyQualifiedName~OrderBasedRoutingValidationTests" --no-restore`: **11 pruebas aprobadas**.
- Las pruebas cubren ausencias, cero, modalidad independiente de la letra, tarifa/notas, recomendación explícita, referencia de combustible en pantalla, C sin publicación propia, petición de otra alternativa y rechazo de una propuesta que no corresponde.
- Se actualizaron las expectativas anteriores que trataban cero como ausencia y el listado de filas para incluir tarifa del operador.
- `npm run build:web`: aprobado, incluidos ambos pasos de lint. Advertencias de boundaries obsoleto y Leaflet CommonJS, sin errores. No se atribuye al usuario una prueba manual de navegador.

## Revisión manual paso a paso

1. **Reiniciar API y web** para usar el endpoint actualizado: `npm run dev:api` y `npm run dev:web` en terminales separadas, con la configuración local habitual. No se necesita reset para comprobar el mapeo.
2. Abrir `http://localhost:4200/rutas` con rol planificador → **+ Nueva Ruta por Pedidos**. Seleccionar pedidos y avanzar hasta la comparación.
3. En F12 → Network abrir la respuesta de `/api/v1/route-plans/suggest`. Verificar `comparison`, sus opciones y `options[0].sourcingOptionId`. Si falta `comparison`, comprobar que reiniciaste la API con esta rama.
4. Comparar cada fila con la tabla de correspondencias anterior. No exigir tres opciones si la API devuelve menos. Un importe cero debe permanecer cero; un campo ausente debe decir «sin dato».
5. Comparar la referencia de combustible con `comparison.fuelPrice`: valor, unidad, fecha y fuente. Si la configuración no la entrega, comprobar «sin dato»; no introducir un precio manual para hacer pasar la prueba.
6. Comparar la estrella y justificación con `recommendedOptionId` y `justification`. Entrar a la tabla no debe cambiar la selección del planificador a la recomendada.
7. Si llega C, verificar modalidad, tarifa, total, riesgos y notas. Seleccionarla: debe vaciarse el mapa operativo y bloquearse la confirmación propia con una explicación visible. Si no llega C, verificar el mensaje de indisponibilidad.
8. Elegir otra alternativa propia: Network debe mostrar una nueva petición con su `optionId`. El `sourcingOptionId` devuelto debe coincidir antes de permitir continuar. Verificar que no conserva el vehículo de una alternativa anterior.
9. Para comprobar campos ausentes, usar la sustitución de respuesta descrita en la guía T1: retirar `comparison.fuelPrice`, un componente del desglose y tanto `recommendedOptionId` como los indicadores `isRecommended`. Esperado: sin precio inventado, celda «sin dato» y ninguna estrella/recomendación inventada. Retirar la sustitución al terminar.
10. Repetir vacío/error de T1 y revisar el modo proyector. El selector de T5 ofrece En vivo y Corrida guardada; no existe modo de cifras de diseño.

## Resultado del usuario

Pendiente registrar selección utilizada, campos comparados, capturas y observaciones. Corregir T2 si falla alguna comprobación antes de continuar con selección del pitch y generador.
