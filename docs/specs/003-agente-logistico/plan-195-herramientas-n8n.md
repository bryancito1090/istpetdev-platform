# Plan de implementación y entrega — issue #195

Fecha: 16-oct-2026. Estado: **plan preparado; implementación y pruebas pendientes**.
Ejecutor: agente OpenCode ya abierto en este proyecto, por encargo de Bryan.
Issue: https://github.com/bryancito1090/istpetdev-platform/issues/195.
Dependencias: #194 (consultas API) y #193 (hora límite del motor).

## Objetivo y alcance autorizado

Añadir `find_stock`, `plan_sourcing` y `vehicle_availability` al AI Agent de
vigía e incidente; ajustar prompt y verificación para que las decisiones consulten
riesgos, stock, abastecimiento y disponibilidad antes de proponer rutas.
Mantener ADR-22: el LLM elige herramientas y explica; el motor calcula cantidades,
asignaciones, prioridades y rutas. Conservar el fallback determinista.

Bryan encarga implementación, pruebas, documentación, rama independiente, PR a
`develop`, resolución de conflictos, revisión/aprobación e integración cuando
cumpla los criterios y las protecciones. No incluye promoción a main, despliegue
ni reemplazo de workflows productivos. No implementar #194/#193 en paralelo a
sus responsables. No editar `RoutePlansController.cs`, donde trabaja #201.

## Hallazgos que condicionan la implementación

