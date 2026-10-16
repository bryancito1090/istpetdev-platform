# Verificación de integración de punta a punta — Issue #207 (16-oct-2026)

Verificación pedida por Bryan sobre la épica
[#207](https://github.com/bryancito1090/istpetdev-platform/issues/207). Por
decisión explícita, este registro solo verifica: no implementa ni cambia código y
**no cierra la épica**. El propio issue indica que durante el evento no abre
implementación propia, que se cumple con #193, #194, #195, #196, #201 y #210, y
que vehículo pesado/normal e incertidumbre del costo quedan para después del
evento por falta de datos.

- **Fecha:** 2026-10-16, 15:10–15:30 (Guayaquil)
- **Rama:** `feature/207-propuesta-unica-e2e`
- **Código evaluado:** `origin/develop` `42bd1b0` (incluye #193 `5cfc657`,
  #194 `a602edd`, #196/#201 `c4e6672`/`5fc378b`, #197 `621fdc0` y #199 `0c296bf`)
- **Checklist de referencia:** [E2E-08](../../006-validacion-pitch-entrega/test-e2e.md#e2e-08--flujo-de-punta-a-punta-propuesta-única-por-prioridad-bodega-vehículo-hora-límite-y-explicación-verificable-issue-207)

## 1. Alcance y registro `istpetdev-prelaunch`

| Control | Estado | Detalle |
|---|---|---|
| Alcance | Local aislado | Suite backend con `WebApplicationFactory` y PostGIS descartable (Docker 29.8.1); scripts de n8n sin credenciales. Sin RDS, Cognito, OSRM público, n8n vivo ni LLM. |
| Cambios de código | No aplica | Solo documentación. No se declara lista ninguna versión ni se despliega. |
| RLS, campos/plantillas, datos clasificados, caminos especiales | No aplica | No hay código nuevo; se reutiliza la evidencia vigente de #194 y #113. |
| Dependencias | Reutilizado | La auditoría npm/.NET corre en CI del PR; no se cambian lockfiles. |
| Ejecución real en n8n/producción | Pendiente | No se ejecutó el recorrido panel → n8n → API → panel. Queda para la validación del sábado 11:30. |

## 2. Pruebas ejecutadas

| Comando | Resultado |
|---|---|
| `dotnet build IstpetDev.sln -c Release` | 0 errores |
| `dotnet test … --filter Category=Unit` | 220/220 |
| `dotnet test … --filter Category=Integration` | 265/265 (PostGIS descartable) |
| `dotnet test … --filter Category=E2E` | 23/23 (total 508/508) |
| `python3 -I scripts/agent/build-workflows.py --check` | sin diferencias |
| `tests/scripts/test_*.py` | 7/7 |

Pruebas dirigidas que respaldan el estado parcial:
`DeliveryStopSequencerTests` (prioridad, fecha y desempate),
`RouteSuggestPriorityTests` (Critical antes que Normal con ventanas reales),
`ConfirmCapacityTests` (sobrepeso, vehículo omitido/desconocido, parcial explícito),
`PartialCapacityPlanningTests` y `PartialLoadTests` (#199), tres casos de hora
límite en `RoutePlannerTests` (#193) y `LogisticsQueriesTests` (#194).

## 3. Dependencias declaradas

| Issue | Estado GitHub | Entrega en `develop` |
|---|---|---|
| #193 hora límite | Cerrado | Integrada (`5cfc657`) |
| #194 consultas stock/origen/vehículos | Cerrado | Integrada (`a602edd`) |
| #195 herramientas n8n | **Abierto** | PR #213 en borrador y con conflictos; n8n aún no usa las consultas de #194 |
| #196 prioridad/stock/costo en Rutas | Cerrado | Integrada |
| #199 carga parcial | Abierto (PR #216 fusionado) | Integrada (`0c296bf`); falta cerrar el issue |
| #201 `/suggest` y `/confirm` | Cerrado | Integrada |
| #210 selección y priorización en el asistente | **Abierto** | Sin PR |

## 4. Criterios de aceptación de #207

Leyenda: **Cumple** · **Parcial** (existe una parte verificada) · **No cumple**.

| # | Criterio | Estado | Evidencia en `42bd1b0` |
|---|---|---|---|
| 1 | Una salida | No cumple | `POST /route-plans/suggest` devuelve `options[]` (A/B) con candidatos de vehículo (`RoutePlansController.SuggestRoutes`); el panel pinta tarjetas «Opción A/B» (`rutas.page.ts`). No hay `requestId`, `proposalId` ni revisión de propuesta; repetir `/confirm` con saldo pendiente crea otra revisión del plan. |
| 2 | Prioridad | Parcial | `DeliveryStopSequencer` ordena Critical > High > Normal > Low, fecha requerida y vecino más cercano (pruebas citadas). Pendiente: `/confirm` acepta la secuencia del cliente sin comprobar ese orden; llegar tarde a una ventana solo genera advertencia en `RouteEvaluator`; el motor con hora límite (#193) sí rechaza con `time_window`. |
| 3 | Vehículo normal | Parcial | `VehicleMatcher` descarta por peso/volumen, pero ordena por `EstimatedTotalCost` = Haversine(depósito, primera parada) × tarifa/km, no por costo completo. No existe clase normal/pesado. Sin fixture del caso 20 kg/0,20 m³. |
| 4 | Necesita pesado | Parcial | Descartes `excede_peso` y `excede_volumen` funcionan cuando hay límite positivo. Sin fixture 500 kg/3 m³ ni caso liviano-voluminoso; sin clase de vehículo. |
| 5 | Dimensiones / NeedsData | No cumple | Sin largo/ancho/alto en producto ni vehículo. `MaxVolume` nulo o peso 0 se tratan como «sin límite» (`PartialLoadTests.Without_free_capacity_nothing_is_taken_and_without_limits_everything_is`); no hay estado `NeedsData`. Diferido por falta de datos. |
| 6 | Capacidad insuficiente | Parcial | #199: parcial explícito con remanente y sin duplicar lo ya planificado (`PartialCapacityPlanningTests`, `ConfirmCapacityTests`). No se modelan bultos indivisibles. |
| 7 | Manifiesto íntegro | No cumple | `/confirm` asigna toda la carga a la primera recogida; no persiste origen/lote/cantidad por bodega; la publicación toma la última recogida como origen. Sin prueba de dos bodegas intercaladas. |
| 8 | Revalidación | Parcial | Rechaza vehículo omitido, desconocido o inactivo y cantidades mayores que el saldo. No comprueba vehículo ocupado ni revisión/snapshot; no revalida stock al confirmar (sí al publicar); horarios por índice (`i × 20 min`). |
| 9 | Costo consistente | No cumple | Motor: km × tarifa + horas × tarifa con salida/regreso. Evaluador del asistente: solo entre paradas, sin depósito ni peajes. Matcher: solo aproximación. Tarifas predeterminadas 0,45 USD/km y 6 USD/h no verificadas. Sin calculador único ni fixture de 87 unidades. |
| 10 | Incertidumbre honesta | No cumple | No existe `uncertainty` en ninguna respuesta. Por ahora tampoco aparece un porcentaje inventado. Diferido: no hay viajes reales conciliados. |
| 11 | Riesgos y restricciones | Parcial | Avería/cierre/demora conocidos afectan al motor y al replanteo; el asistente por pedidos no los consulta. Sin fuente/fecha para deterioro vial ni marca de caché antigua. |
| 12 | n8n E2E | No cumple | Vigía/incidente llaman `POST /route-plans` (reposición por riesgo), no `/suggest`/`/confirm`. #195 sin fusionar. El panel llama directamente a la API. Sí existen HMAC, callback, validador de cifras y plantilla de respaldo (#36/#37/#113). |
| 13 | Evidencia de cierre | No cumple | Depende de los puntos anteriores y de la validación del sábado. |

## 5. Checklist E2E-08 (verificación estática y de pruebas)

| Paso | Estado | Nota |
|---|---|---|
| Prerrequisitos #193 y #194 en `develop` | Verificado | Ambos fusionados. Entorno con `demo-region` solo en pruebas descartables. |
| A.1 Pedido con destino, SKU, cantidad y prioridad | Verificado | `DeliveryOrder`/`DeliveryOrderLine`. |
| A.2 Ventana del punto | Verificado | `/suggest` usa la ventana real del punto (#197). |
| B.1 Stock por bodega | Verificado | La ruta real es `GET /api/v1/logistics/stock`, no `/warehouses/stock`. |
| B.2 FEFO y stock de seguridad | Verificado en pruebas | `LogisticsQueriesTests`. |
| B.3 Faltante explícito | Verificado | `pendingQuantity`/`fullySourced` por línea. |
| C.1 Flota activa | Verificado | La ruta real es `GET /api/v1/logistics/vehicles/available`. |
| C.2 Peso/volumen | Parcial | Solo con límite positivo; un límite nulo no se evalúa. |
| C.3 Tarifas y jornada | Parcial | Tarifa/km por vehículo; tarifa/h y jornada son globales y predeterminadas. |
| D.1–D.5 Hora límite | Verificado en pruebas | Tres casos de `RoutePlannerTests`; motor de reposición, no asistente por pedidos. |
| E.1 Métricas | Verificado | km, minutos y costo en plan y evaluación. |
| E.2 Costo de priorizar | Parcial | Comparación en el panel (#196); no usa un calculador común con el motor. |
| E.3 `facts` | Verificado | Flujo de agente existente (#36). |
| F.1 Herramientas n8n con `X-Agent-Run-Id` | Pendiente | Depende de #195. |
| F.2–F.3 Explicación respaldada y rechazo de cifras | Verificado en pruebas existentes | Validador de explicación y plantilla (#36/#39). |
| F.4 Respaldo ante fallo | Verificado en pruebas existentes | #39/#113; sin ejecución viva en esta corrida. |

## 6. Conclusión

La integración del sábado puede mostrar prioridad, stock por bodega, filtro de
capacidad, hora límite y parciales con remanente. Lo que no puede afirmarse hoy:
propuesta única, costo completo y consistente, vehículo por idoneidad o
dimensiones, manifiesto multibodega íntegro, incertidumbre ni orquestación del
asistente por n8n. **#207 sigue abierto.** Siguen siendo bloqueantes #195 y #210,
además del trabajo propio posterior al evento (contrato de propuesta, calculador
único, revalidación por revisión y datos operativos de vehículos y costos).
