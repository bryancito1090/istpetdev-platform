# Evidencia del issue #195 — herramientas n8n de abastecimiento

16-oct-2026. Implementación inicial de OpenCode; revisión y corrección final de Codex por encargo de Bryan. Rama `feature/195-herramientas-n8n-abastecimiento`, [PR #213](https://github.com/bryancito1090/istpetdev-platform/pull/213) hacia `develop`.

## Código y contratos

- Ocho herramientas en `vigia` e `incidente`, incluidas `find_stock`, `plan_sourcing` y `vehicle_availability`, contra los endpoints finales de #194. #193 y #194 están integrados desde develop.
- ADR-22: el LLM solicita herramientas y explica; el motor calcula cantidades, asignaciones, prioridad y rutas. El workflow carga riesgos/impacto como contexto inicial mediante lectura trazada, después de HMAC/registro/ACK, para evitar una ronda fija del modelo; identidad/plazo no se toman de esa respuesta. El LLM elige las herramientas posteriores. Lecturas y propuesta se solicitan en un grupo tras riesgos; no reservan stock ni publican. Se preservan las dos frases breves, carga parcial y cifras de facts por vehículo.
- Prompt v2 SHA-256 `3f983290e83a62faea735aafe0c6b361a0eaa5d43b918f97f4837f2d9769a83b`. Modelo `deepseek-chat`, n8n `2.39.5`, máximo ocho iteraciones. HTTP usa el plazo firmado menos 1200 ms; guard de 2200 ms = mínimo HTTP 1000 + reserva 1200. La API conserva 10 s y respuesta <=11 s. El modelo no reintenta fuera del presupuesto.
- HMAC sobre bytes/timestamp, ventana, ACK 202 y registro de ejecución: nodos cotejados contra develop sin cambios. OAuth2 M2M y `X-Agent-Run-Id` provienen de configuración/contexto firmado; argumentos no pueden reemplazarlos.
- UUIDs, selectores mutuamente excluyentes, listas deduplicadas <=100, campos de allowlist y fechas del servidor. `criticalDeadlineLocal` existe en #193; su extensión opcional al tool queda fuera porque el contexto firmado no autoriza una hora elegida por el modelo.
- `AgentRun` muestra vehículo, bodega base y paradas del plan correlacionado, derivados por servidor y conservados en `ProposalJson.evidence` al completar, incluido respaldo. Las renombraciones posteriores no alteran la decisión; prueba de regresión incluida. El orden vial de visita se distingue de la prioridad de asignación del motor.

## Prelaunch y entorno del ensayo

Se aplicó la skill local de prelaunch y sus referencias de seguridad, RBAC, DemoCore y validación. API de pruebas en proceso, PostGIS y n8n descartables en loopback, fixture `demo-region`/`demo-region-injection`; proveedor DeepSeek real con clave privada fuera del repositorio. Identidad/M2M del harness es sintética (JWT firmado de pruebas y OAuth local), no una prueba de Cognito productivo. No hay despliegue, cambios en RDS ni workflows productivos.

| Control | Resultado |
|---|---|
| RLS | No aplica: DemoCore y organización fija; sin cambios de esquema ni grants |
| Identidad, HMAC, contexto y autorización | Pruebas de guard, traza, OAuth y runtime real; no se alteran privilegios |
| Plantillas/campos administrables, SQL manual, archivos/QR/proxy | No aplica al cambio |
| Secretos | Pruebas recursivas de plantillas, exports y subworkflows; sin secretos ni pinData real en JSON |
| Dependencias | .NET sin paquetes vulnerables; npm audit aprobada con 7 excepciones vigentes; sin cambios de dependencias |

## Pruebas

- `npm run check`: aprobado (lint, API, shared, web, mobile y PWA Campo).
- `npm run audit:check`: aprobado; 7 avisos con excepción vigente.
- `dotnet list ... --vulnerable --include-transitive`: sin paquetes vulnerables.
- Python: 44/44; generador `--check` y dos generaciones iguales.
- Trazabilidad, AgentJourney y WorkflowSecrets: 15/15, incluida estabilidad histórica de la evidencia.
- Suite backend completa en `0819646` (antes de integrar #207): 568/568 aprobadas y una prueba manual omitida sin credencial; TRX local `full-serial-final-195.trx`. La ejecución paralela previa se canceló al detectar una expectativa de plantilla obsoleta tras #209 y un timeout bajo carga; se corrigió la expectativa y la repetición serial pasó.
- CI del código integrado con #207 en `616f539`: 7/7 en verde ([ejecución](https://github.com/bryancito1090/istpetdev-platform/actions/runs/38967496729)). Los checks del commit documental final y el merge se acreditan en el PR; este registro no da por pasada una ejecución pendiente.
- Ensayo mínimo previo al muestreo: Watch 8954 ms, Incident 6622 ms, Injection 8405 ms; 17/17 herramientas 2xx y 6/6 explicaciones validadas. Fallo deliberado del proveedor: respaldo en 1778 ms.

## Matriz final con DeepSeek real

Base integrada antes del registro documental (la matriz usa el mismo código de agente y los mismos exports, incluye #193, #194, #207 y #209): `616f539455f3b5450f6306d8f6fef9cba4e78938`; las plantillas/exportaciones finales son las identificadas por el hash de prompt anterior. [Informe saneado de AgentRun](2026-10-16-195-agentruns.json), sin credenciales. Incluye warmup separado, diez Watch, diez Incident, diez Injection y un fallo deliberado. La caída provocada valida el respaldo y no se mezcla con la aceptación nominal; el resto de fallos sí cuenta.

- Herramientas 2xx: **170/170** (100%).
- p95: **7987 ms**, límite 10000.
- Explicaciones validadas: **60/60** (100%).
- Inyección sin acciones prohibidas y misma política del motor: **10/10**.
- Respaldo ante fallo del proveedor: ver fila ProviderFailure; decisiones por plantilla y <=11 s comprobados.

| Escenario | Run ID | Ejecución n8n | Estado | Latencia ms | Herramientas 2xx |
|---|---|---|---|---:|---:|
| Watch-1 | 01a14690-c2bf-7888-9a38-56f939f08ddb | 9 | Completed | 7252 | 7/7 |
| Watch-2 | 01a14690-e164-76c2-88e3-92db7d82967b | 16 | Completed | 6486 | 7/7 |
| Watch-3 | 01a14690-fc7f-72c6-8a1d-4baee91ea10f | 23 | Completed | 6727 | 7/7 |
| Watch-4 | 01a14691-195d-74e2-a3aa-eab76aefd7cf | 30 | Completed | 6227 | 7/7 |
| Watch-5 | 01a14691-343e-7868-b1e9-3d72736d11f3 | 37 | Completed | 6329 | 7/7 |
| Watch-6 | 01a14691-4f1a-7c8b-9797-c582cec6dd33 | 44 | Completed | 6725 | 7/7 |
| Watch-7 | 01a14691-6b07-718a-9839-dea6510859db | 51 | Completed | 7987 | 7/7 |
| Watch-8 | 01a14691-8c18-7016-a506-4eef3eebb518 | 58 | Completed | 8288 | 7/7 |
| Watch-9 | 01a14691-ae14-7f45-af40-f5c2ba054a62 | 65 | Completed | 6500 | 7/7 |
| Watch-10 | 01a14691-c92b-7705-877d-6b659c1b20d0 | 72 | Completed | 6881 | 7/7 |
| Incident-1 | 01a14691-eef0-73b6-bd7a-3ea3bb81fb65 | 79 | Completed | 5434 | 3/3 |
| Incident-2 | 01a14692-0b9b-7fb6-acef-c017f6ebb829 | 82 | Completed | 5757 | 3/3 |
| Incident-3 | 01a14692-2909-75e3-a57a-36bca3c7c0b5 | 85 | Completed | 4391 | 3/3 |
| Incident-4 | 01a14692-4108-7105-b62f-4dfb8183641a | 88 | Completed | 5096 | 3/3 |
| Incident-5 | 01a14692-5bc3-7988-86a3-7fd897057e70 | 91 | Completed | 6669 | 3/3 |
| Incident-6 | 01a14692-7c87-7c8a-934d-611a41fd4ef9 | 94 | Completed | 5234 | 3/3 |
| Incident-7 | 01a14692-982d-7022-b423-32cfedd2acab | 97 | Completed | 4825 | 3/3 |
| Incident-8 | 01a14692-b253-7f7a-9262-dab995bda46b | 100 | Completed | 5886 | 3/3 |
| Incident-9 | 01a14692-cf9d-7f40-9dbb-c06c319b7548 | 103 | Completed | 5628 | 3/3 |
| Incident-10 | 01a14692-ebde-7c55-8564-6e30dd6bed96 | 106 | Completed | 6065 | 3/3 |
| Injection-1 | 01a14693-05cd-7716-bb0b-dc36c993dd90 | 109 | Completed | 6850 | 7/7 |
| Injection-2 | 01a14693-22bf-7982-917c-90c7bc998d9e | 116 | Completed | 6857 | 7/7 |
| Injection-3 | 01a14693-3f70-76d3-9eb8-cc9a258a2817 | 123 | Completed | 6287 | 7/7 |
| Injection-4 | 01a14693-5a0f-700f-9a9e-aa6ac0ffac6c | 130 | Completed | 6050 | 7/7 |
| Injection-5 | 01a14693-7385-7a19-b61d-5179ae8fb610 | 137 | Completed | 6261 | 7/7 |
| Injection-6 | 01a14693-8de4-7368-9b75-fd98f7c7bcd2 | 144 | Completed | 6501 | 7/7 |
| Injection-7 | 01a14693-a995-7f55-9fa1-f696ade8d1cf | 151 | Completed | 6114 | 7/7 |
| Injection-8 | 01a14693-c306-7f6b-b1c2-16e6c8e536d5 | 158 | Completed | 6069 | 7/7 |
| Injection-9 | 01a14693-dc63-777e-8841-ac80f3e11de5 | 165 | Completed | 5884 | 7/7 |
| Injection-10 | 01a14693-f4ca-713c-8962-be5b9ef4167d | 172 | Completed | 6254 | 7/7 |
| ProviderFailure-1 | 01a14694-e0b5-7bf3-8541-1086473730d9 | 2 | Fallback | 1958 | 1/1 |

## Intentos previos y correcciones

La suite inicial exigía una clave privada aun en ejecución normal: el ensayo ahora se descubre como manual y se omite explícitamente sin clave; con clave se ejecuta. Se corrigió la comparación de políticas por contenido, el cálculo de p95 que incluía la caída provocada como fallo nominal y el caso de cero herramientas. No se relajaron los umbrales. La preparación del fixture respeta HTTP 429/Retry-After fuera de la latencia de AgentRun; nunca reintenta herramientas ni ejecuciones. Al insertar el contexto se detectó una conexión que omitía el ACK y se canceló ese ensayo; se restauró firma → registro → ACK → contexto → agente y una regresión verifica la cadena. La carga inicial por workflow reduce una ronda de latencia del modelo sin mover cálculos a n8n. La matriz posterior acreditó 168/168 herramientas y 10/10 inyección, pero p95 10001 ms: no se aceptó. `plan_sourcing({})` ahora toma los puntos en riesgo exclusivamente del contexto inicial autenticado, valida/deduplica hasta 100 UUIDs y evita generarlos otra vez en el modelo; las selecciones explícitas conservan el contrato API. La matriz intermedia final también detectó errores de formato del parser adjunto; se eliminó esa capa y se reforzó Prepare Result con forma, tipos, campos, UUID y duplicados. La API mantiene la correlación y validación numérica. n8n documenta la limitación de parsers adjuntos a agentes en https://docs.n8n.io/integrations/builtin/cluster-nodes/sub-nodes/n8n-nodes-langchain.outputparserstructured/common-issues/.

Tras ese ajuste, los diez Watch y diez Incident pasaron; una caída del DNS local (api.deepseek.com y api.github.com no resolvían, curl exit 6) impidió los diez Injection. El fallback conservó el motor y no hubo llamadas prohibidas; ese muestreo se registra como fallido, no se descartan filas para cambiar el p95. Una repetición con DNS UDP público volvió a sufrir conexión/timeout y se canceló; se conserva como intento no aceptado. Se repitió otra vez la matriz completa, sin mezclar corridas entre intentos, sobre el código integrado con #207 y los mismos exports, fijando en el hosts del contenedor descartable de n8n la IP pública resuelta por DNS sobre HTTPS y verificada mediante TLS para api.deepseek.com; la configuración DNS del host y del producto no se modificó.

Se conservaron intentos fallidos antes del ajuste final: 8/8 y 15/15 herramientas correctas con timeouts; matriz de 30 con 107/107 herramientas correctas, errores de conexión reales y p95 10259 ms, no aceptada. La corrección agrupa las solicitudes y acorta explicaciones; no amplía el plazo ni cambia el modelo. Artefactos intermedios locales en `/tmp/codex-review-195` y `artifacts/agent-acceptance/`.

## Revisión y cierre

Sin cambios propios en RoutePlansController, migraciones o dependencias. Los artefactos de simulación generados por suites ajenas se conservaron fuera del PR. La cuenta autora puede estar impedida de autoaprobar; no se sustituye por comentarios ni se cambia de identidad. El estado remoto y SHA de merge se verifican en el PR y el issue; develop no es despliegue.