Inspección local en `c147d2a`; referencia remota almacenada de develop: `1dcfb51`
(merge #192). Las consultas a GitHub fallaron por conexión durante esta revisión;
el texto y las dependencias de #195 proceden del mensaje de Bryan. Verificar
estado remoto al recuperar acceso antes de publicar o afirmar integración.

- Plantillas editables: `automation/n8n/templates/{vigia,incidente}.template.json`.
  Exportaciones generadas: `automation/n8n/workflows/`; generador:
  `scripts/agent/build-workflows.py`. No editar solo los JSON generados.
- Prompt: `automation/n8n/prompts/agente-logistico-v1.md`. Actualmente obliga a
  Watch a ejecutar inmediatamente `get_risks → propose_routes`, y prohíbe otras
  herramientas en Incident. Ambas instrucciones deben cambiar, no basta añadir nodos.
- Ambos agentes tienen cinco herramientas `toolWorkflow`, máximo 8 iteraciones,
  contexto firmado, OAuth2 M2M y callback fijo. Mantener los cinco tools y agregar
  exactamente los tres pedidos, con esquemas específicos.
- `test_workflow_context.py` espera exactamente cinco tools y solo `routePlanId`
  como argumento. `acceptance.py` admite solo cinco endpoints. Actualizar estas
  listas explícitas sin reemplazarlas por permisos abiertos.
- El guard exige 3200 ms restantes, pero HTTP usa timeout de 15000 ms. Añadir
  llamadas aumenta el riesgo de exceder el plazo total de 10 s del AgentRun;
  no elevar límites ni debilitar los umbrales para hacer pasar el ensayo.
- `AgentRunView` expone llamadas y decisiones, pero **no el summary del LLM**.
  `ToolCallTrace` no almacena respuestas de herramientas. `vehicleCode` sí existe;
  bodega y prioridad no tienen una representación suficiente garantizada.
  Un texto del modelo o la mera presencia de llamadas no prueban qué eligió el motor.
- El `propose_routes` actual manda solo `includeConsolidation: true` a
  `POST /route-plans`. `PlanRequest` admite `vehicleCodes`, pero no contiene
  asignaciones de sourcing ni hora límite en el checkout inspeccionado. No
  afirmar que una consulta de origen modifica automáticamente el plan posterior.
- El motor prioriza asignación por riesgo, holgura y criticidad; la secuencia
  vial final puede variar por ventanas y optimización. Prioridad de atención no
  equivale siempre a orden estricto de visita. #193 agrega la restricción horaria.

## 1. Rama y aislamiento obligatorios

1. Leer AGENTS.md, README, CONTRIBUTING.md, README de la bóveda, Hechos canónicos,
   spec/plan/task del 003, ADR-22, n8n e IA, Contratos API y eventos y la nota #192.
   Usar `ponytail`, `istpet-gitflow` y `orca-cli` ya disponibles; aplicar las
   convenciones GitHub efectivas de este repo.
2. Crear **otro checkout** mediante Orca desde `origin/develop` actualizado y
   trabajar exclusivamente en `feature/195-herramientas-n8n-abastecimiento`.
   Reutilizar la sesión OpenCode existente para operar con cwd explícito en ese
   checkout; no cambiar de rama en el directorio compartido.
3. Copiar allí este plan y únicamente la sección de seguimiento #195 de task.md.
   Antigravity trabaja #194 en `/home/bryan/Projects/ISTPETDEV-194`, rama
   `feature/194-consultas-abastecimiento-agente`; no editar ese checkout.
   El checkout original contiene además cambios ajenos del spec 006 y el plan
   de #194. No reset/stash global, no git add indiscriminado, no incluirlos en #195.
4. Puede avanzar en prompt, wrappers, pruebas aisladas y documentación mientras
   llegan las dependencias. El PR puede ser draft; no declararlo listo ni integrarlo
   con contratos supuestos. Integrar develop después de #194/#193, revisar sus
   contratos finales y validar el conjunto. No cherry-pickear trabajo inacabado ajeno.
5. Verificar labels, asignación y milestone en GitHub. Mantener `type::feature`;
   no inventar un milestone ni asignar uno vencido por cumplir una guía GitLab.

## 2. Fijar contratos y cerrar dependencias

Documentar la matriz tool → endpoint → argumentos → respuesta antes de editar
los wrappers. Contratos **propuestos** por el plan de #194, sujetos a su PR final:

| Tool | HTTP propuesto | Argumentos públicos del LLM |
|---|---|---|
| `find_stock` | `GET /api/v1/logistics/stock` | productId; warehouseId opcional |
| `plan_sourcing` | `POST /api/v1/logistics/sourcing` | exactamente una lista orderIds o servicePointIds; productId opcional solo para puntos |
| `vehicle_availability` | `GET /api/v1/logistics/vehicles/available` | objeto vacío para hoy; fecha opcional solo si existe contexto autorizado para otro día |

Usar IDs reales devueltos por API, listas acotadas/deduplicadas y el reloj
operativo del servidor. La demanda por riesgos procede del motor; nunca calcular
cantidades en JavaScript ni pedir al modelo pesos, reservas, stock o asignaciones.

Para `propose_routes`, comprobar el contrato final de #193: nombre/casing real,
formato, zona horaria, procedencia y validación de la hora límite. No inventar
`criticalDeadline` si el campo publicado tiene otro nombre. La hora configurada
debe venir de contexto/configuración autorizados, nunca de una sugerencia libre
del modelo. Si falta, omitirla y preservar el comportamiento anterior. La extensión
es opcional según #195; documentar si queda fuera, sin prometer prioridad horaria.
La dependencia #193 y su comportamiento integrado deben quedar contrastados.

Comprobar que la consulta de vehículos influye correctamente en la propuesta
(por ejemplo, trasladando `vehicleCodes` válidos al contrato existente) sin
trasladar al LLM el cálculo de capacidad. Una lista vacía no debe convertirse
en ausencia de filtro y permitir toda la flota. Validar la pertenencia de los
valores elegidos a la respuesta reciente y la validación autoritativa del API.

**Puerta de coherencia:** comparar origen sugerido, flota disponible y ruta
real calculada. No etiquetar una bodega consultada como origen elegido si el
motor no la usa. Si develop no tiene un camino para aplicar una selección de
origen, mantener diferenciadas recomendación y ruta, y registrar el hueco para
coordinar el cambio mínimo con #194/#201. No cerrar #195 con afirmaciones falsas
ni reimplementar el motor dentro de n8n. Para demo-region se debe acreditar la
coincidencia mediante los datos reales del plan, no codificando sus IDs/nombres.

## 3. Implementar los tres tools preservando seguridad

- Añadir nodos y conexiones `ai_tool` en ambas plantillas usando el patrón
  `toolWorkflow` existente (versión 2.2 del tipo en la plantilla inspeccionada).
  Reutilizar la estructura del subworkflow y del HTTP Request; evitar una nueva
  arquitectura de generación o dependencias.
- JSON Schema por herramienta con `additionalProperties: false`, tipos, campos
  requeridos, UUID/fechas y límites equivalentes a la API. Validar también en el
  wrapper antes de llamar: el schema descriptivo no sustituye validación runtime.
  Admitir las formas de entrada que realmente entrega n8n (input/query), sin
  aceptar campos extra ni reenviar todo el objeto arbitrario.
- `apiBaseUrl` siempre de configuración de servidor. `runId`, `kind`,
  `incidentId` y `deadlineUtc` siempre del payload validado por `Verify Signature`.
  Ni argumentos, ni query, ni un objeto context anidado pueden sobrescribirlos.
  URLs fijas con query/segmentos codificados; cuerpos construidos por allowlist.
- OAuth2 `client_credentials` con la referencia M2M existente y header
  `X-Agent-Run-Id` del contexto firmado en los ocho tools. Nada de tokens,
  passwords, secretos ni credenciales exportadas en plantillas, JSON o evidencia.
- Mantener HMAC sobre bytes originales y timestamp, ventana de 60 s y rechazo
  401 antes de registrar ejecución o invocar tools. Mantener registro de
  executionId, ACK 202, callback fijo y Error Trigger de #113.
- Cada llamada respeta el tiempo restante y deja margen al callback. Ajustar
  timeouts/reintentos al presupuesto efectivo; no usar un timeout de 15 s si
  queda menos. Omitir la llamada si ya no cabe y usar el camino de error/fallback
  existente. Mantener el límite de iteraciones salvo cambio explícito justificado
  y aprobado en contrato; no aumentarlo para esconder bucles.
- Renovar la versión efectiva del prompt y actualizar referencias/tests para
  distinguir la nueva conducta de v1. No cambiar modelo o versión n8n por este issue.

## 4. Prompt y recorrido esperado

Watch: `get_risks → find_stock → plan_sourcing → vehicle_availability →
propose_routes → explicación`. Consultar productos distintos necesarios sin
repetir llamadas por cada punto. `plan_sourcing` debe usar los puntos/pedidos
reales y el motor determina faltantes/orígenes. El prompt elige herramientas,
no fuerza una cantidad de pedidos/rutas independiente de sus resultados.

Explicar el criterio del motor: nivel de riesgo (Critico antes de Alto), menor
holgura, mayor criticidad del punto y desempate determinista existente. La hora
límite, si está configurada y soportada, es una restricción adicional validada
por el motor. Explicar no atendidos y faltantes sin inventar una solución factible.

Incident: comenzar por `get_incident_impact`, preservar contexto del incidente
y prefijo completado; usar consultas nuevas cuando haga falta evaluar reposición
o sustitución, y llamar `replan` por las rutas afectadas. No volver a abastecer
carga que ya está reservada/en tránsito ni proponer una jornada nueva para
resolver un incidente. Un vehículo de la ruta afectada puede aparecer ocupado
en disponibilidad: esto no lo invalida para replanificar su propia ruta.

Mantener el formato final de decisiones, IDs devueltos por el motor, validación
de cifras, mensaje <=160 caracteres y defensa contra instrucciones en nombres
o descripciones. Permitir `get_route_plan` solo cuando necesite detalle que la
respuesta anterior no da; evitar llamadas redundantes y bucles por falta de stock.
Casos sin riesgos, sin flota, sin stock o sin rutas afectadas deben terminar
coherentemente; no fabricar decisiones para satisfacer el parser. Contrastar
`Prepare Result`, que hoy rechaza decisions vacío, con el resultado real de API.

## 5. Evidencia visible en AgentRun

La salida final de `GET /agent/runs/{id}` debe permitir comprobar bodega de
origen, vehículo y criterio/orden de prioridad de la propuesta. Reutilizar
`decisions`, facts, `ProposalJson` y correlación antes de añadir almacenamiento.

1. Revisar qué facts/IDs producen #193/#194 y el plan final. Si ya bastan,
   usarlos. Si faltan, añadir la mínima proyección/enriquecimiento determinista
   en la capa Application de AgentRun/decisiones, con DTO y prueba; no tocar
   RoutePlansController ni guardar respuestas HTTP completas en middleware.
2. Datos derivados por servidor del plan correlacionado: origen(s) real(es),
   vehículo, puntos ordenados y criterio de prioridad/hora límite aplicable.
   Diferenciar prioridad de asignación y secuencia final de visita. Mantener
   una instantánea de los hechos de esa decisión; no reconstruir una historia
   distinta usando stock/riesgo actual tras otras operaciones.
3. No aceptar facts, warehouseId o vehicleId autoritativos del callback LLM.
   No basta summary: no se expone en AgentRunView actual. Evitar extraer evidencia
   de texto libre. No relajar ExplanationValidator para admitir cifras nuevas.
4. Acreditar tanto explicación LLM como fallback con datos del mismo motor;
   explicar divergencias entre sugerencia y propuesta en vez de ocultarlas.

## 6. Pruebas y ejecución real

Antes de levantar API/n8n o declarar lista la versión, usar
`docs/boveda/.local-skills/istpetdev-prelaunch/SKILL.md` y referencias pertinentes
de seguridad, RBAC, DemoCore y validación. Fuente editable única de esa skill:
`docs/boveda/08 Skills/istpetdev-prelaunch.md`. Evidencia por revisión/entorno,
con fallos, pendientes y no aplica. Ensayos sobre PostGIS/n8n locales aislados,
sin reset de RDS compartido ni cambios a workflows productivos.

| Verificación | Resultado exigido |
|---|---|
| Generador | dos generaciones idénticas, --check limpio, prompt/version correctos y 8 tools conectados por agente |
| Wrappers | tres endpoints/métodos finales, query/body correctos, errores 4xx/5xx, UUID/listas inválidas y deadline agotado |
| Contexto | ampliar test_workflow_context por tool; entradas hostiles no cambian URL, run, incidente, deadline ni configuración horaria |
| Firma | payload alterado, firma inválida y timestamp viejo rechazados; firma válida atraviesa el flujo normal |
| Secretos | inspección recursiva de exports, subworkflows embebidos y plantillas; sin credenciales ni pinData con datos reales |
| Allowlist | acceptance.py reconoce exactamente los tres endpoints nuevos; cuentas/publicación/aprobación/reversión siguen prohibidos |
| Motor y evidencia | integración demo-region: origen/vehículo/prioridad del AgentRun coinciden con el plan; no se aceptan facts inventados |
| Fallback | DeepSeek caído, timeout, salida mal formada y cifra inventada conservan motor/plantillas; callback tardío no sobrescribe ni duplica |
| Incidente | respeta rutas afectadas, prefijo completado y reservas; no se duplica replanificación ni se reabastece carga existente |
| Prioridad | sin hora conserva comportamiento; con hora soportada aplica el campo real de #193; inviables explicados por motor |

Extender tests existentes: `test_build_workflows.py`, `test_workflow_context.py`,
`test_acceptance.py`, `WorkflowSecretsTests`, `AgentRunTraceTests` y pruebas
de AgentJourney según el camino afectado. Crear pruebas focalizadas nuevas solo
cuando falte cobertura. Reutilizar `N8nErrorWorkflowTests` como referencia del
runtime n8n real local; su bridge actual no reenvía X-Agent-Run-Id, por lo que
el ensayo nuevo debe conservar esa cabecera para acreditar la traza.

```bash
python3 -I scripts/agent/build-workflows.py
python3 -I scripts/agent/build-workflows.py --check
python3 -I -m unittest discover -s tests/scripts -p 'test_*.py'
dotnet build IstpetDev.sln -c Release
dotnet test tests/IstpetDev.Tests -c Release --no-build
npm run check
npm run audit:check
python3 docs/boveda/.github/scripts/check_vault.py
git diff --check
```

Hacer **al menos un Watch real con n8n y DeepSeek**, usando credenciales existentes
por el mecanismo privado del proyecto, sobre API/DB locales con demo-region.
Guardar runId, executionId, SHA, versión del prompt, entorno, estado, latencia,
traza en orden y respuesta saneada de AgentRun con origen, vehículo y prioridad.
Ejecutar también un Incident de regresión y un fallo controlado del proveedor.
Un modelo simulado sirve para CI, pero no se presenta como ejecución de DeepSeek.
Si faltan red/credenciales/dependencias, continuar los checks independientes,
registrar el bloqueo y mantener abierto ese criterio; no declarar cierre completo.

El cambio de prompt invalida atribuirle métricas históricas: reutilizar el
harness de aceptación del spec 003 para revalidar sus umbrales sobre la revisión
final (matriz Watch/Incident/Injection, p95 <=10 s, herramientas >=95%, cifras
>=90%, inyección 10/10). No confundir una sola corrida con evidencia de p95.
No subir el plazo total de 10 s ni el límite de respuesta de 11 s para maquillar
resultados; corregir redundancias y registrar lo que no pase.

## 7. Documentación, PR y cierre

- Actualizar task.md por avance: código, pruebas locales, runtime real, CI,
  revisión y merge separados. Evidencia en
  `evidencia/2026-10-16-195-herramientas-n8n.md`.
- Actualizar `docs/boveda/05 Automatizacion/n8n e IA.md`, Contratos API y eventos
  si cambia AgentRun, y Fuente y control documental. Actualizar FR-04 del spec
  003 y referencias vigentes que todavía restringen a cinco tools, preservando
  los resultados históricos como históricos. Documentar decisiones de contrato
  antes de implementar; completar su estado después de verificar.
- Revisar diff; commits Conventional Commits; publicar solo la rama #195 y abrir
  PR a develop con plantilla, `Closes #195`, `type::feature`, dependencias y
  evidencia. No copiar archivos/commits de #194 al PR propio.
- Integrar origin/develop actualizado con #194/#193; resolver conflictos
  conservando funcionalidades ajenas, especialmente task.md y contratos. No
  usar ours/theirs indiscriminadamente. Regenerar exports desde plantillas
  fusionadas y repetir pruebas afectadas. Si cambia el prompt final, revalidarlo.
- Verificar HEAD, destino develop, mergeabilidad y checks aplicables en GitHub.
  UNKNOWN/pending o una prueba bloqueada no equivalen a verde. Revisar todos
  los criterios anteriores antes de aprobar. No usar merge administrativo para
  saltar checks/revisiones requeridas.
- Aprobar formalmente si la identidad autenticada está habilitada. Si GitHub
  impide autoaprobar por ser autor, registrar el motivo y solicitar revisor
  habilitado mediante el PR; no simular aprobación ni cambiar de identidad.
  Si esa revisión es obligatoria, mantener pendiente el merge hasta obtenerla.
- Integrar cuando cumpla todos los criterios/protecciones, verificar SHA de merge
  y estado del PR, e informar a Bryan URL, resultados, conflictos resueltos,
  aprobación real, merge y cualquier pendiente. Develop no es despliegue.

## Instrucción de transferencia

OpenCode asume la ejecución completa siguiendo este plan. Puede ajustar detalles
por los contratos finales de las dependencias y debe registrar el motivo; no
eliminar criterios para terminar antes. El agente que prepara el plan finaliza
tras entregarlo. Este documento no acredita implementación, pruebas ni merge.
