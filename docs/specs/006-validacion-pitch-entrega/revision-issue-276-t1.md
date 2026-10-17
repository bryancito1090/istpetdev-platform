# Issue #276 — entrega T0/T1 y revisión manual

Estado: **implementada; pendiente de comprobación del usuario**. No se ha probado manualmente contra la API ni se ha generado una corrida.

## Qué se hizo

- Se actualizó la rama `fix/276-comparacion-abc-datos-reales` con `origin/develop` mediante avance directo, conservando el plan local. Base actual: `ef31353`.
- Se verificó la integración de #241. No se implementó trabajo de #275 ni se ejecutó reset del dataset.
- La comparación inicia en API en vivo. Una lista vacía ya no recupera opciones de diseño.
- Una respuesta vacía muestra «Sin opciones calculadas para esta selección» y el mensaje del servidor; si falta, indica que la API no proporcionó motivo.
- Un error muestra el mismo estado sin opciones con su motivo y permite volver atrás para intentar de nuevo.
- Antes de solicitar opciones y al abrir el asistente se limpian opciones, selección, evaluación, vehículo y mapa anteriores.
- No se permite avanzar/publicar sin opciones reales. El diseño solo se activa manualmente, muestra una advertencia dentro de la tabla y no permite publicar.
- La referencia fija de combustible se oculta en vivo. Su reemplazo con datos del contrato corresponde a T2.

Archivos funcionales: `rutas.page.ts`, `rutas.page.spec.ts` y `shared/api/api.service.ts` (se tipó el mensaje opcional de `/suggest`).

## Verificación ejecutada

- Suite web: **26 archivos y 157 pruebas aprobadas**. La ejecución inicial intentó filtrar desde el script raíz, pero npm interpretó `--include` como configuración y ejecutó la suite completa; el resultado anterior corresponde a toda la suite.
- Comando directo para repetir la suite: `npm run test --workspace=apps/web`.
- `npm run build:web`: aprobado, incluidos los pasos de lint. Advertencias de regla obsoleta de boundaries y dependencia CommonJS de Leaflet; sin errores.
- `git diff --check`: aprobado.
- Pruebas de regresión: inicio sin maqueta, rótulo al activar diseño, publicación bloqueada en diseño, respuesta vacía con motivo, limpieza de selección/vehículo y error de API sin respaldo.

## Cómo comprobarlo manualmente

Preparación: usar API local y sesión con rol planificador. Si los servicios no están iniciados, ejecutar en terminales separadas `npm run dev:api` y `npm run dev:web` con la configuración habitual del proyecto. Abrir `http://localhost:4200/rutas`. No hace falta resetear la base para estos controles.

### 1. Inicio y modo diseño explícito

1. Abrir el asistente de planificación por pedidos desde Rutas.
2. Seleccionar pedidos y avanzar al abastecimiento; después ir a la comparación.
3. Comprobar que el botón dice «API en vivo». No debe aparecer automáticamente el conjunto ilustrativo de tres opciones.
4. Este recorrido quedó obsoleto en T5: el control actual permite elegir En vivo o Corrida guardada, sin cifras de diseño.
5. Activar proyector y comprobar que el origen de datos sigue visible en la tabla.
6. Elegir En vivo y cerrar/abrir el asistente: debe iniciar en vivo.

### 2. API sin opciones, con motivo

Para un resultado repetible sin modificar la base, usar Chrome/Edge DevTools con sustitución local de respuesta:

1. Abrir F12 → Network, habilitar Preserve log y realizar una consulta de sugerencias.
2. Localizar la petición que termina en `/api/v1/route-plans/suggest`.
3. Clic derecho → Override content (sustituir contenido). Elegir una carpeta temporal fuera del repositorio y conceder acceso al navegador si lo solicita.
4. Sustituir el cuerpo por `{"options":[],"message":"Sin abastecimiento para la selección de prueba."}` y guardar. Confirmar que la sustitución está habilitada y responde con estado 200.
5. Volver al paso de pedidos y recalcular. Debe abrir la comparación con «Sin opciones calculadas para esta selección» y el motivo de prueba; sin tabla, recomendación, referencia fija de combustible ni cifras ilustrativas. Continuar/publicar quedan bloqueados.
6. Sustituir por `{"options":[]}`, guardar y repetir: debe decir «La API no proporcionó un motivo.».
7. Desactivar/eliminar la sustitución en Sources → Overrides antes del siguiente caso. Si el navegador no ofrece sustituciones, registrar esa limitación; la prueba automatizada ya cubre ambos caminos, pero no sustituye tu comprobación visual.

### 3. Error de red y datos anteriores

1. Con sustituciones desactivadas, obtener una respuesta real con opciones.
2. Volver al paso de pedidos.
3. En Network, clic derecho sobre `/suggest` → Block request URL. Mantener DevTools abierto y recalcular.
4. Debe verse el estado sin opciones y un mensaje de conexión; no deben reaparecer resultados anteriores ni cambiarse automáticamente a diseño. No se permite continuar.
5. Desbloquear la petición en Network request blocking. Volver al paso de pedidos y recalcular: el flujo debe recuperarse.

## Límite de esta entrega y hallazgo para T2

T1 elimina la caída silenciosa; **todavía no certifica los importes en vivo**. El mapeo conserva rellenos que serán retirados en T2 y la maqueta manual se eliminará en T5.

En la base actual, `/suggest` devuelve `options = new object[] { singleOption }`, una propuesta canónica, y un desglose de logística. `SourcingService` sí define alternativas en `DemandSourcingResult` con `RecommendedOptionId`, `Justification` y `FuelPrice`; sus opciones incluyen `CarrierType`, métricas y `OptionCostBreakdownDto`. Este último contiene `Fuel`, `Wear`, `Driver`, `Helpers`, `HelperCount`, `Shrink`, `CarrierTariff`, `Total`, `PendingDemandCost` y `Notes`, pero no un `FuelPrice` interno. Por tanto, el contrato real no coincide aún con la suposición del issue de obtener todo desde `options[].costBreakdown.fuelPrice` en `/suggest`.

T2 debe resolver explícitamente esta conexión sin sustituir datos ausentes ni duplicar el trabajo de #241. Este hallazgo no bloquea la validación de T1.

## Resultado del usuario

| Caso | Estado |
|---|---|
| En vivo inicial y diseño solo manual, rotulado | Pendiente |
| Vacío con motivo y sin motivo | Pendiente |
| Error sin resultados anteriores ni respaldo automático | Pendiente |
| Recuperación al desbloquear API | Pendiente |

Tras recibir el resultado, corregir T1 si hace falta; avanzar a T2 solo después de su comprobación.
